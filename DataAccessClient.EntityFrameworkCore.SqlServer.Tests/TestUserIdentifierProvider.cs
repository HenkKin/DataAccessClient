using System.Threading.Tasks;

namespace DataAccessClient.EntityFrameworkCore.SqlServer.Tests
{
    public class TestUserIdentifierProvider : IUserIdentifierProvider<int>
    {
        public Task<int> ExecuteAsync()
        {
            return Task.FromResult(1);
        }
    }
}