namespace DataAccessClient.EntityFrameworkCore.SqlServer
{
    public interface IMultiTenancyConfiguration<TTenantIdentifierType>
        where TTenantIdentifierType : struct
    {
        bool IsQueryFilterEnabled { get; }
        RestoreAction EnableQueryFilter();
        RestoreAction DisableQueryFilter();
    }
}