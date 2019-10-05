using System.Threading.Tasks;
using DataAccessClient;

namespace DataAccessClientExample.DataLayer
{
    internal class ExampleUserIdentifierProvider : IUserIdentifierProvider<int>
    {
        public async Task<int> ExecuteAsync()
        {
            return await Task.FromResult(10);
        }
    }
}
