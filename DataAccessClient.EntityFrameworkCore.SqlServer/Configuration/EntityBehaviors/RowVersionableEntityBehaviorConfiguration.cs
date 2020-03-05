using System;
using System.Linq;
using System.Reflection;
using DataAccessClient.EntityBehaviors;
using Microsoft.EntityFrameworkCore;

namespace DataAccessClient.EntityFrameworkCore.SqlServer.Configuration.EntityBehaviors
{
    public class RowVersionableEntityBehaviorConfiguration : IEntityBehaviorConfiguration
    {
        private static readonly MethodInfo ModelBuilderConfigureEntityBehaviorIRowVersionable;

        static RowVersionableEntityBehaviorConfiguration()
        {
            ModelBuilderConfigureEntityBehaviorIRowVersionable = typeof(ModelBuilderExtensions).GetTypeInfo().DeclaredMethods
                .Single(m => m.Name == nameof(ModelBuilderExtensions.ConfigureEntityBehaviorIRowVersionable));
        }
        public void OnModelCreating(ModelBuilder modelBuilder, SqlServerDbContext serverDbContext, Type entityType)
        {
            var entityInterfaces = entityType.GetInterfaces();

            if (entityInterfaces.Any(x => !x.IsGenericType && x == typeof(IRowVersionable)))
            {
                ModelBuilderConfigureEntityBehaviorIRowVersionable.MakeGenericMethod(entityType)
                    .Invoke(null, new object[] {modelBuilder});
            }
        }

        public void OnBeforeSaveChanges(SqlServerDbContext serverDbContext, DateTime onSaveChangesTime)
        {
            foreach (var entityEntry in serverDbContext.ChangeTracker.Entries<IRowVersionable>())
            {
                var rowVersionProperty = entityEntry.Property(u => u.RowVersion);
                var rowVersion = rowVersionProperty.CurrentValue;
                //https://github.com/aspnet/EntityFramework/issues/4512
                rowVersionProperty.OriginalValue = rowVersion;
            }
        }

        public void OnAfterSaveChanges(SqlServerDbContext serverDbContext)
        {
        }
    }
}