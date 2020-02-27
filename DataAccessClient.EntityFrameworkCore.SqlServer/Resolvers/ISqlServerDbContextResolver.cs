namespace DataAccessClient.EntityFrameworkCore.SqlServer.Resolvers
{
    internal interface ISqlServerDbContextResolver<TDbContext>
        where TDbContext : SqlServerDbContext
    {
        TDbContext Execute();
    }
}