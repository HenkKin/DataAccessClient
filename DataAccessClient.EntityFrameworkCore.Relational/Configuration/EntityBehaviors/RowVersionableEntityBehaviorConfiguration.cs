using DataAccessClient.EntityBehaviors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DataAccessClient.EntityFrameworkCore.Relational.Configuration.EntityBehaviors
{
    internal static class RowVersionableEntityBehaviorConfigurationExtensions
    {
        internal static readonly MethodInfo ModelBuilderConfigureEntityBehaviorIRowVersionable;

        static RowVersionableEntityBehaviorConfigurationExtensions()
        {
            ModelBuilderConfigureEntityBehaviorIRowVersionable = typeof(RowVersionableEntityBehaviorConfigurationExtensions).GetTypeInfo()
                .DeclaredMethods
                .Single(m => m.Name == nameof(ConfigureEntityBehaviorIRowVersionable));
        }

        internal static ModelBuilder ConfigureEntityBehaviorIRowVersionable<TEntity, TRowVersionableType>(ModelBuilder modelBuilder)
            where TEntity : class, IRowVersionable<TRowVersionableType>
        {
            modelBuilder.Entity<TEntity>()
                .IsRowVersionable<TEntity, TRowVersionableType>();

            return modelBuilder;
        }

        internal static EntityTypeBuilder<TEntity> IsRowVersionable<TEntity, TRowVersionableType>(this EntityTypeBuilder<TEntity> entity)
            where TEntity : class, IRowVersionable<TRowVersionableType>
        {
            entity.Property(e => e.RowVersion).IsRowVersion();
            return entity;
        }
    }

    public class RowVersionableEntityBehaviorConfiguration : IEntityBehaviorConfiguration
    {
        public void OnRegistering(IServiceCollection serviceCollection)
        {
        }

        public Dictionary<string, dynamic> OnExecutionContextCreating(IServiceProvider scopedServiceProvider)
        {
            var context = new Dictionary<string, dynamic>();

            return context;
        }

        public void OnModelCreating(ModelBuilder modelBuilder, RelationalDbContext relationalDbContext, Type entityType)
        {
            var entityInterfaces = entityType.GetInterfaces();

            if (entityInterfaces.Any(
               x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IRowVersionable<>)))
            {
                var rowVersionableType = entityType.GetInterface(typeof(IRowVersionable<>).Name)
                    .GenericTypeArguments[0];
                RowVersionableEntityBehaviorConfigurationExtensions.ModelBuilderConfigureEntityBehaviorIRowVersionable.MakeGenericMethod(entityType, rowVersionableType)
                    .Invoke(null, new object[] { modelBuilder });
            }
        }

        public void OnBeforeSaveChanges(RelationalDbContext relationalDbContext, DateTime onSaveChangesTime)
        {
            foreach (var entityEntry in relationalDbContext.ChangeTracker.Entries().Where(x => x.Entity.GetType().GetInterface(typeof(IRowVersionable<>).Name) != null))
            {
                // IRowVersionable<string> => I only need the name of the property
                var rowVersionProperty = entityEntry.Property(nameof(IRowVersionable<string>.RowVersion));
                var rowVersion = rowVersionProperty.CurrentValue;
                //https://github.com/aspnet/EntityFramework/issues/4512
                rowVersionProperty.OriginalValue = rowVersion;
            }
        }

        public void OnAfterSaveChanges(RelationalDbContext relationalDbContext)
        {
        }
    }
}