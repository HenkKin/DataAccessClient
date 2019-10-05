using System;

namespace DataAccessClient.EntityBehaviors
{
    public interface ISoftDeletable<TIdentifierType> where TIdentifierType : struct
    {
        bool IsDeleted { get; set; }
        DateTime? DeletedOn { get; set; }
        TIdentifierType? DeletedById { get; set; }
    }
}