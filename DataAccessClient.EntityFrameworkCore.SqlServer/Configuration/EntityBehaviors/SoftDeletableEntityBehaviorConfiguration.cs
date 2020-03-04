using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using DataAccessClient.EntityBehaviors;
using Microsoft.EntityFrameworkCore;

namespace DataAccessClient.EntityFrameworkCore.SqlServer.Configuration.EntityBehaviors
{
    public class SoftDeletableEntityBehaviorConfiguration : IEntityBehaviorConfiguration
    {
        private static readonly PropertyInfo IsSoftDeletableQueryFilterEnabledProperty;
        private static readonly MethodInfo ModelBuilderConfigureEntityBehaviorISoftDeletableMethod;

        static SoftDeletableEntityBehaviorConfiguration()
        {
            IsSoftDeletableQueryFilterEnabledProperty = typeof(SqlServerDbContext).GetProperty(nameof(SqlServerDbContext.IsSoftDeletableQueryFilterEnabled),
                BindingFlags.Instance | BindingFlags.NonPublic);

            ModelBuilderConfigureEntityBehaviorISoftDeletableMethod = typeof(ModelBuilderExtensions).GetTypeInfo().DeclaredMethods
                .Single(m => m.Name == nameof(ModelBuilderExtensions.ConfigureEntityBehaviorISoftDeletable));
        }
        public void Execute(ModelBuilder modelBuilder, SqlServerDbContext serverDbContext, Type entityType)
        {
            var entityInterfaces = entityType.GetInterfaces();

            if (entityInterfaces.Any(x =>
                x.IsGenericType && x.GetGenericTypeDefinition() == typeof(ISoftDeletable<>)))
            {
                var identifierType = entityType.GetInterface(typeof(ISoftDeletable<>).Name)
                    .GenericTypeArguments[0];

                var createSoftDeletableQueryFilter = GetType().GetMethod(nameof(CreateSoftDeletableQueryFilter),
                    BindingFlags.Static | BindingFlags.NonPublic);
                if (createSoftDeletableQueryFilter == null)
                {
                    throw new InvalidOperationException(
                        $"Can not find method {nameof(CreateSoftDeletableQueryFilter)} on class {GetType().FullName}");
                }

                var softDeletableQueryFilterMethod = createSoftDeletableQueryFilter.MakeGenericMethod(entityType, identifierType);
                var softDeletableQueryFilter = softDeletableQueryFilterMethod.Invoke(this, new object[]{serverDbContext});

                ModelBuilderConfigureEntityBehaviorISoftDeletableMethod.MakeGenericMethod(entityType, identifierType)
                    .Invoke(null, new [] { modelBuilder, softDeletableQueryFilter });
            }
        }

        private static bool IsSoftDeletableQueryFilterEnabled(SqlServerDbContext dbContext)
        {
            return (bool)IsSoftDeletableQueryFilterEnabledProperty.GetValue(dbContext);
        }

        private static Expression<Func<TEntity, bool>> CreateSoftDeletableQueryFilter<TEntity,
            TUserIdentifierType>(SqlServerDbContext dbContext)
            where TEntity : class, ISoftDeletable<TUserIdentifierType>
            where TUserIdentifierType : struct
        {
            Expression<Func<TEntity, bool>> softDeletableQueryFilter =
                e => !e.IsDeleted || e.IsDeleted != IsSoftDeletableQueryFilterEnabled(dbContext);
            return softDeletableQueryFilter;
        }
    }
}
