using System;

namespace SubscriptionAPI.Models.Components;

public interface IModifyableEntity
{
    Guid LastUpdatedBy { get; }
    DateTimeOffset LastUpdatedAt { get; }
}
