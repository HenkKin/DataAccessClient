using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DataAccessClient.EntityBehaviors;
using DataAccessClient.EntityFrameworkCore.Relational;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.DependencyInjection;

namespace DataAccessClient.EntityFrameworkCore.Relational.Configuration.EntityBehaviors
{
    internal static class TranslatedPropertyEntityBehaviorConfigurationExtensions
    {
        internal static readonly MethodInfo ModelBuilderConfigureEntityBehaviorTranslatedProperties;

        static TranslatedPropertyEntityBehaviorConfigurationExtensions()
        {
            ModelBuilderConfigureEntityBehaviorTranslatedProperties = typeof(TranslatedPropertyEntityBehaviorConfigurationExtensions).GetTypeInfo()
                .DeclaredMethods
                .Single(m => m.Name == nameof(ConfigureEntityBehaviorTranslatedProperties));
        }

        internal static ModelBuilder ConfigureEntityBehaviorTranslatedProperties<TEntity>(ModelBuilder modelBuilder)
            where TEntity : class
        {
            modelBuilder.Entity<TEntity>()
                .HasTranslatedProperties();

            return modelBuilder;
        }

        internal static EntityTypeBuilder<TEntity> HasTranslatedProperties<TEntity>(
            this EntityTypeBuilder<TEntity> entity)
            where TEntity : class
        {
            var translatedProperties = typeof(TEntity).GetProperties().Where(p =>
                p.PropertyType.IsGenericType &&
                p.PropertyType.GetGenericTypeDefinition() == typeof(TranslatedProperty<>));
            foreach (var translatedProperty in translatedProperties)
            {
                var localeIdentifierType = translatedProperty.PropertyType.GenericTypeArguments[0];
                var ownsOneType = typeof(TranslatedProperty<>).MakeGenericType(localeIdentifierType);
                entity.OwnsOne(ownsOneType, translatedProperty.Name, translatedPropertyBuilder =>
                {
                    var propertyTranslationType = typeof(PropertyTranslation<>).MakeGenericType(localeIdentifierType);
                    translatedPropertyBuilder.OwnsMany(propertyTranslationType,
                        nameof(TranslatedProperty<int>.Translations), builder =>
                        {
                            builder.ToTable(typeof(TEntity).Name + "_" + translatedProperty.Name +
                                            nameof(TranslatedProperty<int>.Translations));
                            builder.WithOwner().HasForeignKey("OwnerId");
                            builder.Property(nameof(PropertyTranslation<int>.LocaleId)).IsRequired();
                            builder.Property(nameof(PropertyTranslation<int>.Translation)).IsRequired();
                        }
                    );
                });
            }

            return entity;
        }
    }

    public class TranslatedPropertyEntityBehaviorConfiguration : IEntityBehaviorConfiguration
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
            TranslatedPropertyEntityBehaviorConfigurationExtensions.ModelBuilderConfigureEntityBehaviorTranslatedProperties.MakeGenericMethod(entityType).Invoke(null, new object[] { modelBuilder });
        }

        public void OnBeforeSaveChanges(RelationalDbContext relationalDbContext, DateTime onSaveChangesTime)
        {
        }

        public void OnAfterSaveChanges(RelationalDbContext relationalDbContext)
        {
        }
    }
}