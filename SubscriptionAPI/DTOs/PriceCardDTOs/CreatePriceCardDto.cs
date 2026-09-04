namespace SubscriptionAPI.DTOs.PriceCardDTOs
{
    public class CreatePriceCardDto
    {
        public required string PriceCardTitle { get; set; }
        public double Price { get; set; }
        public required string PriceDescription { get; set; }

        public List<string> Features { get; set; } = new List<string>();
    }
}
