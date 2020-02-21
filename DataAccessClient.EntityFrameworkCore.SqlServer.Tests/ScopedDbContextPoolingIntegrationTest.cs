using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DataAccessClient.EntityFrameworkCore.SqlServer.Tests
{
    public class ScopedDbContextPoolingIntegrationTest
    {
        [Fact]
        public void Pooled_TestDbContextPoolingWithScoping()
        {
            RunPooled();
        }

        [Fact]
        public void Unpooled_TestDbContextPoolingWithScoping()
        {
            RunUnpooled();
        }

        static void Run(IServiceProvider serviceProvider, string mode)
        {
            using (var scope = serviceProvider.CreateScope())
            {
                ConsoleService consoleService = scope.ServiceProvider.GetRequiredService<ConsoleService>();
                MyDbContext dbContext = scope.ServiceProvider.GetRequiredService<ISqlServerDbContextResolver<MyDbContext, int, int>>().Execute(serviceProvider);
                dbContext.HasSameInstance(consoleService, mode);
            }
        }

        static void RunUnpooled()
        {
            IServiceCollection serviceCollection = new ServiceCollection();
            serviceCollection.AddScoped<ConsoleService>();
            serviceCollection.AddScoped<IUserIdentifierProvider<int>, TestUserIdentifierProvider>();
            serviceCollection.AddScoped<ITenantIdentifierProvider<int>, TestTenantIdentifierProvider>();
            serviceCollection.AddDataAccessClient<MyDbContext, int, int>(o => { o.UseInMemoryDatabase("ScopeTest"); }, new []{typeof(Entity) });
            IServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();
            Run(serviceProvider, "Unpooled");
        }

        static void RunPooled()
        {
            IServiceCollection serviceCollection = new ServiceCollection();
            serviceCollection.AddScoped<ConsoleService>();
            serviceCollection.AddScoped<IUserIdentifierProvider<int>, TestUserIdentifierProvider>();
            serviceCollection.AddScoped<ITenantIdentifierProvider<int>, TestTenantIdentifierProvider>();
            serviceCollection.AddDataAccessClientPool<MyDbContext, int, int>(o => { o.UseInMemoryDatabase("ScopeTest"); }, new[] { typeof(Entity) });
            IServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();
            Run(serviceProvider, "Pooled");
        }


        class ConsoleService
        {
            public Guid Id { get; }

            public ConsoleService()
            {
                Id = Guid.NewGuid();

            }
        }

        class Entity
        {
            public int EntityId { get; set; }
            public string Name { get; set; }
        }

        class MyDbContext : SqlServerDbContext<int, int>
        {
            public MyDbContext(DbContextOptions<MyDbContext> options) : base(options)
            { }

            public DbSet<Entity> Entities { get; set; }

            public void HasSameInstance(ConsoleService expectedConsoleService, string mode)
            {
                var userIdentifierProvider = this.GetService<IUserIdentifierProvider<int>>() as TestUserIdentifierProvider;
                Assert.Same((_userIdentifierProvider as TestUserIdentifierProvider), userIdentifierProvider);
                Assert.Equal((_userIdentifierProvider as TestUserIdentifierProvider).InstanceId, userIdentifierProvider.InstanceId);
            }
        }
    }
}
