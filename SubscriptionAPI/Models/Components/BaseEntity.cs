using System.ComponentModel.DataAnnotations;

namespace SubscriptionAPI.Models.Components
{
    public abstract class BaseEntity
    {
        public Guid Id { get; protected init; } = Guid.CreateVersion7();

        public override bool Equals(object? obj)
        {
            if(obj is BaseEntity other 
                && GetType() == other.GetType() 
                && Id == other.Id)
            {
                return true;
            }
            else if( ReferenceEquals(this, obj))
            {
                return true;
            }
            return false;
        }

        public override int GetHashCode() => HashCode.Combine(GetType(), Id)

    }
}
