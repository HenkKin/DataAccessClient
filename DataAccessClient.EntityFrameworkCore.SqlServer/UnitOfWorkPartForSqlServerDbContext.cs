using System;
using System.Threading.Tasks;
using DataAccessClient.EntityFrameworkCore.SqlServer.Resolvers;

namespace DataAccessClient.EntityFrameworkCore.SqlServer
{
    internal class UnitOfWorkPartForSqlServerDbContext<TDbContext> : IUnitOfWorkPart
        where TDbContext : SqlServerDbContext
    {
        private readonly TDbContext _dbContext;

        public UnitOfWorkPartForSqlServerDbContext(ISqlServerDbContextResolver<TDbContext> sqlServerDbContextResolver)
        {
            _dbContext = sqlServerDbContextResolver.Execute() ?? throw new ArgumentNullException(nameof(sqlServerDbContextResolver));
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