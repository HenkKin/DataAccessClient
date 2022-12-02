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
                new TranslatedPropertyEntityBehaviorConfiguration()
            };

            if (dataAccessClientOptions.DisableUtcDateTimePropertyEntityBehavior == false)
            {
                entityBehaviorConfigurations.Add(new UtcDateTimePropertyEntityBehaviorConfiguration());
            }

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
    }
}