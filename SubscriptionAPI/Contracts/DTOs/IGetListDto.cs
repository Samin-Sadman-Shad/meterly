namespace SubscriptionAPI.Contracts.DTOs
{
    public interface IGetListDto<T>
    {
        List<T> Records { get; set; }
    }
}
