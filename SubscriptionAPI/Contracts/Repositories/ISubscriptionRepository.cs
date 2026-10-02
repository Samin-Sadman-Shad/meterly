using SubscriptionAPI.Models;

namespace SubscriptionAPI.Contracts.Repositories
{
    public interface ISubscriptionRepository : IGenericRepository<Subscription>
    {
        Task<List<Subscription>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
        Task<List<Subscription>> GetActiveAsync(Guid userId, CancellationToken ct = default);
    }
}
