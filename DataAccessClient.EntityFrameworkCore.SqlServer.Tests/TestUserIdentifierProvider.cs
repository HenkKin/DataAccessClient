using System;

namespace DataAccessClient.EntityFrameworkCore.SqlServer.Tests
{
    public class TestUserIdentifierProvider : IUserIdentifierProvider<int>
    {
        public Guid InstanceId { get; }
        public TestUserIdentifierProvider()
        {
            InstanceId = Guid.NewGuid();
        }

        public int? Execute()
        {
            return 1;
        }
    }
}