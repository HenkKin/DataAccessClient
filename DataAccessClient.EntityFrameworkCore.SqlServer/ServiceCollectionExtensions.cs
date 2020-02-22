using System;
using System.Collections.Generic;
using System.Linq;
using DataAccessClient.Configuration;
using DataAccessClient.EntityBehaviors;
using DataAccessClient.EntityFrameworkCore.SqlServer.Resolvers;
using DataAccessClient.EntityFrameworkCore.SqlServer.Searching;
using DataAccessClient.Providers;
using DataAccessClient.Searching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DataAccessClient.EntityFrameworkCore.SqlServer
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddDataAccessClient<TDbContext, TUserIdentifierType, TTenantIdentifierType>(this IServiceCollection services, Action<DbContextOptionsBuilder> optionsAction, Type[] entityTypes)
            where TDbContext : SqlServerDbContext<TUserIdentifierType, TTenantIdentifierType>
            where TUserIdentifierType : struct
            where TTenantIdentifierType : struct
        {
            return services.InternalAddDataAccessClient<TDbContext, TUserIdentifierType, TTenantIdentifierType>(optionsAction, entityTypes, usePooling: false, poolSize: null);
        }
        
        public static IServiceCollection AddDataAccessClientPool<TDbContext, TUserIdentifierType, TTenantIdentifierType>(this IServiceCollection services, Action<DbContextOptionsBuilder> optionsAction, Type[] entityTypes, int? poolSize = null)
            where TDbContext : SqlServerDbContext<TUserIdentifierType, TTenantIdentifierType>
            where TUserIdentifierType : struct
            where TTenantIdentifierType : struct
        {
            return services.InternalAddDataAccessClient<TDbContext, TUserIdentifierType, TTenantIdentifierType>(optionsAction, entityTypes, usePooling: true, poolSize:poolSize);
        }

        private static IServiceCollection InternalAddDataAccessClient<TDbContext, TUserIdentifierType, TTenantIdentifierType>(this IServiceCollection services, Action<DbContextOptionsBuilder> optionsAction, Type[] entityTypes, bool usePooling, int? poolSize)
            where TDbContext : SqlServerDbContext<TUserIdentifierType, TTenantIdentifierType>
            where TUserIdentifierType : struct
            where TTenantIdentifierType : struct
        {
            services.RequireRegistrationFor<IUserIdentifierProvider<TUserIdentifierType>>(ServiceLifetime.Scoped, entityTypes, new[] { typeof(ICreatable<>), typeof(IModifiable<>), typeof(ISoftDeletable<>) });
            services.RequireRegistrationFor<ITenantIdentifierProvider<TTenantIdentifierType>>(ServiceLifetime.Scoped, entityTypes, new[] { typeof(ITenantScopable<>) });

            services.TryAddScoped<ISoftDeletableConfiguration, DefaultSoftDeletableConfiguration>();
            services.TryAddScoped<IMultiTenancyConfiguration, DefaultMultiTenancyConfiguration>();
            
            services.RequireRegistrationFor<ISoftDeletableConfiguration>(ServiceLifetime.Scoped, entityTypes, new[] { typeof(ISoftDeletable<>) });
            services.RequireRegistrationFor<IMultiTenancyConfiguration>(ServiceLifetime.Scoped, entityTypes, new[] { typeof(ITenantScopable<>) });

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
                .AddScoped<IUnitOfWorkPart, UnitOfWorkPartForSqlServerDbContext<TDbContext, TUserIdentifierType, TTenantIdentifierType>>()
                .AddRepositories<TDbContext, TUserIdentifierType, TTenantIdentifierType>(entityTypeList)
                .AddQueryableSearchers(entityTypeList)
                .TryAddScoped<IUnitOfWork, UnitOfWork>();

            services.TryAddScoped<ISqlServerDbContextResolver<TDbContext, TUserIdentifierType, TTenantIdentifierType>, SqlServerDbContextResolver<TDbContext, TUserIdentifierType, TTenantIdentifierType>>();
            
            return services;
        }

        private static void RequireRegistrationFor<TSingletonRegistration>(this IServiceCollection services, ServiceLifetime serviceLifetime, Type[] entityTypes, Type[] entityBehaviors = null)
        {
            var containsEntityBehaviors = entityBehaviors != null && entityTypes
                                              .Any(c => c.GetInterfaces().Any(i =>
                                                  i.IsGenericType && entityBehaviors.Contains(i.GetGenericTypeDefinition())));

            if (containsEntityBehaviors || entityBehaviors == null)
            {
                var isRegisteredWithLifetime =
                    services.Any(s =>
                        s.ServiceType == typeof(TSingletonRegistration) &&
                        s.Lifetime == serviceLifetime);
                if (!isRegisteredWithLifetime)
                {
                    throw new InvalidOperationException(
                        $"No DI registration found for type {typeof(TSingletonRegistration).FullName}, please register with LifeTime {serviceLifetime.ToString()} in DI");
                }
            }
        }

        private static IServiceCollection AddRepositories<TDbContext, TUserIdentifierType, TTenantIdentifierType>(this IServiceCollection services, IEnumerable<Type> entityTypes)
            where TDbContext : SqlServerDbContext<TUserIdentifierType, TTenantIdentifierType>
            where TUserIdentifierType : struct
            where TTenantIdentifierType : struct
        {
            foreach (var entityType in entityTypes)
            {
                Type[] interfaceTypeArgs = { entityType };
                Type[] typeArgs = { typeof(TDbContext), entityType, typeof(TUserIdentifierType), typeof(TTenantIdentifierType) };
                var typedGenericRepositoryType = typeof(SqlServerRepository<,,,>).MakeGenericType(typeArgs);
                var typedRepositoryInterface = typeof(IRepository<>).MakeGenericType(interfaceTypeArgs);

                if (services.Any(s => s.ServiceType == typedRepositoryInterface))
                {
                    throw new InvalidOperationException($"An entity type {entityType.FullName} can only registered once in one DbContext, please provide another entity type for DbContext '{typeof(TDbContext).FullName}', otherwise resolving IRepository<[EntityType]> will not work.");
                }

                services.TryAddScoped(typedRepositoryInterface, typedGenericRepositoryType);
            }

            return services;
        }

        private static IServiceCollection AddQueryableSearchers(this IServiceCollection services, IEnumerable<Type> entityTypes)
        {
            foreach (var entityType in entityTypes)
            {
                Type[] interfaceTypeArgs = { entityType };
                Type[] typeArgs = { entityType };
                var typedQueryableSearcherType = typeof(SqlServerQueryableSearcher<>).MakeGenericType(typeArgs);
                var typedQueryableSearcherInterface = typeof(IQueryableSearcher<>).MakeGenericType(interfaceTypeArgs);

                if (services.Any(s => s.ServiceType == typedQueryableSearcherInterface))
                {
                    throw new InvalidOperationException($"An entity type {entityType.FullName} can only registered once in one QueryableSearcher, please provide another entity type for QueryableSearcher '{typedQueryableSearcherInterface.FullName}', otherwise resolving IQueryableSearcher<[EntityType]> will not work.");
                }

                services.TryAddScoped(typedQueryableSearcherInterface, typedQueryableSearcherType);
            }

            return services;
        }
    }
}