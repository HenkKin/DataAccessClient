using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DataAccessClient.EntityBehaviors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DataAccessClient.EntityFrameworkCore.SqlServer.Configuration.EntityBehaviors
{
    public class IdentifiableEntityBehaviorConfiguration : IEntityBehaviorConfiguration
    {
        private static readonly MethodInfo ModelBuilderConfigureEntityBehaviorIIdentifiable;

        static IdentifiableEntityBehaviorConfiguration()
        {
            ModelBuilderConfigureEntityBehaviorIIdentifiable = typeof(ModelBuilderExtensions).GetTypeInfo().DeclaredMethods
                .Single(m => m.Name == nameof(ModelBuilderExtensions.ConfigureEntityBehaviorIIdentifiable));
        }

        public void OnRegistering(IServiceCollection serviceCollection)
        {
        }

        public Dictionary<string, dynamic> OnExecutionContextCreating(IServiceProvider scopedServiceProvider)
        {
            return new Dictionary<string, dynamic>();
        }

        public void OnModelCreating(ModelBuilder modelBuilder, SqlServerDbContext serverDbContext, Type entityType)
        {
            var entityInterfaces = entityType.GetInterfaces();
            
            if (entityInterfaces.Any(
                x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IIdentifiable<>)))
            {
                var identifierType = entityType.GetInterface(typeof(IIdentifiable<>).Name)
                    .GenericTypeArguments[0];
                ModelBuilderConfigureEntityBehaviorIIdentifiable.MakeGenericMethod(entityType, identifierType)
                    .Invoke(null, new object[] { modelBuilder });
            }
        }

        public void OnBeforeSaveChanges(SqlServerDbContext serverDbContext, DateTime onSaveChangesTime)
        {
        }

        public void OnAfterSaveChanges(SqlServerDbContext serverDbContext)
        {
        }
    }
}