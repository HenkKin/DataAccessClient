using System;
using System.Threading.Tasks;
using DataAccessClient.EntityFrameworkCore.Relational.Resolvers;

namespace DataAccessClient.EntityFrameworkCore.Relational
{
    internal class UnitOfWorkPartForRelationalDbContext<TDbContext> : IUnitOfWorkPart
        where TDbContext : RelationalDbContext
    {
        private readonly TDbContext _dbContext;

        public UnitOfWorkPartForRelationalDbContext(IRelationalDbContextResolver<TDbContext> relationalDbContextResolver)
        {
            _dbContext = relationalDbContextResolver.Execute() ?? throw new ArgumentNullException(nameof(relationalDbContextResolver));
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