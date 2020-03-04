using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using DataAccessClient.Configuration;
using DataAccessClient.EntityBehaviors;
using DataAccessClient.EntityFrameworkCore.SqlServer.Configuration.EntityBehaviors;
using DataAccessClient.EntityFrameworkCore.SqlServer.Infrastructure;
using DataAccessClient.Exceptions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;

namespace DataAccessClient.EntityFrameworkCore.SqlServer
{
    public abstract class SqlServerDbContext : DbContext, IDbContextPoolable
    {
        private static readonly MethodInfo DbContextResetStateMethodInfo;
        private static readonly MethodInfo DbContextResetStateAsyncMethodInfo;
        private static readonly MethodInfo DbContextResurrectMethodInfo;
        private static readonly MethodInfo OnBeforeSaveChangesMethodInfo;
        private static readonly MethodInfo OnAfterSaveChangesMethodInfo;
        private static readonly MethodInfo ModelBuilderConfigureHasUtcDateTimeProperties;

        static SqlServerDbContext()
        {
            DbContextResetStateMethodInfo = typeof(DbContext).GetMethod(
                $"{typeof(IResettableService).FullName}.{nameof(IResettableService.ResetState)}",
                BindingFlags.Instance | BindingFlags.NonPublic);
            DbContextResetStateAsyncMethodInfo = typeof(DbContext).GetMethod(
                $"{typeof(IResettableService).FullName}.{nameof(IResettableService.ResetStateAsync)}",
                BindingFlags.Instance | BindingFlags.NonPublic);
            DbContextResurrectMethodInfo = typeof(DbContext).GetMethod(
                $"{typeof(IDbContextPoolable).FullName}.{nameof(IDbContextPoolable.Resurrect)}",
                BindingFlags.Instance | BindingFlags.NonPublic);
            OnBeforeSaveChangesMethodInfo = typeof(SqlServerDbContext).GetMethod(nameof(OnBeforeSaveChanges),
                BindingFlags.Instance | BindingFlags.NonPublic);
            OnAfterSaveChangesMethodInfo = typeof(SqlServerDbContext).GetMethod(nameof(OnAfterSaveChanges),
                BindingFlags.Instance | BindingFlags.NonPublic);
            ModelBuilderConfigureHasUtcDateTimeProperties = typeof(ModelBuilderExtensions).GetTypeInfo().DeclaredMethods
                .Single(m => m.Name == nameof(ModelBuilderExtensions.ConfigureHasUtcDateTimeProperties));
        }

        protected internal ISoftDeletableConfiguration SoftDeletableConfiguration;
        protected internal IMultiTenancyConfiguration MultiTenancyConfiguration;
        protected internal ILocalizationConfiguration LocalizationConfiguration;
        protected Func<dynamic> UserIdentifierProvider;
        protected Func<dynamic> TenantIdentifierProvider;
        protected Func<dynamic> LocaleIdentifierProvider;

        internal bool IsSoftDeletableQueryFilterEnabled => SoftDeletableConfiguration.IsEnabled && SoftDeletableConfiguration.IsQueryFilterEnabled;
        internal bool IsTenantScopableQueryFilterEnabled => MultiTenancyConfiguration.IsQueryFilterEnabled;
        internal bool IsLocalizationQueryFilterEnabled => LocalizationConfiguration.IsQueryFilterEnabled;

        internal TUserIdentifierType? CurrentUserIdentifier<TUserIdentifierType>()
            where TUserIdentifierType : struct
            => UserIdentifierProvider();

        internal TTenantIdentifierType? CurrentTenantIdentifier<TTenantIdentifierType>()
            where TTenantIdentifierType : struct
            => TenantIdentifierProvider();

        internal TLocaleIdentifierType CurrentLocaleIdentifier<TLocaleIdentifierType>()
            where TLocaleIdentifierType : IConvertible
            => LocaleIdentifierProvider();

