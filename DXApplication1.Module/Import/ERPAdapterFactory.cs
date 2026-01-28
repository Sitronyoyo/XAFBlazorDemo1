using DevExpress.Xpo;
using System;
using System.Collections.Generic;
using System.Text;

namespace DXApplication1.Module.Import
{
    public static class ERPAdapterFactory
    {
        public static IERPAdapter GetAdapter(string erpSystem, Session innerSession, Session dataImportSession)
        {
            switch (erpSystem)
            {
                //case "SAP_ERP":
                //    return new SAPERPAdapter(innerSession, dataImportSession);
                //case "DATEV":
                //    return new DatevERPAdapter(innerSession, dataImportSession);
                //case "Microsoft_Dynamics_365":
                //    return new MicrosoftBC_Adapter(innerSession, dataImportSession);
                //case "Germany_gobd":
                //    return new GobD_Adapter(innerSession, dataImportSession);
                case "Norwegian_SAF_T":
                    return new SAFT_Adapter(innerSession, dataImportSession);
                default:
                    throw new NotSupportedException($"ERP-System '{erpSystem}' wird nicht unterstützt.");
            }
        }
    }
}
