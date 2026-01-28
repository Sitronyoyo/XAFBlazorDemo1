using DevExpress.CodeParser;
using DevExpress.Xpo;
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

        public async Task ImportData(DataImport dataImport, CustomerTenant tenant, IProgress<int> progress = null)
        {
            Debug.WriteLine("Start read SAF_T xml file!");
            //dataImport.File .....
            //.Content return byte[]
            using (var stream = new MemoryStream(dataImport.File.Content))
            {
                var doc = new XmlDocument();
                doc.Load(stream); // load XML

                // Create Namespace Manager
                XmlNamespaceManager nsmgr = new XmlNamespaceManager(doc.NameTable);
                nsmgr.AddNamespace("ns", "urn:StandardAuditFile-Taxation-Financial:NO");


                //1. Dictionary of AccountID : AccountDescription
                var accountDict = new Dictionary<string, string>();

                var masterFilesNode = doc.SelectSingleNode("//ns:MasterFiles/ns:GeneralLedgerAccounts", nsmgr);
                if (masterFilesNode != null)
                {
                    foreach (XmlNode accountNode in masterFilesNode.SelectNodes("ns:Account",nsmgr))
                    {
                        var accountIdNode = accountNode.SelectSingleNode("ns:AccountID", nsmgr);
                        var accountDescNode = accountNode.SelectSingleNode("ns:AccountDescription", nsmgr);
                        if (accountIdNode != null && accountDescNode != null)
                        {
                            var accountId = accountIdNode.InnerText.Trim();
                            var accountDesc = accountDescNode.InnerText.Trim();
                            if (!accountDict.ContainsKey(accountId))
                            {
                                accountDict[accountId] = accountDesc;
                            }
                        }
                    }
                }

                // all Transaction nodes: XmlNode 
                var transactions = doc.SelectNodes("//ns:GeneralLedgerEntries/ns:Journal/ns:Transaction",nsmgr);

                // Print total transactions count in Debug window
                Debug.WriteLine($"Total Tranctions to import: {transactions.Count}");
                
                int totalLines = 0;

                // for progress calculation
                foreach (XmlNode transaction in transactions)
                {
                    totalLines += transaction.SelectNodes("ns:Line", nsmgr).Count;
                }
                // In Debug window
                Debug.WriteLine($"Total Lines to import: {totalLines}");

                int index = 0;

                // Currency <- DefaultCurrencyCode: all share the same currency
                string defaultCurrencyCode = doc.SelectSingleNode("//ns:Header/ns:DefaultCurrencyCode",nsmgr)?.InnerText.Trim() ?? "NOK";

                
                foreach (XmlNode transaction in transactions)
                {
                    //in Transaction: TransactionID(DoucumentNummer), TransactionDate(BookingDate), SystemEntryDate(RecordingDate)
                    
                    var documentNumber = transaction.SelectSingleNode("ns:TransactionID", nsmgr)?.InnerText.Trim();
                    var transactionDateText = transaction.SelectSingleNode("ns:TransactionDate", nsmgr)?.InnerText.Trim();
                    var systemEntryDateText = transaction.SelectSingleNode("ns:SystemEntryDate", nsmgr)?.InnerText.Trim();

                    // if missing date value, set to DateTime.MinValue
                    DateTime bookingDate;
                    if (!string.IsNullOrEmpty(transactionDateText))
                        bookingDate = DateTime.Parse(transactionDateText);
                    else
                        bookingDate = DateTime.MinValue; // no value，= new DateTime(1, 1, 1, 0, 0, 0)

                    DateTime recordingDate;
                    if (!string.IsNullOrEmpty(systemEntryDateText))
                        recordingDate = DateTime.Parse(systemEntryDateText);
                    else
                        recordingDate = DateTime.MinValue;

                    // in Transaction/Line: AccountID, DebitAmount/Amount, CreditAmount/Amount, TaxInformation/TaxCode, User defined in Analysis/AnalysisID starting with "USER"
                    foreach (XmlNode line in transaction.SelectNodes("ns:Line", nsmgr))
                    {
                        var accountId = line.SelectSingleNode("ns:AccountID", nsmgr).InnerText.Trim();

                        //Line -> Analysis -> AnalysisID
                        string analysisID = null;
                        // Loop through Analysis nodes to find one starting with "USER"
                        var analysisNodes = line.SelectNodes("ns:Analysis", nsmgr);
                        if (analysisNodes != null)
                        {
                            foreach (XmlNode analysisNode in analysisNodes)
                            {
                                var id = analysisNode.SelectSingleNode("ns:AnalysisID", nsmgr)?.InnerText?.Trim();
                                if (!string.IsNullOrEmpty(id) && id.StartsWith("USER"))
                                {
                                    analysisID = id;
                                    break; 
                                }
                            }
                        }

                        var ml = new MLDataSet(_innerSession)
                        {
                            DocumentNumber = documentNumber,
                            Currency = defaultCurrencyCode,
                            Account = accountId,
                            AccountName = accountDict.ContainsKey(accountId) ? accountDict[accountId] : null,
                            BookingDate = bookingDate,
                            RecordingDate = recordingDate,
                            User = analysisID
                        };

                        // Amount + BookingIndicator
                        double amount = 0.0;
                        var debitNode = line.SelectSingleNode("ns:DebitAmount/ns:Amount", nsmgr);
                        var creditNode = line.SelectSingleNode("ns:CreditAmount/ns:Amount", nsmgr);

                        if (debitNode != null && double.TryParse(debitNode.InnerText.Trim(), out amount))
                        {
                            ml.Amount = amount;       
                            ml.BookingIndicator = "D";
                        }
                        else if (creditNode != null && double.TryParse(creditNode.InnerText.Trim(), out amount))
                        {
                            ml.Amount = -amount;      // Credit is negative
                            ml.BookingIndicator = "C";
                        }

                        // TaxKey
                        var taxNode = line.SelectSingleNode("ns:TaxInformation/ns:TaxCode", nsmgr);
                        if (taxNode != null)
                        {
                            ml.TaxKey = taxNode.InnerText.Trim();
                        }

                        _innerSession.Save(ml);
                        index++;
                        progress?.Report(index * 100 / totalLines);

                            
                    }
                }

            }

        }

    }
}
