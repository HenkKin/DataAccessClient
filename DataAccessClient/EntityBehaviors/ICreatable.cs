using System;

namespace DataAccessClient.EntityBehaviors
{
    public interface ICreatable<TIdentifierType> where TIdentifierType : struct
    {
        DateTime CreatedOn { get; set; }
        TIdentifierType CreatedById { get; set; }
    }
}