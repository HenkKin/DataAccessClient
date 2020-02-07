using System;
using System.Threading.Tasks;

namespace DataAccessClient.EntityFrameworkCore.SqlServer
{
    internal class UnitOfWorkPartForSqlServerDbContext<TDbContext, TIdentifierType> : IUnitOfWorkPart
        where TDbContext : SqlServerDbContext<TIdentifierType>
        where TIdentifierType : struct
    {
        private readonly TDbContext _dbContext;

        public UnitOfWorkPartForSqlServerDbContext(TDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
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