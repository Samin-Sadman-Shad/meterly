using Identity.Model;
using SubscriptionAPI.Models.Components;

namespace SubscriptionAPI.Models
{
    public class Plan:BaseEntity
    {
        public required string Title { get; set; }
        public required string Subtitle { get; set; }

        public required PriceCard PriceCard { get; set; }

        public bool IsEnabled { get; set; }
        public required string Version { get; set; }
        public List<Subscription> Subscriptions { get; set; } = new List<Subscription>();
    }
}
