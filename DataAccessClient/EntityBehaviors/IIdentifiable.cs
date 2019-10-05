namespace DataAccessClient.EntityBehaviors
{
    public interface IIdentifiable<TIdentifierType> where TIdentifierType : struct
    {
        TIdentifierType Id { get; set; }
    }
}