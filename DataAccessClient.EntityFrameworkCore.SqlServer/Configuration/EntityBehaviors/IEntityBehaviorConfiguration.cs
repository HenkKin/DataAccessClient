using System;
using Microsoft.EntityFrameworkCore;

namespace DataAccessClient.EntityFrameworkCore.SqlServer.Configuration.EntityBehaviors
{
    public interface IEntityBehaviorConfiguration
    {
        void OnModelCreating(ModelBuilder modelBuilder, SqlServerDbContext serverDbContext, Type entityType);

        void OnBeforeSaveChanges(SqlServerDbContext serverDbContext, DateTime onSaveChangesTime);
        void OnAfterSaveChanges(SqlServerDbContext serverDbContext);
    }
}