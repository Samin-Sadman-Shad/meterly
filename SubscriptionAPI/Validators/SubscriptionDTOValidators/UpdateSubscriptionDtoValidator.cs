using FluentValidation;
using SubscriptionAPI.Contracts.Repositories;
using SubscriptionAPI.DTOs.SubscriptionDTOs;
using SubscriptionAPI.Models;

namespace SubscriptionAPI.Validators.SubscriptionDTOValidators
{
    public class UpdateSubscriptionDtoValidator : AbstractValidator<UpdateSubscriptionDto>
    {
        private readonly IPlanRepository _planRepository;

        public UpdateSubscriptionDtoValidator(IPlanRepository planRepository)
        {
            _planRepository = planRepository;

            RuleFor(x => x.PlanPurchased)
                .NotNull()
                .WithMessage("'Plan Purchased' is required.")
                .Must(x => x != null && x.Id != Guid.Empty)
                .WithMessage("'Plan Purchased' must reference a persisted plan.")
                .When(x => x != null);

            RuleFor(x => x.PlanPurchased)
                .MustAsync(async (plan, ct) => await PlanExistsAsync(plan, ct))
                .WithMessage("The selected 'Plan Purchased' does not exist.")
                .When(x => x.PlanPurchased != null && x.PlanPurchased.Id != Guid.Empty);

            RuleFor(x => x.PreviousPlan)
                .Must(x => x == null || x.Id != Guid.Empty)
                .WithMessage("'Previous Plan' must reference a persisted plan when supplied.");

            RuleFor(x => x.PreviousPlan)
                .MustAsync(async (previousPlan, ct) => await PlanExistsAsync(previousPlan, ct))
                .WithMessage("The selected 'Previous Plan' does not exist.")
                .When(x => x.PreviousPlan != null && x.PreviousPlan.Id != Guid.Empty);

            RuleFor(x => x.UserEmail)
                .NotEmpty()
                .EmailAddress();

            RuleFor(x => x.StartDate)
                .NotEqual(default(DateTimeOffset))
                .WithMessage("'Start Date' must be a valid timestamp.");

            RuleFor(x => x.EndDate)
                .NotEqual(default(DateTimeOffset))
                .WithMessage("'End Date' must be a valid timestamp.")
                .GreaterThan(x => x.StartDate)
                .WithMessage("'End Date' must be later than 'Start Date'.");

            RuleFor(x => x.PlanPurchased)
                .Must((dto, plan) => dto.PreviousPlan == null || plan == null || plan.Id != dto.PreviousPlan.Id)
                .WithMessage("'Plan Purchased' must differ from 'Previous Plan'.")
                .When(x => x.PlanPurchased != null);
        }

        private async Task<bool> PlanExistsAsync(Plan? plan, CancellationToken cancellationToken)
        {
            if (plan == null)
            {
                return true;
            }

            return await _planRepository.DoesExist(plan);
        }
    }
}