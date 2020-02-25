using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DataAccessClient.Tests
{
    public class UnitOfWorkIntegrationTests
    {
        [Fact]
        public async Task SaveChangesAsync_WhenHavingMultipleDbContext_ItShouldUseTransactionScope()
        {
            IServiceCollection serviceCollection = new ServiceCollection();
            serviceCollection.AddScoped<IUnitOfWorkPart, TestUnitOfWorkPart<MyFirstDbContext>>();
            serviceCollection.AddScoped<IUnitOfWorkPart, TestUnitOfWorkPart<MySecondDbContext>>();
            serviceCollection.AddScoped<IUnitOfWork, UnitOfWork>();
            serviceCollection.AddDbContextPool<MyFirstDbContext>(o =>
            {
                o.UseInMemoryDatabase(
                    nameof(SaveChangesAsync_WhenHavingMultipleDbContext_ItShouldUseTransactionScope) + "_" + nameof(MyFirstDbContext));
            });

            serviceCollection.AddDbContextPool<MySecondDbContext>(o =>
            {
                o.UseInMemoryDatabase(
                    nameof(SaveChangesAsync_WhenHavingMultipleDbContext_ItShouldUseTransactionScope) + "_" + nameof(MySecondDbContext));
            });

            IServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();

            using var scope1 = serviceProvider.CreateScope();
            IUnitOfWork unitOfWork = scope1.ServiceProvider.GetRequiredService<IUnitOfWork>();
            MyFirstDbContext myFirstDbContext = scope1.ServiceProvider.GetRequiredService<MyFirstDbContext>();
            MySecondDbContext mySecondDbContext = scope1.ServiceProvider.GetRequiredService<MySecondDbContext>();

            var firstEntity = new Entity();
            myFirstDbContext.Entities.Add(firstEntity);

            var secondEntity = new Entity();
            mySecondDbContext.Entities.Add(secondEntity);

            await unitOfWork.SaveAsync();

            using var scope2 = scope1.ServiceProvider.CreateScope();
            MyFirstDbContext myFirstDbContext2 = scope2.ServiceProvider.GetRequiredService<MyFirstDbContext>();
            MySecondDbContext mySecondDbContext2 = scope2.ServiceProvider.GetRequiredService<MySecondDbContext>();

            Assert.Equal(1, await myFirstDbContext2.Entities.CountAsync());
            Assert.Equal(1, await mySecondDbContext2.Entities.CountAsync());
        }


        internal class Entity
        {
            public int EntityId { get; set; }
            public string Name { get; set; }
        }

        internal class MyFirstDbContext : DbContext
        {
            public MyFirstDbContext(DbContextOptions<MyFirstDbContext> options) : base(options)
            {
            }

            public DbSet<Entity> Entities { get; set; }
        }

        internal class MySecondDbContext : DbContext
        {
            public MySecondDbContext(DbContextOptions<MySecondDbContext> options) : base(options)
            {
            }

            public DbSet<Entity> Entities { get; set; }
        }

        internal class TestUnitOfWorkPart<TDbContext> : IUnitOfWorkPart where TDbContext : DbContext
        {
            private readonly TDbContext _dbContext;

            public TestUnitOfWorkPart(TDbContext dbContext)
            {
                _dbContext = dbContext;
            }
            public async Task SaveAsync()
            {
                await _dbContext.SaveChangesAsync();
            }

            public void Reset()
            {
                
            }
        }
    }
}