using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
// ReSharper disable once ClassNeverInstantiated.Local
// ReSharper disable ParameterOnlyUsedForPreconditionCheck.Local
// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedMember.Global

namespace DataAccessClient.EntityFrameworkCore.SqlServer.Tests
{
    public class SqlServerDbContextResolvingWithServiceScopesTests
    {
        [Fact]
        public void WithDbContextPooling_WhenHavingNestedChildScopes_ItShouldResolveDbContextPerChildScope()
        {
            IServiceCollection serviceCollection = new ServiceCollection();
            serviceCollection.AddScoped<IUserIdentifierProvider<int>, TestUserIdentifierProvider>();
            serviceCollection.AddScoped<ITenantIdentifierProvider<int>, TestTenantIdentifierProvider>();
            serviceCollection.AddDataAccessClientPool<MyDbContext, int, int>(o => { o.UseInMemoryDatabase(nameof(WithDbContextPooling_WhenHavingNestedChildScopes_ItShouldResolveDbContextPerChildScope)); }, new[] { typeof(Entity) });
            IServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();
            RunNestedChildScopes(serviceProvider);
        }
        
        [Fact]
        public void WithoutDbContextPooling_WhenHavingChildScope_ItShouldResolveDbContextPerChildScope()
        {
            IServiceCollection serviceCollection = new ServiceCollection();
            serviceCollection.AddScoped<IUserIdentifierProvider<int>, TestUserIdentifierProvider>();
            serviceCollection.AddScoped<ITenantIdentifierProvider<int>, TestTenantIdentifierProvider>();
            serviceCollection.AddDataAccessClient<MyDbContext, int, int>(o => { o.UseInMemoryDatabase(nameof(WithoutDbContextPooling_WhenHavingChildScope_ItShouldResolveDbContextPerChildScope)); }, new[] { typeof(Entity) });
            IServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();
            RunNestedChildScopes(serviceProvider);
        }

        [Fact]
        public void WithDbContextPooling_WhenMultipleDbContextAreResolvedFromSameScope_ItShouldReturnSameDbContextInstance()
        {
            IServiceCollection serviceCollection = new ServiceCollection();
            serviceCollection.AddScoped<IUserIdentifierProvider<int>, TestUserIdentifierProvider>();
            serviceCollection.AddScoped<ITenantIdentifierProvider<int>, TestTenantIdentifierProvider>();
            serviceCollection.AddDataAccessClientPool<MyDbContext, int, int>(o => { o.UseInMemoryDatabase(nameof(WithDbContextPooling_WhenMultipleDbContextAreResolvedFromSameScope_ItShouldReturnSameDbContextInstance)); }, new[] { typeof(Entity) });
            IServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();
            RunMultipleResolvesOfServiceScope(serviceProvider);
        }

        [Fact]
        public void WithoutDbContextPooling_WhenMultipleDbContextAreResolvedFromSameScope_ItShouldReturnSameDbContextInstance()
        {
            IServiceCollection serviceCollection = new ServiceCollection();
            serviceCollection.AddScoped<IUserIdentifierProvider<int>, TestUserIdentifierProvider>();
            serviceCollection.AddScoped<ITenantIdentifierProvider<int>, TestTenantIdentifierProvider>();
            serviceCollection.AddDataAccessClient<MyDbContext, int, int>(o => { o.UseInMemoryDatabase(nameof(WithoutDbContextPooling_WhenMultipleDbContextAreResolvedFromSameScope_ItShouldReturnSameDbContextInstance)); }, new[] { typeof(Entity) });
            IServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();
            RunMultipleResolvesOfServiceScope(serviceProvider);
        }

