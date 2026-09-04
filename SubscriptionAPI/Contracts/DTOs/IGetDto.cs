namespace SubscriptionAPI.Contracts.DTOs
{
    public interface IGetDto<T>
    {
        Guid RecordId { get; set; }
        T Record { get; set; }
    }
}
