using System;
using System.Collections.Generic;
using DataAccessClient.EntityFrameworkCore.Relational.Tests.TestModels;
using DataAccessClient.EntityFrameworkCore.Relational;
using DataAccessClient.EntityFrameworkCore.Relational.Resolvers;
using DataAccessClient.Providers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
// ReSharper disable once ClassNeverInstantiated.Local
// ReSharper disable ParameterOnlyUsedForPreconditionCheck.Local
// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedMember.Global

namespace DataAccessClient.EntityFrameworkCore.Relational.Tests
{
    public class RelationalDbContextResolvingWithServiceScopesTests
    {
        [Fact]
        public void WithDbContextPooling_WhenHavingNestedChildScopes_ItShouldResolveDbContextPerChildScope()
        {
            IServiceCollection serviceCollection = new ServiceCollection();

            serviceCollection.AddScoped<IUserIdentifierProvider<int>, TestUserIdentifierProvider>();
            serviceCollection.AddScoped<ITenantIdentifierProvider<int>, TestTenantIdentifierProvider>();
            serviceCollection.AddScoped<ILocaleIdentifierProvider<string>, TestLocaleIdentifierProvider>();
            
            serviceCollection.AddDataAccessClient<MyDbContext>(
                conf => conf
                    .UsePooling(true)
                    .ConfigureDbContextOptions(builder => builder
                        .UseInMemoryDatabase(nameof(WithDbContextPooling_WhenHavingNestedChildScopes_ItShouldResolveDbContextPerChildScope))
                    )
            );

            IServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();
            RunNestedChildScopes(serviceProvider);
        }
        
        [Fact]
        public void WithoutDbContextPooling_WhenHavingChildScope_ItShouldResolveDbContextPerChildScope()
        {
            IServiceCollection serviceCollection = new ServiceCollection();

            serviceCollection.AddScoped<IUserIdentifierProvider<int>, TestUserIdentifierProvider>();
            serviceCollection.AddScoped<ITenantIdentifierProvider<int>, TestTenantIdentifierProvider>();
            serviceCollection.AddScoped<ILocaleIdentifierProvider<string>, TestLocaleIdentifierProvider>();

            serviceCollection.AddDataAccessClient<MyDbContext>(
                conf => conf
                    .UsePooling(false)
                    .ConfigureDbContextOptions(builder => builder
                        .UseInMemoryDatabase(nameof(WithoutDbContextPooling_WhenHavingChildScope_ItShouldResolveDbContextPerChildScope))
                    )
            );

            IServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();
            RunNestedChildScopes(serviceProvider);
        }

        [Fact]
        public void WithDbContextPooling_WhenMultipleDbContextAreResolvedFromSameScope_ItShouldReturnSameDbContextInstance()
        {
            IServiceCollection serviceCollection = new ServiceCollection();

            serviceCollection.AddScoped<IUserIdentifierProvider<int>, TestUserIdentifierProvider>();
            serviceCollection.AddScoped<ITenantIdentifierProvider<int>, TestTenantIdentifierProvider>();
            serviceCollection.AddScoped<ILocaleIdentifierProvider<string>, TestLocaleIdentifierProvider>();

            serviceCollection.AddDataAccessClient<MyDbContext>(
                conf => conf
                    .UsePooling(true)
                    .ConfigureDbContextOptions(builder => builder
                        .UseInMemoryDatabase(nameof(WithDbContextPooling_WhenMultipleDbContextAreResolvedFromSameScope_ItShouldReturnSameDbContextInstance))
                    )
            );
            IServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();
            RunMultipleResolvesOfServiceScope(serviceProvider);
        }

        [Fact]
        public void WithoutDbContextPooling_WhenMultipleDbContextAreResolvedFromSameScope_ItShouldReturnSameDbContextInstance()
        {
            IServiceCollection serviceCollection = new ServiceCollection();

            serviceCollection.AddScoped<IUserIdentifierProvider<int>, TestUserIdentifierProvider>();
            serviceCollection.AddScoped<ITenantIdentifierProvider<int>, TestTenantIdentifierProvider>();
            serviceCollection.AddScoped<ILocaleIdentifierProvider<string>, TestLocaleIdentifierProvider>();

            serviceCollection.AddDataAccessClient<MyDbContext>(
                conf => conf
                    .UsePooling(false)
                    .ConfigureDbContextOptions(builder => builder
                        .UseInMemoryDatabase(nameof(WithoutDbContextPooling_WhenMultipleDbContextAreResolvedFromSameScope_ItShouldReturnSameDbContextInstance))
                    )
            );
            IServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();
            RunMultipleResolvesOfServiceScope(serviceProvider);
        }

