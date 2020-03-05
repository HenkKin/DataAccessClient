using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using DataAccessClient.Configuration;
using DataAccessClient.EntityBehaviors;
using DataAccessClient.Providers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable StaticMemberInGenericType

namespace DataAccessClient.EntityFrameworkCore.SqlServer.Configuration.EntityBehaviors
{
    public class SoftDeletableEntityBehaviorConfiguration<TUserIdentifierType> : IEntityBehaviorConfiguration
        where TUserIdentifierType : struct
    {
        private static readonly MethodInfo ModelBuilderConfigureEntityBehaviorISoftDeletableMethod;

        static SoftDeletableEntityBehaviorConfiguration()
        {
            ModelBuilderConfigureEntityBehaviorISoftDeletableMethod = typeof(ModelBuilderExtensions).GetTypeInfo()
                .DeclaredMethods
                .Single(m => m.Name == nameof(ModelBuilderExtensions.ConfigureEntityBehaviorISoftDeletable));
        }

        public void OnRegistering(IServiceCollection serviceCollection)
        {
            serviceCollection.TryAddScoped<ISoftDeletableConfiguration, DefaultSoftDeletableConfiguration>();
        }

        public Dictionary<string, dynamic> OnExecutionContextCreating(IServiceProvider scopedServiceProvider)
        {
            var userIdentifierProvider = scopedServiceProvider.GetRequiredService<IUserIdentifierProvider<TUserIdentifierType>>();
            var softDeletableConfiguration = scopedServiceProvider.GetRequiredService<ISoftDeletableConfiguration>();

            var context = new Dictionary<string, dynamic>
            {
                {typeof(IUserIdentifierProvider<TUserIdentifierType>).Name, userIdentifierProvider},
                {typeof(ISoftDeletableConfiguration).Name, softDeletableConfiguration},
            };

            return context;
        }

        public void OnModelCreating(ModelBuilder modelBuilder, SqlServerDbContext serverDbContext, Type entityType)
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

                var softDeletableQueryFilterMethod = createSoftDeletableQueryFilter.MakeGenericMethod(entityType);
                var softDeletableQueryFilter =
                    softDeletableQueryFilterMethod.Invoke(this, new object[] {serverDbContext});

                ModelBuilderConfigureEntityBehaviorISoftDeletableMethod.MakeGenericMethod(entityType, identifierType)
                    .Invoke(null, new[] {modelBuilder, softDeletableQueryFilter});
            }
        }

        public void OnBeforeSaveChanges(SqlServerDbContext serverDbContext, DateTime onSaveChangesTime)
        {
            var softDeletableConfiguration = serverDbContext.ExecutionContext.Get<ISoftDeletableConfiguration>();
            if (softDeletableConfiguration.IsEnabled)
            {
                var userIdentifier = serverDbContext.ExecutionContext
                    .Get<IUserIdentifierProvider<TUserIdentifierType>>().Execute();
                foreach (var entityEntry in serverDbContext.ChangeTracker.Entries<ISoftDeletable<TUserIdentifierType>>()
                    .Where(c => c.State == EntityState.Deleted))
                {
                    entityEntry.State = EntityState.Unchanged;

                    var entity = entityEntry.Entity;
                    var entityIsSoftDeleted =
                        entity.IsDeleted && entity.DeletedById.HasValue && entity.DeletedOn.HasValue;

                    if (entityIsSoftDeleted)
                    {
                        continue;
                    }

                    SetOwnedEntitiesToUnchanged(entityEntry);

                    entityEntry.Entity.IsDeleted = true;
                    entityEntry.Entity.DeletedById = userIdentifier;
                    entityEntry.Entity.DeletedOn = onSaveChangesTime;
                    entityEntry.Member(nameof(ISoftDeletable<TUserIdentifierType>.IsDeleted)).IsModified = true;
                    entityEntry.Member(nameof(ISoftDeletable<TUserIdentifierType>.DeletedById)).IsModified = true;
                    entityEntry.Member(nameof(ISoftDeletable<TUserIdentifierType>.DeletedOn)).IsModified = true;
                }
            }
        }

        protected internal void SetOwnedEntitiesToUnchanged(EntityEntry entityEntry)
        {
            var ownedEntities = entityEntry.References
                .Where(r => r.TargetEntry != null && r.TargetEntry.Metadata.IsOwned())
                .ToList();

            foreach (var ownedEntity in ownedEntities)
            {
                ownedEntity.TargetEntry.State = EntityState.Unchanged;
                SetOwnedEntitiesToUnchanged(ownedEntity.TargetEntry);
            }
        }

        public void OnAfterSaveChanges(SqlServerDbContext serverDbContext)
        {
            var trackedEntities = new HashSet<object>();
            foreach (var dbEntityEntry in serverDbContext.ChangeTracker.Entries().ToArray())
            {
                if (dbEntityEntry.Entity != null)
                {
                    trackedEntities.Add(dbEntityEntry.Entity);
                    RemoveSoftDeletableProperties(serverDbContext, dbEntityEntry, trackedEntities);
                }
            }
        }

        private void RemoveSoftDeletableProperties(SqlServerDbContext serverDbContext, EntityEntry entityEntry, HashSet<object> trackedEntities)
        {
            var navigationPropertiesEntries = entityEntry.Navigations;
            foreach (var navigationEntry in navigationPropertiesEntries)
            {
                if (navigationEntry.Metadata.IsCollection())
                {
                    if (navigationEntry.CurrentValue is ICollection collection)
                    {
                        var notRemovedItems = new Collection<object>();
                        foreach (var collectionItem in collection)
                        {
                            var listItemDbEntityEntry = serverDbContext.ChangeTracker.Context.Entry(collectionItem);
                            if (collectionItem is ISoftDeletable<TUserIdentifierType> deletable && deletable.IsDeleted)
                            {
                                listItemDbEntityEntry.State = EntityState.Detached;
                            }
                            else
                            {
                                notRemovedItems.Add(collectionItem);
                                if (!trackedEntities.Contains(collectionItem))
                                {
                                    trackedEntities.Add(collectionItem);
                                    RemoveSoftDeletableProperties(serverDbContext, listItemDbEntityEntry, trackedEntities);
                                }
                            }
                        }

                        Type constructedType = typeof(Collection<>).MakeGenericType(navigationEntry.Metadata.PropertyInfo.PropertyType.GenericTypeArguments);
                        IList col = (IList) Activator.CreateInstance(constructedType);
                        foreach (var notRemovedItem in notRemovedItems)
                        {
                            col.Add(notRemovedItem);
                        }

                        navigationEntry.CurrentValue = col;
                    }
                }
                else
                {
                    if (navigationEntry.CurrentValue != null)
                    {
                        var propertyEntityEntry = entityEntry.Reference(navigationEntry.Metadata.Name).TargetEntry;

                        if (navigationEntry.CurrentValue is ISoftDeletable<TUserIdentifierType> deletable && deletable.IsDeleted)
                        {
                            propertyEntityEntry.State = EntityState.Detached;
                            navigationEntry.CurrentValue = null;
                        }
                        else
                        {
                            if (!trackedEntities.Contains(navigationEntry.CurrentValue))
                            {
                                trackedEntities.Add(navigationEntry.CurrentValue);
                                RemoveSoftDeletableProperties(serverDbContext,
                                    propertyEntityEntry, trackedEntities);
                            }
                        }
                    }
                }
            }
        }

        private static bool IsSoftDeletableQueryFilterEnabled(SqlServerDbContext dbContext)
        {
            var softDeletableConfiguration = dbContext.ExecutionContext.Get<ISoftDeletableConfiguration>();
            return softDeletableConfiguration.IsEnabled && softDeletableConfiguration.IsQueryFilterEnabled;
        }

        private static Expression<Func<TEntity, bool>> CreateSoftDeletableQueryFilter<TEntity>(
            SqlServerDbContext dbContext)
            where TEntity : class, ISoftDeletable<TUserIdentifierType>
        {
            Expression<Func<TEntity, bool>> softDeletableQueryFilter =
                e => !e.IsDeleted || e.IsDeleted != IsSoftDeletableQueryFilterEnabled(dbContext);
            return softDeletableQueryFilter;
        }
    }
}
