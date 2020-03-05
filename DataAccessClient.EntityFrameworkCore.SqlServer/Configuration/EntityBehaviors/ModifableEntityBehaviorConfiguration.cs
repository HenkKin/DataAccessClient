using System;
using System.Linq;
using System.Reflection;
using DataAccessClient.EntityBehaviors;
using Microsoft.EntityFrameworkCore;
// ReSharper disable StaticMemberInGenericType

namespace DataAccessClient.EntityFrameworkCore.SqlServer.Configuration.EntityBehaviors
{
    public class ModifiableEntityBehaviorConfiguration<TUserIdentifierType> : IEntityBehaviorConfiguration where TUserIdentifierType : struct
    {
        private static readonly MethodInfo ModelBuilderConfigureEntityBehaviorIModifiable;

        static ModifiableEntityBehaviorConfiguration()
        {
            ModelBuilderConfigureEntityBehaviorIModifiable = typeof(ModelBuilderExtensions).GetTypeInfo().DeclaredMethods
                .Single(m => m.Name == nameof(ModelBuilderExtensions.ConfigureEntityBehaviorIModifiable));
        }
        public void OnModelCreating(ModelBuilder modelBuilder, SqlServerDbContext serverDbContext, Type entityType)
        {
            var entityInterfaces = entityType.GetInterfaces();
            
            if (entityInterfaces.Any(
                x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IModifiable<>)))
            {
                var identifierType = entityType.GetInterface(typeof(IModifiable<>).Name)
                    .GenericTypeArguments[0];
                ModelBuilderConfigureEntityBehaviorIModifiable.MakeGenericMethod(entityType, identifierType)
                    .Invoke(null, new object[] { modelBuilder });
            }
        }

        public void OnBeforeSaveChanges(SqlServerDbContext serverDbContext, DateTime onSaveChangesTime)
        {
            var userIdentifier = serverDbContext.CurrentUserIdentifier<TUserIdentifierType>();

            foreach (var entityEntry in serverDbContext.ChangeTracker.Entries<IModifiable<TUserIdentifierType>>()
                .Where(c => c.State == EntityState.Modified))
            {
                entityEntry.Entity.ModifiedById = userIdentifier;
                entityEntry.Entity.ModifiedOn = onSaveChangesTime;
            }
        }

        public void OnAfterSaveChanges(SqlServerDbContext serverDbContext)
        {
        }
    }
}