using DXApplication1.Module.BusinessObjects.Database;
using System;
using System.Collections.Generic;
using System.Text;

namespace DXApplication1.Module.Import
{
    public interface IERPAdapter
    {
        /// <summary>
        /// Importiert die Daten aus den angegebenen FileData-Objekten in das ERP-spezifische Datenmodell.
        /// </summary>
        /// <param name="headerData">Die FileData-Instanz mit den Header-Daten.</param>
        /// <param name="positionData">Die FileData-Instanz mit den Positions-Daten.</param>
        /// <param name="accountData">Die FileData-Instanz mit den Account-Daten.</param>
        /// <param name="auditYear">Das Audit-Jahr.</param>
        /// <param name="client">Der Client für den Import.</param>
        /// <param name="companyCode">Der Firmen-Code für den Import.</param>
        /// <param name="progress">Das IProgress-Objekt zur Berichterstattung des Fortschritts.</param>
        System.Threading.Tasks.Task ImportData(DataImport dataImport, CustomerTenant tenant, IProgress<int> progress = null);
        ImportMode Mode { get; }
    }
}
