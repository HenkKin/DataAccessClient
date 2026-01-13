using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace DataAccessClient.EntityFrameworkCore.Relational.Resolvers
{
    internal class RelationalDbContextResolver<TDbContext> : IRelationalDbContextResolver<TDbContext>
        where TDbContext : RelationalDbContext
    {
        private readonly IServiceProvider _scopedServiceProvider;
        private TDbContext _resolvedDbContext;
        private readonly object _lock = new object();

        public RelationalDbContextResolver(IServiceProvider scopedServiceProvider)
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

                    var executionContext = new RelationalDbContextExecutionContext(context);

                    dbContext.Initialize(executionContext);

                    _resolvedDbContext = dbContext;
                }
            }

            return _resolvedDbContext;
        }
    }
}