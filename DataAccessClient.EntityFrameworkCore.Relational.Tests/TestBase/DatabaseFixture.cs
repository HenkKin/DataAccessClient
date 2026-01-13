using System;
using DataAccessClient.EntityFrameworkCore.Relational.Tests.TestModels;
using DataAccessClient.EntityFrameworkCore.Relational;
using DataAccessClient.EntityFrameworkCore.Relational.Resolvers;
using DataAccessClient.Providers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DataAccessClient.EntityFrameworkCore.Relational.Tests.TestBase;

public class DatabaseFixture : IDisposable
{
    public IUnitOfWork UnitOfWork;
    public IRepository<TestEntity> TestEntityRepository;
    public IRepository<TestEntityView> TestEntityViewRepository;
    public IServiceProvider ServiceProvider;
    public TestDbContext TestDbContext;

    public DatabaseFixture()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddScoped<IUserIdentifierProvider<int>, TestUserIdentifierProvider>();
        serviceCollection.AddScoped<ITenantIdentifierProvider<int>, TestTenantIdentifierProvider>();
        serviceCollection.AddScoped<ILocaleIdentifierProvider<string>, TestLocaleIdentifierProvider>();

        serviceCollection.AddDataAccessClient<TestDbContext>(
            conf => conf
                .UsePooling(true)
                .ConfigureDbContextOptions(builder => builder
                    .UseInMemoryDatabase(/*testName*/ "DataAccessClient.EntityFrameworkCore.Relational.Tests").EnableSensitiveDataLogging().EnableDetailedErrors()
                )
        );
        ServiceProvider = serviceCollection.BuildServiceProvider(true).CreateScope().ServiceProvider;
        UnitOfWork = ServiceProvider.GetRequiredService<IUnitOfWork>();
        TestEntityRepository = ServiceProvider.GetRequiredService<IRepository<TestEntity>>();
        TestEntityViewRepository = ServiceProvider.GetRequiredService<IRepository<TestEntityView>>();
        TestDbContext = ServiceProvider.GetRequiredService<IRelationalDbContextResolver<TestDbContext>>().Execute();
    }

    public void Dispose()
    {
        TestDbContext?.Dispose();
    }
}