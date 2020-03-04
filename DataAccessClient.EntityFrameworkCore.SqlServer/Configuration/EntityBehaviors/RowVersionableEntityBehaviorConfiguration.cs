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
        public void Execute(ModelBuilder modelBuilder, SqlServerDbContext serverDbContext, Type entityType)
        {
            var entityInterfaces = entityType.GetInterfaces();

            if (entityInterfaces.Any(x => !x.IsGenericType && x == typeof(IRowVersionable)))
            {
                ModelBuilderConfigureEntityBehaviorIRowVersionable.MakeGenericMethod(entityType)
                    .Invoke(null, new object[] {modelBuilder});
            }
        }
    }
}