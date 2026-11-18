using System;

namespace SubscriptionAPI.Models.Components;

public interface ICreatableEntity
{
    Guid CreatedBy { get; }
    DateTimeOffset CreatedAt { get; }
}
