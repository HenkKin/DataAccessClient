using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using DataAccessClient.Configuration;
using DataAccessClient.EntityBehaviors;
using DataAccessClient.Providers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

// ReSharper disable StaticMemberInGenericType

namespace DataAccessClient.EntityFrameworkCore.SqlServer
{
    public abstract class SqlServerDbContext<TUserIdentifierType, TTenantIdentifierType> : SqlServerDbContextBase
        where TUserIdentifierType : struct
        where TTenantIdentifierType : struct
    {
        protected internal IUserIdentifierProvider<TUserIdentifierType> UserIdentifierProvider;
        protected internal ITenantIdentifierProvider<TTenantIdentifierType> TenantIdentifierProvider;

        private TTenantIdentifierType? CurrentTenantIdentifier => TenantIdentifierProvider.Execute();
        private TUserIdentifierType? CurrentUserIdentifier => UserIdentifierProvider.Execute();

        protected SqlServerDbContext(DbContextOptions options)
            : base(options)
        {
        }

        // for testing purpose
        // ReSharper disable once UnusedMember.Global
        // ReSharper disable once RedundantBaseConstructorCall
        protected internal SqlServerDbContext() : base()//: this(new DbContextOptions<DbContext>())
        {
        }

        internal void Initialize(
            IUserIdentifierProvider<TUserIdentifierType> userIdentifierProvider,
            ITenantIdentifierProvider<TTenantIdentifierType> tenantIdentifierProvider,
            ISoftDeletableConfiguration softDeletableConfiguration,
            IMultiTenancyConfiguration multiTenancyConfiguration)
        {
            UserIdentifierProvider = userIdentifierProvider;
            TenantIdentifierProvider = tenantIdentifierProvider;
            SoftDeletableConfiguration = softDeletableConfiguration;
            MultiTenancyConfiguration = multiTenancyConfiguration;
        }

        protected override void ResetSqlServerDbContextState()
        {
            UserIdentifierProvider = null;
            TenantIdentifierProvider = null;

            base.ResetSqlServerDbContextState();
        }

        protected internal override Expression<Func<TEntity, bool>> CreateTenantScopableQueryFilter<TEntity, TEntityTenantIdentifierType>()
        {
            Expression<Func<TEntity, bool>> tenantScopableQueryFilter = e =>
                e.TenantId.Equals(CurrentTenantIdentifier) || e.TenantId.Equals(CurrentTenantIdentifier) == IsTenantScopableQueryFilterEnabled;

            return tenantScopableQueryFilter;
        }

        protected internal override void OnBeforeSaveChanges()
        {
            using var withCascadeTimingOnSaveChanges = new WithCascadeTimingOnSaveChanges(this);
            withCascadeTimingOnSaveChanges.Run(() =>
            {
                var saveChangesTime = DateTime.UtcNow;
                var userIdentifier = CurrentUserIdentifier;

                foreach (var entityEntry in ChangeTracker.Entries<IRowVersionable>())
                {
                    var rowVersionProperty = entityEntry.Property(u => u.RowVersion);
                    var rowVersion = rowVersionProperty.CurrentValue;
                    //https://github.com/aspnet/EntityFramework/issues/4512
                    rowVersionProperty.OriginalValue = rowVersion;
                }

                if (SoftDeletableConfiguration.IsEnabled)
                {
                    foreach (var entityEntry in ChangeTracker.Entries<ISoftDeletable<TUserIdentifierType>>()
                        .Where(c => c.State == EntityState.Deleted))
                    {
                        entityEntry.State = EntityState.Unchanged;

                        var entity = entityEntry.Entity;
                        var entityIsSoftDeleted = entity.IsDeleted && entity.DeletedById.HasValue && entity.DeletedOn.HasValue;

                        if (entityIsSoftDeleted)
                        {
                            continue;
                        }

                        SetOwnedEntitiesToUnchanged(entityEntry);

                        entityEntry.Entity.IsDeleted = true;
                        entityEntry.Entity.DeletedById = userIdentifier;
                        entityEntry.Entity.DeletedOn = saveChangesTime;
                        entityEntry.Member(nameof(ISoftDeletable<TUserIdentifierType>.IsDeleted)).IsModified = true;
                        entityEntry.Member(nameof(ISoftDeletable<TUserIdentifierType>.DeletedById)).IsModified = true;
                        entityEntry.Member(nameof(ISoftDeletable<TUserIdentifierType>.DeletedOn)).IsModified = true;
                    }
                }

                foreach (var entityEntry in ChangeTracker.Entries<ITenantScopable<TTenantIdentifierType>>()
                    .Where(c => c.State == EntityState.Added))
                {
                    var tenantId = entityEntry.Entity.TenantId;
                    if (tenantId.Equals(default(TTenantIdentifierType)))
                    {
                        var currentTenantId = TenantIdentifierProvider.Execute();
                        if (currentTenantId.HasValue)
                        {
                            entityEntry.Entity.TenantId = currentTenantId.Value;
                        }
                        else
                        {
                            throw new InvalidOperationException($"CurrentTenantId is needed for new entity of type '{entityEntry.Entity.GetType().FullName}', but the '{typeof(IMultiTenancyConfiguration).FullName}' does not have one at this moment");
                        }
                    }
                }

                foreach (var entityEntry in ChangeTracker.Entries<ICreatable<TUserIdentifierType>>()
                    .Where(c => c.State == EntityState.Added))
                {
                    entityEntry.Entity.CreatedById = userIdentifier.GetValueOrDefault();
                    entityEntry.Entity.CreatedOn = saveChangesTime;
                }

                foreach (var entityEntry in ChangeTracker.Entries<IModifiable<TUserIdentifierType>>()
                    .Where(c => c.State == EntityState.Modified))
                {
                    entityEntry.Entity.ModifiedById = userIdentifier;
                    entityEntry.Entity.ModifiedOn = saveChangesTime;
                }
            });
        }

        protected internal override void OnAfterSaveChanges()
        {
            using var withCascadeTimingOnSaveChanges = new WithCascadeTimingOnSaveChanges(this);
            withCascadeTimingOnSaveChanges.Run(() =>
            {
                var trackedEntities = new HashSet<object>();
                foreach (var dbEntityEntry in ChangeTracker.Entries().ToArray())
                {
                    if (dbEntityEntry.Entity != null)
                    {
                        trackedEntities.Add(dbEntityEntry.Entity);
                        RemoveSoftDeletableProperties(dbEntityEntry, trackedEntities);
                    }
                }
            });
        }

        private void RemoveSoftDeletableProperties(EntityEntry entityEntry, HashSet<object> trackedEntities)
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
                            var listItemDbEntityEntry = ChangeTracker.Context.Entry(collectionItem);
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
                                    RemoveSoftDeletableProperties(listItemDbEntityEntry, trackedEntities);
                                }
                            }
                        }

                        Type constructedType = typeof(Collection<>).MakeGenericType(navigationEntry.Metadata.PropertyInfo.PropertyType.GenericTypeArguments);
                        IList col = (IList)Activator.CreateInstance(constructedType);
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
                                RemoveSoftDeletableProperties(propertyEntityEntry, trackedEntities);
                            }
                        }
                    }
                }
            }
        }

    }
}

