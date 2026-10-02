using SubscriptionAPI.Models;
using System.ComponentModel.DataAnnotations;

namespace SubscriptionAPI.DTOs.SubscriptionDTOs
{
    public class UpdateSubscriptionDto
    {
        public Plan? PreviousPlan { get; set; }
        public required Plan PlanPurchased { get; set; }
        [EmailAddress]
        public required string UserEmail { get; set; }

        public required DateTimeOffset StartDate { get; set; }
        public required DateTimeOffset EndDate { get; set; }
    }
}
