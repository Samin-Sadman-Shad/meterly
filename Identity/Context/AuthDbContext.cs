using Identity.Model;
using Microsoft.EntityFrameworkCore;

namespace Identity.Context;

public class AuthDbContext : DbContext
{
    public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<User>(user =>
        {
            user.HasKey(u => u.Id);
            user.Property(u => u.UserName).HasMaxLength(64).IsRequired();
            user.Property(u => u.NormalizedUserName).HasMaxLength(64).IsRequired();
            user.Property(u => u.Email).HasMaxLength(256).IsRequired();
            user.Property(u => u.NormalizedEmail).HasMaxLength(256).IsRequired();
            user.Property(u => u.FirstName).HasMaxLength(100);
            user.Property(u => u.LastName).HasMaxLength(100);
            user.Property(u => u.PasswordHash).IsRequired();
            user.Property(u => u.Roles); // primitive collection -> JSON column on SQL Server
            user.HasIndex(u => u.NormalizedUserName).IsUnique();
            user.HasIndex(u => u.NormalizedEmail).IsUnique();
        });

        builder.Entity<RefreshToken>(token =>
        {
            token.HasKey(t => t.Id);
            token.Property(t => t.TokenHash).HasMaxLength(64).IsRequired();
            token.HasIndex(t => t.TokenHash).IsUnique();
            token.HasIndex(t => new { t.UserId, t.FamilyId });
            token.Property(t => t.ConcurrencyStamp).IsConcurrencyToken();
            token.HasOne<User>()
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
