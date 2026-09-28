using DataAccessClient.EntityFrameworkCore.Relational.Configuration.EntityBehaviors;
using DataAccessClient.EntityFrameworkCore.Relational.ExceptionHandling;
using DataAccessClient.EntityFrameworkCore.Relational.ExceptionHandling.Providers;
using DataAccessClient.EntityFrameworkCore.Relational.Resolvers;
using DataAccessClient.EntityFrameworkCore.Relational.Searching;
using DataAccessClient.Providers;
using DataAccessClient.Searching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DataAccessClient.EntityFrameworkCore.Relational
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddDataAccessClient<TDbContext>(this IServiceCollection services, Action<DataAccessClientOptionsBuilder> dataAccessClientOptionsBuilderAction)
            where TDbContext : RelationalDbContext
        {
            var dataAccessClientOptionsBuilder = new DataAccessClientOptionsBuilder();
            dataAccessClientOptionsBuilderAction.Invoke(dataAccessClientOptionsBuilder);

            var userIdentifierType = services.GetUserIdentifierType() ?? typeof(int);
            var tenantIdentifierType = services.GetTenantIdentifierType() ?? typeof(int);
            var localeIdentifierType = services.GetLocaleIdentifierType() ?? typeof(string);

            var dataAccessClientOptions = dataAccessClientOptionsBuilder.Options;

            var entityBehaviorConfigurations = new List<IEntityBehaviorConfiguration>
            {
                new IdentifiableEntityBehaviorConfiguration(),
                CreateEntityBehaviorTypeInstance(typeof(CreatableEntityBehaviorConfiguration<>).MakeGenericType(userIdentifierType)),
                CreateEntityBehaviorTypeInstance(typeof(ModifiableEntityBehaviorConfiguration<>).MakeGenericType(userIdentifierType)),
                CreateEntityBehaviorTypeInstance(typeof(SoftDeletableEntityBehaviorConfiguration<>).MakeGenericType(userIdentifierType)),
                new RowVersionableEntityBehaviorConfiguration(),
                CreateEntityBehaviorTypeInstance(typeof(LocalizableEntityBehaviorConfiguration<>).MakeGenericType(localeIdentifierType)),
                CreateEntityBehaviorTypeInstance(typeof(TenantScopeableEntityBehaviorConfiguration<>).MakeGenericType(tenantIdentifierType)),
                new TranslatableEntityBehaviorConfiguration(),
                new TranslatedPropertyEntityBehaviorConfiguration()
            };

            if (dataAccessClientOptions.CustomEntityBehaviors.Any())
            {
                entityBehaviorConfigurations.AddRange(dataAccessClientOptions.CustomEntityBehaviors);
            }

            void ExtendedDbContextOptionsBuilder(DbContextOptionsBuilder dbContextOptionsBuilder)
            {
                dbContextOptionsBuilder.WithEntityBehaviors(entityBehaviorConfigurations);

                dataAccessClientOptions.DbContextOptionsBuilder(dbContextOptionsBuilder);
            }

            foreach (var entityBehavior in entityBehaviorConfigurations)
            {
                entityBehavior.OnRegistering(services);
            }

            if (dataAccessClientOptions.UsePooling)
            {
                if (dataAccessClientOptions.PoolSize.HasValue)
                {
                    services.AddDbContextPool<TDbContext>(ExtendedDbContextOptionsBuilder, dataAccessClientOptions.PoolSize.Value);
                }
                else
                {
                    services.AddDbContextPool<TDbContext>(ExtendedDbContextOptionsBuilder);
                }
            }
            else
            {
                services.AddDbContext<TDbContext>(ExtendedDbContextOptionsBuilder);
            }

            InitializeDbContextOnResolving<TDbContext>(services);

            services.AddScoped<IUnitOfWorkPart, UnitOfWorkPartForRelationalDbContext<TDbContext>>();

            RelationalDbContext.RegisteredDbContextTypes.Add(typeof(TDbContext));

            services.TryAddScoped<IUnitOfWork, UnitOfWork>();
            services.TryAddScoped(typeof(IRepository<>), typeof(RelationalRepository<>));
            services.TryAddScoped(typeof(IQueryableSearcher<>), typeof(RelationalQueryableSearcher<>));
            services.TryAddScoped<IRelationalDbContextForEntityResolver, RelationalDbContextForEntityResolver>();
            services.TryAddScoped<IRelationalDbContextResolver<TDbContext>, RelationalDbContextResolver<TDbContext>>();


            services.AddSingleton<ISaveChangesDbUpdateExceptionHandler, SqlServerExceptionHandler>();
            services.AddSingleton<ISaveChangesDbUpdateExceptionHandler, PostgresExceptionHandler>();
            services.AddSingleton<ISaveChangesDbUpdateExceptionHandler>(sp => new RelationalSaveChangesDbUpdateExceptionHandler(sp.GetServices<ISaveChangesDbUpdateExceptionHandler>()));

            return services;
        }

        // Wraps the TDbContext registration of EntityFrameworkCore, so a TDbContext resolved directly from the container
        // is initialized as well, while the pooling and lifetime of that registration stay untouched
        private static void InitializeDbContextOnResolving<TDbContext>(IServiceCollection services)
            where TDbContext : RelationalDbContext
        {
            var dbContextServiceDescriptor = services.Last(s => s.ServiceType == typeof(TDbContext));

            Func<IServiceProvider, object> dbContextFactory;
            if (dbContextServiceDescriptor.ImplementationFactory != null)
            {
                dbContextFactory = dbContextServiceDescriptor.ImplementationFactory;
            }
            else if (dbContextServiceDescriptor.ImplementationType != null)
            {
                var objectFactory = ActivatorUtilities.CreateFactory(dbContextServiceDescriptor.ImplementationType, Type.EmptyTypes);
                dbContextFactory = sp => objectFactory(sp, null);
            }
            else
            {
                throw new InvalidOperationException(
                    $"'{typeof(TDbContext).FullName}' is registered as an instance, which is not supported. Register it with {nameof(AddDataAccessClient)}<{typeof(TDbContext).Name}>() only.");
            }

            services.Remove(dbContextServiceDescriptor);
            services.Add(new ServiceDescriptor(typeof(TDbContext), sp =>
            {
                var dbContext = (TDbContext)dbContextFactory(sp);

                if (dbContext.ExecutionContext == null)
                {
                    RelationalDbContextInitializer.Initialize(dbContext, sp);
                }

                return dbContext;
            }, dbContextServiceDescriptor.Lifetime));
        }

        private static IEntityBehaviorConfiguration CreateEntityBehaviorTypeInstance(Type entityBehaviorType)
        {
            return (IEntityBehaviorConfiguration)Activator.CreateInstance(entityBehaviorType);
        }

        private static Type GetUserIdentifierType(this IServiceCollection services)
        {
            var registration =
                services.FirstOrDefault(s =>
                    s.ServiceType.IsGenericType &&
                    s.ServiceType.GetGenericTypeDefinition() == typeof(IUserIdentifierProvider<>) &&
                    (s.Lifetime == ServiceLifetime.Scoped || s.Lifetime == ServiceLifetime.Singleton));
            return registration?.ServiceType.GenericTypeArguments[0];
        }

        private static Type GetTenantIdentifierType(this IServiceCollection services)
        {
            var registration =
                services.FirstOrDefault(s =>
                    s.ServiceType.IsGenericType &&
                    s.ServiceType.GetGenericTypeDefinition() == typeof(ITenantIdentifierProvider<>) &&
                    (s.Lifetime == ServiceLifetime.Scoped || s.Lifetime == ServiceLifetime.Singleton));
            return registration?.ServiceType.GenericTypeArguments[0];
        }

        private static Type GetLocaleIdentifierType(this IServiceCollection services)
        {
            var registration =
                services.FirstOrDefault(s =>
                    s.ServiceType.IsGenericType &&
                    s.ServiceType.GetGenericTypeDefinition() == typeof(ILocaleIdentifierProvider<>) &&
                    (s.Lifetime == ServiceLifetime.Scoped || s.Lifetime == ServiceLifetime.Singleton));
            return registration?.ServiceType.GenericTypeArguments[0];
        }
    }
}