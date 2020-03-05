using System;
using System.Linq;
using System.Reflection;
using DataAccessClient.EntityBehaviors;
using Microsoft.EntityFrameworkCore;
// ReSharper disable StaticMemberInGenericType

namespace DataAccessClient.EntityFrameworkCore.SqlServer.Configuration.EntityBehaviors
{
    public class CreatableEntityBehaviorConfiguration<TUserIdentifierType> : IEntityBehaviorConfiguration where TUserIdentifierType : struct
    {
        private static readonly MethodInfo ModelBuilderConfigureEntityBehaviorICreatable;

        static CreatableEntityBehaviorConfiguration()
        {
            ModelBuilderConfigureEntityBehaviorICreatable = typeof(ModelBuilderExtensions).GetTypeInfo().DeclaredMethods
                .Single(m => m.Name == nameof(ModelBuilderExtensions.ConfigureEntityBehaviorICreatable));
        }
        public void OnModelCreating(ModelBuilder modelBuilder, SqlServerDbContext serverDbContext, Type entityType)
        {
            var entityInterfaces = entityType.GetInterfaces();
            
            if (entityInterfaces.Any(
                x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(ICreatable<>)))
            {
                var identifierType = entityType.GetInterface(typeof(ICreatable<>).Name)
                    .GenericTypeArguments[0];
                ModelBuilderConfigureEntityBehaviorICreatable.MakeGenericMethod(entityType, identifierType)
                    .Invoke(null, new object[] { modelBuilder });
            }
        }

        public void OnBeforeSaveChanges(SqlServerDbContext serverDbContext, DateTime onSaveChangesTime)
        {
            var userIdentifier = serverDbContext.CurrentUserIdentifier<TUserIdentifierType>();

            foreach (var entityEntry in serverDbContext.ChangeTracker.Entries<ICreatable<TUserIdentifierType>>()
                .Where(c => c.State == EntityState.Added))
            {
                entityEntry.Entity.CreatedById = userIdentifier.GetValueOrDefault();
                entityEntry.Entity.CreatedOn = onSaveChangesTime;
            }
        }

        public void OnAfterSaveChanges(SqlServerDbContext serverDbContext)
        {
        }
    }
}