﻿using System;
using System.Collections.Generic;
using System.Linq;
using DataAccessClient.EntityFrameworkCore.SqlServer.Configuration.EntityBehaviors;
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
        public static IServiceCollection AddDataAccessClient<TDbContext>(this IServiceCollection services, Action<DataAccessClientOptionsBuilder> dataAccessClientOptionsBuilderAction)
            where TDbContext : SqlServerDbContext
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
                new TranslatedPropertyEntityBehaviorConfiguration(),
                new UtcDateTimePropertyEntityBehaviorConfiguration()
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

            //if (ContainsEntityBehaviors(dataAccessClientOptions.EntityTypes, new[] { typeof(ISoftDeletable<>) }))
            //{
            //    services.RequireRegistrationFor<ISoftDeletableConfiguration>(ServiceLifetime.Scoped);
            //}

            //if (ContainsEntityBehaviors(dataAccessClientOptions.EntityTypes, new[] { typeof(ITenantScopable<>) }))
            //{
            //    services.RequireRegistrationFor<IMultiTenancyConfiguration>(ServiceLifetime.Scoped);
            //}

            //if (ContainsEntityBehaviors(dataAccessClientOptions.EntityTypes, new[] { typeof(ILocalizable<>) }))
            //{
            //    services.RequireRegistrationFor<ILocalizationConfiguration>(ServiceLifetime.Scoped);
            //}

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

            services.AddScoped<IUnitOfWorkPart, UnitOfWorkPartForSqlServerDbContext<TDbContext>>();

            SqlServerDbContext.RegisteredDbContextTypes.Add(typeof(TDbContext));

            services.TryAddScoped<IUnitOfWork, UnitOfWork>();
            services.TryAddScoped(typeof(IRepository<>), typeof(SqlServerRepository<>));
            services.TryAddScoped(typeof(IQueryableSearcher<>), typeof(SqlServerQueryableSearcher<>));
            services.TryAddScoped<ISqlServerDbContextForEntityResolver, SqlServerDbContextForEntityResolver>();
            services.TryAddScoped<ISqlServerDbContextResolver<TDbContext>, SqlServerDbContextResolver<TDbContext>>();

            return services;
        }

        private static IEntityBehaviorConfiguration CreateEntityBehaviorTypeInstance(Type entityBehaviorType)
        {
            return (IEntityBehaviorConfiguration)Activator.CreateInstance(entityBehaviorType);
        }

        private static Type GetUserIdentifierType(this IServiceCollection services)
        {
            var registration =
                services.SingleOrDefault(s =>
                    s.ServiceType.IsGenericType &&
                    s.ServiceType.GetGenericTypeDefinition() == typeof(IUserIdentifierProvider<>) &&
                    s.Lifetime == ServiceLifetime.Scoped);
            return registration?.ServiceType.GenericTypeArguments[0];
        }

        private static Type GetTenantIdentifierType(this IServiceCollection services)
        {
            var registration =
                services.SingleOrDefault(s =>
                    s.ServiceType.IsGenericType &&
                    s.ServiceType.GetGenericTypeDefinition() == typeof(ITenantIdentifierProvider<>) &&
                    s.Lifetime == ServiceLifetime.Scoped);
            return registration?.ServiceType.GenericTypeArguments[0];
        }

        private static Type GetLocaleIdentifierType(this IServiceCollection services)
        {
            var registration =
                services.SingleOrDefault(s =>
                    s.ServiceType.IsGenericType &&
                    s.ServiceType.GetGenericTypeDefinition() == typeof(ILocaleIdentifierProvider<>) &&
                    s.Lifetime == ServiceLifetime.Scoped);
            return registration?.ServiceType.GenericTypeArguments[0];
        }

        //private static void ValidateDataAccessClientOptions(this IServiceCollection services, DataAccessClientOptions dataAccessClientOptions, Type userIdentifierType, Type tenantIdentifierType, Type localeIdentifierType)
        //{

        //    ValidateUserIdentifierType(services, dataAccessClientOptions, userIdentifierType);

        //    ValidateTenantIdentifierType(services, dataAccessClientOptions, tenantIdentifierType);

        //    ValidateLocaleIdentifierType(services, dataAccessClientOptions, localeIdentifierType);
        //}

        //internal static void ValidateTenantIdentifierType(IServiceCollection services, DataAccessClientOptions dataAccessClientOptions, Type tenantIdentifierType)
        //{
        //    var tenantIdentifierRelatedEntityBehaviors = new[] { typeof(ITenantScopable<>) };


        //    bool hasEntityBehaviorsWithTenantIdentifier =
        //        ContainsEntityBehaviors(dataAccessClientOptions.EntityTypes, tenantIdentifierRelatedEntityBehaviors);
        //    if (hasEntityBehaviorsWithTenantIdentifier)
        //    {
        //        services.RequireRegistrationForGeneric(typeof(ITenantIdentifierProvider<>), ServiceLifetime.Scoped);

        //        var tenantIdentifierProviderType = typeof(ITenantIdentifierProvider<>).MakeGenericType(tenantIdentifierType);
        //        services.RequireRegistrationFor(tenantIdentifierProviderType, ServiceLifetime.Scoped);

        //        var entityBehaviorsWithWrongTenantIdentifierType = new Dictionary<Type, List<Type>>();
        //        foreach (var tenantIdentifierRelatedEntityBehavior in tenantIdentifierRelatedEntityBehaviors)
        //        {
        //            var types = GetEntityTypesWithWrongIdentifierTypeInEntityBehavior(
        //                dataAccessClientOptions.EntityTypes, tenantIdentifierRelatedEntityBehavior,
        //                tenantIdentifierType);

        //            if (types.Any())
        //            {
        //                if (entityBehaviorsWithWrongTenantIdentifierType.ContainsKey(tenantIdentifierRelatedEntityBehavior))
        //                {
        //                    entityBehaviorsWithWrongTenantIdentifierType[tenantIdentifierRelatedEntityBehavior]
        //                        .AddRange(types);
        //                }
        //                else
        //                {
        //                    entityBehaviorsWithWrongTenantIdentifierType.Add(tenantIdentifierRelatedEntityBehavior, types);
        //                }
        //            }
        //        }

        //        if (entityBehaviorsWithWrongTenantIdentifierType.Any())
        //        {
        //            var errorMessage = new StringBuilder();
        //            errorMessage.AppendLine("The following entity types have implemented the entityhavior interface with a wrong user identifier type:");
        //            foreach (var entityBehaviorWithWrongTenantIdentifierType in entityBehaviorsWithWrongTenantIdentifierType)
        //            {
        //                errorMessage.AppendLine($"EntityBehavior: {entityBehaviorWithWrongTenantIdentifierType.Key.Name}");
        //                foreach (var type in entityBehaviorWithWrongTenantIdentifierType.Value)
        //                {
        //                    errorMessage.AppendLine($"- {type.Name} ({type.FullName})");
        //                }
        //            }

        //            throw new InvalidOperationException(errorMessage.ToString());
        //        }
        //    }
        //}

        //internal static void ValidateUserIdentifierType(IServiceCollection services, DataAccessClientOptions dataAccessClientOptions, Type userIdentifierType)
        //{
        //    var userIdentifierRelatedEntityBehaviors =
        //        new[] { typeof(ICreatable<>), typeof(IModifiable<>), typeof(ISoftDeletable<>) };


        //    bool hasEntityBehaviorsWithUserIdentifier =
        //        ContainsEntityBehaviors(dataAccessClientOptions.EntityTypes, userIdentifierRelatedEntityBehaviors);
        //    if (hasEntityBehaviorsWithUserIdentifier)
        //    {
        //        services.RequireRegistrationForGeneric(typeof(IUserIdentifierProvider<>), ServiceLifetime.Scoped);

        //        var userIdentifierProviderType = typeof(IUserIdentifierProvider<>).MakeGenericType(userIdentifierType);
        //        services.RequireRegistrationFor(userIdentifierProviderType, ServiceLifetime.Scoped);

        //        var entityBehaviorsWithWrongUserIdentifierType = new Dictionary<Type, List<Type>>();
        //        foreach (var userIdentifierRelatedEntityBehavior in userIdentifierRelatedEntityBehaviors)
        //        {
        //            var types = GetEntityTypesWithWrongIdentifierTypeInEntityBehavior(
        //                dataAccessClientOptions.EntityTypes, userIdentifierRelatedEntityBehavior,
        //                userIdentifierType);

        //            if (types.Any())
        //            {
        //                if (entityBehaviorsWithWrongUserIdentifierType.ContainsKey(userIdentifierRelatedEntityBehavior))
        //                {
        //                    entityBehaviorsWithWrongUserIdentifierType[userIdentifierRelatedEntityBehavior]
        //                        .AddRange(types);
        //                }
        //                else
        //                {
        //                    entityBehaviorsWithWrongUserIdentifierType.Add(userIdentifierRelatedEntityBehavior, types);
        //                }
        //            }
        //        }

        //        if (entityBehaviorsWithWrongUserIdentifierType.Any())
        //        {
        //            var errorMessage = new StringBuilder();
        //            errorMessage.AppendLine(
        //                $"The current UserIdentifier type is: {userIdentifierType.Name}, the following entity types have implemented the entityhavior interface with a wrong user identifier type:");
        //            foreach (var entityBehaviorWithWrongUserIdentifierType in entityBehaviorsWithWrongUserIdentifierType)
        //            {
        //                errorMessage.AppendLine($"EntityBehavior: {entityBehaviorWithWrongUserIdentifierType.Key.Name}");
        //                foreach (var type in entityBehaviorWithWrongUserIdentifierType.Value)
        //                {
        //                    errorMessage.AppendLine($"- {type.Name} ({type.FullName})");
        //                }
        //            }

        //            throw new InvalidOperationException(errorMessage.ToString());
        //        }
        //    }
        //}

        //internal static void ValidateLocaleIdentifierType(IServiceCollection services, DataAccessClientOptions dataAccessClientOptions, Type localeIdentifierType)
        //{
        //    var localeIdentifierRelatedEntityBehaviors =
        //        new[] { typeof(ILocalizable<>) };

        //    bool hasEntityBehaviorsWithLocaleIdentifier =
        //        ContainsEntityBehaviors(dataAccessClientOptions.EntityTypes, localeIdentifierRelatedEntityBehaviors);
        //    if (hasEntityBehaviorsWithLocaleIdentifier)
        //    {
        //        services.RequireRegistrationForGeneric(typeof(ILocaleIdentifierProvider<>), ServiceLifetime.Scoped);

        //        var localeIdentifierProviderType = typeof(ILocaleIdentifierProvider<>).MakeGenericType(localeIdentifierType);
        //        services.RequireRegistrationFor(localeIdentifierProviderType, ServiceLifetime.Scoped);

        //        var entityBehaviorsWithWrongLocaleIdentifierType = new Dictionary<Type, List<Type>>();
        //        foreach (var localeIdentifierRelatedEntityBehavior in localeIdentifierRelatedEntityBehaviors)
        //        {
        //            var types = GetEntityTypesWithWrongIdentifierTypeInEntityBehavior(
        //                dataAccessClientOptions.EntityTypes, localeIdentifierRelatedEntityBehavior,
        //                localeIdentifierType);

        //            if (types.Any())
        //            {
        //                if (entityBehaviorsWithWrongLocaleIdentifierType.ContainsKey(localeIdentifierRelatedEntityBehavior))
        //                {
        //                    entityBehaviorsWithWrongLocaleIdentifierType[localeIdentifierRelatedEntityBehavior]
        //                        .AddRange(types);
        //                }
        //                else
        //                {
        //                    entityBehaviorsWithWrongLocaleIdentifierType.Add(localeIdentifierRelatedEntityBehavior, types);
        //                }
        //            }
        //        }

        //        if (entityBehaviorsWithWrongLocaleIdentifierType.Any())
        //        {
        //            var errorMessage = new StringBuilder();
        //            errorMessage.AppendLine(
        //                $"The current LocaleIdentifier type is: {localeIdentifierType.Name}, the following entity types have implemented the entityhavior interface with a wrong locale identifier type:");
        //            foreach (var entityBehaviorWithWrongLocaleIdentifierType in entityBehaviorsWithWrongLocaleIdentifierType)
        //            {
        //                errorMessage.AppendLine($"EntityBehavior: {entityBehaviorWithWrongLocaleIdentifierType.Key.Name}");
        //                foreach (var type in entityBehaviorWithWrongLocaleIdentifierType.Value)
        //                {
        //                    errorMessage.AppendLine($"- {type.Name} ({type.FullName})");
        //                }
        //            }

        //            throw new InvalidOperationException(errorMessage.ToString());
        //        }
        //    }
        //}

        ////private static void RequireRegistrationFor<TRegistrationType>(this IServiceCollection services,
        ////    ServiceLifetime serviceLifetime)
        ////{
        ////    var isRegisteredWithLifetime =
        ////        services.Any(s =>
        ////            s.ServiceType == typeof(TRegistrationType) &&
        ////            s.Lifetime == serviceLifetime);
        ////    if (!isRegisteredWithLifetime)
        ////    {
        ////        ThrowNoRegistrationFoundException(typeof(TRegistrationType), serviceLifetime);
        ////    }
        ////}


        //private static void RequireRegistrationForGeneric(this IServiceCollection services, Type registrationType, ServiceLifetime serviceLifetime)
        //{
        //    var isRegisteredWithLifetime =
        //        services.Any(s =>
        //            s.ServiceType.IsGenericType &&
        //            s.ServiceType.GetGenericTypeDefinition() == registrationType &&
        //            s.Lifetime == serviceLifetime);
        //    if (!isRegisteredWithLifetime)
        //    {
        //        ThrowNoRegistrationFoundException(registrationType, serviceLifetime);
        //    }
        //}

        //private static void RequireRegistrationFor(this IServiceCollection services, Type registrationType, ServiceLifetime serviceLifetime)
        //{
        //    var isRegisteredWithLifetime =
        //        services.Any(s =>
        //            s.ServiceType == registrationType &&
        //            s.Lifetime == serviceLifetime);
        //    if (!isRegisteredWithLifetime)
        //    {
        //        ThrowNoRegistrationFoundException(registrationType, serviceLifetime);
        //    }
        //}

        //private static void ThrowNoRegistrationFoundException(Type registrationType, ServiceLifetime serviceLifetime)
        //{
        //    throw new InvalidOperationException(
        //        $"No DI registration found for type {registrationType.FullName}, please register with LifeTime {serviceLifetime.ToString()} in DI");

        //}

        //private static bool ContainsEntityBehaviors(Type[] entityTypes, Type[] entityBehaviors = null)
        //{
        //    var containsEntityBehaviors = entityBehaviors != null && entityTypes
        //                                      .Any(c => c.GetInterfaces().Any(i =>
        //                                          i.IsGenericType && entityBehaviors.Contains(i.GetGenericTypeDefinition())));

        //    return containsEntityBehaviors;
        //}

        //private static List<Type> GetEntityTypesWithWrongIdentifierTypeInEntityBehavior(Type[] entityTypes, Type entityBehavior, Type identifierType)
        //{
        //    var entityTypesWithWrongIdentifierTypeInEntityBehavior = entityTypes
        //                                      .Where(c => c.GetInterfaces().Any(i =>
        //                                          i.IsGenericType &&
        //                                          i.GetGenericTypeDefinition() == entityBehavior &&
        //                                          i.GenericTypeArguments[0] != identifierType)).ToList();

        //    return entityTypesWithWrongIdentifierTypeInEntityBehavior;
        //}
    }
}