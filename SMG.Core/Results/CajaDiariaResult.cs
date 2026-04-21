using System;
using System.Collections.Generic;
using System.Text;

namespace SGM.Core.Results
{
    public record CajaDiariaResult
   (
        DateOnly Fecha,
        decimal TotalCobrado,
        int TotalOperaciones,
        decimal TotalEfectivo,
        decimal TotalTransferencia,
        decimal TotalOtro 
        );
}
