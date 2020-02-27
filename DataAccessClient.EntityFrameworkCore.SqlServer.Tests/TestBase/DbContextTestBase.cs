using System;
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
        protected IServiceProvider ServiceProvider;

        public DbContextTestBase(string testName)
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddScoped<IUserIdentifierProvider<int>, TestUserIdentifierProvider>();
            serviceCollection.AddScoped<ITenantIdentifierProvider<int>, TestTenantIdentifierProvider>();

            serviceCollection.AddDataAccessClient<TestDbContext>(
                conf => conf
                    .UsePooling(true)
                    .ConfigureEntityTypes(new[] { typeof(TestEntity), typeof(TestEntityTranslation) })
                    .ConfigureDbContextOptions(builder => builder
                        .UseInMemoryDatabase(testName)
                    )
            );
            ServiceProvider = serviceCollection.BuildServiceProvider().CreateScope().ServiceProvider;
            UnitOfWork = ServiceProvider.GetRequiredService<IUnitOfWork>();
            TestEntityRepository = ServiceProvider.GetRequiredService<IRepository<TestEntity>>();
        }
    }
}
