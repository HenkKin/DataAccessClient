namespace DataAccessClient.Providers
{
    public interface ITenantIdentifierProvider<TIdentifierType>
        where TIdentifierType : struct
    {
        TIdentifierType? Execute();
    }
}
