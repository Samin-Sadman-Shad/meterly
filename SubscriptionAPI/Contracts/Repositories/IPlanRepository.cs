using SubscriptionAPI.Models;

namespace SubscriptionAPI.Contracts.Repositories
{
    public interface IPlanRepository : IGenericRepository<Plan>
    {
        Task<Plan?> GetByTitleAndVersionAsync(string title, string version, CancellationToken ct = default);
    }
}
