namespace DataAccessClient.Providers
{
    public interface ITenantIdentifierProvider<TTenantIdentifierType>
        where TTenantIdentifierType : struct
    {
        TTenantIdentifierType? Execute();
    }
}