        private readonly Action _dbContextResetStateMethod;
        private readonly Func<CancellationToken, Task> _dbContextResetStateAsyncMethod;
        private readonly Action<DbContextPoolConfigurationSnapshot> _dbContextResurrectMethod;
        private readonly MethodInfo _onBeforeSaveChangesMethod;
        private readonly MethodInfo _onAfterSaveChangesMethod;

        internal readonly DataAccessClientOptionsExtension DataAccessClientOptionsExtension;

        protected SqlServerDbContext(DbContextOptions options)
            : base(options)
        {
            _dbContextResetStateMethod = DbContextResetStateMethodInfo.CreateDelegate(typeof(Action), this) as Action;
            _dbContextResetStateAsyncMethod =
                DbContextResetStateAsyncMethodInfo.CreateDelegate(typeof(Func<CancellationToken, Task>), this) as
                    Func<CancellationToken, Task>;
            _dbContextResurrectMethod =
                DbContextResurrectMethodInfo.CreateDelegate(typeof(Action<DbContextPoolConfigurationSnapshot>), this) as
                    Action<DbContextPoolConfigurationSnapshot>;

            DataAccessClientOptionsExtension = options.FindExtension<DataAccessClientOptionsExtension>();

            _onBeforeSaveChangesMethod = OnBeforeSaveChangesMethodInfo.MakeGenericMethod(
                DataAccessClientOptionsExtension.UserIdentifierType,
                DataAccessClientOptionsExtension.TenantIdentifierType);
            _onAfterSaveChangesMethod = OnAfterSaveChangesMethodInfo.MakeGenericMethod(
                DataAccessClientOptionsExtension.UserIdentifierType,
                DataAccessClientOptionsExtension.TenantIdentifierType);
        }

        #region DbContextPooling

        // https://stackoverflow.com/questions/37310896/overriding-explicit-interface-implementations

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        void IDbContextPoolable.Resurrect(DbContextPoolConfigurationSnapshot configurationSnapshot)
        {
            if (_dbContextResurrectMethod != null)
            {
                _dbContextResurrectMethod.Invoke(configurationSnapshot);
            }
            else
            {
                throw new InvalidOperationException(
                    $"Cannot find method {nameof(IDbContextPoolable)}.{nameof(IDbContextPoolable.Resurrect)} on basetype DbContext of {GetType().FullName}");
            }
        }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        void IResettableService.ResetState()
        {
            ResetSqlServerDbContextState();

            if (_dbContextResetStateMethod != null)
            {
                _dbContextResetStateMethod.Invoke();
            }
            else
            {
                throw new InvalidOperationException(
                    $"Cannot find method {nameof(IResettableService)}.{nameof(IResettableService.ResetState)} on basetype DbContext of {GetType().FullName}");
            }
        }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        /// <param name="cancellationToken"> A <see cref="CancellationToken" /> to observe while waiting for the task to complete. </param>
        async Task IResettableService.ResetStateAsync(CancellationToken cancellationToken)
        {
            ResetSqlServerDbContextState();
            if (_dbContextResetStateAsyncMethod != null)
            {
                await _dbContextResetStateAsyncMethod.Invoke(cancellationToken);
            }
            else
            {
                throw new InvalidOperationException(
                    $"Cannot find method {nameof(IResettableService)}.{nameof(IResettableService.ResetState)} on basetype DbContext of {GetType().FullName}");
            }
        }

        protected virtual void ResetSqlServerDbContextState()
        {
            UserIdentifierProvider = null;
            TenantIdentifierProvider = null;
            LocaleIdentifierProvider = null;
            SoftDeletableConfiguration = null;
            MultiTenancyConfiguration = null;
            LocalizationConfiguration = null;
        }

        #endregion

