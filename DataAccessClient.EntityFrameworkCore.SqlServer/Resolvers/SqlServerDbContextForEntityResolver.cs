using System;
using System.Collections.Concurrent;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace DataAccessClient.EntityFrameworkCore.SqlServer.Resolvers
{
    internal class SqlServerDbContextForEntityResolver : ISqlServerDbContextForEntityResolver
    {
        private readonly IServiceProvider _scopedServiceProvider;
        private ConcurrentDictionary<Type, SqlServerDbContext> _resolvedDbContexts = new ConcurrentDictionary<Type, SqlServerDbContext>();
        private readonly object _lock = new object();

        public SqlServerDbContextForEntityResolver(IServiceProvider scopedServiceProvider)
        {
            _scopedServiceProvider = scopedServiceProvider;
        }

        public SqlServerDbContext Execute<TEntity>() where TEntity : class
        {
            if (!_resolvedDbContexts.ContainsKey(typeof(TEntity)))
            {
                lock (_lock)
                {
                    if (_resolvedDbContexts.ContainsKey(typeof(TEntity)))
                    {
                        return _resolvedDbContexts[typeof(TEntity)];
                    }

                    Type dbContextType =
                        SqlServerDbContext.RegisteredEntityTypesPerDbContexts.Where(c =>
                            c.Value.Any(entityType => entityType == typeof(TEntity))).Select(x => x.Key).SingleOrDefault();

                    SqlServerDbContext dbContext = null;
                    if (dbContextType != null)
                    {
                        dbContext = ResolveDbContextInstance<TEntity>(_scopedServiceProvider, dbContextType);
                    }

                    if (dbContext == null)
                    {
                        foreach (var registeredDbContextType in SqlServerDbContext.RegisteredDbContextTypes)
                        {
                            dbContext = ResolveDbContextInstance<TEntity>(_scopedServiceProvider, registeredDbContextType);

                            if (dbContext != null)
                            {
                                break;
                            }
                        }
                    }

                    _resolvedDbContexts.TryAdd(typeof(TEntity), dbContext ?? throw new InvalidOperationException($"Can not find IRepository instance for type {typeof(TEntity).FullName}"));
                    return dbContext;
                }
            }

            return _resolvedDbContexts[typeof(TEntity)];
        }

        private SqlServerDbContext ResolveDbContextInstance<TEntity>(IServiceProvider scopedServiceProvider, Type dbContextType) where TEntity : class
        {
            var dbContextResolverType = typeof(ISqlServerDbContextResolver<>).MakeGenericType(dbContextType);
            var executeMethod =
                dbContextResolverType.GetMethod(nameof(ISqlServerDbContextResolver<SqlServerDbContext>.Execute));
            var dbContext =
                executeMethod?.Invoke(scopedServiceProvider.GetService(dbContextResolverType), new object[0]) as
                    SqlServerDbContext ??
                throw new ArgumentNullException(nameof(SqlServerDbContext));
            if (dbContext.Model.FindEntityType(typeof(TEntity)) != null)
            {
                return dbContext;
            }

            return null;
        }
    }
}