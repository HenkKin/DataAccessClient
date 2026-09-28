using System;
using Microsoft.Extensions.DependencyInjection;

namespace DataAccessClient.EntityFrameworkCore.Relational.Resolvers
{
    internal class RelationalDbContextResolver<TDbContext> : IRelationalDbContextResolver<TDbContext>
        where TDbContext : RelationalDbContext
    {
        private readonly IServiceProvider _scopedServiceProvider;

        public RelationalDbContextResolver(IServiceProvider scopedServiceProvider)
        {
            _scopedServiceProvider = scopedServiceProvider;
        }

        public TDbContext Execute()
        {
            return _scopedServiceProvider.GetRequiredService<TDbContext>();
        }
    }
}