using System;

namespace DataAccessClient.EntityFrameworkCore.SqlServer
{
    internal interface ISqlServerDbContextResolver<TDbContext, TUserIdentifierType, TTenantIdentifierType>
        where TDbContext : SqlServerDbContext<TUserIdentifierType, TTenantIdentifierType>
        where TUserIdentifierType : struct
        where TTenantIdentifierType : struct
    {
        TDbContext Execute(IServiceProvider scopedServiceProvider);
    }
}