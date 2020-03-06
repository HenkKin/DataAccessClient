using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DataAccessClient.EntityBehaviors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.DependencyInjection;

namespace DataAccessClient.EntityFrameworkCore.SqlServer.Configuration.EntityBehaviors
{
    internal static class IdentifiableEntityBehaviorConfigurationExtensions
    {
        internal static readonly MethodInfo ModelBuilderConfigureEntityBehaviorIIdentifiable;

        static IdentifiableEntityBehaviorConfigurationExtensions()
        {
            ModelBuilderConfigureEntityBehaviorIIdentifiable = typeof(IdentifiableEntityBehaviorConfigurationExtensions).GetTypeInfo()
                .DeclaredMethods
                .Single(m => m.Name == nameof(ConfigureEntityBehaviorIIdentifiable));
        }

        internal static ModelBuilder ConfigureEntityBehaviorIIdentifiable<TEntity, TIdentifierType>(ModelBuilder modelBuilder)
            where TEntity : class, IIdentifiable<TIdentifierType>
            where TIdentifierType : struct
        {
            modelBuilder.Entity<TEntity>()
                .IsIdentifiable<TEntity, TIdentifierType>();

            return modelBuilder;
        }

        internal static EntityTypeBuilder<TEntity> IsIdentifiable<TEntity, TIdentifierType>(this EntityTypeBuilder<TEntity> entity)
            where TEntity : class, IIdentifiable<TIdentifierType>
            where TIdentifierType : struct
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .IsRequired();

            return entity;
        }
    }

    public class IdentifiableEntityBehaviorConfiguration : IEntityBehaviorConfiguration
    {
        public void OnRegistering(IServiceCollection serviceCollection)
        {
        }

        public Dictionary<string, dynamic> OnExecutionContextCreating(IServiceProvider scopedServiceProvider)
        {
            return new Dictionary<string, dynamic>();
        }

        public void OnModelCreating(ModelBuilder modelBuilder, SqlServerDbContext serverDbContext, Type entityType)
        {
            var entityInterfaces = entityType.GetInterfaces();
            
            if (entityInterfaces.Any(
                x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IIdentifiable<>)))
            {
                var identifierType = entityType.GetInterface(typeof(IIdentifiable<>).Name)
                    .GenericTypeArguments[0];
                IdentifiableEntityBehaviorConfigurationExtensions.ModelBuilderConfigureEntityBehaviorIIdentifiable.MakeGenericMethod(entityType, identifierType)
                    .Invoke(null, new object[] { modelBuilder });
            }
        }

        public void OnBeforeSaveChanges(SqlServerDbContext serverDbContext, DateTime onSaveChangesTime)
        {
        }

        public void OnAfterSaveChanges(SqlServerDbContext serverDbContext)
        {
        }
    }
}