using Identity.Model;

namespace SubscriptionAPI.DTOs.PlanDTOs
{
    public class PlanDtoWithDetails:PlanDto
    {
        public required ApplicationUser CreatedBy { get; set; }
        public required DateTimeOffset CreatedAt { get; set; }
    }
}
