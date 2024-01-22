using System;
using System.Threading.Tasks;
using DataAccessClient.EntityFrameworkCore.SqlServer.Tests.TestBase;
using DataAccessClient.EntityFrameworkCore.SqlServer.Tests.TestModels;
using DataAccessClient.Providers;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DataAccessClient.EntityFrameworkCore.SqlServer.Tests.Configuration.EntityBehaviors.IntegrationsTests
{
    public class CreatableIntegrationTests : DbContextTestBase
    {
        public CreatableIntegrationTests(DatabaseFixture databaseFixture) : base(nameof(CreatableIntegrationTests), databaseFixture)
        {
        }

        [Fact]
        public async Task Creatable_WhenCalled_ItShouldSetCreatablePropertiesOnInsert()
        {
            // Arrange
            var userIdentifierProvider = (TestUserIdentifierProvider)DatabaseFixture.ServiceProvider.GetRequiredService<IUserIdentifierProvider<int>>();

            var userIdentifier = 15;
            userIdentifierProvider.ChangeUserIdentifier(userIdentifier);
            var testEntity = new TestEntity();
            DatabaseFixture.TestEntityRepository.Add(testEntity);
            await DatabaseFixture.UnitOfWork.SaveAsync();

            Assert.Equal(userIdentifier, testEntity.CreatedById);
            Assert.NotEqual(DateTime.MinValue, testEntity.CreatedOn);

            var createdById = testEntity.CreatedById;
            var createdOn = testEntity.CreatedOn;

            userIdentifierProvider.ChangeUserIdentifier(16);
            testEntity.Description = "updated";

            await DatabaseFixture.UnitOfWork.SaveAsync();

            Assert.Equal(createdById, testEntity.CreatedById);
            Assert.Equal(createdOn, testEntity.CreatedOn);
        }
    }
}
