using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Layout;
using DevExpress.ExpressApp.Model.NodeGenerators;
using DevExpress.ExpressApp.SystemModule;
using DevExpress.ExpressApp.Templates;
using DevExpress.ExpressApp.Utils;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;
using DXApplication1.Module.BusinessObjects.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DXApplication1.Module.Controllers
{
    // For more typical usage scenarios, be sure to check out https://docs.devexpress.com/eXpressAppFramework/DevExpress.ExpressApp.ViewController.
    public partial class InvoiceController : ObjectViewController<DetailView,Invoice>
    {
        // Use CodeRush to create Controllers and Actions with a few keystrokes.
        // https://docs.devexpress.com/CodeRushForRoslyn/403133/

        public SimpleAction SubmitInvoiceAction { get; private set; }
        public SimpleAction PayInvoiceAction { get; private set; }
        public SimpleAction CancelInvoiceAction { get; private set; }

        public InvoiceController()
        {
            InitializeComponent();
            // Target required Views (via the TargetXXX properties) and create their Actions.
         
            // Submit Invoice
            SubmitInvoiceAction = new SimpleAction(
                this, "SubmitInvoice", PredefinedCategory.Edit
            );
            SubmitInvoiceAction.Execute += SubmitInvoiceAction_Execute;

            // Pay Invoice
            PayInvoiceAction = new SimpleAction(
                this, "PayInvoice", PredefinedCategory.Edit
            );
            PayInvoiceAction.Execute += PayInvoiceAction_Execute;

            // Cancel Invoice
            CancelInvoiceAction = new SimpleAction(
                this, "CancelInvoice", PredefinedCategory.Edit
            );
            CancelInvoiceAction.Execute += CancelInvoiceAction_Execute;

        }

        private void CancelInvoiceAction_Execute(object sender, SimpleActionExecuteEventArgs e)
        {
            if (View.CurrentObject is Invoice invoice)
            {
                if (invoice.InvoiceStatus == Status.Draft || invoice.InvoiceStatus == Status.Submitted)
                {
                    invoice.InvoiceStatus = Status.Cancelled;
                    ObjectSpace.CommitChanges();
                    Application.ShowViewStrategy.ShowMessage(
                        "Invoice cancelled successfully.", InformationType.Success
                    );
                }
                else
                {
                    Application.ShowViewStrategy.ShowMessage(
                        "Invoice cannot be cancelled in current state.", InformationType.Warning
                    );
                }
            }
            UpdateActions();
        }

        private void PayInvoiceAction_Execute(object sender, SimpleActionExecuteEventArgs e)
        {
            if (View.CurrentObject is Invoice invoice)
            {
                if (invoice.InvoiceStatus == Status.Submitted)
                {
                    invoice.InvoiceStatus = Status.Paid;
                    ObjectSpace.CommitChanges();
                    Application.ShowViewStrategy.ShowMessage(
                        "Invoice paid successfully.", InformationType.Success
                    );
                }
                else
                {
                    Application.ShowViewStrategy.ShowMessage(
                        "Invoice cannot be paid in current state.", InformationType.Warning
                    );
                }
            }
            UpdateActions();
        }

        private void SubmitInvoiceAction_Execute(object sender, SimpleActionExecuteEventArgs e)
        {
            if (View.CurrentObject is Invoice invoice)
            {
                if (invoice.InvoiceStatus == Status.Draft)
                {
                    invoice.InvoiceStatus = Status.Submitted;
                    ObjectSpace.CommitChanges(); // 保存更改
                    Application.ShowViewStrategy.ShowMessage(
                        "Invoice submitted successfully.", InformationType.Success
                    );
                }
                else
                {
                    Application.ShowViewStrategy.ShowMessage(
                        "Invoice cannot be submitted in current state.", InformationType.Warning
                    );
                }
            }
            UpdateActions(); // 刷新按钮状态

        }

        protected override void OnActivated()
        {
            base.OnActivated();
            // Perform various tasks depending on the target View.
            // 订阅 CurrentObjectChanged 事件，用于刷新 Action 可用性
            View.CurrentObjectChanged += View_CurrentObjectChanged;
            UpdateActions();
        }

        private void UpdateActions()
        {
            if (View.CurrentObject is Invoice invoice)
            {
                // Invoice 有 InvoiceStatus 属性: Draft, Submitted, Paid, Cancelled
                SubmitInvoiceAction.Enabled.SetItemValue("StatusCheck", invoice.InvoiceStatus == Status.Draft);
                PayInvoiceAction.Enabled.SetItemValue("StatusCheck", invoice.InvoiceStatus == Status.Submitted);
                CancelInvoiceAction.Enabled.SetItemValue("StatusCheck", invoice.InvoiceStatus == Status.Draft || invoice.InvoiceStatus == Status.Submitted);
            }
            else
            {
                SubmitInvoiceAction.Enabled.SetItemValue("StatusCheck", false);
                PayInvoiceAction.Enabled.SetItemValue("StatusCheck", false);
                CancelInvoiceAction.Enabled.SetItemValue("StatusCheck", false);
            }
        }

        protected override void OnViewControlsCreated()
        {
            base.OnViewControlsCreated();
            // Access and customize the target View control.
            // 获取 InvoiceStatus 的枚举编辑器, 设置只读，用户无法修改枚举值
            var statusEditor = View.FindItem("InvoiceStatus") as PropertyEditor;
            if (statusEditor != null)
            {
                // 设置为只读，用户无法直接在下拉框中修改状态
          
                statusEditor.AllowEdit.SetItemValue("ReadOnly", true);
            }
        }
        protected override void OnDeactivated()
        {
            View.CurrentObjectChanged -= View_CurrentObjectChanged;
            // Unsubscribe from previously subscribed events and release other references and resources.
            base.OnDeactivated();
        }

        private void View_CurrentObjectChanged(object sender, EventArgs e)
        {
            UpdateActions();
        }
    }
}
