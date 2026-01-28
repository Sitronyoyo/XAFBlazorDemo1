using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.SystemModule;
using DevExpress.Persistent.Base;
using DevExpress.Xpo;
using DXApplication1.Module.BusinessObjects;
using DXApplication1.Module.BusinessObjects.Database;
using System.Diagnostics;
using System.IO;



namespace DXApplication1.Module.Controllers
{
    public class InvoiceImportController : ObjectViewController<ListView, Invoice>
    {
        //public PopupWindowShowAction ImportAction { get; private set; }
        public PopupWindowShowAction PopupWindow { get; private set; }

        public InvoiceImportController()
            
        {
          
            //创建按钮
            PopupWindow = new PopupWindowShowAction(this, "ImportCSV", PredefinedCategory.Edit);
            
            //添加属性
            PopupWindow.Caption = "Import CSV"; 
            PopupWindow.ConfirmationMessage="Du you want to import invoices from a CSV file ?";//点击按钮就先显示这个pop
            PopupWindow.AcceptButtonCaption="Import"; //上传CSV的页面显示这个
            PopupWindow.CancelButtonCaption= "Cancel";
            
            //添加事件events
            PopupWindow.CustomizePopupWindowParams += ImportCsvAction_CustomizePopupWindowParams;
            PopupWindow.Execute += ImportCsvAction_Execute;
        }

        //CustomizePopupWindowParams 创建弹窗,上传CSV 和选择客户
        private void ImportCsvAction_CustomizePopupWindowParams(object sender, CustomizePopupWindowParamsEventArgs e)
        {
            //1.ObjectSpace，通过这个创建的对象，XAF 会跟踪它们，不会影响当前 View 的 ObjectSpace
            var objectSpace = Application.CreateObjectSpace(typeof(InvoiceImportModel));

            //2.创建InvoiceImportModel的实例
            var invoiceModel = objectSpace.CreateObject<InvoiceImportModel>();

            // 3.基于刚才创建的 ObjectSpace 和临时对象创建一个 DetailView，弹窗中显示的表单就是这个 DetailView，
            // 只有共用ObjectSpace,才设置为false
            var detailView = Application.CreateDetailView(objectSpace, invoiceModel,true);


            //3.设置 DetailView 为可编辑模式,用户可以在弹窗中输入/选择/上传数据
            detailView.ViewEditMode = ViewEditMode.Edit;

            //4.把创建好的 DetailView 指定给弹窗参数 e.View
            e.View = detailView;

            //5. DialogController 配置
            e.DialogController.SaveOnAccept = false; // 不自动保存到数据库，先处理 CSV
        }

        /*    //Execute读取已经上传的CSV，然后存到数据库, 已经成功得到上传的文件和选择的客户
            private void ImportCsvAction_Execute(object sender, PopupWindowShowActionExecuteEventArgs e)
            {
                if (e.PopupWindowViewCurrentObject is InvoiceImportModel invoiceModel && invoiceModel.CsvFile != null)
                {
                    Console.WriteLine("进入ImportCsvAction_Execute");
                    Debug.WriteLine("进入 ImportCsvAction_Execute");
                    Debug.WriteLine($"e.PopupWindowView.ObjectSpace: {e.PopupWindowView.ObjectSpace}");
                    Debug.WriteLine($"View.ObjectSpace OS: {View.ObjectSpace}");

                    Debug.WriteLine($"e.PopupWindowViewCurrentObject: {invoiceModel}");
                    Debug.WriteLine($"View.CurrentObject: {View.CurrentObject}");
                    //var objectSpace = e.PopupWindowView.ObjectSpace;

                    var objectSpace = View.ObjectSpace;

                    // ❌ 直接用 invoiceModel.SelectedCustomer 会报错
                    //var selectedCustomer = invoiceModel.SelectedCustomer;

                    // ✅ 映射到弹窗 ObjectSpace
                    var selectedCustomer = objectSpace.GetObject(invoiceModel.SelectedCustomer);

                    using (var stream = new MemoryStream(invoiceModel.CsvFile.Content))
                    using (var reader = new StreamReader(stream))
                    {
                        bool isFirstLine = true;
                        // 假设 CSV 第一列是发票号，第二列是金额
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

                        // 所有对象创建完成后再提交，❌ UI 不显示
                        objectSpace.CommitChanges();
                        objectSpace.Refresh(); // 刷新 ObjectSpace内所有的Invoice，确保数据最新
                        //View.CollectionSource.Reload();//刷新当前UI

                        // e.CanCloseWindow = true; // 处理完成关闭弹窗
                    }

                    //for testing 
                    var loadedCount = objectSpace.GetObjects<Invoice>().Count;
                    System.Diagnostics.Debug.WriteLine("ObjectSpace 已加载 Invoice 数量: " + loadedCount);
                }
            }*/


