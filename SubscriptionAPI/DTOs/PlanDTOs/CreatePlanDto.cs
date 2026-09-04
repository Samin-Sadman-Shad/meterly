using Identity.Model;
using SubscriptionAPI.Contracts.DTOs;

namespace SubscriptionAPI.DTOs.PlanDTOs
{
    public class CreatePlanDto : ICreateDto
    {
        public required string Title { get; set; }
        public required string Subtitle { get; set; }

        public required string PriceCardTitle { get; set; }

        public required string Version { get; set; }


    }
}
