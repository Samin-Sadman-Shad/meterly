using SubscriptionAPI.Models.Components;

namespace SubscriptionAPI.Contracts.Repositories
{
    public interface IGenericRepository<T> where T : BaseEntity
    {
        IAsyncEnumerable<T> GetAllAsync(CancellationToken ct = default);
        Task<T?> GetAsync(Guid id, CancellationToken ct = default);
        ValueTask AddAsync(T entity, CancellationToken ct = default);
        Task UpdateAsync(T entity, CancellationToken ct = default);
        Task DeleteAsync(T entity, CancellationToken ct = default);
        Task<bool> DoesExist(T entity, CancellationToken ct = default);
        Task<bool> AnyAsync(CancellationToken ct = default);
        Task<int> CountAsync(CancellationToken ct = default);
        Task<int> SaveChangesAsync(CancellationToken ct = default);
    }
}
