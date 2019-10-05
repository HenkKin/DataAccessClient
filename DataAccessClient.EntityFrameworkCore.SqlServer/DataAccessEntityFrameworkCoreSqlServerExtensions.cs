using System;
using System.Collections.Generic;
using System.Linq;
using DataAccessClient.EntityFrameworkCore.SqlServer.Searching;
using DataAccessClient.Searching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DataAccessClient.EntityFrameworkCore.SqlServer
{
    public static class DataAccessEntityFrameworkCoreSqlServerExtensions
    {
        public static IServiceCollection AddDataAccessClient<TDbContext, TIdentifierType, TUserIdentifierProvider>(this IServiceCollection services, Action<DbContextOptionsBuilder> optionsAction, IEnumerable<Type> entityTypes)
            where TDbContext : SqlServerDbContext<TIdentifierType>
            where TIdentifierType : struct
            where TUserIdentifierProvider : class, IUserIdentifierProvider<TIdentifierType>
        {
            services.AddSingleton<IUserIdentifierProvider<TIdentifierType>, TUserIdentifierProvider>();
            return services.InternalAddDataAccessClient<TDbContext, TIdentifierType>(optionsAction, entityTypes, usePooling: false);
        }

        public static IServiceCollection AddDataAccessClient<TDbContext, TIdentifierType>(this IServiceCollection services, Action<DbContextOptionsBuilder> optionsAction, IEnumerable<Type> entityTypes)
            where TDbContext : SqlServerDbContext<TIdentifierType>
            where TIdentifierType : struct
        {
            return services.InternalAddDataAccessClient<TDbContext, TIdentifierType>(optionsAction, entityTypes, usePooling: false);
        }

        public static IServiceCollection AddDataAccessClientPool<TDbContext, TIdentifierType, TUserIdentifierProvider>(this IServiceCollection services, Action<DbContextOptionsBuilder> optionsAction, IEnumerable<Type> entityTypes)
            where TDbContext : SqlServerDbContext<TIdentifierType>
            where TIdentifierType : struct
            where TUserIdentifierProvider : class, IUserIdentifierProvider<TIdentifierType>
        {
            services.AddSingleton<IUserIdentifierProvider<TIdentifierType>, TUserIdentifierProvider>();
            return services.InternalAddDataAccessClient<TDbContext, TIdentifierType>(optionsAction, entityTypes, usePooling: true);
        }

        public static IServiceCollection AddDataAccessClientPool<TDbContext, TIdentifierType>(this IServiceCollection services, Action<DbContextOptionsBuilder> optionsAction, IEnumerable<Type> entityTypes)
            where TDbContext : SqlServerDbContext<TIdentifierType>
            where TIdentifierType : struct
        {
            return services.InternalAddDataAccessClient<TDbContext, TIdentifierType>(optionsAction, entityTypes, usePooling: true);
        }

        private static IServiceCollection InternalAddDataAccessClient<TDbContext, TIdentifierType>(this IServiceCollection services, Action<DbContextOptionsBuilder> optionsAction, IEnumerable<Type> entityTypes, bool usePooling)
            where TDbContext : SqlServerDbContext<TIdentifierType>
            where TIdentifierType : struct
        {
            var isUserIdentifierProviderRegisteredAsSingleTon =
                services.Any(s => s.ServiceType == typeof(IUserIdentifierProvider<TIdentifierType>) && s.Lifetime == ServiceLifetime.Singleton);

            if (!isUserIdentifierProviderRegisteredAsSingleTon)
            {
                throw new InvalidOperationException(
                    $"No DI registration found for type {typeof(IUserIdentifierProvider<>).FullName}, please register with LifeTime Singleton in DI");
            }

            if (usePooling)
            {
                services.AddDbContextPool<TDbContext>(optionsAction);
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