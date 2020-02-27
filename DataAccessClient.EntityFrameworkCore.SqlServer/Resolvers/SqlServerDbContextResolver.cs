using System;
using System.Reflection;
using DataAccessClient.Configuration;
using DataAccessClient.Providers;
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

                    var userIdentifierProviderType = typeof(IUserIdentifierProvider<>).MakeGenericType(dbContext.DataAccessClientOptionsExtension.UserIdentifierType);
                    var tenantIdentifierProviderType = typeof(ITenantIdentifierProvider<>).MakeGenericType(dbContext.DataAccessClientOptionsExtension.TenantIdentifierType);

                    var executeUserIdentifierProviderTypeMethod = userIdentifierProviderType.GetMethod(nameof(IUserIdentifierProvider<int>.Execute), BindingFlags.Instance | BindingFlags.Public);
                    var executeTenantIdentifierProviderTypeMethod = tenantIdentifierProviderType.GetMethod(nameof(ITenantIdentifierProvider<int>.Execute), BindingFlags.Instance | BindingFlags.Public);

                    var userIdentifierProvider = _scopedServiceProvider.GetRequiredService(userIdentifierProviderType);
                    var tenantIdentifierProvider = _scopedServiceProvider.GetRequiredService(tenantIdentifierProviderType);
                    var softDeletableConfiguration = _scopedServiceProvider.GetRequiredService<ISoftDeletableConfiguration>();
                    var multiTenancyConfiguration = _scopedServiceProvider.GetRequiredService<IMultiTenancyConfiguration>();

                    dbContext.Initialize(
                        () => executeUserIdentifierProviderTypeMethod.Invoke(userIdentifierProvider, new object[0]),
                        () => executeTenantIdentifierProviderTypeMethod.Invoke(tenantIdentifierProvider, new object[0]),
                        softDeletableConfiguration,
                        multiTenancyConfiguration);

                    _resolvedDbContext = dbContext;
                }
            }

            return _resolvedDbContext;
        }
    }
}