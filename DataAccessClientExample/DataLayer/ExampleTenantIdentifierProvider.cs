using DataAccessClient.Providers;

namespace DataAccessClientExample.DataLayer
{
    internal class ExampleTenantIdentifierProvider : ITenantIdentifierProvider<int>
    {
        public int? Execute()
        {
            return 1;
        }
    }
}