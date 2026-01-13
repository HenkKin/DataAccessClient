using System;
using DataAccessClient.Providers;

namespace DataAccessClient.EntityFrameworkCore.Relational.Tests.TestModels
{
    public class TestTenantIdentifierProvider : ITenantIdentifierProvider<int>
    {
        public Guid InstanceId { get; }
        public TestTenantIdentifierProvider()
        {
            InstanceId = Guid.NewGuid();
        }

        public int? TenantId { get; private set; } = 1;

        public int? Execute()
        {
            return TenantId;
        }

        public void ChangeTenantIdentifier(int? tenantIdentifier)
        {
            TenantId = tenantIdentifier;
        }
    }
}
