namespace DataAccessClient.EntityBehaviors
{
    public interface ITenantScopable<TTenantIdentifierType> where TTenantIdentifierType : struct
    {
        TTenantIdentifierType TenantId { get; set; }
    }
}