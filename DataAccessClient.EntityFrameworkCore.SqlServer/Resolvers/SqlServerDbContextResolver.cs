using System;
using DataAccessClient.Configuration;
using DataAccessClient.Providers;
using Microsoft.Extensions.DependencyInjection;

namespace DataAccessClient.EntityFrameworkCore.SqlServer.Resolvers
{
    internal class SqlServerDbContextResolver<TDbContext, TUserIdentifierType, TTenantIdentifierType> : ISqlServerDbContextResolver<TDbContext, TUserIdentifierType, TTenantIdentifierType>
        where TDbContext : SqlServerDbContext<TUserIdentifierType, TTenantIdentifierType>
        where TUserIdentifierType : struct
        where TTenantIdentifierType : struct
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

                    var userIdentifierProvider = _scopedServiceProvider.GetRequiredService<IUserIdentifierProvider<TUserIdentifierType>>();
                    var tenantIdentifierProvider = _scopedServiceProvider.GetRequiredService<ITenantIdentifierProvider<TTenantIdentifierType>>();
                    var softDeletableConfiguration = _scopedServiceProvider.GetRequiredService<ISoftDeletableConfiguration>();
                    var multiTenancyConfiguration = _scopedServiceProvider.GetRequiredService<IMultiTenancyConfiguration>();

                    dbContext.Initialize(userIdentifierProvider, tenantIdentifierProvider, softDeletableConfiguration, multiTenancyConfiguration);

                    _resolvedDbContext = dbContext;
                }
            }

            return _resolvedDbContext;
        }
    }
}