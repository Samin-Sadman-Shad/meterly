using System;

namespace SubscriptionAPI.Models.Components;

///<summary>
/// Represents an entity that can be soft deleted.
/// Nothing will be removed from the database, but the entity will be marked as deleted and can be filtered out of queries.
/// Nothing in this interface is allowed to be set manually, that will be done by the repository or service layer.
///</summary>
public interface ISoftDeletable
{
    bool IsDeleted { get; }
    //values will be set by EntityEntry.Property('propertyName').CurrentValue = value
    DateTimeOffset? DeletedAt { get;  }
    Guid? DeletedBy { get; }
}
