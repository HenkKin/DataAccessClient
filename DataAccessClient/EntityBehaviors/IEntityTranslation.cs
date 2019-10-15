namespace DataAccessClient.EntityBehaviors
{
    /// <summary>
    /// DO NOT USE this interface. It is only used as marker interface for ITranslatable generic where clause
    /// </summary>
    public interface IEntityTranslation<TIdentifierType> where TIdentifierType : struct
    {
        TIdentifierType TranslatedEntityId { get; set; }
        string Language { get; set; }
    }

    /// <summary>
    /// Use this interface to decorate entities with translatable properties
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <typeparam name="TIdentifierType"></typeparam>
    public interface IEntityTranslation<TEntity, TIdentifierType> : IEntityTranslation<TIdentifierType>
            where TEntity : class
            where TIdentifierType : struct
    {
        TEntity TranslatedEntity { get; set; }
    }
}