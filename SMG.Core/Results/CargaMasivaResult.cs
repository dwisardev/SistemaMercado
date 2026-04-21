using System;
using System.Collections.Generic;
using System.Text;

namespace SGM.Core.Results
{
    public record CargaMasivaResult
    (
        int PuestosAfectados,
        decimal MontoTotal,
        Guid LoteId,
        string ConceptoNombre,
        string Periodo
     );
}
