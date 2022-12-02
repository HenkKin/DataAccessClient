using System;
using System.Collections.Generic;
using DataAccessClient.EntityFrameworkCore.SqlServer.Configuration.EntityBehaviors;
using Microsoft.EntityFrameworkCore;

namespace DataAccessClient.EntityFrameworkCore.SqlServer
{
    public class DataAccessClientOptions
    {
        public Action<DbContextOptionsBuilder> DbContextOptionsBuilder { get; internal set; }
        public bool DisableUtcDateTimePropertyEntityBehavior { get; internal set; }
        public bool UsePooling { get; internal set; }
        public int? PoolSize { get; internal set; }
        public IList<IEntityBehaviorConfiguration> CustomEntityBehaviors { get; internal set; } = new List<IEntityBehaviorConfiguration>();
    }
}