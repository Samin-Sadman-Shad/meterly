using FluentValidation;
using SubscriptionAPI.Contracts.Repositories;
using SubscriptionAPI.DTOs.PlanDTOs;
using SubscriptionAPI.Models;

namespace SubscriptionAPI.Validators.PlanDTOValidators
{
    public class CreatePlanDtoValidator : AbstractValidator<CreatePlanDto>
    {
        private readonly IPlanRepository _planRepository;

        public CreatePlanDtoValidator(IPlanRepository planRepository)
        {
            _planRepository = planRepository;

            RuleFor(x => x.Title)
                .NotEmpty()
                .MaximumLength(200);

            RuleFor(x => x.Subtitle)
                .NotEmpty()
                .MaximumLength(500);

            RuleFor(x => x.PriceCardTitle)
                .NotEmpty()
                .MaximumLength(200);

            RuleFor(x => x.Version)
                .NotEmpty()
                .MaximumLength(50);

            RuleFor(x => x.Title)
                .MustAsync(async (dto, title, ct) => !await PlanExistsAsync(dto, title, ct))
                .WithMessage("A plan with the given 'Title' and 'Version' already exists.");
        }

        private async Task<bool> PlanExistsAsync(CreatePlanDto dto, string title, CancellationToken cancellationToken)
        {
            var probe = new Plan
            {
                Title = title,
                Version = dto.Version,
                Subtitle = string.Empty,
                PriceCard = null!,
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = null!
            };

            return await _planRepository.DoesExist(probe);
        }
    }
}