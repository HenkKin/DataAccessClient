namespace DataAccessClient.EntityBehaviors
{
    public interface ITenantScopable<TIdentifierType> where TIdentifierType : struct
    {
        TIdentifierType TenantId { get; set; }
    }
}