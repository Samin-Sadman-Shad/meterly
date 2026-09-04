using SubscriptionAPI.Models.Components;

namespace SubscriptionAPI.DTOs.PlanDTOs
{
    public class PlanDto
    {
        public required string Title { get; set; }
        public required string Subtitle { get; set; }

        public required string Version { get; set; }

    }
}
