using System;
using DataAccessClient.EntityFrameworkCore.SqlServer.Resolvers;
using DataAccessClient.EntityFrameworkCore.SqlServer.Tests.TestModels;
using DataAccessClient.Providers;
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
            serviceCollection.AddScoped<ILocaleIdentifierProvider<string>, TestLocaleIdentifierProvider>();
            
            serviceCollection.AddDataAccessClient<MyDbContext>(
                conf => conf
                    .UsePooling(true)
                    .ConfigureEntityTypes(new[] {typeof(Entity)})
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
                    .ConfigureEntityTypes(new[] { typeof(Entity) })
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
                    .ConfigureEntityTypes(new[] { typeof(Entity) })
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
                    .ConfigureEntityTypes(new[] { typeof(Entity) })
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
            MyDbContext dbContext1 = scope1.ServiceProvider.GetRequiredService<ISqlServerDbContextResolver<MyDbContext>>().Execute();

            object UserIdentifierProvider1Func() => userIdentifierProvider1?.Execute();
            object TenantIdentifierProvider1Func() => tenantIdentifierProvider1?.Execute();
            object LocaleIdentifierProvider1Func() => localeIdentifierProvider1?.Execute();
            dbContext1.Initialize(UserIdentifierProvider1Func, TenantIdentifierProvider1Func, LocaleIdentifierProvider1Func, null, null, null);

            dbContext1.HasSameUserIdentifierProvider(UserIdentifierProvider1Func);
            dbContext1.HasSameTenantIdentifierProvider(TenantIdentifierProvider1Func);

            using var scope2 = scope1.ServiceProvider.CreateScope();
            TestUserIdentifierProvider userIdentifierProvider2 = scope2.ServiceProvider.GetRequiredService<IUserIdentifierProvider<int>>() as TestUserIdentifierProvider;
            TestTenantIdentifierProvider tenantIdentifierProvider2 = scope2.ServiceProvider.GetRequiredService<ITenantIdentifierProvider<int>>() as TestTenantIdentifierProvider;
            TestLocaleIdentifierProvider localeIdentifierProvider2 = scope2.ServiceProvider.GetRequiredService<ILocaleIdentifierProvider<string>>() as TestLocaleIdentifierProvider;
            MyDbContext dbContext2 = scope2.ServiceProvider.GetRequiredService<ISqlServerDbContextResolver<MyDbContext>>().Execute();
            object UserIdentifierProvider2Func() => userIdentifierProvider2?.Execute();
            object TenantIdentifierProvider2Func() => tenantIdentifierProvider2?.Execute();
            object LocaleIdentifierProvider2Func() => localeIdentifierProvider2?.Execute();
            dbContext2.Initialize(UserIdentifierProvider2Func, TenantIdentifierProvider2Func, LocaleIdentifierProvider2Func, null, null, null);

            Assert.NotSame(dbContext1, dbContext2);
            Assert.NotEqual(dbContext1.ContextId, dbContext2.ContextId);

            dbContext2.HasSameUserIdentifierProvider(UserIdentifierProvider2Func);
            dbContext2.HasSameTenantIdentifierProvider(TenantIdentifierProvider2Func);

            using var scope3 = scope2.ServiceProvider.CreateScope();
            TestUserIdentifierProvider userIdentifierProvider3 = scope3.ServiceProvider.GetService<IUserIdentifierProvider<int>>() as TestUserIdentifierProvider;
            TestTenantIdentifierProvider tenantIdentifierProvider3 = scope3.ServiceProvider.GetService<ITenantIdentifierProvider<int>>() as TestTenantIdentifierProvider;
            TestLocaleIdentifierProvider localeIdentifierProvider3 = scope3.ServiceProvider.GetRequiredService<ILocaleIdentifierProvider<string>>() as TestLocaleIdentifierProvider;
            MyDbContext dbContext3 = scope3.ServiceProvider.GetRequiredService<ISqlServerDbContextResolver<MyDbContext>>().Execute();

            object UserIdentifierProvider3Func() => userIdentifierProvider3?.Execute();
            object TenantIdentifierProvider3Func() => tenantIdentifierProvider3?.Execute();
            object LocaleIdentifierProvider3Func() => localeIdentifierProvider3?.Execute();
            dbContext3.Initialize(UserIdentifierProvider3Func, TenantIdentifierProvider3Func, LocaleIdentifierProvider3Func, null, null, null);

            Assert.NotSame(dbContext1, dbContext3);
            Assert.NotEqual(dbContext1.ContextId, dbContext3.ContextId);
            Assert.NotSame(dbContext2, dbContext3);
            Assert.NotEqual(dbContext2.ContextId, dbContext3.ContextId);

            dbContext3.HasSameUserIdentifierProvider(UserIdentifierProvider3Func);
            dbContext3.HasSameTenantIdentifierProvider(TenantIdentifierProvider3Func);
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
                .GetRequiredService<ISqlServerDbContextResolver<MyDbContext>>()
                .Execute();

            object UserIdentifierProvider1Func() => userIdentifierProvider1?.Execute();
            object TenantIdentifierProvider1Func() => tenantIdentifierProvider1?.Execute();
            object LocaleIdentifierProvider1Func() => localeIdentifierProvider1?.Execute();
            dbContext1.Initialize(UserIdentifierProvider1Func, TenantIdentifierProvider1Func, LocaleIdentifierProvider1Func, null, null, null);

            dbContext1.HasSameUserIdentifierProvider(UserIdentifierProvider1Func);
            dbContext1.HasSameTenantIdentifierProvider(TenantIdentifierProvider1Func);

            MyDbContext dbContext2 = scope1.ServiceProvider
                .GetRequiredService<ISqlServerDbContextResolver<MyDbContext>>()
                .Execute();

            dbContext2.Initialize(UserIdentifierProvider1Func, TenantIdentifierProvider1Func, LocaleIdentifierProvider1Func, null, null, null);

            Assert.Same(dbContext1, dbContext2);
            Assert.Equal(dbContext1.ContextId, dbContext2.ContextId);

            dbContext2.HasSameUserIdentifierProvider(UserIdentifierProvider1Func);
            dbContext2.HasSameTenantIdentifierProvider(TenantIdentifierProvider1Func);

            MyDbContext dbContext3 = scope1.ServiceProvider
                .GetRequiredService<ISqlServerDbContextResolver<MyDbContext>>()
                .Execute();
            dbContext3.Initialize(UserIdentifierProvider1Func, TenantIdentifierProvider1Func, LocaleIdentifierProvider1Func, null, null, null);

            Assert.Same(dbContext1, dbContext3);
            Assert.Equal(dbContext1.ContextId, dbContext3.ContextId);
            Assert.Same(dbContext2, dbContext3);
            Assert.Equal(dbContext2.ContextId, dbContext3.ContextId);

            dbContext3.HasSameUserIdentifierProvider(UserIdentifierProvider1Func);
            dbContext3.HasSameTenantIdentifierProvider(TenantIdentifierProvider1Func);
        }

        class Entity
        {
            public int EntityId { get; set; }
            public string Name { get; set; }
        }

        private class MyDbContext : SqlServerDbContext
        {
            public MyDbContext(DbContextOptions<MyDbContext> options) : base(options)
            { }

            public DbSet<Entity> Entities { get; set; }

            public void HasSameUserIdentifierProvider(Func<object> expectedUserIdentifierProvider)
            {
                //Assert.Same(expectedUserIdentifierProvider, UserIdentifierProvider);
                Assert.Equal(expectedUserIdentifierProvider(), UserIdentifierProvider());
            }

            public void HasSameTenantIdentifierProvider(Func<object> expectedTenantIdentifierProvider)
            {
                //Assert.Same(expectedTenantIdentifierProvider, TenantIdentifierProvider);
                // Assert.Equal(expectedTenantIdentifierProvider.InstanceId, ((TestTenantIdentifierProvider) TenantIdentifierProvider).InstanceId);
                Assert.Equal(expectedTenantIdentifierProvider(), TenantIdentifierProvider());
            }
        }
    }
}
