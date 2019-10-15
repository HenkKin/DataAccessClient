using System.Collections.Generic;

namespace DataAccessClient.EntityBehaviors
{
    /// <summary>
    /// Use this interface to mark your entity as translatable. All translatable properties should be moved to the IEntityTranslation entity
    /// </summary>
    /// <typeparam name="TEntityTranslation"></typeparam>
    /// <typeparam name="TIdentifierType"></typeparam>
    public interface ITranslatable<TEntityTranslation, TIdentifierType>
        where TEntityTranslation : class, IEntityTranslation<TIdentifierType>
        where TIdentifierType : struct
    {
        ICollection<TEntityTranslation> Translations { get; set; }
    }
}
