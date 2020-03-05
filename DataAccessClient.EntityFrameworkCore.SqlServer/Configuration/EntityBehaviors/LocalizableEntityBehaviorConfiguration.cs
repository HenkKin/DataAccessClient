using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using DataAccessClient.Configuration;
using DataAccessClient.EntityBehaviors;
using DataAccessClient.Providers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
// ReSharper disable StaticMemberInGenericType

namespace DataAccessClient.EntityFrameworkCore.SqlServer.Configuration.EntityBehaviors
{
    public class LocalizableEntityBehaviorConfiguration<TLocaleIdentifierType> : IEntityBehaviorConfiguration where TLocaleIdentifierType : IConvertible
    {
        private static readonly MethodInfo ModelBuilderConfigureEntityBehaviorILocalizableMethod;

        static LocalizableEntityBehaviorConfiguration()
        {
            ModelBuilderConfigureEntityBehaviorILocalizableMethod = typeof(ModelBuilderExtensions).GetTypeInfo().DeclaredMethods
                .Single(m => m.Name == nameof(ModelBuilderExtensions.ConfigureEntityBehaviorILocalizable));
        }

        public void OnRegistering(IServiceCollection serviceCollection)
        {
            serviceCollection.TryAddScoped<ILocalizationConfiguration, DefaultLocalizationConfiguration>();
        }

        public Dictionary<string, dynamic> OnExecutionContextCreating(IServiceProvider scopedServiceProvider)
        {
            var localeIdentifierProvider = scopedServiceProvider.GetRequiredService<ILocaleIdentifierProvider<TLocaleIdentifierType>>();
            var localizationConfiguration = scopedServiceProvider.GetRequiredService<ILocalizationConfiguration>();

            var context = new Dictionary<string, dynamic>
            {
                {typeof(ILocaleIdentifierProvider<TLocaleIdentifierType>).Name, localeIdentifierProvider},
                {typeof(ILocalizationConfiguration).Name, localizationConfiguration},
            };

            return context;
        }

        public void OnModelCreating(ModelBuilder modelBuilder, SqlServerDbContext serverDbContext, Type entityType)
        {
            var entityInterfaces = entityType.GetInterfaces();

            if (entityInterfaces.Any(x =>
                x.IsGenericType && x.GetGenericTypeDefinition() == typeof(ILocalizable<>)))
            {
                var identifierType = entityType.GetInterface(typeof(ILocalizable<>).Name)
                    .GenericTypeArguments[0];

                var createLocalizableQueryFilter = GetType().GetMethod(nameof(CreateLocalizableQueryFilter),
                    BindingFlags.Static | BindingFlags.NonPublic);
                if (createLocalizableQueryFilter == null)
                {
                    throw new InvalidOperationException(
                        $"Can not find method {nameof(CreateLocalizableQueryFilter)} on class {GetType().FullName}");
                }

                var tenantScopableQueryFilterMethod = createLocalizableQueryFilter.MakeGenericMethod(entityType);
                var tenantScopableQueryFilter = tenantScopableQueryFilterMethod.Invoke(this, new object[]{serverDbContext});

                ModelBuilderConfigureEntityBehaviorILocalizableMethod.MakeGenericMethod(entityType, identifierType)
                    .Invoke(null, new [] { modelBuilder, tenantScopableQueryFilter });
            }
        }

        public void OnBeforeSaveChanges(SqlServerDbContext serverDbContext, DateTime onSaveChangesTime)
        {
        }

        public void OnAfterSaveChanges(SqlServerDbContext serverDbContext)
        {
        }

        private static TLocaleIdentifierType CurrentLocaleIdentifier(SqlServerDbContext dbContext)
        {
            var localeIdentifier = dbContext.ExecutionContext.Get<ILocaleIdentifierProvider<TLocaleIdentifierType>>().Execute();
            return localeIdentifier;
        }

        private static bool IsLocalizationQueryFilterEnabled(SqlServerDbContext dbContext)
        {
            var localizationConfiguration = dbContext.ExecutionContext.Get<ILocalizationConfiguration>();
            return localizationConfiguration.IsQueryFilterEnabled;
        }

        private static Expression<Func<TEntity, bool>> CreateLocalizableQueryFilter<TEntity>(SqlServerDbContext dbContext)
            where TEntity : class, ILocalizable<TLocaleIdentifierType>
        {
            Expression<Func<TEntity, bool>> tenantScopableQueryFilter = e =>
                e.LocaleId.Equals(CurrentLocaleIdentifier(dbContext)) ||
                e.LocaleId.Equals(CurrentLocaleIdentifier(dbContext)) ==
                IsLocalizationQueryFilterEnabled(dbContext);

            return tenantScopableQueryFilter;
        }
    }
}
