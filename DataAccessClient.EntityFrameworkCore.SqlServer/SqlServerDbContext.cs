using System;
using System.Collections.Generic;
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
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;

namespace DataAccessClient.EntityFrameworkCore.SqlServer
{
    public abstract class SqlServerDbContext : DbContext, IDbContextPoolable
    {
        private static readonly MethodInfo DbContextResetStateMethodInfo;
        private static readonly MethodInfo DbContextResetStateAsyncMethodInfo;
        private static readonly MethodInfo DbContextResurrectMethodInfo;
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
        private readonly IReadOnlyList<IEntityBehaviorConfiguration> _entityBehaviorConfigurations;

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

            var entityBehaviorConfigurations = new List<IEntityBehaviorConfiguration>
            {
                new IdentifiableEntityBehaviorConfiguration(),
                CreateEntityBehaviorTypeInstance(typeof(CreatableEntityBehaviorConfiguration<>).MakeGenericType(DataAccessClientOptionsExtension.UserIdentifierType)),
                CreateEntityBehaviorTypeInstance(typeof(ModifiableEntityBehaviorConfiguration<>).MakeGenericType(DataAccessClientOptionsExtension.UserIdentifierType)),
                CreateEntityBehaviorTypeInstance(typeof(SoftDeletableEntityBehaviorConfiguration<>).MakeGenericType(DataAccessClientOptionsExtension.UserIdentifierType)),
                new RowVersionableEntityBehaviorConfiguration(),
                new LocalizableEntityBehaviorConfiguration(),
                CreateEntityBehaviorTypeInstance(typeof(TenantScopeableEntityBehaviorConfiguration<>).MakeGenericType(DataAccessClientOptionsExtension.TenantIdentifierType)),
                new TranslatableEntityBehaviorConfiguration()
            };

            if (DataAccessClientOptionsExtension.CustomEntityBehaviorsTypes.Any())
            {
                entityBehaviorConfigurations.AddRange(
                    DataAccessClientOptionsExtension.CustomEntityBehaviorsTypes.Select(CreateEntityBehaviorTypeInstance));
            }

            _entityBehaviorConfigurations = entityBehaviorConfigurations;
        }

        private static IEntityBehaviorConfiguration CreateEntityBehaviorTypeInstance(Type entityBehaviorType)
        {
            return (IEntityBehaviorConfiguration) Activator.CreateInstance(entityBehaviorType);
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

            foreach (var entityType in entityTypes)
            {
                foreach (var entityBehaviorConfiguration in _entityBehaviorConfigurations)
                {
                    entityBehaviorConfiguration.OnModelCreating(modelBuilder, this, entityType.ClrType);
                }

                ModelBuilderConfigureHasUtcDateTimeProperties
                    .MakeGenericMethod(entityType.ClrType)
                    .Invoke(null, new object[] {modelBuilder, utcDateTimeValueConverter});
            }

            base.OnModelCreating(modelBuilder);
        }

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

        public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess,
            CancellationToken cancellationToken = new CancellationToken())
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

        private void OnBeforeSaveChanges()
        {
            using var withCascadeTimingOnSaveChanges = new WithCascadeTimingOnSaveChanges(this);
            withCascadeTimingOnSaveChanges.Run(() =>
            {
                var saveChangesTime = DateTime.UtcNow;

                foreach (var entityBehaviorConfiguration in _entityBehaviorConfigurations)
                {
                    entityBehaviorConfiguration.OnBeforeSaveChanges(this, saveChangesTime);
                }
            });
        }

        private void OnAfterSaveChanges()
        {
            using var withCascadeTimingOnSaveChanges = new WithCascadeTimingOnSaveChanges(this);
            withCascadeTimingOnSaveChanges.Run(() =>
            {
                foreach (var entityBehaviorConfiguration in _entityBehaviorConfigurations)
                {
                    entityBehaviorConfiguration.OnAfterSaveChanges(this);
                }
            });
        }
    }
}
