namespace SubscriptionAPI.Models.Components
{
    public class PriceCard
    {
        public int Price { get; set; }
        public required string PriceDescription { get; set; }

        public List<string> Features { get; set; } = new List<string>();
    }
}
