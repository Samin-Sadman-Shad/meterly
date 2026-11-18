using System;

namespace SubscriptionAPI.Models.Components;

public interface IDomainEvent
{
    DateTimeOffset OccurredAt { get; }
}
