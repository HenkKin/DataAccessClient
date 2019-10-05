using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using DataAccessClient.EntityBehaviors;
using DataAccessClient.Exceptions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace DataAccessClient.EntityFrameworkCore.SqlServer
{
    public abstract class SqlServerDbContext<TIdentifierType> : DbContext where TIdentifierType : struct
    {
        private readonly IUserIdentifierProvider<TIdentifierType> _authenticatedUserIdentifierProvider;

        protected SqlServerDbContext(DbContextOptions options)
            : base(options)
        {
            _authenticatedUserIdentifierProvider = this.GetService<IUserIdentifierProvider<TIdentifierType>>();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var entityBehaviors = new[]
            {
                typeof(IIdentifiable<>),
                typeof(ICreatable<>),
                typeof(IModifiable<>),
                typeof(ISoftDeletable<>),
                typeof(IRowVersionable),
            };

            var entityTypes = modelBuilder.Model
                .GetEntityTypes()
                .Where(t => t.ClrType.GetInterfaces().Any(x =>
                    x.IsGenericType &&
                    entityBehaviors.Contains(x.GetGenericTypeDefinition())));

            var configureEntityBehaviorIIdentifiable = typeof(ModelBuilderExtensions).GetTypeInfo().DeclaredMethods.Single(m => m.Name == nameof(ModelBuilderExtensions.ConfigureEntityBehaviorIIdentifiable));
            var configureEntityBehaviorICreatable = typeof(ModelBuilderExtensions).GetTypeInfo().DeclaredMethods.Single(m => m.Name == nameof(ModelBuilderExtensions.ConfigureEntityBehaviorICreatable));
            var configureEntityBehaviorIModifiable = typeof(ModelBuilderExtensions).GetTypeInfo().DeclaredMethods.Single(m => m.Name == nameof(ModelBuilderExtensions.ConfigureEntityBehaviorIModifiable));
            var configureEntityBehaviorISoftDeletable = typeof(ModelBuilderExtensions).GetTypeInfo().DeclaredMethods.Single(m => m.Name == nameof(ModelBuilderExtensions.ConfigureEntityBehaviorISoftDeletable));
            var configureEntityBehaviorIRowVersionable = typeof(ModelBuilderExtensions).GetTypeInfo().DeclaredMethods.Single(m => m.Name == nameof(ModelBuilderExtensions.ConfigureEntityBehaviorIRowVersionable));

            var args = new object[] { modelBuilder };
            foreach (var entityType in entityTypes)
            {
                var entityInterfaces = entityType.ClrType.GetInterfaces();

                if (entityInterfaces.Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IIdentifiable<>)))
                {
                    var identifierType = entityType.ClrType.GetInterface(typeof(IIdentifiable<>).Name).GenericTypeArguments[0];
                    configureEntityBehaviorIIdentifiable.MakeGenericMethod(entityType.ClrType, identifierType).Invoke(null, args);
                }

                if (entityInterfaces.Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(ICreatable<>)))
                {
                    var identifierType = entityType.ClrType.GetInterface(typeof(ICreatable<>).Name).GenericTypeArguments[0];
                    configureEntityBehaviorICreatable.MakeGenericMethod(entityType.ClrType, identifierType).Invoke(null, args);
                }

                if (entityInterfaces.Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IModifiable<>)))
                {
                    var identifierType = entityType.ClrType.GetInterface(typeof(IModifiable<>).Name).GenericTypeArguments[0];
                    configureEntityBehaviorIModifiable.MakeGenericMethod(entityType.ClrType, identifierType).Invoke(null, args);
                }

                if (entityInterfaces.Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(ISoftDeletable<>)))
                {
                    var identifierType = entityType.ClrType.GetInterface(typeof(ISoftDeletable<>).Name).GenericTypeArguments[0];
                    configureEntityBehaviorISoftDeletable.MakeGenericMethod(entityType.ClrType, identifierType).Invoke(null, args);
                }

                if (entityInterfaces.Any(x => !x.IsGenericType && x == typeof(IRowVersionable)))
                {
                    configureEntityBehaviorIRowVersionable.MakeGenericMethod(entityType.ClrType).Invoke(null, args);
                }
            }

            base.OnModelCreating(modelBuilder);
        }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            var authenticatedUserIdentifier = _authenticatedUserIdentifierProvider.ExecuteAsync().Result;

            AdjustEntities(authenticatedUserIdentifier);

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
            var authenticatedUser = await _authenticatedUserIdentifierProvider.ExecuteAsync();

            AdjustEntities(authenticatedUser);
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

        private void AdjustEntities(TIdentifierType authenticatedUserIdentifier)
        {
            foreach (var entityEntry in ChangeTracker.Entries<IRowVersionable>())
            {
                var rowVersionProperty = entityEntry.Property(u => u.RowVersion);
                var rowVersion = rowVersionProperty.CurrentValue;
                //https://github.com/aspnet/EntityFramework/issues/4512
                rowVersionProperty.OriginalValue = rowVersion;
            }

            foreach (var entityEntry in ChangeTracker.Entries<ISoftDeletable<TIdentifierType>>().Where(c => c.State == EntityState.Deleted))
            {
                entityEntry.State = EntityState.Unchanged;

                entityEntry.Entity.IsDeleted = true;
                entityEntry.Entity.DeletedById = authenticatedUserIdentifier;
                entityEntry.Entity.DeletedOn = DateTime.UtcNow;
                entityEntry.Member(nameof(ISoftDeletable<TIdentifierType>.IsDeleted)).IsModified = true;
                entityEntry.Member(nameof(ISoftDeletable<TIdentifierType>.DeletedById)).IsModified = true;
                entityEntry.Member(nameof(ISoftDeletable<TIdentifierType>.DeletedOn)).IsModified = true;
            }

            foreach (var entityEntry in ChangeTracker.Entries<ICreatable<TIdentifierType>>().Where(c => c.State == EntityState.Added))
            {
                entityEntry.Entity.CreatedById = authenticatedUserIdentifier;
                entityEntry.Entity.CreatedOn = DateTime.UtcNow;
            }

            foreach (var entityEntry in ChangeTracker.Entries<IModifiable<TIdentifierType>>().Where(c => c.State == EntityState.Modified))
            {
                entityEntry.Entity.ModifiedById = authenticatedUserIdentifier;
                entityEntry.Entity.ModifiedOn = DateTime.UtcNow;
            }
        }
    }
}

