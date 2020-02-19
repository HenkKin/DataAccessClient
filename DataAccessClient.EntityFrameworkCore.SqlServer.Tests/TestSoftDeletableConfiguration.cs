namespace DataAccessClient.EntityFrameworkCore.SqlServer.Tests
{
    public class TestSoftDeletableConfiguration : ISoftDeletableConfiguration
    {
        public bool IsEnabled { get; private set; } = true;

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