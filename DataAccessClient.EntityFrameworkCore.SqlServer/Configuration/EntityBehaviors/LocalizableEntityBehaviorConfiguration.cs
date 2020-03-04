using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using DataAccessClient.EntityBehaviors;
using Microsoft.EntityFrameworkCore;

namespace DataAccessClient.EntityFrameworkCore.SqlServer.Configuration.EntityBehaviors
{
    public class LocalizableEntityBehaviorConfiguration : IEntityBehaviorConfiguration
    {
        private static readonly PropertyInfo IsLocalizationQueryFilterEnabledProperty;
        private static readonly MethodInfo CurrentLocaleIdentifierGenericMethod;
        private static readonly MethodInfo ModelBuilderConfigureEntityBehaviorILocalizableMethod;

        static LocalizableEntityBehaviorConfiguration()
        {
            IsLocalizationQueryFilterEnabledProperty = typeof(SqlServerDbContext).GetProperty(nameof(SqlServerDbContext.IsLocalizationQueryFilterEnabled),
                BindingFlags.Instance | BindingFlags.NonPublic);

            CurrentLocaleIdentifierGenericMethod = typeof(SqlServerDbContext).GetMethod(nameof(SqlServerDbContext.CurrentLocaleIdentifier),
                    BindingFlags.Instance | BindingFlags.NonPublic);

            ModelBuilderConfigureEntityBehaviorILocalizableMethod = typeof(ModelBuilderExtensions).GetTypeInfo().DeclaredMethods
                .Single(m => m.Name == nameof(ModelBuilderExtensions.ConfigureEntityBehaviorILocalizable));
        }
        public void Execute(ModelBuilder modelBuilder, SqlServerDbContext serverDbContext, Type entityType)
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

                var tenantScopableQueryFilterMethod = createLocalizableQueryFilter.MakeGenericMethod(entityType, identifierType);
                var tenantScopableQueryFilter = tenantScopableQueryFilterMethod.Invoke(this, new object[]{serverDbContext});

                ModelBuilderConfigureEntityBehaviorILocalizableMethod.MakeGenericMethod(entityType, identifierType)
                    .Invoke(null, new [] { modelBuilder, tenantScopableQueryFilter });
            }
        }

        private static TLocaleIdentifierType CurrentLocaleIdentifier<TLocaleIdentifierType>(SqlServerDbContext dbContext) where TLocaleIdentifierType : IConvertible
        {
            var currentLocaleIdentifierMethod = CurrentLocaleIdentifierGenericMethod.MakeGenericMethod(typeof(TLocaleIdentifierType));
            return (TLocaleIdentifierType) currentLocaleIdentifierMethod.Invoke(dbContext, new object[0]);
        }

        private static bool IsLocalizationQueryFilterEnabled(SqlServerDbContext dbContext)
        {
            return (bool)IsLocalizationQueryFilterEnabledProperty.GetValue(dbContext);
        }

        private static Expression<Func<TEntity, bool>> CreateLocalizableQueryFilter<TEntity,
            TLocaleIdentifierType>(SqlServerDbContext dbContext)
            where TEntity : class, ILocalizable<TLocaleIdentifierType>
            where TLocaleIdentifierType : IConvertible
        {
            Expression<Func<TEntity, bool>> tenantScopableQueryFilter = e =>
                e.LocaleId.Equals(CurrentLocaleIdentifier<TLocaleIdentifierType>(dbContext)) ||
                e.LocaleId.Equals(CurrentLocaleIdentifier<TLocaleIdentifierType>(dbContext)) ==
                IsLocalizationQueryFilterEnabled(dbContext);

            return tenantScopableQueryFilter;
        }
    }
}
