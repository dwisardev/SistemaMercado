using FluentValidation;
using SGM.API.DTOs.Request;

namespace SGM.API.Validators
{
    public class CreateUsuarioValidator : AbstractValidator<CreateUsuarioDto>
    {
        private static readonly string[] _rolesValidos = ["Admin", "Cajero", "Dueno"];

        public CreateUsuarioValidator()
        {
            RuleFor(x => x.NombreCompleto).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(200);
            RuleFor(x => x.Password).NotEmpty().MinimumLength(6)
                .WithMessage("La contraseña debe tener al menos 6 caracteres.");
            RuleFor(x => x.Rol)
                .Must(r => _rolesValidos.Contains(r))
                .WithMessage($"Rol inválido. Valores permitidos: {string.Join(", ", _rolesValidos)}.");
        }
    }
}
