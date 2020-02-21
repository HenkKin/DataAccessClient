namespace DataAccessClient.EntityFrameworkCore.SqlServer
{
    public interface IUserIdentifierProvider<TIdentifierType>
        where TIdentifierType : struct
    {
        TIdentifierType? Execute();
    }
}
