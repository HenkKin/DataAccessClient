using System;
using System.Linq;
using System.Reflection;
using DataAccessClient.EntityBehaviors;
using Microsoft.EntityFrameworkCore;

namespace DataAccessClient.EntityFrameworkCore.SqlServer.Configuration.EntityBehaviors
{
    public class CreatableEntityBehaviorConfiguration : IEntityBehaviorConfiguration
    {
        private static readonly MethodInfo ModelBuilderConfigureEntityBehaviorICreatable;

        static CreatableEntityBehaviorConfiguration()
        {
            ModelBuilderConfigureEntityBehaviorICreatable = typeof(ModelBuilderExtensions).GetTypeInfo().DeclaredMethods
                .Single(m => m.Name == nameof(ModelBuilderExtensions.ConfigureEntityBehaviorICreatable));
        }
        public void Execute(ModelBuilder modelBuilder, SqlServerDbContext serverDbContext, Type entityType)
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
    }
}