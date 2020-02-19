namespace DataAccessClient.EntityFrameworkCore.SqlServer.Tests
{
    public class TestMultiTenancyConfiguration : IMultiTenancyConfiguration<int>
    {
        private readonly ITenantIdentifierProvider<int> _tenantIdentifierProvider;
        public bool IsEnabled { get; private set; } = true;
        public int? CurrentTenantId => _tenantIdentifierProvider.Execute();
        
        public TestMultiTenancyConfiguration(ITenantIdentifierProvider<int> tenantIdentifierProvider)
        {
            _tenantIdentifierProvider = tenantIdentifierProvider;
        }

        public RestoreAction Enable()
        {
            var originalIsEnabled = IsEnabled;
            IsEnabled = true;
            return new RestoreAction(() => IsEnabled = originalIsEnabled);
        }

        public RestoreAction Disable()
        {
            var originalIsEnabled = IsEnabled;
            IsEnabled = false;
            return new RestoreAction(() => IsEnabled = originalIsEnabled);
        }
    }
}