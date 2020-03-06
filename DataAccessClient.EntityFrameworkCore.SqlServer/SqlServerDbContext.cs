using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using DataAccessClient.EntityBehaviors;
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
        }

        internal SqlServerDbContextExecutionContext ExecutionContext { get; private set; }

        private readonly Action _dbContextResetStateMethod;
        private readonly Func<CancellationToken, Task> _dbContextResetStateAsyncMethod;
        private readonly Action<DbContextPoolConfigurationSnapshot> _dbContextResurrectMethod;

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
            ExecutionContext = null;
        }

        #endregion

        internal void Initialize(SqlServerDbContextExecutionContext executionContext)
        {
            ExecutionContext = executionContext;
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

            foreach (var entityType in entityTypes)
            {
                foreach (var entityBehaviorConfiguration in DataAccessClientOptionsExtension.EntityBehaviors)
                {
                    entityBehaviorConfiguration.OnModelCreating(modelBuilder, this, entityType.ClrType);
                }
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

                foreach (var entityBehaviorConfiguration in DataAccessClientOptionsExtension.EntityBehaviors)
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
                foreach (var entityBehaviorConfiguration in DataAccessClientOptionsExtension.EntityBehaviors)
                {
                    entityBehaviorConfiguration.OnAfterSaveChanges(this);
                }
            });
        }
    }
}
