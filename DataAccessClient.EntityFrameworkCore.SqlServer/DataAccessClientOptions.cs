using System;
using Microsoft.EntityFrameworkCore;

namespace DataAccessClient.EntityFrameworkCore.SqlServer
{
    public class DataAccessClientOptions
    {
        public Action<DbContextOptionsBuilder> DbContextOptionsBuilder { get; set; }
        public Type UserIdentifierType { get; set; }
        public Type TenantIdentifierType { get; set; }
        public Type[] EntityTypes { get; set; }
        public bool UsePooling { get; set; }
        public int? PoolSize { get; set; }
    }
}