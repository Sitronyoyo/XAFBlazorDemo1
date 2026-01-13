using DevExpress.Data.Filtering;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl;
using DevExpress.Xpo;
using DevExpress.Xpo.Metadata;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;

namespace DXApplication1.Module.BusinessObjects.Database { 

    [DefaultClassOptions]
    public partial class Customer
    {
        public Customer(Session session) : base(session) { }
        public override void AfterConstruction() { base.AfterConstruction(); }


    }

}
