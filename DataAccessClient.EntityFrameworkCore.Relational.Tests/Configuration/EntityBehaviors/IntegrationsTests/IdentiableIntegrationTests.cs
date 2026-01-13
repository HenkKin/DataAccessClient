using System.Threading.Tasks;
using DataAccessClient.EntityFrameworkCore.Relational.Tests.TestBase;
using DataAccessClient.EntityFrameworkCore.Relational.Tests.TestModels;
using Xunit;

namespace DataAccessClient.EntityFrameworkCore.Relational.Tests.Configuration.EntityBehaviors.IntegrationsTests
{
    public class IdentiableIntegrationTests : DbContextTestBase
    {
        public IdentiableIntegrationTests(DatabaseFixture databaseFixture) : base(nameof(IdentiableIntegrationTests), databaseFixture)
        {
        }

        [Fact]
        public async Task Identiable_WhenCalled_ItShouldSetIdentiablePropertiesOnInsert()
        {
            // Arrange
            var testEntity1 = new TestEntity();
            var testEntity2 = new TestEntity();
            var testEntity3 = new TestEntity();

            DatabaseFixture.TestEntityRepository.Add(testEntity1);
            DatabaseFixture.TestEntityRepository.Add(testEntity2);
            DatabaseFixture.TestEntityRepository.Add(testEntity3);

            Assert.Equal(1, testEntity1.Id);
            Assert.Equal(2, testEntity2.Id);
            Assert.Equal(3, testEntity3.Id);

            await DatabaseFixture.UnitOfWork.SaveAsync();

            Assert.Equal(1, testEntity1.Id);
            Assert.Equal(2, testEntity2.Id);
            Assert.Equal(3, testEntity3.Id);

            testEntity1.Description = "updated1";
            testEntity2.Description = "updated2";
            testEntity3.Description = "updated3";

            await DatabaseFixture.UnitOfWork.SaveAsync();

            Assert.Equal(1, testEntity1.Id);
            Assert.Equal(2, testEntity2.Id);
            Assert.Equal(3, testEntity3.Id);
        }
    }
}
