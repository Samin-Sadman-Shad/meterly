using FluentValidation;
using SubscriptionAPI.DTOs.PlanDTOs;

namespace SubscriptionAPI.Validators.PlanDTOValidators
{
    public class PlanDtoWithDetailsValidator : AbstractValidator<PlanDtoWithDetails>
    {
        public PlanDtoWithDetailsValidator()
        {
            Include(new PlanDtoValidator());

            RuleFor(x => x.CreatedAt)
                .NotEqual(default(DateTimeOffset))
                .WithMessage(CONST_STRING.MustBeValidTimestamp);

            RuleFor(x => x.CreatedBy)
                .NotNull()
                .WithMessage(CONST_STRING.IsRequired);
        }
    }
}
