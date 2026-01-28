using System;
using System.Linq;

namespace DataAccessClient.EntityFrameworkCore.SqlServer.Resolvers
{
    internal class SqlServerDbContextForEntityResolver : ISqlServerDbContextForEntityResolver
    {
        private readonly IServiceProvider _scopedServiceProvider;

        public SqlServerDbContextForEntityResolver(IServiceProvider scopedServiceProvider)
        {
            _scopedServiceProvider = scopedServiceProvider;
        }

        public SqlServerDbContext Execute<TEntity>() where TEntity : class
        {
            return Execute(typeof(TEntity));
        }

        public SqlServerDbContext Execute(Type entityType)
        {
            Type dbContextType =
                SqlServerDbContext.RegisteredEntityTypesPerDbContexts.Where(c =>
                    c.Value.Any(et => et == entityType)).Select(x => x.Key).SingleOrDefault();

            SqlServerDbContext dbContext = null;
            if (dbContextType != null)
            {
                dbContext = ResolveDbContextInstance(_scopedServiceProvider, entityType, dbContextType);
            }

            if (dbContext == null)
            {
                foreach (var registeredDbContextType in SqlServerDbContext.RegisteredDbContextTypes)
                {
                    dbContext = ResolveDbContextInstance(_scopedServiceProvider, entityType, registeredDbContextType);

                    if (dbContext != null)
                    {
                        break;
                    }
                }
            }

            return dbContext;
        }

        private SqlServerDbContext ResolveDbContextInstance(IServiceProvider scopedServiceProvider, Type entityType, Type dbContextType)
        {
            var dbContextResolverType = typeof(ISqlServerDbContextResolver<>).MakeGenericType(dbContextType);
            var executeMethod =
                dbContextResolverType.GetMethod(nameof(ISqlServerDbContextResolver<SqlServerDbContext>.Execute));
            var dbContext =
                executeMethod?.Invoke(scopedServiceProvider.GetService(dbContextResolverType), new object[0]) as
                    SqlServerDbContext ??
                throw new ArgumentNullException(nameof(SqlServerDbContext));
            if (dbContext.Model.FindEntityType(entityType) != null)
            {
                return dbContext;
            }

            return null;
        }
    }
}