        internal void Initialize(
            Func<dynamic> userIdentifierProvider,
            Func<dynamic> tenantIdentifierProvider,
            Func<dynamic> localeIdentifierProvider,
            ISoftDeletableConfiguration softDeletableConfiguration,
            IMultiTenancyConfiguration multiTenancyConfiguration,
            ILocalizationConfiguration localizationConfiguration)
        {
            UserIdentifierProvider = userIdentifierProvider;
            TenantIdentifierProvider = tenantIdentifierProvider;
            LocaleIdentifierProvider = localeIdentifierProvider;
            SoftDeletableConfiguration = softDeletableConfiguration;
            MultiTenancyConfiguration = multiTenancyConfiguration;
            LocalizationConfiguration = localizationConfiguration;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var ignoredEntityTypes = new[]
            {
                typeof(PropertyTranslation<>),
                typeof(TranslatedProperty<>),
            };

            var entityTypes = modelBuilder.Model.GetEntityTypes()
                .Where(p => (!p.ClrType.IsGenericType && !ignoredEntityTypes.Contains(p.ClrType)) ||
                            (p.ClrType.IsGenericType && !ignoredEntityTypes.Contains(p.ClrType.GetGenericTypeDefinition())))
                .ToList();

            var utcDateTimeValueConverter = new UtcDateTimeValueConverter();

            var entityBehaviorConfigurations = new List<IEntityBehaviorConfiguration>
            {
                new IdentifiableEntityBehaviorConfiguration(),
                new CreatableEntityBehaviorConfiguration(),
                new ModifiableEntityBehaviorConfiguration(),
                new SoftDeletableEntityBehaviorConfiguration(),
                new RowVersionableEntityBehaviorConfiguration(),
                new LocalizableEntityBehaviorConfiguration(),
                new TenantScopeableEntityBehaviorConfiguration(),
                new TranslatableEntityBehaviorConfiguration()
            };

            foreach (var entityType in entityTypes)
            {
                foreach (var entityBehaviorConfiguration in entityBehaviorConfigurations)
                {
                    entityBehaviorConfiguration.Execute(modelBuilder, this, entityType.ClrType);
                }

                ModelBuilderConfigureHasUtcDateTimeProperties
                    .MakeGenericMethod(entityType.ClrType)
                    .Invoke(null, new object[] {modelBuilder, utcDateTimeValueConverter});
            }

            base.OnModelCreating(modelBuilder);
        }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            _onBeforeSaveChangesMethod.Invoke(this, new object[] { });

            try
            {
                var result = base.SaveChanges(acceptAllChangesOnSuccess);
                _onAfterSaveChangesMethod.Invoke(this, new object[] { });
                return result;
            }
            catch (DbUpdateConcurrencyException exception)
            {
                throw new RowVersioningException(exception.Message, exception);
            }
            catch (DbUpdateException exception)
            {
                if (exception.InnerException != null)
                {
                    if (exception.InnerException is SqlException sqlException)
                    {
                        switch (sqlException.Number)
                        {
                            //case 2627:  // Unique constraint error
                            //case 547:   // Constraint check violation
                            case 2601: // Duplicated key row error
                                // Constraint violation exception
                                // A custom exception of yours for concurrency issues
                                throw new DuplicateKeyException(sqlException.Message, exception);
                            default:
                                // A custom exception of yours for other DB issues
                                throw;
                        }
                    }

                }

                throw;
            }
        }

