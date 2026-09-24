using System;
using System.Collections.Generic;
using System.Linq;
using DataAccessClient.EntityFrameworkCore.Relational.Resolvers;
using DataAccessClient.EntityFrameworkCore.Relational.Tests.TestModels;
using DataAccessClient.Providers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DataAccessClient.EntityFrameworkCore.Relational.Tests
{
    public class DbContextResolutionTests
    {
        // EntityFrameworkCore builds an internal IServiceProvider per AddDataAccessClient registration and warns above twenty,
        // so the service providers are shared by all tests which can use the same configuration
        private static readonly Lazy<IServiceProvider> PoolingServiceProvider =
            new Lazy<IServiceProvider>(() => BuildServiceProvider());

        private static readonly Lazy<IServiceProvider> WithoutPoolingServiceProvider =
            new Lazy<IServiceProvider>(() => BuildServiceProvider(usePooling: false));

        [Fact]
        public void WhenSavingWithDbContextNotResolvedFromServiceProvider_ItShouldThrowInvalidOperationException()
        {
            using var scope = PoolingServiceProvider.Value.CreateScope();
            using var dbContext = new TestDbContext(scope.ServiceProvider.GetRequiredService<DbContextOptions<TestDbContext>>());

            Assert.Null(dbContext.ExecutionContext);

            dbContext.TestEntities.Add(new TestEntity { Description = "ORD-1" });

            var exception = Assert.Throws<InvalidOperationException>(() => dbContext.SaveChanges());

            Assert.True(HasNotInitializedMessage(exception));
        }

        [Fact]
        public void WhenQueryingWithDbContextNotResolvedFromServiceProvider_ItShouldThrowInvalidOperationException()
        {
            using var scope = PoolingServiceProvider.Value.CreateScope();
            using var dbContext = new TestDbContext(scope.ServiceProvider.GetRequiredService<DbContextOptions<TestDbContext>>());

            Assert.Null(dbContext.ExecutionContext);

            // EntityFrameworkCore wraps exceptions thrown while evaluating a query filter, so the chain is inspected
            var exception = Assert.ThrowsAny<Exception>(() => dbContext.TestEntities.ToList());

            Assert.True(HasNotInitializedMessage(exception));
        }

        [Fact]
        public void WhenDbContextIsResolvedWithResolverFirst_ItShouldReturnSameInitializedDbContextInstance()
        {
            using var scope = PoolingServiceProvider.Value.CreateScope();
            var resolvedDbContext = scope.ServiceProvider.GetRequiredService<IRelationalDbContextResolver<TestDbContext>>().Execute();
            var dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();

            Assert.Same(resolvedDbContext, dbContext);
            Assert.NotNull(dbContext.ExecutionContext);
        }

        [Fact]
        public void WhenDbContextIsResolvedFromServiceProvider_ItShouldBeInitialized()
        {
            var serviceProvider = PoolingServiceProvider.Value;

            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();

            Assert.NotNull(dbContext.ExecutionContext);

            dbContext.TestEntities.Add(new TestEntity { Description = "ORD-1" });
            dbContext.SaveChanges();

            var testEntity = Assert.Single(dbContext.TestEntities.ToList());
            Assert.Equal(1, testEntity.TenantId);
            Assert.Equal(1, testEntity.CreatedById);
        }

        [Fact]
        public void WithoutPooling_WhenDbContextIsResolvedFromServiceProvider_ItShouldBeInitialized()
        {
            var serviceProvider = WithoutPoolingServiceProvider.Value;

            using var scope1 = serviceProvider.CreateScope();
            using var scope2 = serviceProvider.CreateScope();

            var dbContext1 = scope1.ServiceProvider.GetRequiredService<TestDbContext>();
            var dbContext2 = scope2.ServiceProvider.GetRequiredService<TestDbContext>();

            Assert.NotNull(dbContext1.ExecutionContext);
            Assert.NotNull(dbContext2.ExecutionContext);
            Assert.NotSame(dbContext1, dbContext2);
            Assert.NotSame(dbContext1.ExecutionContext, dbContext2.ExecutionContext);

            dbContext1.TestEntities.Add(new TestEntity { Description = "ORD-1" });
            dbContext1.SaveChanges();

            var testEntity = Assert.Single(dbContext1.TestEntities.ToList());
            Assert.Equal(1, testEntity.TenantId);
            Assert.Equal(1, testEntity.CreatedById);
        }

        [Fact]
        public void WhenDbContextIsResolvedWithResolver_ItShouldReturnSameInitializedDbContextInstance()
        {
            var serviceProvider = PoolingServiceProvider.Value;

            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            var executionContext = dbContext.ExecutionContext;

            var resolvedDbContext = scope.ServiceProvider.GetRequiredService<IRelationalDbContextResolver<TestDbContext>>().Execute();

            Assert.Same(dbContext, resolvedDbContext);
            Assert.Same(executionContext, resolvedDbContext.ExecutionContext);
        }

        [Fact]
        public void WhenUsingMultipleScopes_ItShouldUseTenantIdentifierOfOwnScope()
        {
            var tenantIdentifiers = new Queue<int>(new[] { 1, 1, 2 });
            var serviceProvider = BuildServiceProvider(
                tenantIdentifierProviderFactory: () => CreateTenantIdentifierProvider(tenantIdentifiers.Dequeue()));

            using (var seedScope = serviceProvider.CreateScope())
            {
                var seedDbContext = seedScope.ServiceProvider.GetRequiredService<TestDbContext>();
                seedDbContext.TestEntities.Add(new TestEntity { Description = "ORD-1" });
                seedDbContext.SaveChanges();
            }

            using (var ownerScope = serviceProvider.CreateScope())
            {
                var ownerDbContext = ownerScope.ServiceProvider.GetRequiredService<TestDbContext>();
                Assert.Equal("ORD-1", Assert.Single(ownerDbContext.TestEntities.ToList()).Description);
            }

            using (var otherTenantScope = serviceProvider.CreateScope())
            {
                var otherTenantDbContext = otherTenantScope.ServiceProvider.GetRequiredService<TestDbContext>();
                Assert.Empty(otherTenantDbContext.TestEntities.ToList());
            }
        }

        [Fact]
        public void WhenUsingMultipleScopes_ItShouldReusePooledDbContextInstances()
        {
            var serviceProvider = PoolingServiceProvider.Value;

            Guid firstInstanceId;
            using (var scope1 = serviceProvider.CreateScope())
            {
                firstInstanceId = scope1.ServiceProvider.GetRequiredService<TestDbContext>().ContextId.InstanceId;
            }

            using var scope2 = serviceProvider.CreateScope();
            var dbContext = scope2.ServiceProvider.GetRequiredService<TestDbContext>();

            Assert.Equal(firstInstanceId, dbContext.ContextId.InstanceId);
            Assert.NotNull(dbContext.ExecutionContext);
        }

        [Fact]
        public void WhenTenantIdentifierProviderIsNotRegistered_ItShouldThrowInvalidOperationException()
        {
            var serviceProvider = BuildServiceProvider(
                tenantIdentifierProviderFactory: () => null);

            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();

            dbContext.TestEntities.Add(new TestEntity { Description = "ORD-1" });

            var exception = Assert.Throws<InvalidOperationException>(() => dbContext.SaveChanges());

            Assert.Contains(nameof(ITenantIdentifierProvider<int>), exception.Message);
        }

        private static IServiceProvider BuildServiceProvider(bool usePooling = true,
            Func<ITenantIdentifierProvider<int>> tenantIdentifierProviderFactory = null)
        {
            var databaseName = Guid.NewGuid().ToString();

            IServiceCollection serviceCollection = new ServiceCollection();
            serviceCollection.AddScoped<IUserIdentifierProvider<int>, TestUserIdentifierProvider>();
            serviceCollection.AddScoped<ILocaleIdentifierProvider<string>, TestLocaleIdentifierProvider>();
            serviceCollection.AddScoped(_ => tenantIdentifierProviderFactory != null
                ? tenantIdentifierProviderFactory()
                : new TestTenantIdentifierProvider());

            serviceCollection.AddDataAccessClient<TestDbContext>(
                conf => conf
                    .UsePooling(usePooling)
                    .ConfigureDbContextOptions(builder => builder
                        .UseInMemoryDatabase(databaseName)
                    )
            );

            return serviceCollection.BuildServiceProvider();
        }

        private static ITenantIdentifierProvider<int> CreateTenantIdentifierProvider(int tenantIdentifier)
        {
            var tenantIdentifierProvider = new TestTenantIdentifierProvider();
            tenantIdentifierProvider.ChangeTenantIdentifier(tenantIdentifier);
            return tenantIdentifierProvider;
        }

        private static bool HasNotInitializedMessage(Exception exception)
        {
            while (exception != null)
            {
                if (exception is InvalidOperationException &&
                    exception.Message.Contains(typeof(TestDbContext).FullName) &&
                    exception.Message.Contains(nameof(IRelationalDbContextResolver<RelationalDbContext>)))
                {
                    return true;
                }

                exception = exception.InnerException;
            }

            return false;
        }
    }
}
