using FluentValidation;
using SubscriptionAPI.DTOs.PlanDTOs;

namespace SubscriptionAPI.Validators.PlanDTOValidators
{
    public class PlanDtoValidator : AbstractValidator<PlanDto>
    {
        public PlanDtoValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty()
                .WithMessage(CONST_STRING.IsRequired)
                .MaximumLength(200)
                .WithMessage(CONST_STRING.MaxLengthExceeded);

            RuleFor(x => x.Subtitle)
                .NotEmpty()
                .WithMessage(CONST_STRING.IsRequired)
                .MaximumLength(500)
                .WithMessage(CONST_STRING.MaxLengthExceeded);

            RuleFor(x => x.Version)
                .NotEmpty()
                .WithMessage(CONST_STRING.IsRequired)
                .MaximumLength(50)
                .WithMessage(CONST_STRING.MaxLengthExceeded);
        }
    }
}
