
namespace DataAccessClient.EntityFrameworkCore.SqlServer.Tests
{
    public class TestTenantIdentifierProvider : ITenantIdentifierProvider<int>
    {
        public int Execute()
        {
            return 1;
        }
    }
}