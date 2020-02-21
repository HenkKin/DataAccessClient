using DataAccessClient.Configuration;
using DataAccessClient.Utilities;

namespace DataAccessClient.EntityFrameworkCore.SqlServer
{
    internal class DefaultMultiTenancyConfiguration : IMultiTenancyConfiguration
    {
        public bool IsQueryFilterEnabled { get; private set; } = true;

        public RestoreAction EnableQueryFilter()
        {
            var originalIsQueryFilterEnabled = IsQueryFilterEnabled;
            IsQueryFilterEnabled = true;
            return new RestoreAction(() => IsQueryFilterEnabled = originalIsQueryFilterEnabled);
        }

        public RestoreAction DisableQueryFilter()
        {
            var originalIsQueryFilterEnabled = IsQueryFilterEnabled;
            IsQueryFilterEnabled = false;
            return new RestoreAction(() => IsQueryFilterEnabled = originalIsQueryFilterEnabled);
        }
    }
}