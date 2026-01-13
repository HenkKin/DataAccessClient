using System;
using System.Linq;

namespace DataAccessClient.EntityFrameworkCore.Relational.Resolvers
{
    internal class RelationalDbContextForEntityResolver : IRelationalDbContextForEntityResolver
    {
        private readonly IServiceProvider _scopedServiceProvider;

        public RelationalDbContextForEntityResolver(IServiceProvider scopedServiceProvider)
        {
            _scopedServiceProvider = scopedServiceProvider;
        }

        public RelationalDbContext Execute<TEntity>() where TEntity : class
        {
            Type dbContextType =
                RelationalDbContext.RegisteredEntityTypesPerDbContexts.Where(c =>
                    c.Value.Any(entityType => entityType == typeof(TEntity))).Select(x => x.Key).SingleOrDefault();

            RelationalDbContext dbContext = null;
            if (dbContextType != null)
            {
                dbContext = ResolveDbContextInstance<TEntity>(_scopedServiceProvider, dbContextType);
            }

            if (dbContext == null)
            {
                foreach (var registeredDbContextType in RelationalDbContext.RegisteredDbContextTypes)
                {
                    dbContext = ResolveDbContextInstance<TEntity>(_scopedServiceProvider, registeredDbContextType);

                    if (dbContext != null)
                    {
                        break;
                    }
                }
            }

            return dbContext;
        }

        private RelationalDbContext ResolveDbContextInstance<TEntity>(IServiceProvider scopedServiceProvider, Type dbContextType) where TEntity : class
        {
            var dbContextResolverType = typeof(IRelationalDbContextResolver<>).MakeGenericType(dbContextType);
            var executeMethod =
                dbContextResolverType.GetMethod(nameof(IRelationalDbContextResolver<RelationalDbContext>.Execute));
            var dbContext =
                executeMethod?.Invoke(scopedServiceProvider.GetService(dbContextResolverType), new object[0]) as
                    RelationalDbContext ??
                throw new ArgumentNullException(nameof(RelationalDbContext));
            if (dbContext.Model.FindEntityType(typeof(TEntity)) != null)
            {
                return dbContext;
            }

            return null;
        }
    }
}