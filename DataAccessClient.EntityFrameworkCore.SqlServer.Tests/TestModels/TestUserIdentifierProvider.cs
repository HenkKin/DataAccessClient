using System;
using DataAccessClient.Providers;

namespace DataAccessClient.EntityFrameworkCore.SqlServer.Tests.TestModels
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