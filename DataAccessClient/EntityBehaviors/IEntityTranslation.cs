using System;

namespace DataAccessClient.EntityBehaviors
{
    /// <summary>
    /// DO NOT USE this interface. It is only used as marker interface for ITranslatable generic where clause
    /// </summary>
    public interface IEntityTranslation<TIdentifierType, TLocaleIdentifierType> 
        where TIdentifierType : struct
        where TLocaleIdentifierType : IConvertible
    {
        TIdentifierType TranslatedEntityId { get; set; }
        TLocaleIdentifierType LocaleId { get; set; }
    }

    /// <summary>
    /// Use this interface to decorate entities with translatable properties
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <typeparam name="TIdentifierType"></typeparam>
    /// <typeparam name="TLocaleIdentifierType"></typeparam>
    public interface IEntityTranslation<TEntity, TIdentifierType, TLocaleIdentifierType> : IEntityTranslation<TIdentifierType, TLocaleIdentifierType>
        where TEntity : class
        where TIdentifierType : struct
        where TLocaleIdentifierType : IConvertible
    {
        TEntity TranslatedEntity { get; set; }
    }
}