using System.ComponentModel.DataAnnotations;
using Identity.Model;
using SubscriptionAPI.Models;

namespace SubscriptionAPI.DTOs.SubscriptionDTOs
{
    public class CreateSubscriptionDto
    {
        public required Plan PlanPurchased {get; set;}
        [EmailAddress]
        public required string UserEmail { get; set; }

        public required DateTimeOffset StartDate { get; set; }
        public required DateTimeOffset EndDate { get; set; }
    }
}
