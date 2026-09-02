using FluentValidation;
using SecureWebApi.DTOs;

namespace SecureWebApi.Validators
{
    public class CreateProductValidator:AbstractValidator<CreateProductRequest>
    {
        public CreateProductValidator()
        {
            RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Product Name is Required")
            .MaximumLength(100)
            .WithMessage("product name cannot exceed 100");
            RuleFor(x => x.Description)
                .NotEmpty()
                .WithMessage("Description is required")
                .MaximumLength(500)
                .WithMessage("Descreeption cannot exceed 500 ");
            RuleFor(x => x.Price)
                .GreaterThan(0)
                .WithMessage("Price must be greate than 0");
            RuleFor(x => x.Stock)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Stock cannot be negative");
            RuleFor(x => x.Category)
                .NotEmpty()
                .WithMessage("Category is required")
                .MaximumLength(100)
                .WithMessage("Category cannot exceed 100 characters.");
            
        }

    }
}
