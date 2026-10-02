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
                .WithMessage("'Created At' must be a valid timestamp.");

            RuleFor(x => x.CreatedBy)
                .NotNull()
                .WithMessage("'Created By' is required.");
        }
    }
}