using System;
using Microsoft.EntityFrameworkCore;

namespace DataAccessClient.EntityFrameworkCore.SqlServer.Configuration.EntityBehaviors
{
    public interface IEntityBehaviorConfiguration
    {
        void Execute(ModelBuilder modelBuilder, SqlServerDbContext serverDbContext, Type entityType);
    }
}