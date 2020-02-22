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

        public int? TenantId { get; private set; } = 1;

        public int? Execute()
        {
            return TenantId;
        }

        public void ChangeUserIdentifier(int? tenantIdentifier)
        {
            TenantId = tenantIdentifier;
        }
    }
}