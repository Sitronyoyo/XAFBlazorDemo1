using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl;
using DevExpress.Persistent.Validation;
using DevExpress.Xpo;
using DXApplication1.Module.BusinessObjects.Database;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DXApplication1.Module.BusinessObjects
{
    /*
     * 表示这个类不在数据库中存储，只是 UI 使用用于导入发票的模型类
     */
    //[DefaultClassOptions] //show in UI

    //[ImageName("BO_Invoice")]
    //[NavigationItem(false)] 
    //[DomainComponent]  // ⭐ 必须

    public class InvoiceImportModel : BaseObject 
    {

        public InvoiceImportModel(Session session) : base(session) { }

        private FileData csvFile;

        public FileData CsvFile
        {
            get => csvFile;
            set => SetPropertyValue(nameof(CsvFile), ref csvFile, value);
        }

        private Customer selectedCustomer;
        public Customer SelectedCustomer
        {
            get => selectedCustomer;
            set => SetPropertyValue(nameof(SelectedCustomer), ref selectedCustomer, value);
        }

     /*   // ⚡ 导入 CSV 方法
        public void ImportCsv(IObjectSpace objectSpace)
        {
            if (CsvFile == null)
                throw new InvalidOperationException("请先选择 CSV 文件！");
            //if (SelectedCustomer == null)
                //throw new UserFriendlyException("请选择 Customer");

            // 确保 Customer 属于同一个 ObjectSpace
            var selectedCustomer = objectSpace.GetObject(SelectedCustomer);

            using (var stream = new MemoryStream(CsvFile.Content))
            using (var reader = new StreamReader(stream))
            {
                // 假设 CSV 第一列是发票号，第二列是金额
                
                    bool isFirstLine = true;
                    while (!reader.EndOfStream)
                    {
                        var line = reader.ReadLine();
                        if (string.IsNullOrWhiteSpace(line)) continue;

                        // 跳过表头
                        if (isFirstLine)
                        {
                            isFirstLine = false;
                            if (line.StartsWith("InvoiceDate")) continue;
                        }

                        var values = line.Split(';');
                        if (values.Length < 5) continue; // 列数不够，跳过

                        // 先解析所有值，确保合法
                        if (!DateTime.TryParseExact(values[0], "dd.MM.yyyy", null, System.Globalization.DateTimeStyles.None, out var invoiceDate))
                            continue;

                        var invoiceNumber = values[1];
                        if (string.IsNullOrWhiteSpace(invoiceNumber)) continue;

                        if (!DateTime.TryParseExact(values[2], "dd.MM.yyyy", null, System.Globalization.DateTimeStyles.None, out var dueDate))
                            continue;

                        if (!double.TryParse(values[3], out var amount)) continue;
                        if (!double.TryParse(values[4], out var tax)) continue;

                        // ✅ 全部合法后再创建对象
                        var invoice = objectSpace.CreateObject<Invoice>();
                        invoice.InvoiceDate = invoiceDate;
                        invoice.InvoiceNumber = invoiceNumber;
                        invoice.DueDate = dueDate;
                        invoice.Amount = amount;
                        invoice.Tax = tax;
                        invoice.Customer = selectedCustomer;
                    }

                    // 所有对象创建完成后再提交
                    objectSpace.CommitChanges();
                
            }
        }*/
    }
}