        static void RunNestedChildScopes(IServiceProvider serviceProvider)
        {
            using var scope1 = serviceProvider.CreateScope();
            TestUserIdentifierProvider userIdentifierProvider1 = scope1.ServiceProvider.GetRequiredService<IUserIdentifierProvider<int>>() as TestUserIdentifierProvider;
            TestTenantIdentifierProvider tenantIdentifierProvider1 = scope1.ServiceProvider.GetRequiredService<ITenantIdentifierProvider<int>>() as TestTenantIdentifierProvider;
            TestLocaleIdentifierProvider localeIdentifierProvider1 = scope1.ServiceProvider.GetRequiredService<ILocaleIdentifierProvider<string>>() as TestLocaleIdentifierProvider;
            MyDbContext dbContext1 = scope1.ServiceProvider.GetRequiredService<IRelationalDbContextResolver<MyDbContext>>().Execute();

            var executionContext = new RelationalDbContextExecutionContext(new Dictionary<string, dynamic> {
                {typeof(IUserIdentifierProvider<int>).Name,  userIdentifierProvider1},
                {typeof(ITenantIdentifierProvider<int>).Name,  tenantIdentifierProvider1},
                {typeof(ILocaleIdentifierProvider<string>).Name,  localeIdentifierProvider1}
            });
            dbContext1.Initialize(executionContext);

            dbContext1.HasSameUserIdentifierProvider(userIdentifierProvider1);
            dbContext1.HasSameTenantIdentifierProvider(tenantIdentifierProvider1);
            dbContext1.HasSameLocaleIdentifierProvider(localeIdentifierProvider1);

            using var scope2 = scope1.ServiceProvider.CreateScope();
            TestUserIdentifierProvider userIdentifierProvider2 = scope2.ServiceProvider.GetRequiredService<IUserIdentifierProvider<int>>() as TestUserIdentifierProvider;
            TestTenantIdentifierProvider tenantIdentifierProvider2 = scope2.ServiceProvider.GetRequiredService<ITenantIdentifierProvider<int>>() as TestTenantIdentifierProvider;
            TestLocaleIdentifierProvider localeIdentifierProvider2 = scope2.ServiceProvider.GetRequiredService<ILocaleIdentifierProvider<string>>() as TestLocaleIdentifierProvider;
            MyDbContext dbContext2 = scope2.ServiceProvider.GetRequiredService<IRelationalDbContextResolver<MyDbContext>>().Execute();

            var executionContext2 = new RelationalDbContextExecutionContext(new Dictionary<string, dynamic> {
                {typeof(IUserIdentifierProvider<int>).Name,  userIdentifierProvider2},
                {typeof(ITenantIdentifierProvider<int>).Name,  tenantIdentifierProvider2},
                {typeof(ILocaleIdentifierProvider<string>).Name,  localeIdentifierProvider2}
            });
            dbContext2.Initialize(executionContext2);

            Assert.NotSame(dbContext1, dbContext2);
            Assert.NotEqual(dbContext1.ContextId, dbContext2.ContextId);

            dbContext2.HasSameUserIdentifierProvider(userIdentifierProvider2);
            dbContext2.HasSameTenantIdentifierProvider(tenantIdentifierProvider2);
            dbContext2.HasSameLocaleIdentifierProvider(localeIdentifierProvider2);

            using var scope3 = scope2.ServiceProvider.CreateScope();
            TestUserIdentifierProvider userIdentifierProvider3 = scope3.ServiceProvider.GetService<IUserIdentifierProvider<int>>() as TestUserIdentifierProvider;
            TestTenantIdentifierProvider tenantIdentifierProvider3 = scope3.ServiceProvider.GetService<ITenantIdentifierProvider<int>>() as TestTenantIdentifierProvider;
            TestLocaleIdentifierProvider localeIdentifierProvider3 = scope3.ServiceProvider.GetRequiredService<ILocaleIdentifierProvider<string>>() as TestLocaleIdentifierProvider;
            MyDbContext dbContext3 = scope3.ServiceProvider.GetRequiredService<IRelationalDbContextResolver<MyDbContext>>().Execute();

            var executionContext3 = new RelationalDbContextExecutionContext(new Dictionary<string, dynamic> {
                {typeof(IUserIdentifierProvider<int>).Name,  userIdentifierProvider3},
                {typeof(ITenantIdentifierProvider<int>).Name,  tenantIdentifierProvider3},
                {typeof(ILocaleIdentifierProvider<string>).Name,  localeIdentifierProvider3}
            });
            dbContext3.Initialize(executionContext3);

            Assert.NotSame(dbContext1, dbContext3);
            Assert.NotEqual(dbContext1.ContextId, dbContext3.ContextId);
            Assert.NotSame(dbContext2, dbContext3);
            Assert.NotEqual(dbContext2.ContextId, dbContext3.ContextId);

            dbContext3.HasSameUserIdentifierProvider(userIdentifierProvider3);
            dbContext3.HasSameTenantIdentifierProvider(tenantIdentifierProvider3);
            dbContext3.HasSameTenantIdentifierProvider(tenantIdentifierProvider3);
        }

