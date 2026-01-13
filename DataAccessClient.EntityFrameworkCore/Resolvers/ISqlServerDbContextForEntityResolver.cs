namespace DataAccessClient.EntityFrameworkCore.Relational.Resolvers
{
    public interface IRelationalDbContextForEntityResolver
    {
        RelationalDbContext Execute<TEntity>() where TEntity : class;
    }
}