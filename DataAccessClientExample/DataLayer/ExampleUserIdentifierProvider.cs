using DataAccessClient.Providers;

namespace DataAccessClientExample.DataLayer
{
    internal class ExampleUserIdentifierProvider : IUserIdentifierProvider<int>
    {
        public int? Execute()
        {
            return 10;
        }
    }
}
