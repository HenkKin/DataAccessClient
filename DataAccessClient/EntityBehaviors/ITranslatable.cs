using System;
using System.Collections.Generic;

namespace DataAccessClient.EntityBehaviors
{
    /// <summary>
    /// Use this interface to mark your entity as translatable. All translatable properties should be moved to the IEntityTranslation entity
    /// </summary>
    /// <typeparam name="TEntityTranslation"></typeparam>
    /// <typeparam name="TIdentifierType"></typeparam>
    /// <typeparam name="TLocaleIdentifierType"></typeparam>
    public interface ITranslatable<TEntityTranslation, TIdentifierType, TLocaleIdentifierType>
        where TEntityTranslation : class, IEntityTranslation<TIdentifierType, TLocaleIdentifierType>
        where TIdentifierType : struct
        where TLocaleIdentifierType : IConvertible

    {
        ICollection<TEntityTranslation> Translations { get; set; }
    }
}
