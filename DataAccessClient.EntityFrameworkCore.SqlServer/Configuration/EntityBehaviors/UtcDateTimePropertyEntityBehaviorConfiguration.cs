using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.DependencyInjection;

namespace DataAccessClient.EntityFrameworkCore.SqlServer.Configuration.EntityBehaviors
{
    internal static class UtcDateTimePropertyEntityBehaviorConfigurationExtensions
    {
        internal static readonly MethodInfo ModelBuilderConfigureHasUtcDateTimeProperties;
        internal static readonly UtcDateTimeValueConverter UtcDateTimeValueConverter = new UtcDateTimeValueConverter();

        static UtcDateTimePropertyEntityBehaviorConfigurationExtensions()
        {
            ModelBuilderConfigureHasUtcDateTimeProperties = typeof(UtcDateTimePropertyEntityBehaviorConfigurationExtensions).GetTypeInfo().DeclaredMethods
                .Single(m => m.Name == nameof(ConfigureHasUtcDateTimeProperties));
        }

        internal static ModelBuilder ConfigureHasUtcDateTimeProperties<TEntity>(ModelBuilder modelBuilder,
            ValueConverter<DateTime?, DateTime?> dateTimeValueConverter)
            where TEntity : class
        {
            modelBuilder.Entity<TEntity>()
                .HasUtcDateTimeProperties(dateTimeValueConverter);

            return modelBuilder;
        }

        internal static EntityTypeBuilder<TEntity> HasUtcDateTimeProperties<TEntity>(
            this EntityTypeBuilder<TEntity> entity, ValueConverter<DateTime?, DateTime?> dateTimeValueConverter)
            where TEntity : class
        {
            var datetimeProperties = typeof(TEntity).GetProperties()
                .Where(property =>
                    property.CanWrite && (property.PropertyType == typeof(DateTime) ||
                                          property.PropertyType == typeof(DateTime?))
                )
                .ToList();

            datetimeProperties.ForEach(property =>
            {
                entity.Property(property.Name)
                    .HasConversion(dateTimeValueConverter);
            });

            return entity;
        }
    }

    public class UtcDateTimePropertyEntityBehaviorConfiguration : IEntityBehaviorConfiguration
    {
        public void OnRegistering(IServiceCollection serviceCollection)
        {
        }

        public Dictionary<string, dynamic> OnExecutionContextCreating(IServiceProvider scopedServiceProvider)
        {
            var context = new Dictionary<string, dynamic>();
            return context;
        }

        public void OnModelCreating(ModelBuilder modelBuilder, SqlServerDbContext serverDbContext, Type entityType)
        {
            UtcDateTimePropertyEntityBehaviorConfigurationExtensions.ModelBuilderConfigureHasUtcDateTimeProperties
                .MakeGenericMethod(entityType)
                .Invoke(null, new object[] { modelBuilder, UtcDateTimePropertyEntityBehaviorConfigurationExtensions.UtcDateTimeValueConverter });
        }

        public void OnBeforeSaveChanges(SqlServerDbContext serverDbContext, DateTime onSaveChangesTime)
        {
        }

        public void OnAfterSaveChanges(SqlServerDbContext serverDbContext)
        {
        }
    }
}