using System;
using System.Collections.Generic;
using System.Text;

namespace SGM.Core.Results
{
    public record DeudaPendienteResult
   (
        Guid DeudaId,
        string PuestoCodigo,
        string? DuenoNombre,
        string? ConceptoNombre,
        decimal Monto,
        string Periodo,
        DateOnly FechaEmision,
        DateOnly? FechaVencimiento,
        int DiasMora
        );
}