        static void RunNestedChildScopes(IServiceProvider serviceProvider)
        {
            using var scope1 = serviceProvider.CreateScope();
            TestUserIdentifierProvider userIdentifierProvider1 = scope1.ServiceProvider.GetService<IUserIdentifierProvider<int>>() as TestUserIdentifierProvider;
            TestTenantIdentifierProvider tenantIdentifierProvider1 = scope1.ServiceProvider.GetService<ITenantIdentifierProvider<int>>() as TestTenantIdentifierProvider;
            MyDbContext dbContext1 = scope1.ServiceProvider.GetRequiredService<ISqlServerDbContextResolver<MyDbContext, int, int>>().Execute(scope1.ServiceProvider);
            dbContext1.Initialize(userIdentifierProvider1, tenantIdentifierProvider1, null, null);

            dbContext1.HasSameUserIdentifierProvider(userIdentifierProvider1);
            dbContext1.HasSameTenantIdentifierProvider(tenantIdentifierProvider1);

            using var scope2 = scope1.ServiceProvider.CreateScope();
            TestUserIdentifierProvider userIdentifierProvider2 = scope2.ServiceProvider.GetService<IUserIdentifierProvider<int>>() as TestUserIdentifierProvider;
            TestTenantIdentifierProvider tenantIdentifierProvider2 = scope2.ServiceProvider.GetService<ITenantIdentifierProvider<int>>() as TestTenantIdentifierProvider;
            MyDbContext dbContext2 = scope2.ServiceProvider.GetRequiredService<ISqlServerDbContextResolver<MyDbContext, int, int>>().Execute(scope2.ServiceProvider);
            dbContext2.Initialize(userIdentifierProvider2, tenantIdentifierProvider2, null, null);

            Assert.NotSame(dbContext1, dbContext2);
            Assert.NotEqual(dbContext1.ContextId, dbContext2.ContextId);

            dbContext2.HasSameUserIdentifierProvider(userIdentifierProvider2);
            dbContext2.HasSameTenantIdentifierProvider(tenantIdentifierProvider2);

            using var scope3 = scope2.ServiceProvider.CreateScope();
            TestUserIdentifierProvider userIdentifierProvider3 = scope3.ServiceProvider.GetService<IUserIdentifierProvider<int>>() as TestUserIdentifierProvider;
            TestTenantIdentifierProvider tenantIdentifierProvider3 = scope3.ServiceProvider.GetService<ITenantIdentifierProvider<int>>() as TestTenantIdentifierProvider;
            MyDbContext dbContext3 = scope3.ServiceProvider.GetRequiredService<ISqlServerDbContextResolver<MyDbContext, int, int>>().Execute(scope3.ServiceProvider);
            dbContext3.Initialize(userIdentifierProvider3, tenantIdentifierProvider3, null, null);

            Assert.NotSame(dbContext1, dbContext3);
            Assert.NotEqual(dbContext1.ContextId, dbContext3.ContextId);
            Assert.NotSame(dbContext2, dbContext3);
            Assert.NotEqual(dbContext2.ContextId, dbContext3.ContextId);

            dbContext3.HasSameUserIdentifierProvider(userIdentifierProvider3);
            dbContext3.HasSameTenantIdentifierProvider(tenantIdentifierProvider3);
        }

        static void RunMultipleResolvesOfServiceScope(IServiceProvider serviceProvider)
        {
            using var scope1 = serviceProvider.CreateScope();
            TestUserIdentifierProvider userIdentifierProvider1 =
                scope1.ServiceProvider.GetService<IUserIdentifierProvider<int>>() as TestUserIdentifierProvider;
            TestTenantIdentifierProvider tenantIdentifierProvider1 =
                scope1.ServiceProvider.GetService<ITenantIdentifierProvider<int>>() as TestTenantIdentifierProvider;
            MyDbContext dbContext1 = scope1.ServiceProvider
                .GetRequiredService<ISqlServerDbContextResolver<MyDbContext, int, int>>()
                .Execute(scope1.ServiceProvider);
            dbContext1.Initialize(userIdentifierProvider1, tenantIdentifierProvider1, null, null);

            dbContext1.HasSameUserIdentifierProvider(userIdentifierProvider1);
            dbContext1.HasSameTenantIdentifierProvider(tenantIdentifierProvider1);

            MyDbContext dbContext2 = scope1.ServiceProvider
                .GetRequiredService<ISqlServerDbContextResolver<MyDbContext, int, int>>()
                .Execute(scope1.ServiceProvider);
            dbContext2.Initialize(userIdentifierProvider1, tenantIdentifierProvider1, null, null);

            Assert.Same(dbContext1, dbContext2);
            Assert.Equal(dbContext1.ContextId, dbContext2.ContextId);

            dbContext2.HasSameUserIdentifierProvider(userIdentifierProvider1);
            dbContext2.HasSameTenantIdentifierProvider(tenantIdentifierProvider1);

            MyDbContext dbContext3 = scope1.ServiceProvider
                .GetRequiredService<ISqlServerDbContextResolver<MyDbContext, int, int>>()
                .Execute(scope1.ServiceProvider);
            dbContext3.Initialize(userIdentifierProvider1, tenantIdentifierProvider1, null, null);

            Assert.Same(dbContext1, dbContext3);
            Assert.Equal(dbContext1.ContextId, dbContext3.ContextId);
            Assert.Same(dbContext2, dbContext3);
            Assert.Equal(dbContext2.ContextId, dbContext3.ContextId);

            dbContext3.HasSameUserIdentifierProvider(userIdentifierProvider1);
            dbContext3.HasSameTenantIdentifierProvider(tenantIdentifierProvider1);
        }

        class Entity
        {
            public int EntityId { get; set; }
            public string Name { get; set; }
        }

        private class MyDbContext : SqlServerDbContext<int, int>
        {
            public MyDbContext(DbContextOptions<MyDbContext> options) : base(options)
            { }

            public DbSet<Entity> Entities { get; set; }

            public void HasSameUserIdentifierProvider(TestUserIdentifierProvider expectedUserIdentifierProvider)
            {
                Assert.Same(expectedUserIdentifierProvider, (UserIdentifierProvider as TestUserIdentifierProvider));
                Assert.Equal(expectedUserIdentifierProvider.InstanceId, ((TestUserIdentifierProvider) UserIdentifierProvider).InstanceId);
            }

            public void HasSameTenantIdentifierProvider(TestTenantIdentifierProvider expectedTenantIdentifierProvider)
            {
                Assert.Same(expectedTenantIdentifierProvider, TenantIdentifierProvider as TestTenantIdentifierProvider);
                Assert.Equal(expectedTenantIdentifierProvider.InstanceId, ((TestTenantIdentifierProvider) TenantIdentifierProvider).InstanceId);
            }
        }
    }
}
