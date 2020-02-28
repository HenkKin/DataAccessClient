using DataAccessClient.EntityFrameworkCore.SqlServer.Tests.TestModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DataAccessClient.EntityFrameworkCore.SqlServer.Tests
{
    public class WithCascadeTimingOnSaveChangesTests
    {
        [Fact]
        public void WhenWorkIsDoneWithCascadeTimingOnSaveChanges_ItShouldResetToCascadeTimingImmediate()
        {
            // Arrange
            IServiceCollection services = new ServiceCollection();

            services.AddDbContext<TestDbContext>(builder => builder
                .WithUserIdentifierType(typeof(int))
                .WithTenantIdentifierType(typeof(int))
                .WithLocaleIdentifierType(typeof(string))
                .UseInMemoryDatabase(nameof(WhenWorkIsDoneWithCascadeTimingOnSaveChanges_ItShouldResetToCascadeTimingImmediate))
            );

            var serviceProvider = services.BuildServiceProvider().CreateScope();

            var dbContext = serviceProvider.ServiceProvider.GetService<TestDbContext>();

            // EF Core defaults to Immediate since EF Core 3.0, before 3.0 to OnSaveChanges
            // https://docs.microsoft.com/en-us/ef/core/what-is-new/ef-core-3.0/breaking-changes#cascade
            Assert.Equal(CascadeTiming.Immediate, dbContext.CascadeDeleteTiming);
            Assert.Equal(CascadeTiming.Immediate, dbContext.DeleteOrphansTiming);

            using (var subject = new WithCascadeTimingOnSaveChanges(dbContext))
            {

                subject.Run(() =>
                {
                    Assert.Equal(CascadeTiming.OnSaveChanges, dbContext.CascadeDeleteTiming);
                    Assert.Equal(CascadeTiming.OnSaveChanges, dbContext.DeleteOrphansTiming);
                });
            }

            // Act
            Assert.Equal(CascadeTiming.Immediate, dbContext.CascadeDeleteTiming);
            Assert.Equal(CascadeTiming.Immediate, dbContext.DeleteOrphansTiming);
        }
    }
}
