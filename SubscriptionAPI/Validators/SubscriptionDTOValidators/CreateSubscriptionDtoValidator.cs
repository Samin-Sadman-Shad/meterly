using FluentValidation;
using SubscriptionAPI.Contracts.Repositories;
using SubscriptionAPI.DTOs.SubscriptionDTOs;
using SubscriptionAPI.Models;
using SubscriptionAPI.Validators;

namespace SubscriptionAPI.Validators.SubscriptionDTOValidators
{
    public class CreateSubscriptionDtoValidator : AbstractValidator<CreateSubscriptionDto>
    {
        private readonly IPlanRepository _planRepository;

        public CreateSubscriptionDtoValidator(IPlanRepository planRepository)
        {
            _planRepository = planRepository;

            RuleFor(x => x.PlanPurchased)
                .NotNull()
                .WithMessage(CONST_STRING.IsRequired)
                .Must(x => x != null && x.Id != Guid.Empty)
                .WithMessage(CONST_STRING.MustReferencePersistedPlan)
                .When(x => x != null);

            RuleFor(x => x.PlanPurchased)
                .MustAsync(async (plan, ct) => await PlanExistsAsync(plan, ct))
                .WithMessage(CONST_STRING.DoesNotExist)
                .When(x => x.PlanPurchased != null && x.PlanPurchased.Id != Guid.Empty);

            RuleFor(x => x.UserEmail)
                .NotEmpty()
                .WithMessage(CONST_STRING.IsRequired)
                .EmailAddress()
                .WithMessage(CONST_STRING.InvalidEmailAddress);

            RuleFor(x => x.StartDate)
                .NotEqual(default(DateTimeOffset))
                .WithMessage(CONST_STRING.MustBeValidTimestamp);

            RuleFor(x => x.EndDate)
                .NotEqual(default(DateTimeOffset))
                .WithMessage(CONST_STRING.MustBeValidTimestamp)
                .GreaterThan(x => x.StartDate)
                .WithMessage(CONST_STRING.MustBeLaterThan);
        }

        private async Task<bool> PlanExistsAsync(Plan? plan, CancellationToken cancellationToken)
        {
            if (plan == null)
            {
                return true;
            }

            return await _planRepository.DoesExist(plan, cancellationToken);
        }
    }
}
