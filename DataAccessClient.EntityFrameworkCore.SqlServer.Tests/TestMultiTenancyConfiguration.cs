namespace DataAccessClient.EntityFrameworkCore.SqlServer.Tests
{
    public class TestMultiTenancyConfiguration : IMultiTenancyConfiguration<int>
    {
        private readonly ITenantIdentifierProvider<int> _tenantIdentifierProvider;
        public bool IsQueryFilterEnabled { get; private set; } = true;
        public int? CurrentTenantId => _tenantIdentifierProvider.Execute();
        
        public TestMultiTenancyConfiguration(ITenantIdentifierProvider<int> tenantIdentifierProvider)
        {
            _tenantIdentifierProvider = tenantIdentifierProvider;
        }

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