namespace DataAccessClient.EntityFrameworkCore.SqlServer.Resolvers
{
    internal interface ISqlServerDbContextForEntityResolver
    {
        SqlServerDbContext Execute<TEntity>() where TEntity : class;
    }
}