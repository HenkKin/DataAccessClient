﻿using DataAccessClient.EntityFrameworkCore.SqlServer;

namespace DataAccessClientExample.DataLayer
{
    internal class ExampleTenantIdentifierProvider : ITenantIdentifierProvider<int>
    {
        public int? Execute()
        {
            return 1;
        }
    }
}