using DevExpress.Data.Filtering;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;
using DevExpress.Xpo;
using DevExpress.Xpo.Metadata;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;

namespace DXApplication1.Module.BusinessObjects.Database
{

    // 1. 一定要写[DefaultClassOptions]， 才会显示在UI
    [DefaultClassOptions]
    [RuleCriteria("DueDate_Rule", DefaultContexts.Save, "DueDate >= InvoiceDate", "DueDate")]
    public partial class Invoice
    {
        // 2.构造
        public Invoice(Session session) : base(session) { }

        // 3.AfterConstruction逻辑， 比如初始化某些值
        public override void AfterConstruction() { 
            base.AfterConstruction(); 
            InvoiceDate = DateTime.Today; //默认是今天
            InvoiceStatus = Status.Draft; //默认是Draft
        
        }

        //4. 建议自定义其他属性：枚举enum
        private Status invoiceStatus;
        public Status InvoiceStatus
        {
            get => invoiceStatus;
            set => SetPropertyValue(nameof(InvoiceStatus), ref invoiceStatus, value);
        }   
    }

    public enum Status
    {
        Draft,
        Submitted,
        Paid,
        Cancelled
    }

}
