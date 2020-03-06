using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace DataAccessClient.EntityFrameworkCore.SqlServer.Resolvers
{
    internal class SqlServerDbContextResolver<TDbContext> : ISqlServerDbContextResolver<TDbContext>
        where TDbContext : SqlServerDbContext
    {
        private readonly IServiceProvider _scopedServiceProvider;
        private TDbContext _resolvedDbContext;
        private readonly object _lock = new object();

        public SqlServerDbContextResolver(IServiceProvider scopedServiceProvider)
        {
            _scopedServiceProvider = scopedServiceProvider;
        }

        public TDbContext Execute()
        {
            if (_resolvedDbContext == null)
            {
                lock (_lock)
                {
                    if (_resolvedDbContext != null)
                    {
                        return _resolvedDbContext;
                    }

                    var dbContext = _scopedServiceProvider.GetRequiredService<TDbContext>();

                    var context = new Dictionary<string, dynamic>();
                   
                    foreach (var entityBehaviorConfiguration in dbContext.DataAccessClientOptionsExtension.EntityBehaviors)
                    {
                        foreach (var entityBehaviorContext in entityBehaviorConfiguration.OnExecutionContextCreating(_scopedServiceProvider))
                        {
                            context.TryAdd(entityBehaviorContext.Key, entityBehaviorContext.Value);
                        }
                    }

                    var executionContext = new SqlServerDbContextExecutionContext(context);

                    dbContext.Initialize(executionContext);

                    _resolvedDbContext = dbContext;
                }
            }

            return _resolvedDbContext;
        }
    }
}