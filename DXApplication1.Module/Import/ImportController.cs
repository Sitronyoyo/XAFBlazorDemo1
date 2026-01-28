using DevExpress.Data.Filtering;
using DevExpress.DocumentServices.ServiceModel.DataContracts;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Layout;
using DevExpress.ExpressApp.Model.NodeGenerators;
using DevExpress.ExpressApp.MultiTenancy;
using DevExpress.ExpressApp.SystemModule;
using DevExpress.ExpressApp.Templates;
using DevExpress.ExpressApp.Utils;
using DevExpress.ExpressApp.Xpo;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;
using DevExpress.Xpo;
using DevExpress.Xpo.DB;
using DevExpress.XtraCharts;
using DXApplication1.Module.BusinessObjects.Database;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace DXApplication1.Module.Import
{
    public partial class ImportController : ViewController<ListView>
    {
        // Use CodeRush to create Controllers and Actions with a few keystrokes.
        // https://docs.devexpress.com/CodeRushForRoslyn/403133/

        PopupWindowShowAction importCsvAction;
        DeleteObjectsViewController deleteObjectsViewController;

        CustomerTenant currentTenant;
        DataImport importParams;
        TimeSpan ImportSqlCommandTimeout;
        View view;


        public ImportController()
        {
            InitializeComponent();
            TargetObjectType = typeof(DataImport);


            //CSV.Import Accounts
            importCsvAction = new PopupWindowShowAction(this, "ImportFinanceData", PredefinedCategory.Edit)
            {
                Caption = "Import Finance Data",
                ImageName = "Action_Export_ToCSV"
            };
            importCsvAction.CustomizePopupWindowParams += ImportCsvAction_CustomizePopupWindowParams;
            importCsvAction.Execute += ImportCsvAction_Execute;

            Actions.Add(importCsvAction);

        }



        private void ImportCsvAction_CustomizePopupWindowParams(object sender, CustomizePopupWindowParamsEventArgs e)
        {
            IObjectSpace objectSpace = Application.CreateObjectSpace(typeof(DataImport));
            var dataImportObject = objectSpace.CreateObject<DataImport>();
            var detailView = Application.CreateDetailView(objectSpace, dataImportObject);

            ////Set Import logging
            //dataImportObject.Auditor = objectSpace.GetObject(SecuritySystem.CurrentUser as ApplicationUser) ?? null;
            //dataImportObject.ImportDate = System.DateTime.Now;

            detailView.ViewEditMode = ViewEditMode.Edit;

            e.View = detailView;
        }

        //disable controller if 2x Imports is ongoing
        void CheckImportWorkload()
        {
            //var checkOS = Application.CreateObjectSpace(typeof(DataImport));

            //var inProgress = checkOS.GetObjects<DataImport>().Where(p => p.ImportStatus == ImportStatus.InProgress ||
            //p.ImportStatus == ImportStatus.CreateModel);

            //if (inProgress.Count() > 2)
            //{
            //    importCsvAction.Enabled["Active"] = false;
            //}
            //else
            //{
            //    importCsvAction.Enabled["Active"] = true;
            //}

        }

        private async void ImportCsvAction_Execute(object sender, PopupWindowShowActionExecuteEventArgs e)
        {

            var dataImportParams = e.PopupWindowViewCurrentObject as DataImport;
            //Validate Rules:
            //var validator = Application.ServiceProvider.GetRequiredService<IValidator>();

            //// Save-Kontext als ContextIdentifiers-Set:
            //var result = validator.RuleSet.ValidateTarget(e.PopupWindowView.ObjectSpace, dataImportParams, ContextIdentifier.Save);

            //if (result.State != ValidationState.Valid)
            //{
            //    Application.ShowViewStrategy.ShowMessage("Bitte alle Import-Parameter auswählen");
            //    return;
            //}

            e.PopupWindowView.ObjectSpace.CommitChanges();

            ////Check if Data already imported
            //var importCheck = ObjectSpace.FirstOrDefault<DataImport>(p => p.Client.Oid == dataImportParams.Client.Oid &&
            //p.AuditYear.Oid == dataImportParams.AuditYear.Oid &&
            //p.CompanyCode == dataImportParams.CompanyCode && p.ImportStatus == ImportStatus.Succeeded && p.DeletionState == DeletionState.None);

            //if (importCheck != null)
            //{
            //    Application.ShowViewStrategy.ShowMessage("Already Data successful imported", InformationType.Info);
            //    return;
            //}


            try
            {
                await ImportData(dataImportParams);
                CheckImportWorkload();

            }
            catch (Exception ex)
            {
                Application.ShowViewStrategy.ShowMessage("Error, see details in import parameter", InformationType.Error);
                CheckImportWorkload();
                view.RefreshDataSource();
                view.Refresh();
                return;
            }


        }


        public async System.Threading.Tasks.Task ImportData(DataImport dataImport)
        {
            //Create Outer Session
            //IDataLayer dataLayer = XpoDefault.GetDataLayer(currentTenant.ConnectionString, AutoCreateOption.SchemaAlreadyExists);
            //var outerSession = new UnitOfWork(dataLayer);

            //Create innner Session
            //var store = (ConnectionProviderSql)XpoDefault.GetConnectionProvider("XpoProvider=MySql;Server=localhost;User ID=root;Password=1234;Database=testemilia;Persist Security Info= true;Charset=utf8", AutoCreateOption.SchemaAlreadyExists);
            
            var store = XpoDefault.GetConnectionProvider("XpoProvider=MySql;Server=localhost;User ID=root;Password=1234;Database=testemilia;Persist Security Info= true;Charset=utf8", AutoCreateOption.SchemaAlreadyExists);

            // 如果需要操作 store，可以继续写这里
            Debug.WriteLine("ConnectionProvider 创建成功");

            using var dl = new SimpleDataLayer(XpoTypesInfoHelper.GetXpoTypeInfoSource().XPDictionary, store);
            using var session = new Session(dl);

            //Create DataImport Session           
            using var dlImport = new SimpleDataLayer(XpoTypesInfoHelper.GetXpoTypeInfoSource().XPDictionary, store);
            using var dataImportSession = new Session(dlImport);
            var dataImportParam = dataImportSession.GetObjectByKey<DataImport>(dataImport.Oid);
            //dataImportParam.UpdateUI += DataImportParam_UpdateUI;

            await dataImportSession.ExplicitBeginTransactionAsync();
            await session.ExplicitBeginTransactionAsync();
            session.BeginTrackingChanges();
          
            
     

            //int lastReportedProgress = 0;

            //bool progressEnabled = true;

            //IProgress<int> progress = new Progress<int>(value =>
            //{
            //    if (!progressEnabled) return;

            //    if (value >= lastReportedProgress + 1)  // Commit nur bei einer Erhöhung von 1%
            //    {
            //        dataImportParam.ProgressPercentage = value;
            //        lastReportedProgress = value;

            //        dataImportParam.Save();
            //        dataImportSession.ExplicitCommitTransactionAsync();
            //        dataImportParam.TriggerUpdateUI();
            //    }


            //});


            // ✅ 3 Phasen
            //var stagingProgress = ProgressPhases.MapToRange(progress, 0, 50);
            //var integrateProgress = ProgressPhases.MapToRange(progress, 50, 80);
            //var ragProgress = ProgressPhases.MapToRange(progress, 80, 100);

           

            try
            {
                //dataImportParam.ImportStatus = ImportStatus.InProgress;
                //progress.Report(0);

                var adapter = ERPAdapterFactory.GetAdapter(dataImportParam.ERPSystem.ToString(), session, dataImportSession);

                // ✅ Staging oder DirectToCentralModel sauber mappen
                if (adapter.Mode == ImportMode.DirectToCentralModel)
                    await adapter.ImportData(dataImportParam, currentTenant, null);
                else
                    await adapter.ImportData(dataImportParam, currentTenant, null);

                //dataImportParam.ImportStatus = ImportStatus.CreateModel;

                //if (adapter.Mode == ImportMode.StagingThenIntegrate)
                //{
                //    var integrator = new CentralModelIntegrator(session, dataImportSession);
                //    await integrator.IntegrateData(
                //        dataImportParam.ERPSystem.ToString(),
                //        dataImportParam,
                //        currentTenant,
                //        integrateProgress);
                //}

                // Differences merken (für finalen Status nach RAG)
                // bool hasDifferences = (dataImportParam.RowDifference != 0 || Math.Abs(dataImportParam.AmountDifference) > 0.08M);

                // ✅ Import committen (atomar)
                await session.CommitTransactionAsync();
                await session.ExplicitCommitTransactionAsync();
                session.Dispose();
                session.Disconnect();

                //// ✅ Phase 3: RAG
                //dataImportParam.ImportStatus = ImportStatus.BuildRag;
                //progress.Report(80);

                //// Create RAG-Session (separat!)
                //var ragStore = (ConnectionProviderSql)XpoDefault.GetConnectionProvider(
                //    currentTenant.ConnectionString,
                //    AutoCreateOption.SchemaAlreadyExists);

                //var ragDl = new SimpleDataLayer(XpoTypesInfoHelper.GetXpoTypeInfoSource().XPDictionary, ragStore);
                //var ragSession = new Session(ragDl);

                //try
                //{

                //    // ❗ KEIN ExplicitBeginTransaction hier, wenn der Builder batch-committet
                //    // ragSession.BeginTrackingChanges(); // optional, kann man auch weglassen für read-heavy / batch-writes

                //    await CentralModelRagBuilder.BuildRagFromCentralModelAsync(
                //        ragSession,
                //        currentTenant,
                //        dataImportParam.Client.Oid,
                //        dataImportParam.AuditYear.Oid,
                //        dataImportParam.CompanyCode,
                //        ragProgress);

                //    // ✅ finaler Status nach erfolgreichem RAG
                //    dataImportParam.ImportStatus = hasDifferences
                //        ? ImportStatus.SucceededWithDifferences
                //        : ImportStatus.Succeeded;

                //    progress.Report(100);
                //}
                //catch (Exception ragEx)
                //{
                //    // ✅ Import bleibt erfolgreich, nur RAG ist fehlgeschlagen
                //    dataImportParam.ImportStatus = ImportStatus.SucceededWithRagErrors;
                //    dataImportParam.ErrorMessage = $"RAG: {ragEx.Message}";
                //    dataImportParam.Save();

                //    // optional: progress trotzdem auf 100, damit UI nicht “hängt”
                //    progress.Report(100);
                //}
                //finally
                //{
                //    if (ragSession != null)
                //    {
                //        try
                //        {
                //            // falls doch irgendwo eine TX offen ist
                //            if (ragSession.InTransaction)
                //                await ragSession.ExplicitRollbackTransactionAsync();
                //        }
                //        catch { /* ignore */ }

                //        try { ragSession.Dispose(); } catch { /* ignore */ }
                //        try { ragSession.Disconnect(); } catch { /* ignore */ }
                //    }
                //}
            }
            catch (Exception ex)
            {
                dataImportParam.ImportStatus = ImportStatus.Failed;

                try
                {
                    if (session.InTransaction)
                        await session.ExplicitRollbackTransactionAsync();
                }
                catch { /* ignore rollback errors */ }

                dataImportParam.ErrorMessage = $"IMPORT: {ex.Message}";
                dataImportParam.Save();

                try { session.Dispose(); } catch { }
                try { session.Disconnect(); } catch { }

                throw;
            }
            finally
            {

                dataImportParam.Save();
                //dataImportParam.TriggerUpdateUI();

                if (dataImportSession.InTransaction)
                    await dataImportSession.ExplicitCommitTransactionAsync();

               // progress.Report(100);

                // optional: gib dem UI-Context die Chance, den Callback auszuführen
                await System.Threading.Tasks.Task.Yield();

                //progressEnabled = false;

                dataImportSession.Dispose();
                dataImportSession.Disconnect();
            }
        }

        private void DataImportParam_UpdateUI(object sender, EventArgs e)
        {
            if (!view.IsDisposed)
            {
                view.RefreshDataSource();
                view.Refresh();
            }
        }

        private CustomerTenant GetCurrentTenant()
        {
            //Get Tenant-Information and Connection-String            
            CustomerTenant currentTenant = ObjectSpace.FirstOrDefault<CustomerTenant>(p => p.Name =="auditheroes.de");
            return currentTenant;
        }


        protected override void OnActivated()
        {
            base.OnActivated();
            // Perform various tasks depending on the target View.

            ////set SQL Command Timeout 
            //var sp = Application.Modules.FindModule<FlowAuditorInspectorModule>().ServiceProvider;
            //var configuration = sp.GetService<IConfiguration>();
            //string timeout = configuration.GetSection("ImportSQLCommandTimeoutInMinutes").GetSection("timeout").Value;
            //ImportSqlCommandTimeout = TimeSpan.FromMinutes(double.Parse(timeout ?? "30"));

            currentTenant = GetCurrentTenant();


            view = View;

            if (View is DetailView)
            {
                importParams = View.CurrentObject as DataImport;
                //Check importstate
                if (importParams.ImportStatus == ImportStatus.InProgress ||
                    importParams.ImportStatus == ImportStatus.CreateModel ||
                    importParams.ImportStatus == ImportStatus.Succeeded)
                {
                    // Alle Property Editors durchlaufen
                    View.AllowEdit.SetItemValue("Editable", false);
                }
            }

            //Check currentImport Workload and control import logic:
            CheckImportWorkload();
        }



        private void SaveAction_Execute(object sender, SimpleActionExecuteEventArgs e)
        {
            importCsvAction.Enabled["Active"] = true;
        }

        public IObjectSpace CreateImportOS()
        {
            var os = Application.CreateObjectSpace<DataImport>();

            return os;
        }

        protected override void OnViewControlsCreated()
        {
            base.OnViewControlsCreated();
            // Access and customize the target View control.
        }
        protected override void OnDeactivated()
        {
            // Unsubscribe from previously subscribed events and release other references and resources.           
            base.OnDeactivated();

        }

    }
}


