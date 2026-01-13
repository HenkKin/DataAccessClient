using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DataAccessClient.EntityBehaviors;
using DataAccessClient.EntityFrameworkCore.Relational;
using DataAccessClient.Providers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.DependencyInjection;

namespace DataAccessClient.EntityFrameworkCore.Relational.Configuration.EntityBehaviors
{
    internal static class ModifiableEntityBehaviorConfigurationExtensions
    {
        internal static readonly MethodInfo ModelBuilderConfigureEntityBehaviorIModifiable;

        static ModifiableEntityBehaviorConfigurationExtensions()
        {
            ModelBuilderConfigureEntityBehaviorIModifiable = typeof(ModifiableEntityBehaviorConfigurationExtensions).GetTypeInfo()
                .DeclaredMethods
                .Single(m => m.Name == nameof(ConfigureEntityBehaviorIModifiable));
        }

        internal static ModelBuilder ConfigureEntityBehaviorIModifiable<TEntity, TIdentifierType>(
            ModelBuilder modelBuilder)
            where TEntity : class, IModifiable<TIdentifierType>
            where TIdentifierType : struct
        {
            modelBuilder.Entity<TEntity>()
                .IsModifiable<TEntity, TIdentifierType>();

            return modelBuilder;
        }

        internal static EntityTypeBuilder<TEntity> IsModifiable<TEntity, TIdentifierType>(
            this EntityTypeBuilder<TEntity> entity)
            where TEntity : class, IModifiable<TIdentifierType>
            where TIdentifierType : struct
        {
            entity.Property(e => e.ModifiedById).IsRequired(false);
            entity.Property(e => e.ModifiedOn).IsRequired(false);
            return entity;
        }
    }

    public class ModifiableEntityBehaviorConfiguration<TUserIdentifierType> : IEntityBehaviorConfiguration where TUserIdentifierType : struct
    {
        public void OnRegistering(IServiceCollection serviceCollection)
        {
        }

        public Dictionary<string, dynamic> OnExecutionContextCreating(IServiceProvider scopedServiceProvider)
        {
            var userIdentifierProvider = scopedServiceProvider.GetService<IUserIdentifierProvider<TUserIdentifierType>>();

            var context = new Dictionary<string, dynamic>();
            if (userIdentifierProvider != null)
            {
                context.Add(typeof(IUserIdentifierProvider<TUserIdentifierType>).Name, userIdentifierProvider);
            }

            return context;
        }

        public void OnModelCreating(ModelBuilder modelBuilder, RelationalDbContext relationalDbContext, Type entityType)
        {
            var entityInterfaces = entityType.GetInterfaces();
            
            if (entityInterfaces.Any(
                x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IModifiable<>)))
            {
                var identifierType = entityType.GetInterface(typeof(IModifiable<>).Name)
                    .GenericTypeArguments[0];
                ModifiableEntityBehaviorConfigurationExtensions.ModelBuilderConfigureEntityBehaviorIModifiable.MakeGenericMethod(entityType, identifierType)
                    .Invoke(null, new object[] { modelBuilder });
            }
        }

        public void OnBeforeSaveChanges(RelationalDbContext relationalDbContext, DateTime onSaveChangesTime)
        {
            var userIdentifier = relationalDbContext.ExecutionContext
                .Get<IUserIdentifierProvider<TUserIdentifierType>>().Execute();

            foreach (var entityEntry in relationalDbContext.ChangeTracker.Entries<IModifiable<TUserIdentifierType>>()
                .Where(c => c.State == EntityState.Modified))
            {
                entityEntry.Entity.ModifiedById = userIdentifier;
                entityEntry.Entity.ModifiedOn = onSaveChangesTime;
            }
        }

        public void OnAfterSaveChanges(RelationalDbContext relationalDbContext)
        {
        }
    }
}