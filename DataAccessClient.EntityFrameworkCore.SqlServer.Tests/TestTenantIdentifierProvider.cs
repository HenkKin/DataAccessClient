
using System;

namespace DataAccessClient.EntityFrameworkCore.SqlServer.Tests
{
    public class TestTenantIdentifierProvider : ITenantIdentifierProvider<int>
    {
        public Guid InstanceId { get; }
        public TestTenantIdentifierProvider()
        {
            InstanceId = Guid.NewGuid();
        }

        public int? Execute()
        {
            return 1;
        }
    }
}