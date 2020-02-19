using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using DataAccessClient.EntityBehaviors;
using DataAccessClient.Exceptions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace DataAccessClient.EntityFrameworkCore.SqlServer
{
    public abstract class SqlServerDbContext<TIdentifierType> : DbContext where TIdentifierType : struct
    {
        private IUserIdentifierProvider<TIdentifierType> _userIdentifierProvider;
        private ITenantIdentifierProvider<TIdentifierType> _tenantIdentifierProvider;
        private ISoftDeletableConfiguration _softDeletableConfiguration;
        private IMultiTenancyConfiguration<TIdentifierType> _multiTenancyConfiguration;

        protected SqlServerDbContext(DbContextOptions options)
            : base(options)
        {
            // this is needed to support creating new pooled dbcontexts
            _userIdentifierProvider = this.GetService<IUserIdentifierProvider<TIdentifierType>>();
            _tenantIdentifierProvider = this.GetService<ITenantIdentifierProvider<TIdentifierType>>();
            _softDeletableConfiguration = this.GetService<ISoftDeletableConfiguration>();
            _multiTenancyConfiguration = this.GetService<IMultiTenancyConfiguration<TIdentifierType>>();
        }

        // for testing purpose
        // ReSharper disable once UnusedMember.Global
        // ReSharper disable once RedundantBaseConstructorCall
        protected internal SqlServerDbContext() : base()
        {
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
            var args = new object[] {modelBuilder};

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
                    RequireUserIdentifierProvider(typeof(ICreatable<>));

                    var identifierType = entityType.ClrType.GetInterface(typeof(ICreatable<>).Name)
                        .GenericTypeArguments[0];
                    configureEntityBehaviorICreatable.MakeGenericMethod(entityType.ClrType, identifierType)
                        .Invoke(null, args);
                }

                if (entityInterfaces.Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IModifiable<>)))
                {
                    RequireUserIdentifierProvider(typeof(IModifiable<>));

                    var identifierType = entityType.ClrType.GetInterface(typeof(IModifiable<>).Name)
                        .GenericTypeArguments[0];
                    configureEntityBehaviorIModifiable.MakeGenericMethod(entityType.ClrType, identifierType)
                        .Invoke(null, args);
                }

                if (entityInterfaces.Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(ISoftDeletable<>)))
                {
                    RequireUserIdentifierProvider(typeof(ISoftDeletable<>));
                    RequireSoftDeletableConfiguration(typeof(ISoftDeletable<>));

                    var identifierType = entityType.ClrType.GetInterface(typeof(ISoftDeletable<>).Name)
                        .GenericTypeArguments[0];

                    var softDeletableQueryFilterMethod = createSoftDeletableQueryFilter.MakeGenericMethod(entityType.ClrType, identifierType);
                    var softDeletableQueryFilter = softDeletableQueryFilterMethod.Invoke(this, null);

                    configureEntityBehaviorISoftDeletable.MakeGenericMethod(entityType.ClrType, identifierType)
                        .Invoke(null, new [] { modelBuilder, softDeletableQueryFilter });
                }

                if (entityInterfaces.Any(x =>
                    x.IsGenericType && x.GetGenericTypeDefinition() == typeof(ITenantScopable<>)))
                {
                    RequireTenantIdentifierProvider(typeof(ITenantScopable<>));
                    RequireMultiTenancyConfiguration(typeof(ITenantScopable<>));

                    var identifierType = entityType.ClrType.GetInterface(typeof(ITenantScopable<>).Name)
                        .GenericTypeArguments[0];

                    var tenantScopableQueryFilterMethod = createTenantScopableQueryFilter.MakeGenericMethod(entityType.ClrType, identifierType);
                        var tenantScopableQueryFilter = tenantScopableQueryFilterMethod.Invoke(this, null);

                    configureEntityBehaviorITenantScopable.MakeGenericMethod(entityType.ClrType, identifierType)
                        .Invoke(null, new [] { modelBuilder, tenantScopableQueryFilter });
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
                    .Invoke(null, new object[] {modelBuilder, utcDateTimeValueConverter});
            }

            base.OnModelCreating(modelBuilder);
        }

        private void RequireUserIdentifierProvider(Type entityBehaviorType)
        {
            if (_userIdentifierProvider == null)
            {
                // this is needed to support creating first pooled dbcontext
                _userIdentifierProvider = this.GetService<IUserIdentifierProvider<TIdentifierType>>();
                if (_userIdentifierProvider == null)
                {
                    throw new InvalidOperationException(
                        $"No DI registration found for type '{typeof(IUserIdentifierProvider<TIdentifierType>).FullName}' while there is an entity with EntityBehavior '{entityBehaviorType.FullName}', please register with LifeTime Singleton in Dependency Injection");
                }
            }
        }

        private void RequireTenantIdentifierProvider(Type entityBehaviorType)
        {
            if (_tenantIdentifierProvider == null)
            {
                // this is needed to support creating first pooled dbcontext
                _tenantIdentifierProvider = this.GetService<ITenantIdentifierProvider<TIdentifierType>>();
                if (_tenantIdentifierProvider == null)
                {
                    throw new InvalidOperationException(
                        $"No DI registration found for type '{typeof(ITenantIdentifierProvider<TIdentifierType>).FullName}' while there is an entity with EntityBehavior '{entityBehaviorType.FullName}', please register with LifeTime Singleton in Dependency Injection");
                }
            }
        }

        private void RequireSoftDeletableConfiguration(Type entityBehaviorType)
        {
            if (_softDeletableConfiguration == null)
            {
                // this is needed to support creating first pooled dbcontext
                _softDeletableConfiguration = this.GetService<ISoftDeletableConfiguration>();
                if (_softDeletableConfiguration == null)
                {
                    throw new InvalidOperationException(
                        $"No DI registration found for type '{typeof(ISoftDeletableConfiguration).FullName}' while there is an entity with EntityBehavior '{entityBehaviorType.FullName}', please register with LifeTime Singleton in Dependency Injection");
                }
            }
        }

        private void RequireMultiTenancyConfiguration(Type entityBehaviorType)
        {
            if (_multiTenancyConfiguration == null)
            {
                // this is needed to support creating first pooled dbcontext
                _multiTenancyConfiguration = this.GetService<IMultiTenancyConfiguration<TIdentifierType>>();
                if (_multiTenancyConfiguration == null)
                {
                    throw new InvalidOperationException(
                        $"No DI registration found for type '{typeof(IMultiTenancyConfiguration<TIdentifierType>).FullName}' while there is an entity with EntityBehavior '{entityBehaviorType.FullName}', please register an Singleton instance via Dependency Injection");
                }
            }
        }

        protected Expression<Func<TEntity, bool>> CreateSoftDeletableQueryFilter<TEntity, TUserIdentifierType>()
            where TEntity : class, ISoftDeletable<TUserIdentifierType>
            where TUserIdentifierType : struct
        {
            Expression<Func<TEntity, bool>> softDeletableQueryFilter =
                e => !_softDeletableConfiguration.IsEnabled || !e.IsDeleted;
            return softDeletableQueryFilter;
        }

        protected Expression<Func<TEntity, bool>> CreateTenantScopableQueryFilter<TEntity, TTenantIdentifierType>()
            where TEntity : class, ITenantScopable<TTenantIdentifierType>
            where TTenantIdentifierType : struct
        {
            Expression<Func<TEntity, bool>> tenantScopableQueryFilter = e =>
                (_multiTenancyConfiguration.IsEnabled && e.TenantId.Equals(_multiTenancyConfiguration.CurrentTenantId))
                || !_multiTenancyConfiguration.IsEnabled;

            return tenantScopableQueryFilter;
        }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            var authenticatedUserIdentifier = _userIdentifierProvider.ExecuteAsync().Result;

            OnBeforeSaveChanges(authenticatedUserIdentifier);

            try
            {
                var result = base.SaveChanges(acceptAllChangesOnSuccess);
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
            var authenticatedUser = await _userIdentifierProvider.ExecuteAsync();

            OnBeforeSaveChanges(authenticatedUser);
            try
            {
                var result = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
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

        private void OnBeforeSaveChanges(TIdentifierType authenticatedUserIdentifier)
        {
            var originalCascadeDeleteTiming = ChangeTracker.CascadeDeleteTiming;
            var originalDeleteOrphansTiming = ChangeTracker.DeleteOrphansTiming;
            try
            {
                ChangeTracker.CascadeDeleteTiming = CascadeTiming.OnSaveChanges;
                ChangeTracker.DeleteOrphansTiming = CascadeTiming.OnSaveChanges;
                foreach (var entityEntry in ChangeTracker.Entries<IRowVersionable>())
                {
                    var rowVersionProperty = entityEntry.Property(u => u.RowVersion);
                    var rowVersion = rowVersionProperty.CurrentValue;
                    //https://github.com/aspnet/EntityFramework/issues/4512
                    rowVersionProperty.OriginalValue = rowVersion;
                }

                if (_softDeletableConfiguration.IsEnabled)
                {
                    foreach (var entityEntry in ChangeTracker.Entries<ISoftDeletable<TIdentifierType>>()
                        .Where(c => c.State == EntityState.Deleted))
                    {
                        entityEntry.State = EntityState.Unchanged;

                        entityEntry.Entity.IsDeleted = true;
                        entityEntry.Entity.DeletedById = authenticatedUserIdentifier;
                        entityEntry.Entity.DeletedOn = DateTime.UtcNow;
                        entityEntry.Member(nameof(ISoftDeletable<TIdentifierType>.IsDeleted)).IsModified = true;
                        entityEntry.Member(nameof(ISoftDeletable<TIdentifierType>.DeletedById)).IsModified = true;
                        entityEntry.Member(nameof(ISoftDeletable<TIdentifierType>.DeletedOn)).IsModified = true;
                    }
                }

                foreach (var entityEntry in ChangeTracker.Entries<ITenantScopable<TIdentifierType>>()
                    .Where(c => c.State == EntityState.Added))
                {
                    var tenantId = entityEntry.Entity.TenantId;
                    if (tenantId.Equals(default(TIdentifierType)))
                    {
                        if (_multiTenancyConfiguration.CurrentTenantId.HasValue)
                        {
                            entityEntry.Entity.TenantId = _multiTenancyConfiguration.CurrentTenantId.Value;
                        }
                        else
                        {
                            throw new InvalidOperationException($"CurrentTenantId is needed for new entity of type '{entityEntry.Entity.GetType().FullName}', but the '{typeof(IMultiTenancyConfiguration<>).FullName}' does not have one at this moment");
                        }
                    }
                }

                foreach (var entityEntry in ChangeTracker.Entries<ICreatable<TIdentifierType>>()
                    .Where(c => c.State == EntityState.Added))
                {
                    entityEntry.Entity.CreatedById = authenticatedUserIdentifier;
                    entityEntry.Entity.CreatedOn = DateTime.UtcNow;
                }

                foreach (var entityEntry in ChangeTracker.Entries<IModifiable<TIdentifierType>>()
                    .Where(c => c.State == EntityState.Modified))
                {
                    entityEntry.Entity.ModifiedById = authenticatedUserIdentifier;
                    entityEntry.Entity.ModifiedOn = DateTime.UtcNow;
                }
            }
            finally
            {
                ChangeTracker.CascadeDeleteTiming = originalCascadeDeleteTiming;
                ChangeTracker.DeleteOrphansTiming = originalDeleteOrphansTiming;
            }
        }

        public void Reset()
        {
            var originalCascadeDeleteTiming = ChangeTracker.CascadeDeleteTiming;
            var originalDeleteOrphansTiming = ChangeTracker.DeleteOrphansTiming;
            try
            {
                ChangeTracker.CascadeDeleteTiming = CascadeTiming.OnSaveChanges;
                ChangeTracker.DeleteOrphansTiming = CascadeTiming.OnSaveChanges;

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
            }
            finally
            {
                ChangeTracker.CascadeDeleteTiming = originalCascadeDeleteTiming;
                ChangeTracker.DeleteOrphansTiming = originalDeleteOrphansTiming;
            }
        }
    }
}

