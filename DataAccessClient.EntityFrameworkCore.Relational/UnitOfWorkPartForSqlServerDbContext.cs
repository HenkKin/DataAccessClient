using System;
using System.Threading.Tasks;
using DataAccessClient.EntityFrameworkCore.Relational.Resolvers;

namespace DataAccessClient.EntityFrameworkCore.Relational
{
    internal class UnitOfWorkPartForRelationalDbContext<TDbContext> : IUnitOfWorkPart
        where TDbContext : RelationalDbContext
    {
        private readonly TDbContext _dbContext;

        public UnitOfWorkPartForRelationalDbContext(IRelationalDbContextResolver<TDbContext> relationalServerDbContextResolver)
        {
            _dbContext = relationalServerDbContextResolver.Execute() ?? throw new ArgumentNullException(nameof(relationalServerDbContextResolver));
        }

        public async Task SaveAsync()
        {
            await _dbContext.SaveChangesAsync();
        }

        public void Reset()
        {
            _dbContext.Reset();
        }
    }
}