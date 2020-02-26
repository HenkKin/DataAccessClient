using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using DataAccessClient.Configuration;
using DataAccessClient.EntityBehaviors;
using DataAccessClient.Exceptions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;

namespace DataAccessClient.EntityFrameworkCore.SqlServer
{
    public abstract class SqlServerDbContextBase : DbContext, IDbContextPoolable
    {
        private static readonly MethodInfo DbContextResetStateMethodInfo;
        private static readonly MethodInfo DbContextResetStateAsyncMethodInfo;
        private static readonly MethodInfo DbContextResurrectMethodInfo;

        static SqlServerDbContextBase()
        {
            DbContextResetStateMethodInfo = typeof(DbContext).GetMethod($"{typeof(IResettableService).FullName}.{nameof(IResettableService.ResetState)}",
                BindingFlags.Instance | BindingFlags.NonPublic);
            DbContextResetStateAsyncMethodInfo = typeof(DbContext).GetMethod($"{typeof(IResettableService).FullName}.{nameof(IResettableService.ResetStateAsync)}",
                BindingFlags.Instance | BindingFlags.NonPublic);
            DbContextResurrectMethodInfo = typeof(DbContext).GetMethod($"{typeof(IDbContextPoolable).FullName}.{nameof(IDbContextPoolable.Resurrect)}",
                BindingFlags.Instance | BindingFlags.NonPublic);
        }

        protected internal ISoftDeletableConfiguration SoftDeletableConfiguration;
        protected internal IMultiTenancyConfiguration MultiTenancyConfiguration;

        protected internal bool IsSoftDeletableQueryFilterEnabled => SoftDeletableConfiguration.IsEnabled && SoftDeletableConfiguration.IsQueryFilterEnabled;
        protected internal bool IsTenantScopableQueryFilterEnabled => MultiTenancyConfiguration.IsQueryFilterEnabled;


        private readonly Action _dbContextResetStateMethod;
        private readonly Func<CancellationToken, Task> _dbContextResetStateAsyncMethod;
        private readonly Action<DbContextPoolConfigurationSnapshot> _dbContextResurrectMethod;

        protected SqlServerDbContextBase(DbContextOptions options)
            : base(options)
        {
            _dbContextResetStateMethod = DbContextResetStateMethodInfo.CreateDelegate(typeof(Action), this) as Action;
            _dbContextResetStateAsyncMethod = DbContextResetStateAsyncMethodInfo.CreateDelegate(typeof(Func<CancellationToken, Task>), this) as Func<CancellationToken, Task>;
            _dbContextResurrectMethod = DbContextResurrectMethodInfo.CreateDelegate(typeof(Action<DbContextPoolConfigurationSnapshot>), this) as Action<DbContextPoolConfigurationSnapshot>;
        }

        // for testing purpose
        // ReSharper disable once UnusedMember.Global
        // ReSharper disable once RedundantBaseConstructorCall
        protected internal SqlServerDbContextBase() : base()//: this(new DbContextOptions<DbContext>())
        {
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
            SoftDeletableConfiguration = null;
            MultiTenancyConfiguration = null;
        }

        #endregion

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
            var configureEntityBehaviorIRowVersionable = typeof(ModelBuilderExtensions).GetTypeInfo().DeclaredMethods
                .Single(m => m.Name == nameof(ModelBuilderExtensions.ConfigureEntityBehaviorIRowVersionable));
            var configureEntityBehaviorITranslatable = typeof(ModelBuilderExtensions).GetTypeInfo().DeclaredMethods
                .Single(m => m.Name == nameof(ModelBuilderExtensions.ConfigureEntityBehaviorITranslatable));
            var configureEntityBehaviorTranslatedProperties = typeof(ModelBuilderExtensions).GetTypeInfo().DeclaredMethods
                .Single(m => m.Name == nameof(ModelBuilderExtensions.ConfigureEntityBehaviorTranslatedProperties));
            var configureHasUtcDateTimeProperties = typeof(ModelBuilderExtensions).GetTypeInfo().DeclaredMethods
                .Single(m => m.Name == nameof(ModelBuilderExtensions.ConfigureHasUtcDateTimeProperties));

            var createSoftDeletableQueryFilter = GetType().GetMethod(nameof(CreateSoftDeletableQueryFilter), BindingFlags.Instance | BindingFlags.NonPublic);
            if (createSoftDeletableQueryFilter == null)
            {
                throw new InvalidOperationException($"Can not find method {nameof(CreateSoftDeletableQueryFilter)} on class {GetType().FullName}");
            }
            var createTenantScopableQueryFilter = GetType().GetMethod(nameof(CreateTenantScopableQueryFilter), BindingFlags.Instance | BindingFlags.NonPublic);
            if (createTenantScopableQueryFilter == null)
            {
                throw new InvalidOperationException($"Can not find method {nameof(CreateTenantScopableQueryFilter)} on class {GetType().FullName}");
            }
            var args = new object[] { modelBuilder };

