using System;
using System.Collections.Generic;
using System.Text;

namespace SGM.Core.Results
{
    public record MorosidadResult
    (
     string PuestoCodigo,
     string ? DuenoNombre,
     string ? DuenoTefelono,
     int CantidadDeudasVencidas,
     decimal MontoTotalAdeudado,
     DateOnly DeudaMasAntigua,
     int DiasMayorAtraso
        );
}
