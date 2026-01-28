using System;
using DevExpress.Xpo;
using DevExpress.Xpo.Metadata;
using DevExpress.Data.Filtering;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using DevExpress.Persistent.BaseImpl;
namespace DXApplication1.Module.BusinessObjects.Database
{

    public partial class DataImport
    {
        public DataImport(Session session) : base(session) { }
        public override void AfterConstruction() { base.AfterConstruction(); }

        public FileData File { get; set; }

    }

}
