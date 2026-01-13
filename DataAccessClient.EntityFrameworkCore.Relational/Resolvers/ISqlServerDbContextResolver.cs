namespace DataAccessClient.EntityFrameworkCore.Relational.Resolvers
{
    public interface IRelationalDbContextResolver<TDbContext>
        where TDbContext : RelationalDbContext
    {
        TDbContext Execute();
    }
}