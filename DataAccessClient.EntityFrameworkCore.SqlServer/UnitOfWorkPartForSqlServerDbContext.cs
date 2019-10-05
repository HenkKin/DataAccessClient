using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

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
            var entries = _dbContext.ChangeTracker.Entries().Where(e => e.State != EntityState.Unchanged).ToArray();
            foreach (var entry in entries)
            {
                switch (entry.State)
                {
                    case EntityState.Modified:
                        entry.State = EntityState.Unchanged;
                        break;
                    case EntityState.Added:
                        entry.State = EntityState.Detached;
                        break;
                    case EntityState.Deleted:
                        entry.Reload();
                        break;
                }
            }
        }
    }
}