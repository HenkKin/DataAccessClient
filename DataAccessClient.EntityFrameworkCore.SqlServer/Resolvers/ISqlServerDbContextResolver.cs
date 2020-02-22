namespace DataAccessClient.EntityFrameworkCore.SqlServer.Resolvers
{
    internal interface ISqlServerDbContextResolver<TDbContext, TUserIdentifierType, TTenantIdentifierType>
        where TDbContext : SqlServerDbContext<TUserIdentifierType, TTenantIdentifierType>
        where TUserIdentifierType : struct
        where TTenantIdentifierType : struct
    {
        TDbContext Execute();
    }
}