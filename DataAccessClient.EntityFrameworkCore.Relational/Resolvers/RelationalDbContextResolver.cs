using System;
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
                    
                    _resolvedDbContext = _scopedServiceProvider.GetRequiredService<TDbContext>();
                }
            }

            return _resolvedDbContext;
        }
    }
}