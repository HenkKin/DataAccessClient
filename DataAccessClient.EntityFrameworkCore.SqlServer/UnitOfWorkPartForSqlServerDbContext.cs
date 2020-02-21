using System;
using System.Threading.Tasks;

namespace DataAccessClient.EntityFrameworkCore.SqlServer
{
    internal class UnitOfWorkPartForSqlServerDbContext<TDbContext, TUserIdentifierType, TTenantIdentifierType> : IUnitOfWorkPart
        where TDbContext : SqlServerDbContext<TUserIdentifierType, TTenantIdentifierType>
        where TUserIdentifierType : struct
        where TTenantIdentifierType : struct
    {
        internal readonly TDbContext DbContext;

        public UnitOfWorkPartForSqlServerDbContext(ISqlServerDbContextResolver<TDbContext, TUserIdentifierType, TTenantIdentifierType> sqlServerDbContextResolver, IServiceProvider scopedServiceProvider)
        {
            DbContext = sqlServerDbContextResolver.Execute(scopedServiceProvider) ?? throw new ArgumentNullException(nameof(sqlServerDbContextResolver));
        }

        public async Task SaveAsync()
        {
            await DbContext.SaveChangesAsync();
        }

        public void Reset()
        {
            DbContext.Reset();
        }
    }
}