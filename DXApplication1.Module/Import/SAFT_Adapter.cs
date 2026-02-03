using DevExpress.CodeParser;
using DevExpress.Data.Helpers;
using DevExpress.Xpo;
using DevExpress.XtraCharts.Native;
using DevExpress.XtraRichEdit.Import.Html;
using DXApplication1.Module.BusinessObjects.Database;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Xml;
using XmlNode = System.Xml.XmlNode;


namespace DXApplication1.Module.Import
{
    internal class SAFT_Adapter : IERPAdapter
    {
        public ImportMode Mode => ImportMode.DirectToCentralModel;

        Session _innerSession;
        Session _dataImportSession;

        public SAFT_Adapter(Session innerSession, Session dataImport)
        {
            _innerSession = innerSession;
            _dataImportSession = dataImport;
        }

        //必须属性就不使用问好，让异常被捕获
        public async Task ImportData(DataImport dataImport, CustomerTenant tenant, IProgress<int> progress = null)
        {
            Debug.WriteLine("Start read SAF_T xml file!");
            //dataImport.File .....
            //.Content return byte[]
            using (var stream = new MemoryStream(dataImport.File.Content))
            {
                //1. Load XML Document
                var doc = new XmlDocument();
                doc.Load(stream); // load XML

                // Create Namespace Manager
                XmlNamespaceManager nsmgr = new XmlNamespaceManager(doc.NameTable);
                nsmgr.AddNamespace("ns", "urn:StandardAuditFile-Taxation-Financial:NO");

                //2.Dictionary of Account{ AccountID : AccountDescription }
                // MasterFiles/GeneralLedgerAccounts：Mandantaty[1..1]
                var accountDict = new Dictionary<string, string>();
                var masterFilesNode = doc.SelectSingleNode("//ns:MasterFiles/ns:GeneralLedgerAccounts", nsmgr);
                //Account nodes: Mandantaty[1..N]   
                foreach (XmlNode accountNode in masterFilesNode.SelectNodes("ns:Account",nsmgr))
                {
                    //AccountID and AccountDescription: Mandantaty[1..1]
                    var accountId = accountNode.SelectSingleNode("ns:AccountID", nsmgr).InnerText.Trim();
                    var accountDesc = accountNode.SelectSingleNode("ns:AccountDescription", nsmgr).InnerText.Trim();
                    if (accountDict.ContainsKey(accountId))
                    {
                        throw new Exception(
                            $"SAF‑T file content error: MasterFiles/GeneralLedgerAccounts contains duplicate AccountID '{accountId}'. Each AccountID must be unique in this part.");  
                    }
                    accountDict[accountId] = accountDesc;
                }

                //2.Header/DefaultCurrencyCode(Currency in MLDataSet): Mandatory[1..1]
                string defaultCurrencyCode = doc.SelectSingleNode("//ns:Header/ns:DefaultCurrencyCode", nsmgr).InnerText.Trim();
                Debug.WriteLine($"Default Currency Code in Header: {defaultCurrencyCode}");

                // Print GeneralLedgerEntries summary info in Debug window
                //NumberOfEntries, TotalDebit, TotalCredit -->Mandantaty[1..1]
                var numberOfEntries = doc.SelectSingleNode("//ns:GeneralLedgerEntries/ns:NumberOfEntries", nsmgr).InnerText.Trim();
                Debug.WriteLine($"Number of Entries (Total transactions) in GeneralLedgerEntries: {numberOfEntries}");
                var totalDebit = doc.SelectSingleNode("//ns:GeneralLedgerEntries/ns:TotalDebit", nsmgr).InnerText.Trim();
                Debug.WriteLine($"Total Debit Amount in GeneralLedgerEntries: {totalDebit}");
                var totalCredit = doc.SelectSingleNode("//ns:GeneralLedgerEntries/ns:TotalCredit", nsmgr).InnerText.Trim();
                Debug.WriteLine($"Total Credit Amount in GeneralLedgerEntries: {totalCredit}");

                //3. Find GeneralLedgerEntries/Journal with Type='GL'. Optional[0..N], return XmlNodeList
                var glJournalNodes = doc.SelectNodes("//ns:GeneralLedgerEntries/ns:Journal[ns:Type='GL']", nsmgr);
                Debug.WriteLine($"Total GL Journals found: {glJournalNodes.Count}");

                foreach (XmlNode journalNode in glJournalNodes)
                {
                    //JornalID: Mandantaty[1..1]
                    var journalID = journalNode.SelectSingleNode("ns:JournalID", nsmgr).InnerText.Trim();
                    
                    //Description: Mandantaty[1..1]
                    var journalDesc = journalNode.SelectSingleNode("ns:Description", nsmgr).InnerText.Trim();
                   
                    Debug.WriteLine($"Journal ID: {journalID}, Description: {journalDesc}");


                    //4. Transaction: Optional[0..N],  return XmlNodeList
                    var transactions = journalNode.SelectNodes("ns:Transaction", nsmgr);
                    // Print total transactions count in Debug window
                    Debug.WriteLine($"Total Tranctions in GL Journal (ID: {journalID}): {transactions.Count}");

                    foreach (XmlNode transaction in transactions)
                    {
                        //TransactionID(DoucumentNummer)：Mandantaty[1..1]
                        var documentNumber = transaction.SelectSingleNode("ns:TransactionID", nsmgr).InnerText.Trim();
                        //GlPostingDate(BookingDate)： Mandantaty[1..1]
                        var postingDateText = transaction.SelectSingleNode("ns:GLPostingDate", nsmgr).InnerText.Trim();
                        var bookingDate = DateTime.Parse(postingDateText);
                        //SystemEntryDate(RecordingDate)： Mandantaty[1..1]
                        var systemEntryDateText = transaction.SelectSingleNode("ns:SystemEntryDate", nsmgr).InnerText.Trim();
                        var recordingDate = DateTime.Parse(systemEntryDateText);

                        //SourceID(User in MLDataSet): Optional[0..1], Using question mark '?' to avoid exception
                        var user = transaction.SelectSingleNode("ns:SourceID", nsmgr)?.InnerText?.Trim();

                        //5. Transaction/Line: Mandantaty[1..N]
                        foreach (XmlNode line in transaction.SelectNodes("ns:Line", nsmgr))
                        {
                            //AccountID(Account in MLDataSet): Mandantaty[1..1]
                            var accountId = line.SelectSingleNode("ns:AccountID", nsmgr).InnerText.Trim();
                        
                            var accountName = accountDict[accountId]; //accountDict must contain this accountId, otherwise throws KeyNotFoundException


                            // Amount + BookingIndicator：need validation
                            // DebitAmount/Amount Mandatory [1..1] or  CreditAmount/Amount: Mandantory[1..1]
                            //使用问号，如果没有字段返回null, 如果有字段但是没有值，返回空字符串， 两种情况都要判断
                            var debitAmount = line.SelectSingleNode("ns:DebitAmount/ns:Amount", nsmgr)?.InnerText?.Trim();
                            var creditAmount = line.SelectSingleNode("ns:CreditAmount/ns:Amount", nsmgr)?.InnerText?.Trim();

                            bool hasDebit = !string.IsNullOrWhiteSpace(debitAmount); 
                            bool hasCredit = !string.IsNullOrWhiteSpace(creditAmount);
                          

                            if ((hasDebit && hasCredit) || (!hasDebit && !hasCredit))
                            {
                                throw new Exception(
                                     $"SAF‑T file content error: TransactionID '{documentNumber}', " +
                                     $"Line has invalid DebitAmount/CreditAmount. Exactly one of DebitAmount or CreditAmount must be provided.");
                            }
                        

                            // get Amount and BookingIndicator
                            double amount = hasDebit ? double.Parse(debitAmount) : - double.Parse(creditAmount);
                            var indicator = hasDebit ? "D" : "C";

                            // TaxCode(TaxKey in MLDataSet): Optional[0..1], using question mark to avoid exception
                            var taxKey = line.SelectSingleNode("ns:TaxInformation/ns:TaxCode", nsmgr)?.InnerText?.Trim();
                          
                            var ml = new MLDataSet(_innerSession)
                            {
                                DocumentNumber = documentNumber,
                                BookingDate = bookingDate,
                                RecordingDate = recordingDate,
                                Currency = defaultCurrencyCode,
                                Account = accountId,
                                AccountName = accountName,
                                User = user, //SourceID, can be null
                                Amount = amount,
                                BookingIndicator = indicator,
                                TaxKey = taxKey //TaxCode, can be null
                            };

                            _innerSession.Save(ml);
                        }
                    }                   
                }

            }

        }

    }
}
