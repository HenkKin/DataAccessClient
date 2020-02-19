using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
                e => !_softDeletableConfiguration.IsQueryFilterEnabled || !e.IsDeleted;
            return softDeletableQueryFilter;
        }

        protected Expression<Func<TEntity, bool>> CreateTenantScopableQueryFilter<TEntity, TTenantIdentifierType>()
            where TEntity : class, ITenantScopable<TTenantIdentifierType>
            where TTenantIdentifierType : struct
        {
            Expression<Func<TEntity, bool>> tenantScopableQueryFilter = e =>
                (_multiTenancyConfiguration.IsQueryFilterEnabled && e.TenantId.Equals(_multiTenancyConfiguration.CurrentTenantId))
                || !_multiTenancyConfiguration.IsQueryFilterEnabled;

            return tenantScopableQueryFilter;
        }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            var authenticatedUserIdentifier = _userIdentifierProvider.ExecuteAsync().Result;

            OnBeforeSaveChanges(authenticatedUserIdentifier);

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
            var authenticatedUserIdentifier = await _userIdentifierProvider.ExecuteAsync();

            OnBeforeSaveChanges(authenticatedUserIdentifier);
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

        private void OnBeforeSaveChanges(TIdentifierType authenticatedUserIdentifier)
        {
            using var withCascadeTimingOnSaveChanges = new WithCascadeTimingOnSaveChanges(this);
            withCascadeTimingOnSaveChanges.Run(() =>
            {
                var saveChangesTime = DateTime.UtcNow;

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

                        var entity = entityEntry.Entity;
                        var entityIsSoftDeleted = entity.IsDeleted && entity.DeletedById.HasValue && entity.DeletedOn.HasValue;

                        if (entityIsSoftDeleted)
                        {
                            continue;
                        }

                        SetOwnedEntitiesToUnchanged(entityEntry);

                        entityEntry.Entity.IsDeleted = true;
                        entityEntry.Entity.DeletedById = authenticatedUserIdentifier;
                        entityEntry.Entity.DeletedOn = saveChangesTime;
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
                    entityEntry.Entity.CreatedOn = saveChangesTime;
                }

                foreach (var entityEntry in ChangeTracker.Entries<IModifiable<TIdentifierType>>()
                    .Where(c => c.State == EntityState.Modified))
                {
                    entityEntry.Entity.ModifiedById = authenticatedUserIdentifier;
                    entityEntry.Entity.ModifiedOn = saveChangesTime;
                }
            });
        }

        private void OnAfterSaveChanges()
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
                            if (collectionItem is ISoftDeletable<TIdentifierType> deletable && deletable.IsDeleted)
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

                        if (navigationEntry.CurrentValue is ISoftDeletable<TIdentifierType> deletable && deletable.IsDeleted)
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

        private void SetOwnedEntitiesToUnchanged(EntityEntry entityEntry)
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

