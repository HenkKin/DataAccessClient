using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DataAccessClient.EntityBehaviors;
using DataAccessClient.Providers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.DependencyInjection;

namespace DataAccessClient.EntityFrameworkCore.Relational.Configuration.EntityBehaviors
{
    internal static class CreatableEntityBehaviorConfigurationExtensions
    {
        internal static readonly MethodInfo ModelBuilderConfigureEntityBehaviorICreatable;

        static CreatableEntityBehaviorConfigurationExtensions()
        {
            ModelBuilderConfigureEntityBehaviorICreatable = typeof(CreatableEntityBehaviorConfigurationExtensions).GetTypeInfo().DeclaredMethods
                .Single(m => m.Name == nameof(ConfigureEntityBehaviorICreatable));
        }

        internal static ModelBuilder ConfigureEntityBehaviorICreatable<TEntity, TIdentifierType>(ModelBuilder modelBuilder)
            where TEntity : class, ICreatable<TIdentifierType>
            where TIdentifierType : struct
        {
            modelBuilder.Entity<TEntity>()
                .IsCreatable<TEntity, TIdentifierType>();

            return modelBuilder;
        }

        internal static EntityTypeBuilder<TEntity> IsCreatable<TEntity, TIdentifierType>(this EntityTypeBuilder<TEntity> entity)
            where TEntity : class, ICreatable<TIdentifierType>
            where TIdentifierType : struct
        {
            entity.Property(e => e.CreatedById).IsRequired();
            entity.Property(e => e.CreatedOn).IsRequired();
            return entity;
        }
    }

    public class CreatableEntityBehaviorConfiguration<TUserIdentifierType> : IEntityBehaviorConfiguration where TUserIdentifierType : struct
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
                x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(ICreatable<>)))
            {
                var identifierType = entityType.GetInterface(typeof(ICreatable<>).Name)
                    .GenericTypeArguments[0];
                CreatableEntityBehaviorConfigurationExtensions.ModelBuilderConfigureEntityBehaviorICreatable.MakeGenericMethod(entityType, identifierType)
                    .Invoke(null, new object[] { modelBuilder });
            }
        }
        
        public void OnBeforeSaveChanges(RelationalDbContext relationalDbContext, DateTime onSaveChangesTime)
        {
            var userIdentifier = relationalDbContext.ExecutionContext
                .Get<IUserIdentifierProvider<TUserIdentifierType>>().Execute();

            foreach (var entityEntry in relationalDbContext.ChangeTracker.Entries<ICreatable<TUserIdentifierType>>()
                .Where(c => c.State == EntityState.Added))
            {
                entityEntry.Entity.CreatedById = userIdentifier.GetValueOrDefault();
                entityEntry.Entity.CreatedOn = onSaveChangesTime;
            }
        }

        public void OnAfterSaveChanges(RelationalDbContext relationalDbContext)
        {
        }
    }
}