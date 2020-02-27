using System;
using Microsoft.EntityFrameworkCore;

namespace DataAccessClient.EntityFrameworkCore.SqlServer
{
    public class DataAccessClientOptions
    {
        public Action<DbContextOptionsBuilder> DbContextOptionsBuilder { get; internal set; }
        public Type[] EntityTypes { get; internal set; }
        public bool UsePooling { get; internal set; }
        public int? PoolSize { get; internal set; }
    }
}