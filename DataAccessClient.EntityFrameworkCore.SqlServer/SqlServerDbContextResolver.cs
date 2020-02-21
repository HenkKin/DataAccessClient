using System;
using Microsoft.Extensions.DependencyInjection;

namespace DataAccessClient.EntityFrameworkCore.SqlServer
{
    internal class SqlServerDbContextResolver<TDbContext, TUserIdentifierType, TTenantIdentifierType> : ISqlServerDbContextResolver<TDbContext, TUserIdentifierType, TTenantIdentifierType>
        where TDbContext : SqlServerDbContext<TUserIdentifierType, TTenantIdentifierType>
        where TUserIdentifierType : struct
        where TTenantIdentifierType : struct
    {
        private TDbContext _resolvedDbContext;
        private readonly object _lock = new object();

        public TDbContext Execute(IServiceProvider scopedServiceProvider)
        {
            if (_resolvedDbContext == null)
            {
                lock (_lock)
                {
                    if (_resolvedDbContext != null)
                    {
                        return _resolvedDbContext;
                    }

                    var dbContext = scopedServiceProvider.GetRequiredService<TDbContext>();

                    var userIdentifierProvider =
                        scopedServiceProvider.GetRequiredService<IUserIdentifierProvider<TUserIdentifierType>>();
                    var tenantIdentifierProvider = scopedServiceProvider
                        .GetRequiredService<ITenantIdentifierProvider<TTenantIdentifierType>>();
                    var softDeletableConfiguration =
                        scopedServiceProvider.GetRequiredService<ISoftDeletableConfiguration>();
                    var multiTenancyConfiguration = scopedServiceProvider
                        .GetRequiredService<IMultiTenancyConfiguration<TTenantIdentifierType>>();

                    dbContext.Initialize(userIdentifierProvider, tenantIdentifierProvider, softDeletableConfiguration, multiTenancyConfiguration);

                    _resolvedDbContext = dbContext;
                }
            }

            return _resolvedDbContext;
        }
    }
}