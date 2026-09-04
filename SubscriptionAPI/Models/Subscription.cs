using Identity.Model;
using SubscriptionAPI.Models.Components;
using System.ComponentModel.DataAnnotations.Schema;

namespace SubscriptionAPI.Models
{
    public class Subscription:BaseEntity
    {
        public Guid PlanId { get; set; }
        [ForeignKey(nameof(PlanId))]
        public required Plan PlanPurchased { get; set; }

        public Guid UserId {  get; set; }
        [ForeignKey(nameof(UserId))]
        public required ApplicationUser User { get; set; }

        public SubscriptionStatusEnum Status { get; set; }

        public DateTimeOffset StartDate { get; set; }
        public DateTimeOffset EndDate { get; set; }
    }
}
