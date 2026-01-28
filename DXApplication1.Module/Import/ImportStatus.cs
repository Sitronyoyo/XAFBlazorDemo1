using DevExpress.Persistent.Base;
using System;
using System.Collections.Generic;
using System.Text;

namespace DXApplication1.Module.Import
{
    public enum ImportStatus
    {
        NotStarted,

        [ImageName("State_Validation_Information")]
        InProgress,            // Adapter läuft (ERP -> Staging oder direkt)

        [ImageName("State_Validation_Skipped")]
        CreateModel,           // Integrator / Central Model build

        [ImageName("State_Validation_Information")]
        BuildRag,              // ✅ neu: RAG/Embeddings aus Central Model

        [ImageName("State_Validation_Valid")]
        Succeeded,             // Import + RAG erfolgreich

        [ImageName("State_Validation_Warning")]
        SucceededWithDifferences, // Import ok, aber Deltas

        [ImageName("State_Validation_Warning")]
        SucceededWithRagErrors,   // ✅ neu: Import ok, RAG (teilweise) fehlgeschlagen

        [ImageName("State_Validation_Invalid")]
        Failed
    }
}