        static void RunMultipleResolvesOfServiceScope(IServiceProvider serviceProvider)
        {
            using var scope1 = serviceProvider.CreateScope();
            TestUserIdentifierProvider userIdentifierProvider1 =
                scope1.ServiceProvider.GetService<IUserIdentifierProvider<int>>() as TestUserIdentifierProvider;
            TestTenantIdentifierProvider tenantIdentifierProvider1 =
                scope1.ServiceProvider.GetService<ITenantIdentifierProvider<int>>() as TestTenantIdentifierProvider;
            TestLocaleIdentifierProvider localeIdentifierProvider1 = scope1.ServiceProvider.GetRequiredService<ILocaleIdentifierProvider<string>>() as TestLocaleIdentifierProvider;
            MyDbContext dbContext1 = scope1.ServiceProvider
                .GetRequiredService<IRelationalDbContextResolver<MyDbContext>>()
                .Execute();

            var executionContext = new RelationalDbContextExecutionContext(new Dictionary<string, dynamic> {
                {typeof(IUserIdentifierProvider<int>).Name,  userIdentifierProvider1},
                {typeof(ITenantIdentifierProvider<int>).Name,  tenantIdentifierProvider1},
                {typeof(ILocaleIdentifierProvider<string>).Name,  localeIdentifierProvider1}
            });
            dbContext1.Initialize(executionContext);
            dbContext1.HasSameUserIdentifierProvider(userIdentifierProvider1);
            dbContext1.HasSameTenantIdentifierProvider(tenantIdentifierProvider1);
            dbContext1.HasSameLocaleIdentifierProvider(localeIdentifierProvider1);

            MyDbContext dbContext2 = scope1.ServiceProvider
                .GetRequiredService<IRelationalDbContextResolver<MyDbContext>>()
                .Execute();

            Assert.Same(dbContext1, dbContext2);
            Assert.Equal(dbContext1.ContextId, dbContext2.ContextId);

            dbContext2.HasSameUserIdentifierProvider(userIdentifierProvider1);
            dbContext2.HasSameTenantIdentifierProvider(tenantIdentifierProvider1);
            dbContext2.HasSameLocaleIdentifierProvider(localeIdentifierProvider1);

            MyDbContext dbContext3 = scope1.ServiceProvider
                .GetRequiredService<IRelationalDbContextResolver<MyDbContext>>()
                .Execute();

            Assert.Same(dbContext1, dbContext3);
            Assert.Equal(dbContext1.ContextId, dbContext3.ContextId);
            Assert.Same(dbContext2, dbContext3);
            Assert.Equal(dbContext2.ContextId, dbContext3.ContextId);

            dbContext3.HasSameUserIdentifierProvider(userIdentifierProvider1);
            dbContext3.HasSameTenantIdentifierProvider(tenantIdentifierProvider1);
            dbContext3.HasSameLocaleIdentifierProvider(localeIdentifierProvider1);
        }

        class Entity
        {
            public int EntityId { get; set; }
            public string Name { get; set; }
        }

        private class MyDbContext : RelationalDbContext
        {
            public MyDbContext(DbContextOptions<MyDbContext> options) : base(options)
            { }

            public DbSet<Entity> Entities { get; set; }

            public void HasSameUserIdentifierProvider(TestUserIdentifierProvider expectedUserIdentifierProvider)
            {
                Assert.Same(expectedUserIdentifierProvider, ExecutionContext.Get<IUserIdentifierProvider<int>>());
                Assert.Equal(expectedUserIdentifierProvider.InstanceId, ((TestUserIdentifierProvider)ExecutionContext.Get<IUserIdentifierProvider<int>>()).InstanceId);
            }

            public void HasSameTenantIdentifierProvider(TestTenantIdentifierProvider expectedTenantIdentifierProvider)
            {
                Assert.Same(expectedTenantIdentifierProvider, ExecutionContext.Get<ITenantIdentifierProvider<int>>());
                Assert.Equal(expectedTenantIdentifierProvider.InstanceId, ((TestTenantIdentifierProvider)ExecutionContext.Get<ITenantIdentifierProvider<int>>()).InstanceId);
            }

            public void HasSameLocaleIdentifierProvider(TestLocaleIdentifierProvider expectedLocaleIdentifierProvider)
            {
                Assert.Same(expectedLocaleIdentifierProvider, ExecutionContext.Get<ILocaleIdentifierProvider<string>>());
                Assert.Equal(expectedLocaleIdentifierProvider.InstanceId, ((TestLocaleIdentifierProvider)ExecutionContext.Get<ILocaleIdentifierProvider<string>>()).InstanceId);
            }
        }
    }
}
