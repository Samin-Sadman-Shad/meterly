using Identity.Context;
using Identity.Contracts.Repositories;
using Identity.Model;
using Microsoft.EntityFrameworkCore;

namespace Identity.Repositories;

public class UserRepository : IUserRepository<User>
{
    private readonly AuthDbContext _db;

    public UserRepository(AuthDbContext db) => _db = db;

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _db.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task<User?> GetByUserNameOrEmailAsync(string identifier, CancellationToken cancellationToken = default)
    {
        var normalized = User.Normalize(identifier);
        return _db.Users.FirstOrDefaultAsync(
            u => u.NormalizedUserName == normalized || u.NormalizedEmail == normalized, cancellationToken);
    }

    public Task<bool> ExistsAsync(string userName, string email, CancellationToken cancellationToken = default)
    {
        var normalizedUserName = User.Normalize(userName);
        var normalizedEmail = User.Normalize(email);
        return _db.Users.AnyAsync(
            u => u.NormalizedUserName == normalizedUserName || u.NormalizedEmail == normalizedEmail, cancellationToken);
    }

    public async Task<bool> AddAsync(User user, CancellationToken cancellationToken = default)
    {
        _db.Users.Add(user);
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException)
        {
            // Unique index on user name / email lost a race with a concurrent registration.
            _db.Entry(user).State = EntityState.Detached;
            return false;
        }
    }

    public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        if (_db.Entry(user).State == EntityState.Detached)
        {
            _db.Users.Update(user);
        }
        await _db.SaveChangesAsync(cancellationToken);
    }
}
