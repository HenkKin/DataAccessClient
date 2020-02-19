namespace DataAccessClient
{
    public interface ITenantIdentifierProvider<out TIdentifierType> where TIdentifierType : struct
    {
        TIdentifierType Execute();
    }
}
