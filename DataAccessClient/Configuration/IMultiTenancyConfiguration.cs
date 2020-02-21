using DataAccessClient.Utilities;

namespace DataAccessClient.Configuration
{
    public interface IMultiTenancyConfiguration
    {
        bool IsQueryFilterEnabled { get; }
        RestoreAction EnableQueryFilter();
        RestoreAction DisableQueryFilter();
    }
}