        public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess,
            CancellationToken cancellationToken = new CancellationToken())
        {
            _onBeforeSaveChangesMethod.Invoke(this, new object[] { });

            try
            {
                var result = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
                _onAfterSaveChangesMethod.Invoke(this, new object[] { });
                return result;
            }
            catch (DbUpdateConcurrencyException exception)
            {
                throw new RowVersioningException(exception.Message, exception);
            }
            catch (DbUpdateException exception)
            {
                if (exception.InnerException != null)
                {
                    if (exception.InnerException is SqlException sqlException)
                    {
                        switch (sqlException.Number)
                        {
                            //case 2627:  // Unique constraint error
                            //case 547:   // Constraint check violation
                            case 2601: // Duplicated key row error
                                // Constraint violation exception
                                // A custom exception of yours for concurrency issues
                                throw new DuplicateKeyException(sqlException.Message, exception);
                            default:
                                // A custom exception of yours for other DB issues
                                throw;
                        }
                    }

                }

                throw;
            }
        }

        public void Reset()
        {
            using var withCascadeTimingOnSaveChanges = new WithCascadeTimingOnSaveChanges(this);
            withCascadeTimingOnSaveChanges.Run(() =>
            {
                var entries = ChangeTracker.Entries().Where(e => e.State != EntityState.Unchanged).ToArray();
                foreach (var entry in entries)
                {
                    switch (entry.State)
                    {
                        case EntityState.Modified:
                            entry.State = EntityState.Unchanged;
                            break;
                        case EntityState.Added:
                            entry.State = EntityState.Detached;
                            break;
                        case EntityState.Deleted:
                            entry.Reload();
                            break;
                    }
                }
            });
        }

        private void OnBeforeSaveChanges<TUserIdentifierType, TTenantIdentifierType>()
            where TUserIdentifierType : struct
            where TTenantIdentifierType : struct
        {
            using var withCascadeTimingOnSaveChanges = new WithCascadeTimingOnSaveChanges(this);
            withCascadeTimingOnSaveChanges.Run(() =>
            {
                var saveChangesTime = DateTime.UtcNow;
                var userIdentifier = CurrentUserIdentifier<TUserIdentifierType>();
                var tenantIdentifier = CurrentTenantIdentifier<TTenantIdentifierType>();

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
                        var entityIsSoftDeleted =
                            entity.IsDeleted && entity.DeletedById.HasValue && entity.DeletedOn.HasValue;

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
                        if (tenantIdentifier.HasValue)
                        {
                            entityEntry.Entity.TenantId = tenantIdentifier.Value;
                        }
                        else
                        {
                            throw new InvalidOperationException(
                                $"CurrentTenantId is needed for new entity of type '{entityEntry.Entity.GetType().FullName}', but the '{typeof(IMultiTenancyConfiguration).FullName}' does not have one at this moment");
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

        private void OnAfterSaveChanges<TUserIdentifierType, TTenantIdentifierType>()
            where TUserIdentifierType : struct
            where TTenantIdentifierType : struct
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
                        RemoveSoftDeletableProperties<TUserIdentifierType, TTenantIdentifierType>(dbEntityEntry,
                            trackedEntities);
                    }
                }
            });
        }

        private void RemoveSoftDeletableProperties<TUserIdentifierType, TTenantIdentifierType>(EntityEntry entityEntry,
            HashSet<object> trackedEntities)
            where TUserIdentifierType : struct
            where TTenantIdentifierType : struct
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
                                    RemoveSoftDeletableProperties<TUserIdentifierType, TTenantIdentifierType>(
                                        listItemDbEntityEntry, trackedEntities);
                                }
                            }
                        }

                        Type constructedType = typeof(Collection<>).MakeGenericType(navigationEntry.Metadata
                            .PropertyInfo.PropertyType.GenericTypeArguments);
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

                        if (navigationEntry.CurrentValue is ISoftDeletable<TUserIdentifierType> deletable &&
                            deletable.IsDeleted)
                        {
                            propertyEntityEntry.State = EntityState.Detached;
                            navigationEntry.CurrentValue = null;
                        }
                        else
                        {
                            if (!trackedEntities.Contains(navigationEntry.CurrentValue))
                            {
                                trackedEntities.Add(navigationEntry.CurrentValue);
                                RemoveSoftDeletableProperties<TUserIdentifierType, TTenantIdentifierType>(
                                    propertyEntityEntry, trackedEntities);
                            }
                        }
                    }
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
    }
}
