using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using DataAccessClient.Configuration;
using DataAccessClient.EntityBehaviors;
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
        }

        protected internal ISoftDeletableConfiguration SoftDeletableConfiguration;
        protected internal IMultiTenancyConfiguration MultiTenancyConfiguration;
        protected internal ILocalizationConfiguration LocalizationConfiguration;
        protected Func<dynamic> UserIdentifierProvider;
        protected Func<dynamic> TenantIdentifierProvider;
        protected Func<dynamic> LocaleIdentifierProvider;

        protected internal bool IsSoftDeletableQueryFilterEnabled => SoftDeletableConfiguration.IsEnabled && SoftDeletableConfiguration.IsQueryFilterEnabled;
        protected internal bool IsTenantScopableQueryFilterEnabled => MultiTenancyConfiguration.IsQueryFilterEnabled;
        protected internal bool IsLocalizationQueryFilterEnabled => LocalizationConfiguration.IsQueryFilterEnabled;

        protected TUserIdentifierType? CurrentUserIdentifier<TUserIdentifierType>()
            where TUserIdentifierType : struct
            => UserIdentifierProvider();

        protected TTenantIdentifierType? CurrentTenantIdentifier<TTenantIdentifierType>()
            where TTenantIdentifierType : struct
            => TenantIdentifierProvider();

        protected TLocaleIdentifierType CurrentLocaleIdentifier<TLocaleIdentifierType>()
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
            var configureEntityBehaviorIIdentifiable = typeof(ModelBuilderExtensions).GetTypeInfo().DeclaredMethods
                .Single(m => m.Name == nameof(ModelBuilderExtensions.ConfigureEntityBehaviorIIdentifiable));
            var configureEntityBehaviorICreatable = typeof(ModelBuilderExtensions).GetTypeInfo().DeclaredMethods
                .Single(m => m.Name == nameof(ModelBuilderExtensions.ConfigureEntityBehaviorICreatable));
            var configureEntityBehaviorIModifiable = typeof(ModelBuilderExtensions).GetTypeInfo().DeclaredMethods
                .Single(m => m.Name == nameof(ModelBuilderExtensions.ConfigureEntityBehaviorIModifiable));
            var configureEntityBehaviorISoftDeletable = typeof(ModelBuilderExtensions).GetTypeInfo().DeclaredMethods
                .Single(m => m.Name == nameof(ModelBuilderExtensions.ConfigureEntityBehaviorISoftDeletable));
            var configureEntityBehaviorITenantScopable = typeof(ModelBuilderExtensions).GetTypeInfo().DeclaredMethods
                .Single(m => m.Name == nameof(ModelBuilderExtensions.ConfigureEntityBehaviorITenantScopable));
            var configureEntityBehaviorILocalizable = typeof(ModelBuilderExtensions).GetTypeInfo().DeclaredMethods
                .Single(m => m.Name == nameof(ModelBuilderExtensions.ConfigureEntityBehaviorILocalizable));
            var configureEntityBehaviorIRowVersionable = typeof(ModelBuilderExtensions).GetTypeInfo().DeclaredMethods
                .Single(m => m.Name == nameof(ModelBuilderExtensions.ConfigureEntityBehaviorIRowVersionable));
            var configureEntityBehaviorITranslatable = typeof(ModelBuilderExtensions).GetTypeInfo().DeclaredMethods
                .Single(m => m.Name == nameof(ModelBuilderExtensions.ConfigureEntityBehaviorITranslatable));
            var configureEntityBehaviorTranslatedProperties = typeof(ModelBuilderExtensions).GetTypeInfo().DeclaredMethods
                .Single(m => m.Name == nameof(ModelBuilderExtensions.ConfigureEntityBehaviorTranslatedProperties));
            var configureHasUtcDateTimeProperties = typeof(ModelBuilderExtensions).GetTypeInfo().DeclaredMethods
                .Single(m => m.Name == nameof(ModelBuilderExtensions.ConfigureHasUtcDateTimeProperties));

            var createSoftDeletableQueryFilter = GetType().GetMethod(nameof(CreateSoftDeletableQueryFilter),
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (createSoftDeletableQueryFilter == null)
            {
                throw new InvalidOperationException(
                    $"Can not find method {nameof(CreateSoftDeletableQueryFilter)} on class {GetType().FullName}");
            }

            var createTenantScopableQueryFilter = GetType().GetMethod(nameof(CreateTenantScopableQueryFilter),
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (createTenantScopableQueryFilter == null)
            {
                throw new InvalidOperationException(
                    $"Can not find method {nameof(CreateTenantScopableQueryFilter)} on class {GetType().FullName}");
            }

            var createLocalizableQueryFilter = GetType().GetMethod(nameof(CreateLocalizationQueryFilter),
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (createLocalizableQueryFilter == null)
            {
                throw new InvalidOperationException(
                    $"Can not find method {nameof(CreateLocalizationQueryFilter)} on class {GetType().FullName}");
            }

            var args = new object[] {modelBuilder};

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

            foreach (var entityType in entityTypes)
            {
                var entityInterfaces = entityType.ClrType.GetInterfaces();

                if (entityInterfaces.Any(
                    x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IIdentifiable<>)))
                {
                    var identifierType = entityType.ClrType.GetInterface(typeof(IIdentifiable<>).Name)
                        .GenericTypeArguments[0];
                    configureEntityBehaviorIIdentifiable.MakeGenericMethod(entityType.ClrType, identifierType)
                        .Invoke(null, args);
                }

                if (entityInterfaces.Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(ICreatable<>)))
                {
                    var identifierType = entityType.ClrType.GetInterface(typeof(ICreatable<>).Name)
                        .GenericTypeArguments[0];
                    configureEntityBehaviorICreatable.MakeGenericMethod(entityType.ClrType, identifierType)
                        .Invoke(null, args);
                }

                if (entityInterfaces.Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IModifiable<>)))
                {
                    var identifierType = entityType.ClrType.GetInterface(typeof(IModifiable<>).Name)
                        .GenericTypeArguments[0];
                    configureEntityBehaviorIModifiable.MakeGenericMethod(entityType.ClrType, identifierType)
                        .Invoke(null, args);
                }

                if (entityInterfaces.Any(x =>
                    x.IsGenericType && x.GetGenericTypeDefinition() == typeof(ISoftDeletable<>)))
                {
                    var identifierType = entityType.ClrType.GetInterface(typeof(ISoftDeletable<>).Name)
                        .GenericTypeArguments[0];

                    var softDeletableQueryFilterMethod =
                        createSoftDeletableQueryFilter.MakeGenericMethod(entityType.ClrType, identifierType);
                    var softDeletableQueryFilter = softDeletableQueryFilterMethod.Invoke(this, null);

                    configureEntityBehaviorISoftDeletable.MakeGenericMethod(entityType.ClrType, identifierType)
                        .Invoke(null, new[] {modelBuilder, softDeletableQueryFilter});
                }

                if (entityInterfaces.Any(x =>
                    x.IsGenericType && x.GetGenericTypeDefinition() == typeof(ITenantScopable<>)))
                {
                    var identifierType = entityType.ClrType.GetInterface(typeof(ITenantScopable<>).Name)
                        .GenericTypeArguments[0];

                    var tenantScopableQueryFilterMethod =
                        createTenantScopableQueryFilter.MakeGenericMethod(entityType.ClrType, identifierType);
                    var tenantScopableQueryFilter = tenantScopableQueryFilterMethod.Invoke(this, null);

                    configureEntityBehaviorITenantScopable.MakeGenericMethod(entityType.ClrType, identifierType)
                        .Invoke(null, new[] { modelBuilder, tenantScopableQueryFilter });
                }

                if (entityInterfaces.Any(x =>
                    x.IsGenericType && x.GetGenericTypeDefinition() == typeof(ILocalizable<>)))
                {
                    var identifierType = entityType.ClrType.GetInterface(typeof(ILocalizable<>).Name)
                        .GenericTypeArguments[0];

                    var localizableQueryFilterMethod =
                        createLocalizableQueryFilter.MakeGenericMethod(entityType.ClrType, identifierType);
                    var localizableQueryFilter = localizableQueryFilterMethod.Invoke(this, null);

                    configureEntityBehaviorILocalizable.MakeGenericMethod(entityType.ClrType, identifierType)
                        .Invoke(null, new[] { modelBuilder, localizableQueryFilter });
                }

                if (entityInterfaces.Any(x => !x.IsGenericType && x == typeof(IRowVersionable)))
                {
                    configureEntityBehaviorIRowVersionable.MakeGenericMethod(entityType.ClrType).Invoke(null, args);
                }

                if (entityInterfaces.Any(x =>
                    x.IsGenericType && x.GetGenericTypeDefinition() == typeof(ITranslatable<,,>)))
                {
                    var entityTranslationType = entityType.ClrType.GetInterface(typeof(ITranslatable<,,>).Name)
                        .GenericTypeArguments[0];
                    var identifierType = entityType.ClrType.GetInterface(typeof(ITranslatable<,,>).Name)
                        .GenericTypeArguments[1];
                    var localeType = entityType.ClrType.GetInterface(typeof(ITranslatable<,,>).Name)
                        .GenericTypeArguments[2];

                    configureEntityBehaviorITranslatable
                        .MakeGenericMethod(entityType.ClrType, entityTranslationType, identifierType, localeType)
                        .Invoke(null, args);
                }

                configureEntityBehaviorTranslatedProperties.MakeGenericMethod(entityType.ClrType).Invoke(null, args);

                configureHasUtcDateTimeProperties
                    .MakeGenericMethod(entityType.ClrType)
                    .Invoke(null, new object[] {modelBuilder, utcDateTimeValueConverter});
            }

            base.OnModelCreating(modelBuilder);
        }

        protected internal Expression<Func<TEntity, bool>> CreateSoftDeletableQueryFilter<TEntity,
            TUserIdentifierType>()
            where TEntity : class, ISoftDeletable<TUserIdentifierType>
            where TUserIdentifierType : struct
        {
            Expression<Func<TEntity, bool>> softDeletableQueryFilter =
                e => !e.IsDeleted || e.IsDeleted != IsSoftDeletableQueryFilterEnabled;
            return softDeletableQueryFilter;
        }

        protected internal Expression<Func<TEntity, bool>> CreateTenantScopableQueryFilter<TEntity,
            TTenantIdentifierType>()
            where TEntity : class, ITenantScopable<TTenantIdentifierType>
            where TTenantIdentifierType : struct
        {
            Expression<Func<TEntity, bool>> tenantScopableQueryFilter = e =>
                e.TenantId.Equals(CurrentTenantIdentifier<TTenantIdentifierType>()) ||
                e.TenantId.Equals(CurrentTenantIdentifier<TTenantIdentifierType>()) ==
                IsTenantScopableQueryFilterEnabled;

            return tenantScopableQueryFilter;
        }

        protected internal Expression<Func<TEntity, bool>> CreateLocalizationQueryFilter<TEntity,
            TLocaleIdentifierType>()
            where TEntity : class, ILocalizable<TLocaleIdentifierType>
            where TLocaleIdentifierType : IConvertible
        {
            Expression<Func<TEntity, bool>> tenantScopableQueryFilter = e =>
                e.LocaleId.Equals(CurrentLocaleIdentifier<TLocaleIdentifierType>()) ||
                e.LocaleId.Equals(CurrentLocaleIdentifier<TLocaleIdentifierType>()) ==
                IsLocalizationQueryFilterEnabled;

            return tenantScopableQueryFilter;
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
