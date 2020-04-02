namespace DataAccessClient.EntityFrameworkCore.SqlServer.Resolvers
{
    public interface ISqlServerDbContextForEntityResolver
    {
        SqlServerDbContext Execute<TEntity>() where TEntity : class;
    }
}