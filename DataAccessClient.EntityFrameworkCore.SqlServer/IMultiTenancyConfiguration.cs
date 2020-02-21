namespace DataAccessClient.EntityFrameworkCore.SqlServer
{
    public interface IMultiTenancyConfiguration
    {
        bool IsQueryFilterEnabled { get; }
        RestoreAction EnableQueryFilter();
        RestoreAction DisableQueryFilter();
    }
}