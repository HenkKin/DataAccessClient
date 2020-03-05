using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DataAccessClient.EntityBehaviors;
using DataAccessClient.Providers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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

        public void OnRegistering(IServiceCollection serviceCollection)
        {
        }

        public Dictionary<string, dynamic> OnExecutionContextCreating(IServiceProvider scopedServiceProvider)
        {
            var userIdentifierProvider = scopedServiceProvider.GetRequiredService<IUserIdentifierProvider<TUserIdentifierType>>();

            var context = new Dictionary<string, dynamic>
            {
                {typeof(IUserIdentifierProvider<TUserIdentifierType>).Name, userIdentifierProvider},
            };

            return context;
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
            var userIdentifier = serverDbContext.ExecutionContext
                .Get<IUserIdentifierProvider<TUserIdentifierType>>().Execute();

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