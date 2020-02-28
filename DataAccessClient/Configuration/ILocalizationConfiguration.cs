using DataAccessClient.Utilities;

namespace DataAccessClient.Configuration
{
    public interface ILocalizationConfiguration
    {
        bool IsQueryFilterEnabled { get; }
        RestoreAction EnableQueryFilter();
        RestoreAction DisableQueryFilter();
    }
}