using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using DataAccessClient.Configuration;
using DataAccessClient.EntityBehaviors;
using DataAccessClient.Providers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DataAccessClient.EntityFrameworkCore.Relational.Configuration.EntityBehaviors
{
    internal static class TenantScopeableEntityBehaviorConfigurationExtensions
    {
        internal static readonly MethodInfo ModelBuilderConfigureEntityBehaviorITenantScopableMethod;

        static TenantScopeableEntityBehaviorConfigurationExtensions()
        {
            ModelBuilderConfigureEntityBehaviorITenantScopableMethod = typeof(TenantScopeableEntityBehaviorConfigurationExtensions).GetTypeInfo()
                .DeclaredMethods
                .Single(m => m.Name == nameof(ConfigureEntityBehaviorITenantScopable));
        }

        internal static ModelBuilder ConfigureEntityBehaviorITenantScopable<TEntity, TIdentifierType>(
            ModelBuilder modelBuilder, Expression<Func<TEntity, bool>> queryFilter)
            where TEntity : class, ITenantScopable<TIdentifierType>
            where TIdentifierType : struct
        {
            modelBuilder.Entity<TEntity>()
                .IsTenantScopable<TEntity, TIdentifierType>(queryFilter);

            return modelBuilder;
        }

        internal static EntityTypeBuilder<TEntity> IsTenantScopable<TEntity, TIdentifierType>(
            this EntityTypeBuilder<TEntity> entity, Expression<Func<TEntity, bool>> queryFilter)
            where TEntity : class, ITenantScopable<TIdentifierType>
            where TIdentifierType : struct
        {
            entity.Property(e => e.TenantId).IsRequired();

            entity.AppendQueryFilter(queryFilter);

            return entity;
        }
    }

    public class TenantScopeableEntityBehaviorConfiguration<TTenantIdentifierType> : IEntityBehaviorConfiguration where TTenantIdentifierType : struct
    {
        public void OnRegistering(IServiceCollection serviceCollection)
        {
            serviceCollection.TryAddScoped<IMultiTenancyConfiguration, DefaultMultiTenancyConfiguration>();
        }

        public Dictionary<string, dynamic> OnExecutionContextCreating(IServiceProvider scopedServiceProvider)
        {
            var tenantIdentifierProvider = scopedServiceProvider.GetService<ITenantIdentifierProvider<TTenantIdentifierType>>();
            var multiTenancyConfiguration = scopedServiceProvider.GetService<IMultiTenancyConfiguration>();

            var context = new Dictionary<string, dynamic>();
            if (tenantIdentifierProvider != null)
            {
                context.Add(typeof(ITenantIdentifierProvider<TTenantIdentifierType>).Name, tenantIdentifierProvider);
            }
            if (multiTenancyConfiguration != null)
            {
                context.Add(typeof(IMultiTenancyConfiguration).Name, multiTenancyConfiguration);
            }

            return context;
        }

        public void OnModelCreating(ModelBuilder modelBuilder, RelationalDbContext serverDbContext, Type entityType)
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

                TenantScopeableEntityBehaviorConfigurationExtensions.ModelBuilderConfigureEntityBehaviorITenantScopableMethod.MakeGenericMethod(entityType, identifierType)
                    .Invoke(null, new [] { modelBuilder, tenantScopableQueryFilter });
            }
        }

        public void OnBeforeSaveChanges(RelationalDbContext serverDbContext, DateTime onSaveChangesTime)
        {
            var tenantIdentifier = serverDbContext.ExecutionContext
                .Get<ITenantIdentifierProvider<TTenantIdentifierType>>().Execute();

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
                            $"CurrentTenantId is needed for new entity of type '{entityEntry.Entity.GetType().FullName}', but the '{typeof(RelationalDbContext).FullName}' does not have one at this moment");
                    }
                }
            }
        }

        public void OnAfterSaveChanges(RelationalDbContext serverDbContext)
        {
            
        }

        private static TTenantIdentifierType? CurrentTenantIdentifier(RelationalDbContext dbContext)
        {
            var tenantIdentifier = dbContext.ExecutionContext.Get<ITenantIdentifierProvider<TTenantIdentifierType>>().Execute();
            return tenantIdentifier;
        }

        private static bool IsTenantScopableQueryFilterEnabled(RelationalDbContext dbContext)
        {
            var multiTenancyConfiguration = dbContext.ExecutionContext.Get<IMultiTenancyConfiguration>();
            return multiTenancyConfiguration.IsQueryFilterEnabled;
        }

        private static Expression<Func<TEntity, bool>> CreateTenantScopableQueryFilter<TEntity>(RelationalDbContext dbContext)
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
