using FluentValidation;
using SubscriptionAPI.Contracts.Repositories;
using SubscriptionAPI.DTOs.PlanDTOs;
using SubscriptionAPI.Models;
using SubscriptionAPI.Validators;

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
                .WithMessage(CONST_STRING.IsRequired)
                .MaximumLength(200)
                .WithMessage(CONST_STRING.MaxLengthExceeded);

            RuleFor(x => x.Subtitle)
                .NotEmpty()
                .WithMessage(CONST_STRING.IsRequired)
                .MaximumLength(500)
                .WithMessage(CONST_STRING.MaxLengthExceeded);

            RuleFor(x => x.PriceCardTitle)
                .NotEmpty()
                .WithMessage(CONST_STRING.IsRequired)
                .MaximumLength(200)
                .WithMessage(CONST_STRING.MaxLengthExceeded);

            RuleFor(x => x.Version)
                .NotEmpty()
                .WithMessage(CONST_STRING.IsRequired)
                .MaximumLength(50)
                .WithMessage(CONST_STRING.MaxLengthExceeded);

            RuleFor(x => x.Title)
                .MustAsync(async (dto, title, ct) => !await PlanExistsAsync(dto, title, ct))
                .WithMessage(CONST_STRING.PlanAlreadyExists);
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

            return await _planRepository.DoesExist(probe, cancellationToken);
        }
    }
}
