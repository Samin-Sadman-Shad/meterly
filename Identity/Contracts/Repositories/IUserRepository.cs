namespace Identity.Contracts.Repositories;

public interface IUserRepository<TUser> where TUser : class
{
    Task<TUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Looks up by user name or email (case-insensitive).</summary>
    Task<TUser?> GetByUserNameOrEmailAsync(string identifier, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(string userName, string email, CancellationToken cancellationToken = default);

    /// <summary>Persists the user. Returns false on a user name / email uniqueness conflict.</summary>
    Task<bool> AddAsync(TUser user, CancellationToken cancellationToken = default);

    Task UpdateAsync(TUser user, CancellationToken cancellationToken = default);
}
