﻿using DataAccessClient.Providers;

namespace DataAccessClientExample.DataLayer
{
    internal class ExampleTenantIdentifierProvider : ITenantIdentifierProvider<int>
    {
        public int? TenantId { get; private set; } = 1;
        public int? Execute()
        {
            return TenantId;
        }

        public void ChangeTentantIdentifier(int? tenantIdentifier)
        {
            TenantId = tenantIdentifier;
        }
    }
}