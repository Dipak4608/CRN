using FluentValidation;
using SecureWebApi.DTOs;

namespace SecureWebApi.Validators
{
    public class UpdateProductValidator:AbstractValidator<UpdateProductRequest>
    {
        public UpdateProductValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .MaximumLength(100);
            RuleFor(x => x.Description)
                .NotEmpty()
                .MaximumLength(500);
            RuleFor(x => x.Price)
                .GreaterThan(0);
            RuleFor(x => x.Stock)
                .GreaterThanOrEqualTo(0);
            RuleFor(x=>x.Category)
                .NotEmpty()
                .MaximumLength(100);
            
        }
    }
}
