namespace DataAccessClient.EntityFrameworkCore.SqlServer
{
    public interface ITenantIdentifierProvider<TIdentifierType>
        where TIdentifierType : struct
    {
        TIdentifierType? Execute();
    }
}
