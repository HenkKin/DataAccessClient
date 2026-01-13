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
    internal static class TranslatableEntityBehaviorConfigurationExtensions
    {
        internal static readonly MethodInfo ModelBuilderConfigureEntityBehaviorITranslatable;

        static TranslatableEntityBehaviorConfigurationExtensions()
        {
            ModelBuilderConfigureEntityBehaviorITranslatable = typeof(TranslatableEntityBehaviorConfigurationExtensions).GetTypeInfo()
                .DeclaredMethods
                .Single(m => m.Name == nameof(ConfigureEntityBehaviorITranslatable));
        }

        internal static ModelBuilder ConfigureEntityBehaviorITranslatable<TEntity, TEntityTranslation, TIdentifierType,
            TLocaleIdentifierType>(
            ModelBuilder modelBuilder)
            where TEntity : class, ITranslatable<TEntityTranslation, TIdentifierType, TLocaleIdentifierType>
            where TEntityTranslation : class, IEntityTranslation<TEntity, TIdentifierType, TLocaleIdentifierType>
            where TIdentifierType : struct
            where TLocaleIdentifierType : IConvertible
        {
            modelBuilder.Entity<TEntity>()
                .IsTranslatable<TEntity, TEntityTranslation, TIdentifierType, TLocaleIdentifierType>();

            modelBuilder.Entity<TEntityTranslation>()
                .IsEntityTranslation<TEntityTranslation, TEntity, TIdentifierType, TLocaleIdentifierType>();

            return modelBuilder;
        }

        internal static EntityTypeBuilder<TEntity> IsTranslatable<TEntity, TEntityTranslation, TIdentifierType,
            TLocaleIdentifierType>(this EntityTypeBuilder<TEntity> entity)
            where TEntity : class, ITranslatable<TEntityTranslation, TIdentifierType, TLocaleIdentifierType>
            where TEntityTranslation : class, IEntityTranslation<TEntity, TIdentifierType, TLocaleIdentifierType>
            where TIdentifierType : struct
            where TLocaleIdentifierType : IConvertible
        {
            entity.HasMany(x => x.Translations)
                .WithOne(x => x.TranslatedEntity)
                .HasForeignKey(x => x.TranslatedEntityId)
                .OnDelete(DeleteBehavior.Cascade);
            return entity;
        }

        internal static EntityTypeBuilder<TEntity> IsEntityTranslation<TEntity, TTranslatableEntity, TIdentifierType,
            TLocaleIdentifierType>(this EntityTypeBuilder<TEntity> entity)
            where TEntity : class, IEntityTranslation<TTranslatableEntity, TIdentifierType, TLocaleIdentifierType>
            where TTranslatableEntity : class, ITranslatable<TEntity, TIdentifierType, TLocaleIdentifierType>
            where TIdentifierType : struct
            where TLocaleIdentifierType : IConvertible
        {
            entity.HasOne(x => x.TranslatedEntity)
                .WithMany(x => x.Translations)
                .HasForeignKey(x => x.TranslatedEntityId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasKey(e => new {e.TranslatedEntityId, Language = e.LocaleId});
            entity.Property(e => e.TranslatedEntityId).IsRequired();
            entity.Property(e => e.LocaleId).IsRequired();

            return entity;
        }
    }

    public class TranslatableEntityBehaviorConfiguration : IEntityBehaviorConfiguration
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

            if (entityInterfaces.Any(x =>
                x.IsGenericType && x.GetGenericTypeDefinition() == typeof(ITranslatable<,,>)))
            {
                var entityTranslationType = entityType.GetInterface(typeof(ITranslatable<,,>).Name)
                    .GenericTypeArguments[0];
                var identifierType = entityType.GetInterface(typeof(ITranslatable<,,>).Name)
                    .GenericTypeArguments[1];
                var localeType = entityType.GetInterface(typeof(ITranslatable<,,>).Name)
                    .GenericTypeArguments[2];

                TranslatableEntityBehaviorConfigurationExtensions.ModelBuilderConfigureEntityBehaviorITranslatable
                    .MakeGenericMethod(entityType, entityTranslationType, identifierType, localeType)
                    .Invoke(null, new object[] { modelBuilder });
            }
        }

        public void OnBeforeSaveChanges(RelationalDbContext relationalDbContext, DateTime onSaveChangesTime)
        {
        }

        public void OnAfterSaveChanges(RelationalDbContext relationalDbContext)
        {
        }
    }
}