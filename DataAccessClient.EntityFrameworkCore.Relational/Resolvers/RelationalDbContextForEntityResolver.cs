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
            return Execute(typeof(TEntity));
        }

        public RelationalDbContext Execute(Type entityType)
        {
            Type dbContextType =
                RelationalDbContext.RegisteredEntityTypesPerDbContexts.Where(c =>
                    c.Value.Any(et => et == entityType)).Select(x => x.Key).SingleOrDefault();

            RelationalDbContext dbContext = null;
            if (dbContextType != null)
            {
                dbContext = ResolveDbContextInstance(_scopedServiceProvider, entityType, dbContextType);
            }

            if (dbContext == null)
            {
                foreach (var registeredDbContextType in RelationalDbContext.RegisteredDbContextTypes)
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

        //private RelationalDbContext ResolveDbContextInstance<TEntity>(IServiceProvider scopedServiceProvider, Type dbContextType) where TEntity : class
        //{
        //    return ResolveDbContextInstance(scopedServiceProvider, typeof(TEntity), dbContextType);
        //}

        private RelationalDbContext ResolveDbContextInstance(IServiceProvider scopedServiceProvider, Type entityType, Type dbContextType)
        {
            var dbContextResolverType = typeof(IRelationalDbContextResolver<>).MakeGenericType(dbContextType);
            var executeMethod =
                dbContextResolverType.GetMethod(nameof(IRelationalDbContextResolver<RelationalDbContext>.Execute));
            var dbContext =
                executeMethod?.Invoke(scopedServiceProvider.GetService(dbContextResolverType), new object[0]) as
                    RelationalDbContext ??
                throw new ArgumentNullException(nameof(RelationalDbContext));
            if (dbContext.Model.FindEntityType(entityType) != null)
            {
                return dbContext;
            }

            return null;
        }
    }
}