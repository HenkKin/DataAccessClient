using System;

namespace DataAccessClient.EntityBehaviors
{
    public interface IModifiable<TIdentifierType> where TIdentifierType: struct
    {
        DateTime? ModifiedOn { get; set; }
        TIdentifierType? ModifiedById { get; set; }
    }
}