using Identity.Model;
using System.ComponentModel.DataAnnotations;

namespace SubscriptionAPI.Models
{
    public class BaseEntity
    {
        [Key]
        public Guid Id { get; set; }
        public required DateTime CreatedAt { get; set; }
        public required ApplicationUser CreatedBy { get; set; }
    }
}
