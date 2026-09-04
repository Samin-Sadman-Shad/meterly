using SubscriptionAPI.Models.Components;

namespace SubscriptionAPI.Contracts.Repositories
{
    public interface IGenericRepository<T> where T:BaseEntity
    {
        Task<List<T>> GetAllAsync();
        Task<T> GetAsync(Guid id);
        Task<T> AddAsync(T entity);
        Task<T> UpdateAsync(T entity);
        Task DeleteAsync(T entity);

        Task<bool> DoesExist(T entity);
    }
}
