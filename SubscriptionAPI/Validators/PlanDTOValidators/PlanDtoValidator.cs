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
                .MaximumLength(200);

            RuleFor(x => x.Subtitle)
                .NotEmpty()
                .MaximumLength(500);

            RuleFor(x => x.Version)
                .NotEmpty()
                .MaximumLength(50);
        }
    }
}