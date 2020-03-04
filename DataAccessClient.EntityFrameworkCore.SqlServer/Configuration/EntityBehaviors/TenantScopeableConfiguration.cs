using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using DataAccessClient.EntityBehaviors;
using Microsoft.EntityFrameworkCore;

namespace DataAccessClient.EntityFrameworkCore.SqlServer.Configuration.EntityBehaviors
{

    public class TenantScopeableConfiguration
    {
        private static readonly PropertyInfo IsTenantScopableQueryFilterEnabledProperty;
        private static readonly MethodInfo CurrentTenantIdentifierGenericMethod;
        private static readonly MethodInfo ModelBuilderConfigureEntityBehaviorITenantScopableMethod;

        static TenantScopeableConfiguration()
        {
            IsTenantScopableQueryFilterEnabledProperty = typeof(SqlServerDbContext).GetProperty("IsTenantScopableQueryFilterEnabled",
                BindingFlags.Instance | BindingFlags.NonPublic);

            CurrentTenantIdentifierGenericMethod = typeof(SqlServerDbContext).GetMethod("CurrentTenantIdentifier",
                    BindingFlags.Instance | BindingFlags.NonPublic);

            ModelBuilderConfigureEntityBehaviorITenantScopableMethod = typeof(ModelBuilderExtensions).GetTypeInfo().DeclaredMethods
                .Single(m => m.Name == nameof(ModelBuilderExtensions.ConfigureEntityBehaviorITenantScopable));
        }
        public void Execute(ModelBuilder modelBuilder, SqlServerDbContext serverDbContext, Type entityType)
        {
            var entityInterfaces = entityType.GetInterfaces();

            if (entityInterfaces.Any(x =>
                x.IsGenericType && x.GetGenericTypeDefinition() == typeof(ITenantScopable<>)))
            {
                var identifierType = entityType.GetInterface(typeof(ITenantScopable<>).Name)
                    .GenericTypeArguments[0];

                var createTenantScopableQueryFilter = GetType().GetMethod(nameof(CreateTenantScopableQueryFilter),
                    BindingFlags.Static | BindingFlags.NonPublic);
                if (createTenantScopableQueryFilter == null)
                {
                    throw new InvalidOperationException(
                        $"Can not find method {nameof(CreateTenantScopableQueryFilter)} on class {GetType().FullName}");
                }

                var tenantScopableQueryFilterMethod = createTenantScopableQueryFilter.MakeGenericMethod(entityType, identifierType);
                var tenantScopableQueryFilter = tenantScopableQueryFilterMethod.Invoke(this, new []{serverDbContext});

                ModelBuilderConfigureEntityBehaviorITenantScopableMethod.MakeGenericMethod(entityType, identifierType)
                    .Invoke(null, new object[] { modelBuilder, tenantScopableQueryFilter });
            }
        }

        private static TTenantIdentifierType CurrentTenantIdentifier<TTenantIdentifierType>(SqlServerDbContext dbContext) where TTenantIdentifierType : struct
        {
            var currentTenantIdentifierMethod = CurrentTenantIdentifierGenericMethod.MakeGenericMethod(typeof(TTenantIdentifierType));
            return (TTenantIdentifierType) currentTenantIdentifierMethod.Invoke(dbContext, new object[0]);
        }

        private static bool IsTenantScopableQueryFilterEnabled(SqlServerDbContext dbContext)
        {
            return (bool)IsTenantScopableQueryFilterEnabledProperty.GetValue(dbContext);
        }

        private static Expression<Func<TEntity, bool>> CreateTenantScopableQueryFilter<TEntity,
            TTenantIdentifierType>(SqlServerDbContext dbContext)
            where TEntity : class, ITenantScopable<TTenantIdentifierType>
            where TTenantIdentifierType : struct
        {

            Expression<Func<TEntity, bool>> tenantScopableQueryFilter = e =>
                e.TenantId.Equals(CurrentTenantIdentifier<TTenantIdentifierType>(dbContext)) ||
                e.TenantId.Equals(CurrentTenantIdentifier<TTenantIdentifierType>(dbContext)) ==
                IsTenantScopableQueryFilterEnabled(dbContext);

            return tenantScopableQueryFilter;
        }
    }
}
