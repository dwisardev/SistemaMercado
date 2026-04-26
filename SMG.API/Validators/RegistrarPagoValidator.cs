using FluentValidation;
using SGM.API.DTOs.Request;

namespace SGM.API.Validators
{
    public class RegistrarPagoValidator : AbstractValidator<RegistrarPagoDto>
    {
        private static readonly string[] _metodosValidos = ["Efectivo", "Transferencia", "Tarjeta", "Cheque"];

        public RegistrarPagoValidator()
        {
            RuleFor(x => x.DeudaId).NotEmpty();
            RuleFor(x => x.MontoPagado).GreaterThan(0).WithMessage("El monto debe ser mayor a 0.");
            RuleFor(x => x.Metodo)
                .Must(m => _metodosValidos.Contains(m))
                .WithMessage($"Método inválido. Valores permitidos: {string.Join(", ", _metodosValidos)}.");
        }
    }
}
