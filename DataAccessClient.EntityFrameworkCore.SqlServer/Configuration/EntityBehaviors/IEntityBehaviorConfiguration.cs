﻿using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DataAccessClient.EntityFrameworkCore.SqlServer.Configuration.EntityBehaviors
{
    public interface IEntityBehaviorConfiguration
    {
        void OnRegistering(IServiceCollection serviceCollection);
        Dictionary<string, dynamic> OnExecutionContextCreating(IServiceProvider scopedServiceProvider);
        void OnModelCreating(ModelBuilder modelBuilder, SqlServerDbContext serverDbContext, Type entityType);

        void OnBeforeSaveChanges(SqlServerDbContext serverDbContext, DateTime onSaveChangesTime);
        void OnAfterSaveChanges(SqlServerDbContext serverDbContext);
    }
}