            var ignoredEntityTypes = new[]
            {
                typeof(PropertyTranslation),
                typeof(TranslatedProperty),
            };

            var entityTypes = modelBuilder.Model.GetEntityTypes()
                .Where(p => !ignoredEntityTypes.Contains(p.ClrType))
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

                if (entityInterfaces.Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(ISoftDeletable<>)))
                {
                    var identifierType = entityType.ClrType.GetInterface(typeof(ISoftDeletable<>).Name)
                        .GenericTypeArguments[0];

                    var softDeletableQueryFilterMethod = createSoftDeletableQueryFilter.MakeGenericMethod(entityType.ClrType, identifierType);
                    var softDeletableQueryFilter = softDeletableQueryFilterMethod.Invoke(this, null);

                    configureEntityBehaviorISoftDeletable.MakeGenericMethod(entityType.ClrType, identifierType)
                        .Invoke(null, new[] { modelBuilder, softDeletableQueryFilter });
                }

                if (entityInterfaces.Any(x =>
                    x.IsGenericType && x.GetGenericTypeDefinition() == typeof(ITenantScopable<>)))
                {
                    var identifierType = entityType.ClrType.GetInterface(typeof(ITenantScopable<>).Name)
                        .GenericTypeArguments[0];

                    var tenantScopableQueryFilterMethod = createTenantScopableQueryFilter.MakeGenericMethod(entityType.ClrType, identifierType);
                    var tenantScopableQueryFilter = tenantScopableQueryFilterMethod.Invoke(this, null);

                    configureEntityBehaviorITenantScopable.MakeGenericMethod(entityType.ClrType, identifierType)
                        .Invoke(null, new[] { modelBuilder, tenantScopableQueryFilter });
                }

                if (entityInterfaces.Any(x => !x.IsGenericType && x == typeof(IRowVersionable)))
                {
                    configureEntityBehaviorIRowVersionable.MakeGenericMethod(entityType.ClrType).Invoke(null, args);
                }

                if (entityInterfaces.Any(x =>
                    x.IsGenericType && x.GetGenericTypeDefinition() == typeof(ITranslatable<,>)))
                {
                    var entityTranslationType = entityType.ClrType.GetInterface(typeof(ITranslatable<,>).Name)
                        .GenericTypeArguments[0];
                    var identifierType = entityType.ClrType.GetInterface(typeof(ITranslatable<,>).Name)
                        .GenericTypeArguments[1];

                    configureEntityBehaviorITranslatable
                        .MakeGenericMethod(entityType.ClrType, entityTranslationType, identifierType)
                        .Invoke(null, args);
                }

                configureEntityBehaviorTranslatedProperties.MakeGenericMethod(entityType.ClrType).Invoke(null, args);

                configureHasUtcDateTimeProperties
                    .MakeGenericMethod(entityType.ClrType)
                    .Invoke(null, new object[] { modelBuilder, utcDateTimeValueConverter });
            }

            base.OnModelCreating(modelBuilder);
        }

        protected internal Expression<Func<TEntity, bool>> CreateSoftDeletableQueryFilter<TEntity, TEntityUserIdentifierType>()
            where TEntity : class, ISoftDeletable<TEntityUserIdentifierType>
            where TEntityUserIdentifierType : struct
        {
            Expression<Func<TEntity, bool>> softDeletableQueryFilter =
                e => !e.IsDeleted || e.IsDeleted != IsSoftDeletableQueryFilterEnabled;
            return softDeletableQueryFilter;
        }

        protected internal abstract Expression<Func<TEntity, bool>> CreateTenantScopableQueryFilter<TEntity,
            TEntityTenantIdentifierType>()
            where TEntity : class, ITenantScopable<TEntityTenantIdentifierType>
            where TEntityTenantIdentifierType : struct;

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            OnBeforeSaveChanges();

            try
            {
                var result = base.SaveChanges(acceptAllChangesOnSuccess);
                OnAfterSaveChanges();
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

        public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = new CancellationToken())
        {
            OnBeforeSaveChanges();
            try
            {
                var result = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
                OnAfterSaveChanges();
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

        protected internal abstract void OnBeforeSaveChanges();
        protected internal abstract void OnAfterSaveChanges();

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