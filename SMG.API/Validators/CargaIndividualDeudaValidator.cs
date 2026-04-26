using FluentValidation;
using SGM.API.DTOs.Request;

namespace SGM.API.Validators
{
    public class CargaIndividualDeudaValidator : AbstractValidator<CargaIndividualDeudaDto>
    {
        public CargaIndividualDeudaValidator()
        {
            RuleFor(x => x.PuestoId).NotEmpty();
            RuleFor(x => x.ConceptoId).NotEmpty();
            RuleFor(x => x.Monto).GreaterThan(0).WithMessage("El monto debe ser mayor a 0.");
            RuleFor(x => x.FechaVencimiento)
                .NotEmpty()
                .Must(f => DateOnly.TryParse(f, out _))
                .WithMessage("Fecha de vencimiento inválida. Use el formato YYYY-MM-DD.");
            RuleFor(x => x.Periodo).NotEmpty().MaximumLength(20);
        }
    }
}
