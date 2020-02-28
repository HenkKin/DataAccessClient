using System;
using DataAccessClient.EntityFrameworkCore.SqlServer.Resolvers;
using DataAccessClient.EntityFrameworkCore.SqlServer.Tests.TestModels;
using DataAccessClient.Providers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DataAccessClient.EntityFrameworkCore.SqlServer.Tests.TestBase
{
    public abstract class DbContextTestBase
    {
        protected IUnitOfWork UnitOfWork;
        protected IRepository<TestEntity> TestEntityRepository;
        protected IRepository<TestEntityView> TestEntityViewRepository;
        protected IServiceProvider ServiceProvider;
        protected TestDbContext TestDbContext;

        public DbContextTestBase(string testName)
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddScoped<IUserIdentifierProvider<int>, TestUserIdentifierProvider>();
            serviceCollection.AddScoped<ITenantIdentifierProvider<int>, TestTenantIdentifierProvider>();
            serviceCollection.AddScoped<ILocaleIdentifierProvider<string>, TestLocaleIdentifierProvider>();

            serviceCollection.AddDataAccessClient<TestDbContext>(
                conf => conf
                    .UsePooling(true)
                    .ConfigureEntityTypes(new[] { typeof(TestEntity), typeof(TestEntityTranslation), typeof(TestEntityView) })
                    .ConfigureDbContextOptions(builder => builder
                        .UseInMemoryDatabase(testName).EnableSensitiveDataLogging().EnableDetailedErrors()
                    )
            );
            ServiceProvider = serviceCollection.BuildServiceProvider().CreateScope().ServiceProvider;
            UnitOfWork = ServiceProvider.GetRequiredService<IUnitOfWork>();
            TestEntityRepository = ServiceProvider.GetRequiredService<IRepository<TestEntity>>();
            TestEntityViewRepository = ServiceProvider.GetRequiredService<IRepository<TestEntityView>>();
            TestDbContext = ServiceProvider.GetRequiredService<ISqlServerDbContextResolver<TestDbContext>>().Execute();
        }
    }
}
