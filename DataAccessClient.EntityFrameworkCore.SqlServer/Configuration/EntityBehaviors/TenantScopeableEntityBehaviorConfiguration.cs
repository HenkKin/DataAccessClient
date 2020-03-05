using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using DataAccessClient.EntityBehaviors;
using Microsoft.EntityFrameworkCore;
// ReSharper disable StaticMemberInGenericType

namespace DataAccessClient.EntityFrameworkCore.SqlServer.Configuration.EntityBehaviors
{
    public class TenantScopeableEntityBehaviorConfiguration<TTenantIdentifierType> : IEntityBehaviorConfiguration where TTenantIdentifierType : struct
    {
        private static readonly PropertyInfo IsTenantScopableQueryFilterEnabledProperty;
        private static readonly MethodInfo CurrentTenantIdentifierGenericMethod;
        private static readonly MethodInfo ModelBuilderConfigureEntityBehaviorITenantScopableMethod;

        static TenantScopeableEntityBehaviorConfiguration()
        {
            IsTenantScopableQueryFilterEnabledProperty = typeof(SqlServerDbContext).GetProperty(nameof(SqlServerDbContext.IsTenantScopableQueryFilterEnabled),
                BindingFlags.Instance | BindingFlags.NonPublic);

            CurrentTenantIdentifierGenericMethod = typeof(SqlServerDbContext).GetMethod(nameof(SqlServerDbContext.CurrentTenantIdentifier),
                    BindingFlags.Instance | BindingFlags.NonPublic);

            ModelBuilderConfigureEntityBehaviorITenantScopableMethod = typeof(ModelBuilderExtensions).GetTypeInfo().DeclaredMethods
                .Single(m => m.Name == nameof(ModelBuilderExtensions.ConfigureEntityBehaviorITenantScopable));
        }
        public void OnModelCreating(ModelBuilder modelBuilder, SqlServerDbContext serverDbContext, Type entityType)
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

                var tenantScopableQueryFilterMethod = createTenantScopableQueryFilter.MakeGenericMethod(entityType);
                var tenantScopableQueryFilter = tenantScopableQueryFilterMethod.Invoke(this, new object[]{serverDbContext});

                ModelBuilderConfigureEntityBehaviorITenantScopableMethod.MakeGenericMethod(entityType, identifierType)
                    .Invoke(null, new [] { modelBuilder, tenantScopableQueryFilter });
            }
        }

        public void OnBeforeSaveChanges(SqlServerDbContext serverDbContext, DateTime onSaveChangesTime)
        {
            var tenantIdentifier = serverDbContext.CurrentTenantIdentifier<TTenantIdentifierType>();

            foreach (var entityEntry in serverDbContext.ChangeTracker.Entries<ITenantScopable<TTenantIdentifierType>>()
                .Where(c => c.State == EntityState.Added))
            {
                var tenantId = entityEntry.Entity.TenantId;
                if (tenantId.Equals(default(TTenantIdentifierType)))
                {
                    if (tenantIdentifier.HasValue)
                    {
                        entityEntry.Entity.TenantId = tenantIdentifier.Value;
                    }
                    else
                    {
                        throw new InvalidOperationException(
                            $"CurrentTenantId is needed for new entity of type '{entityEntry.Entity.GetType().FullName}', but the '{typeof(SqlServerDbContext).FullName}' does not have one at this moment");
                    }
                }
            }
        }

        public void OnAfterSaveChanges(SqlServerDbContext serverDbContext)
        {
            
        }

        private static TTenantIdentifierType CurrentTenantIdentifier(SqlServerDbContext dbContext)
        {
            var currentTenantIdentifierMethod = CurrentTenantIdentifierGenericMethod.MakeGenericMethod(typeof(TTenantIdentifierType));
            return (TTenantIdentifierType) currentTenantIdentifierMethod.Invoke(dbContext, new object[0]);
        }

        private static bool IsTenantScopableQueryFilterEnabled(SqlServerDbContext dbContext)
        {
            return (bool)IsTenantScopableQueryFilterEnabledProperty.GetValue(dbContext);
        }

        private static Expression<Func<TEntity, bool>> CreateTenantScopableQueryFilter<TEntity>(SqlServerDbContext dbContext)
            where TEntity : class, ITenantScopable<TTenantIdentifierType>
        {

            Expression<Func<TEntity, bool>> tenantScopableQueryFilter = e =>
                e.TenantId.Equals(CurrentTenantIdentifier(dbContext)) ||
                e.TenantId.Equals(CurrentTenantIdentifier(dbContext)) ==
                IsTenantScopableQueryFilterEnabled(dbContext);

            return tenantScopableQueryFilter;
        }
    }
}
