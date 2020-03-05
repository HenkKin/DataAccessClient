using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace DataAccessClient.EntityFrameworkCore.SqlServer
{
    public class DataAccessClientOptions
    {
        public Action<DbContextOptionsBuilder> DbContextOptionsBuilder { get; internal set; }
        public Type[] EntityTypes { get; internal set; }
        public bool UsePooling { get; internal set; }
        public int? PoolSize { get; internal set; }
        public IList<Type> CustomEntityBehaviorTypes { get; internal set; } = new List<Type>();
    }
}