namespace DataAccessClient.EntityFrameworkCore.SqlServer.Resolvers
{
    public interface ISqlServerDbContextResolver<TDbContext>
        where TDbContext : SqlServerDbContext
    {
        TDbContext Execute();
    }
}