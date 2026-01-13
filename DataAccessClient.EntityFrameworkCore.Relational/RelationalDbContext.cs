using DataAccessClient.EntityBehaviors;
using DataAccessClient.EntityFrameworkCore.Relational.ExceptionHandling;
using DataAccessClient.EntityFrameworkCore.Relational.Infrastructure;
using DataAccessClient.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace DataAccessClient.EntityFrameworkCore.Relational
{
    public abstract class RelationalDbContext : DbContext, IResettableService
    {
        private static readonly MethodInfo DbContextResetStateMethodInfo;
        private static readonly MethodInfo DbContextResetStateAsyncMethodInfo;
        internal static ConcurrentBag<Type> RegisteredDbContextTypes = new ConcurrentBag<Type>();
        internal static ConcurrentDictionary<Type, List<Type>> RegisteredEntityTypesPerDbContexts = new ConcurrentDictionary<Type, List<Type>>();

        static RelationalDbContext()
        {
            DbContextResetStateMethodInfo = typeof(DbContext).GetMethod(
                $"{typeof(IResettableService).FullName}.{nameof(IResettableService.ResetState)}",
                BindingFlags.Instance | BindingFlags.NonPublic);
            DbContextResetStateAsyncMethodInfo = typeof(DbContext).GetMethod(
                $"{typeof(IResettableService).FullName}.{nameof(IResettableService.ResetStateAsync)}",
                BindingFlags.Instance | BindingFlags.NonPublic);
        }

        internal RelationalDbContextExecutionContext ExecutionContext { get; private set; }

        private readonly Action _dbContextResetStateMethod;
        private readonly Func<CancellationToken, Task> _dbContextResetStateAsyncMethod;

        internal readonly DataAccessClientOptionsExtension DataAccessClientOptionsExtension;

        protected RelationalDbContext(DbContextOptions options)
            : base(options)
        {
            _dbContextResetStateMethod = DbContextResetStateMethodInfo.CreateDelegate(typeof(Action), this) as Action;
            _dbContextResetStateAsyncMethod =
                DbContextResetStateAsyncMethodInfo.CreateDelegate(typeof(Func<CancellationToken, Task>), this) as
                    Func<CancellationToken, Task>;
            DataAccessClientOptionsExtension = options.FindExtension<DataAccessClientOptionsExtension>();
        }

        #region IResettableService  overrides to support DbContextPooling

        void IResettableService.ResetState()
        {
            ResetRelationalDbContextState();

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

        async Task IResettableService.ResetStateAsync(CancellationToken cancellationToken)
        {
            ResetRelationalDbContextState();
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

        protected virtual void ResetRelationalDbContextState()
        {
            ExecutionContext = null;
        }

        #endregion

        internal void Initialize(RelationalDbContextExecutionContext executionContext)
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

            var allEntityTypes = modelBuilder.Model.GetEntityTypes()
                .Where(p => (!p.ClrType.IsGenericType && !ignoredEntityTypes.Contains(p.ClrType)) ||
                            (p.ClrType.IsGenericType && !ignoredEntityTypes.Contains(p.ClrType.GetGenericTypeDefinition())))
                .ToList();
            RegisteredEntityTypesPerDbContexts.TryAdd(GetType(), allEntityTypes.Select(e => e.ClrType).ToList());
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
                var kind = SaveChangesDbUpdateExceptionHandler.Handle(exception, out var info);
                if (kind == DbErrorKind.DuplicateKey)
                    throw new DuplicateKeyException(info?.Message ?? "Duplicate key.", exception);

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
                    /*if (exception.InnerException is SqlException sqlException)
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
                    }*/

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
