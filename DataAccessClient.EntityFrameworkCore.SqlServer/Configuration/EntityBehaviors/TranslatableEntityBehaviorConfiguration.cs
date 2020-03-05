using System;
using System.Linq;
using System.Reflection;
using DataAccessClient.EntityBehaviors;
using Microsoft.EntityFrameworkCore;

namespace DataAccessClient.EntityFrameworkCore.SqlServer.Configuration.EntityBehaviors
{
    public class TranslatableEntityBehaviorConfiguration : IEntityBehaviorConfiguration
    {
        private static readonly MethodInfo ModelBuilderConfigureEntityBehaviorITranslatable;
        private static readonly MethodInfo ModelBuilderConfigureEntityBehaviorTranslatedProperties;

        static TranslatableEntityBehaviorConfiguration()
        {
            ModelBuilderConfigureEntityBehaviorITranslatable = typeof(ModelBuilderExtensions).GetTypeInfo().DeclaredMethods
                .Single(m => m.Name == nameof(ModelBuilderExtensions.ConfigureEntityBehaviorITranslatable));
            ModelBuilderConfigureEntityBehaviorTranslatedProperties = typeof(ModelBuilderExtensions).GetTypeInfo().DeclaredMethods
                .Single(m => m.Name == nameof(ModelBuilderExtensions.ConfigureEntityBehaviorTranslatedProperties));
        }
        public void OnModelCreating(ModelBuilder modelBuilder, SqlServerDbContext serverDbContext, Type entityType)
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

                ModelBuilderConfigureEntityBehaviorITranslatable
                    .MakeGenericMethod(entityType, entityTranslationType, identifierType, localeType)
                    .Invoke(null, new object[] { modelBuilder });
            }

            ModelBuilderConfigureEntityBehaviorTranslatedProperties.MakeGenericMethod(entityType).Invoke(null, new object[] { modelBuilder });
        }

        public void OnBeforeSaveChanges(SqlServerDbContext serverDbContext, DateTime onSaveChangesTime)
        {
        }

        public void OnAfterSaveChanges(SqlServerDbContext serverDbContext)
        {
        }
    }
}