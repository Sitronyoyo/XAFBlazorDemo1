using DevExpress.ExpressApp.DC;
using System;
using System.Collections.Generic;
using System.Text;

namespace DXApplication1.Module.Import
{
    public enum ERPSystem
    {
        [XafDisplayName("SAP ERP")]
        SAP_ERP,
        [XafDisplayName("DATEV")]
        DATEV,
        [XafDisplayName("Microsoft Dynamics BC")]
        Microsoft_Dynamics_365,
        [XafDisplayName("Germany GoBD")]
        Germany_gobd,
        [XafDisplayName("Norwegian SAF-T")]
        Norwegian_SAF_T
    }
}
