using System;
using System.Collections.Generic;
using DataAccessClient.EntityFrameworkCore.Relational;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DataAccessClient.EntityFrameworkCore.Relational.Configuration.EntityBehaviors
{
    public interface IEntityBehaviorConfiguration
    {
        void OnRegistering(IServiceCollection serviceCollection);
        Dictionary<string, dynamic> OnExecutionContextCreating(IServiceProvider scopedServiceProvider);
        void OnModelCreating(ModelBuilder modelBuilder, RelationalDbContext relationalServerDbContext, Type entityType);

        void OnBeforeSaveChanges(RelationalDbContext relationalServerDbContext, DateTime onSaveChangesTime);
        void OnAfterSaveChanges(RelationalDbContext relationalServerDbContext);
    }
}