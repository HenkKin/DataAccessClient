using System;
using System.Collections.Generic;
using System.Linq;
using DataAccessClient.EntityBehaviors;
using DataAccessClient.EntityFrameworkCore.SqlServer.Searching;
using DataAccessClient.Searching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DataAccessClient.EntityFrameworkCore.SqlServer
{
    public static class ServiceCollectionExtensions
    {
        //public static IServiceCollection AddUserIdentifierProvider<TUserIdentifierProvider, TIdentifierType>(this IServiceCollection services)
        //    where TUserIdentifierProvider : class, IUserIdentifierProvider<TIdentifierType>
        //    where TIdentifierType : struct
        //{
        //    return services.AddSingleton<IUserIdentifierProvider<TIdentifierType>, TUserIdentifierProvider>();
        //}

        //public static IServiceCollection AddTenantIdentifierProvider<TTenantIdentifierProvider, TIdentifierType>(this IServiceCollection services)
        //    where TTenantIdentifierProvider : class, ITenantIdentifierProvider<TIdentifierType>
        //    where TIdentifierType : struct
        //{
        //    return services.AddSingleton<ITenantIdentifierProvider<TIdentifierType>, TTenantIdentifierProvider>();
        //}

        //public static IServiceCollection AddMultiTenancyConfiguration<TMultiTenancyConfiguration, TIdentifierType>(this IServiceCollection services)
        //    where TMultiTenancyConfiguration : class, IMultiTenancyConfiguration<TIdentifierType>
        //    where TIdentifierType : struct
        //{
        //    return services.AddSingleton<IMultiTenancyConfiguration<TIdentifierType>, TMultiTenancyConfiguration>();
        //}
        
        //public static IServiceCollection AddSoftDeletableConfiguration<TSoftDeletableConfiguration>(this IServiceCollection services)
        //    where TSoftDeletableConfiguration : class, ISoftDeletableConfiguration
        //{
        //    return services.AddSingleton<ISoftDeletableConfiguration, TSoftDeletableConfiguration>();
        //}

        public static IServiceCollection AddDataAccessClient<TDbContext, TIdentifierType>(this IServiceCollection services, Action<DbContextOptionsBuilder> optionsAction, Type[] entityTypes)
            where TDbContext : SqlServerDbContext<TIdentifierType>
            where TIdentifierType : struct
        {
            return services.InternalAddDataAccessClient<TDbContext, TIdentifierType>(optionsAction, entityTypes, usePooling: false, poolSize: null);
        }
        
        public static IServiceCollection AddDataAccessClientPool<TDbContext, TIdentifierType>(this IServiceCollection services, Action<DbContextOptionsBuilder> optionsAction, Type[] entityTypes, int? poolSize = null)
            where TDbContext : SqlServerDbContext<TIdentifierType>
            where TIdentifierType : struct
        {
            return services.InternalAddDataAccessClient<TDbContext, TIdentifierType>(optionsAction, entityTypes, usePooling: true, poolSize:poolSize);
        }

        private static IServiceCollection InternalAddDataAccessClient<TDbContext, TIdentifierType>(this IServiceCollection services, Action<DbContextOptionsBuilder> optionsAction, Type[] entityTypes, bool usePooling, int? poolSize)
            where TDbContext : SqlServerDbContext<TIdentifierType>
            where TIdentifierType : struct
        {
            RequireSingletonRegistrationFor<IUserIdentifierProvider<TIdentifierType>>(services, entityTypes, new[]{ typeof(ICreatable<>), typeof(IModifiable<>), typeof(ISoftDeletable<>) });
            RequireSingletonRegistrationFor<ITenantIdentifierProvider<TIdentifierType>>(services, entityTypes, new[]{ typeof(ITenantScopable<>) });
            RequireSingletonRegistrationFor<ISoftDeletableConfiguration>(services, entityTypes, new[]{ typeof(ISoftDeletable<>) });
            RequireSingletonRegistrationFor<IMultiTenancyConfiguration<TIdentifierType>>(services, entityTypes, new[]{ typeof(ITenantScopable<>) });

            if (usePooling)
            {
                if (poolSize.HasValue)
                {
                    services.AddDbContextPool<TDbContext>(optionsAction, poolSize.Value);
                }
                else
                {
                    services.AddDbContextPool<TDbContext>(optionsAction);
                }
            }
            else
            {
                services.AddDbContext<TDbContext>(optionsAction);
            }

            var entityTypeList = entityTypes.ToArray();

            services
                .AddScoped<IUnitOfWorkPart, UnitOfWorkPartForSqlServerDbContext<TDbContext, TIdentifierType>>()
                .AddRepositories<TDbContext, TIdentifierType>(entityTypeList)
                .AddQueryableSearcher<TDbContext, TIdentifierType>(entityTypeList)
                .TryAddScoped<IUnitOfWork, UnitOfWork>();

            return services;
        }

        private static void RequireSingletonRegistrationFor<TSingletonRegistration>(IServiceCollection services, Type[] entityTypes, Type[] entityBehaviors)
        {
            var containsEntityBehaviors = entityTypes
                .Any(c => c.GetInterfaces().Any(i => i.IsGenericType && entityBehaviors.Contains(i.GetGenericTypeDefinition())));

            if (containsEntityBehaviors)
            {
                var isRegisteredAsSingleTon =
                    services.Any(s =>
                        s.ServiceType == typeof(TSingletonRegistration) &&
                        s.Lifetime == ServiceLifetime.Singleton);
                if (!isRegisteredAsSingleTon)
                {
                    throw new InvalidOperationException(
                        $"No DI registration found for type {typeof(TSingletonRegistration).FullName}, please register with LifeTime Singleton in DI");
                }
            }
        }

        private static IServiceCollection AddRepositories<TDbContext, TIdentifierType>(this IServiceCollection services, IEnumerable<Type> entityTypes)
            where TDbContext : SqlServerDbContext<TIdentifierType>
            where TIdentifierType : struct
        {
            foreach (var entityType in entityTypes)
            {
                Type[] interfaceTypeArgs = { entityType };
                Type[] typeArgs = { typeof(TDbContext), entityType, typeof(TIdentifierType) };
                var typedGenericRepositoryType = typeof(SqlServerRepository<,,>).MakeGenericType(typeArgs);
                var typedRepositoryInterface = typeof(IRepository<>).MakeGenericType(interfaceTypeArgs);

                if (services.Any(s => s.ServiceType == typedRepositoryInterface))
                {
                    throw new InvalidOperationException($"An entity type {entityType.FullName} can only registered once in one DbContext, please provide another entity type for DbContext '{typeof(TDbContext).FullName}', otherwise resolving IRepository<[EntityType]> will not work.");
                }

                services.AddScoped(typedRepositoryInterface, typedGenericRepositoryType);
            }

            return services;
        }

        private static IServiceCollection AddQueryableSearcher<TDbContext, TIdentifierType>(this IServiceCollection services, IEnumerable<Type> entityTypes)
            where TDbContext : SqlServerDbContext<TIdentifierType>
            where TIdentifierType : struct
        {
            foreach (var entityType in entityTypes)
            {
                Type[] interfaceTypeArgs = { entityType };
                Type[] typeArgs = { entityType };
                var typedQueryableSearcherType = typeof(SqlServerQueryableSearcher<>).MakeGenericType(typeArgs);
                var typedQueryableSearcherInterface = typeof(IQueryableSearcher<>).MakeGenericType(interfaceTypeArgs);

                if (services.Any(s => s.ServiceType == typedQueryableSearcherInterface))
                {
                    throw new InvalidOperationException($"An entity type {entityType.FullName} can only registered once in one QueryableSearcher, please provide another entity type for QueryableSearcher '{typeof(TDbContext).FullName}', otherwise resolving IQueryableSearcher<[EntityType]> will not work.");
                }

                services.AddScoped(typedQueryableSearcherInterface, typedQueryableSearcherType);
            }

            return services;
        }
    }
}