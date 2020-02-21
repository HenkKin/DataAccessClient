
using System;
using DataAccessClient.Providers;

namespace DataAccessClient.EntityFrameworkCore.SqlServer.Tests.TestModels
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