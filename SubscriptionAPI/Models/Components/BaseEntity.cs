using Identity.Model;
using System.ComponentModel.DataAnnotations;

namespace SubscriptionAPI.Models.Components
{
    public class BaseEntity
    {
        [Key]
        public Guid Id { get; set; }
        public required DateTimeOffset CreatedAt { get; set; }
        public required ApplicationUser CreatedBy { get; set; }
    }
}