        //⚡ 异步导入 CSV 方法
        // 方法声明为 async void
        private async void ImportCsvAction_Execute(object sender, PopupWindowShowActionExecuteEventArgs e)
        {
            if (e.PopupWindowViewCurrentObject is InvoiceImportModel invoiceModel && invoiceModel.CsvFile != null)
            {

                //safe Key for Kunder, 先取key, 然后从DataBase中取对应的Customer对象， 安全可靠
                var customerOiD = invoiceModel.SelectedCustomer.Oid;

                Console.WriteLine("进入ImportCsvAction_Execute");
                Debug.WriteLine("进入 ImportCsvAction_Execute");
                Debug.WriteLine($"e.PopupWindowView.ObjectSpace: {e.PopupWindowView.ObjectSpace.GetType().FullName}");
                Debug.WriteLine($"View.ObjectSpace OS: {View.ObjectSpace.GetType().FullName}");

                Debug.WriteLine($"e.PopupWindowViewCurrentObject: {invoiceModel}");
                Debug.WriteLine($"View.CurrentObject: {View.CurrentObject}");
               

                //每次Task 都创建一个新的 ObjectSpace, 避免多线程冲突
                var objectSpace = Application.CreateObjectSpace<Invoice>(); 

                var transferredCustomer = objectSpace.FirstOrDefault<Customer>(p => p.Oid == customerOiD); 

                // ❌ 直接用 invoiceModel.SelectedCustomer 会报错
                //var selectedCustomer = invoiceModel.SelectedCustomer;

                // ✅ 将弹窗内的SelectedCustomer， 映射到当前的 ObjectSpace
                // var selectedCustomer = objectSpace.GetObject(invoiceModel.SelectedCustomer);

                using var stream = new MemoryStream(invoiceModel.CsvFile.Content);
                using var reader = new StreamReader(stream);

                int batchsize = 500;
                int counter = 0;

                /* 使用Session 代替ObjectSpace 示例，需要学习
                 Session session = new Session();

                 Invoice invoice1 = new Invoice(session);
                 invoice1.Save();

                 session.RollbackTransaction(); // 回滚事务，如果上传数据出现错误必须回滚，避免部分数据写入数据库


                 session.CommitTransaction();
                 session.CommitTransactionAsync(); // 异步提交事务
                 session.Dispose();*/

                bool isFirstLine = true;
                // 假设 CSV 第一列是发票号，第二列是金额
                while (!reader.EndOfStream)
                {
                    //2. 异步读取每行,避免阻塞 UI, async 方法内可以用 await
                    var line = await reader.ReadLineAsync();

                    if (string.IsNullOrWhiteSpace(line)) continue;

                    // 跳过表头
                    if (isFirstLine)
                    {
                        isFirstLine = false; 
                        continue;
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

                    //3. ObjectSpace 创建对象,必须在 当前线程 / 同步上下文,不要 Task.Run
                    var invoice = objectSpace.CreateObject<Invoice>();
                    invoice.InvoiceDate = invoiceDate;
                    invoice.InvoiceNumber = invoiceNumber;
                    invoice.DueDate = dueDate;
                    invoice.Amount = amount;
                    invoice.Tax = tax;
                    // 注意：这个.GetObject必须在这里调用， 如果在前面调用会报错
                    // ✅ 将弹窗内的SelectedCustomer， 映射到当前的 ObjectSpace
                    invoice.Customer = transferredCustomer;
                    invoice.Save();

                    counter++;

                    if (batchsize == counter)
                    {
                        // 4. Commit + Refresh
                        // 同步提交 所有对象创建完成后再提交，❌ UI 不显示
                        objectSpace.CommitChanges();

                        counter = 0;
                    }
                
                }

                objectSpace.CommitChanges();
                objectSpace.Dispose();


                //for testing 
                var loadedCount = objectSpace.GetObjects<Invoice>().Count;
                System.Diagnostics.Debug.WriteLine("ObjectSpace 已加载 Invoice 数量: " + loadedCount);
            }
        }            
            
    }

}
