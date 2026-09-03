using Microsoft.EntityFrameworkCore;
using SubscriptionAPI.Models;

namespace SubscriptionAPI.Repositories
{
    public class SubscriptionDbContext:DbContext
    {
        public SubscriptionDbContext(DbContextOptions options):base(options)
        {
            
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Subscription>()
                .HasIndex(sub => new { sub.PlanId, sub.UserId })
                .IsUnique()
                .HasDatabaseName("IX_User_Id_Plan_Id_Unique");
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            foreach(var entry in ChangeTracker.Entries<BaseEntity>())
            {
                if(entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                }
            }
            return base.SaveChangesAsync(cancellationToken);
        }
    }
}
