using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
        private static readonly MethodInfo InternalAddDataAccessClientMethodInfo;

        static ServiceCollectionExtensions()
        {
            InternalAddDataAccessClientMethodInfo = typeof(ServiceCollectionExtensions).GetMethod(nameof(InternalAddDataAccessClient), BindingFlags.Static | BindingFlags.NonPublic);
        }

        [Obsolete("Please use `IServiceCollection AddDataAccessClient<TDbContext>(this IServiceCollection services, Action<DataAccessClientOptionsBuilder> optionsAction)`")]
        public static IServiceCollection AddDataAccessClient<TDbContext, TUserIdentifierType, TTenantIdentifierType>(this IServiceCollection services, Action<DbContextOptionsBuilder> optionsAction, Type[] entityTypes)
            where TDbContext : SqlServerDbContext<TUserIdentifierType, TTenantIdentifierType>
            where TUserIdentifierType : struct
            where TTenantIdentifierType : struct
        {
            return services.InternalAddDataAccessClient<TDbContext, TUserIdentifierType, TTenantIdentifierType>(optionsAction, entityTypes, usePooling: false, poolSize: null);
        }

        [Obsolete("Please use `IServiceCollection AddDataAccessClient<TDbContext>(this IServiceCollection services, Action<DataAccessClientOptionsBuilder> optionsAction)`")]
        public static IServiceCollection AddDataAccessClientPool<TDbContext, TUserIdentifierType, TTenantIdentifierType>(this IServiceCollection services, Action<DbContextOptionsBuilder> optionsAction, Type[] entityTypes, int? poolSize = null)
            where TDbContext : SqlServerDbContext<TUserIdentifierType, TTenantIdentifierType>
            where TUserIdentifierType : struct
            where TTenantIdentifierType : struct
        {
            return services.InternalAddDataAccessClient<TDbContext, TUserIdentifierType, TTenantIdentifierType>(optionsAction, entityTypes, usePooling: true, poolSize: poolSize);
        }

        public static IServiceCollection AddDataAccessClient<TDbContext>(this IServiceCollection services, Action<DataAccessClientOptionsBuilder> optionsAction)
            where TDbContext : SqlServerDbContextBase
        {
            if (!typeof(TDbContext).IsSubclassOfRawGeneric(typeof(SqlServerDbContext<,>)))
            {
                throw new ArgumentException($"Generic parameter {nameof(TDbContext)} in method {nameof(ServiceCollectionExtensions)}.{nameof(ServiceCollectionExtensions.AddDataAccessClient)} is not sub class of {typeof(SqlServerDbContext<,>).FullName}");
            }
            var dataAccessClientOptionsBuilder = new DataAccessClientOptionsBuilder();
            optionsAction.Invoke(dataAccessClientOptionsBuilder);

            var options = dataAccessClientOptionsBuilder.Options;

            if (options.EntityTypes == null)
            {
                throw new ArgumentException($"{nameof(DataAccessClientOptionsBuilder)}.{nameof(DataAccessClientOptionsBuilder.WithEntityTypes)} is not specified");
            }

            if (options.DbContextOptionsBuilder == null)
            {
                throw new ArgumentException($"{nameof(DataAccessClientOptionsBuilder)}.{nameof(DataAccessClientOptionsBuilder.WithDbContextOptions)} is not specified");
            }

            var userIdentifierRelatedEntityBehaviors = new[] {typeof(ICreatable<>), typeof(IModifiable<>), typeof(ISoftDeletable<>)};

            var userIdentifierType = options.UserIdentifierType ?? typeof(int);
            var userIdentifierProviderType = typeof(IUserIdentifierProvider<>).MakeGenericType(userIdentifierType);
            bool hasEntityBehaviorsWithUserIdentifier = ContainsEntityBehaviors(options.EntityTypes, userIdentifierRelatedEntityBehaviors);
            if (hasEntityBehaviorsWithUserIdentifier )
            {
                if (options.UserIdentifierType == null)
                {
                    throw new ArgumentException($"{nameof(DataAccessClientOptionsBuilder)}.{nameof(DataAccessClientOptionsBuilder.WithUserIdentifierType)} is not specified");
                }

                services.RequireRegistrationFor(userIdentifierProviderType, ServiceLifetime.Scoped);
            }

            var tenantIdentifierRelatedEntityBehaviors = new[] { typeof(ITenantScopable<>) };

            var tenantIdentifierType = options.TenantIdentifierType ?? typeof(int);
            var tenantIdentifierProviderType = typeof(ITenantIdentifierProvider<>).MakeGenericType(tenantIdentifierType);
            bool hasEntityBehaviorsWithTenantIdentifier = ContainsEntityBehaviors(options.EntityTypes, tenantIdentifierRelatedEntityBehaviors);
            if (hasEntityBehaviorsWithTenantIdentifier)
            {
                if (options.TenantIdentifierType == null)
                {
                    throw new ArgumentException($"{nameof(DataAccessClientOptionsBuilder)}.{nameof(DataAccessClientOptionsBuilder.WithTenantIdentifierType)} is not specified");
                }

                services.RequireRegistrationFor(tenantIdentifierProviderType, ServiceLifetime.Scoped);
            }
    
            return (IServiceCollection)InternalAddDataAccessClientMethodInfo.MakeGenericMethod(typeof(TDbContext), options.UserIdentifierType, options.TenantIdentifierType)
                .Invoke(null, new object[]{services, options.DbContextOptionsBuilder, options.EntityTypes, options.UsePooling, options.PoolSize});
        }

        private static IServiceCollection InternalAddDataAccessClient<TDbContext, TUserIdentifierType, TTenantIdentifierType>(this IServiceCollection services, Action<DbContextOptionsBuilder> optionsAction, Type[] entityTypes, bool usePooling, int? poolSize)
            where TDbContext : SqlServerDbContext<TUserIdentifierType, TTenantIdentifierType>
            where TUserIdentifierType : struct
            where TTenantIdentifierType : struct
        {
            services.TryAddScoped<ISoftDeletableConfiguration, DefaultSoftDeletableConfiguration>();
            services.TryAddScoped<IMultiTenancyConfiguration, DefaultMultiTenancyConfiguration>();

            if (ContainsEntityBehaviors(entityTypes, new[] {typeof(ISoftDeletable<>)}))
            {
                services.RequireRegistrationFor<ISoftDeletableConfiguration>(ServiceLifetime.Scoped);
            }

            if (ContainsEntityBehaviors(entityTypes, new[] {typeof(ITenantScopable<>)}))
            {
                services.RequireRegistrationFor<IMultiTenancyConfiguration>(ServiceLifetime.Scoped);
            }
            
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

        private static void RequireRegistrationFor<TRegistrationType>(this IServiceCollection services,
            ServiceLifetime serviceLifetime)
        {
            var isRegisteredWithLifetime =
                services.Any(s =>
                    s.ServiceType == typeof(TRegistrationType) &&
                    s.Lifetime == serviceLifetime);
            if (!isRegisteredWithLifetime)
            {
                ThrowNoRegistrationFoundException(typeof(TRegistrationType), serviceLifetime);
            }
        }

        private static void RequireRegistrationFor(this IServiceCollection services, Type registrationType, ServiceLifetime serviceLifetime)
        {
            var isRegisteredWithLifetime =
                services.Any(s =>
                    s.ServiceType == registrationType &&
                    s.Lifetime == serviceLifetime);
            if (!isRegisteredWithLifetime)
            {
                ThrowNoRegistrationFoundException(registrationType, serviceLifetime);
            }
        }

        private static void ThrowNoRegistrationFoundException(Type registrationType, ServiceLifetime serviceLifetime)
        {
            throw new InvalidOperationException(
                $"No DI registration found for type {registrationType.FullName}, please register with LifeTime {serviceLifetime.ToString()} in DI");

        }

        private static bool ContainsEntityBehaviors(Type[] entityTypes, Type[] entityBehaviors = null)
        {
            var containsEntityBehaviors = entityBehaviors != null && entityTypes
                                              .Any(c => c.GetInterfaces().Any(i =>
                                                  i.IsGenericType && entityBehaviors.Contains(i.GetGenericTypeDefinition())));

            return containsEntityBehaviors;
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

        private static bool IsSubclassOfRawGeneric(this Type toCheck, Type generic)
        {
            while (toCheck != null && toCheck != typeof(object))
            {
                var cur = toCheck.IsGenericType ? toCheck.GetGenericTypeDefinition() : toCheck;
                if (generic == cur)
                {
                    return true;
                }
                toCheck = toCheck.BaseType;
            }
            return false;
        }
    }
}