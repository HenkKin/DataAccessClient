using System.Threading.Tasks;
using DataAccessClient.EntityFrameworkCore.SqlServer.Tests.TestBase;
using DataAccessClient.EntityFrameworkCore.SqlServer.Tests.TestModels;
using Xunit;

namespace DataAccessClient.EntityFrameworkCore.SqlServer.Tests.EntityBehaviors
{
    public class IdentiableIntegrationTests : DbContextTestBase
    {
        public IdentiableIntegrationTests() : base(nameof(IdentiableIntegrationTests))
        {
        }

        [Fact]
        public async Task Identiable_WhenCalled_ItShouldSetIdentiablePropertiesOnInsert()
        {
            // Arrange
            var testEntity1 = new TestEntity();
            var testEntity2 = new TestEntity();
            var testEntity3 = new TestEntity();

            TestEntityRepository.Add(testEntity1);
            TestEntityRepository.Add(testEntity2);
            TestEntityRepository.Add(testEntity3);

            Assert.Equal(1, testEntity1.Id);
            Assert.Equal(2, testEntity2.Id);
            Assert.Equal(3, testEntity3.Id);

            await UnitOfWork.SaveAsync();

            Assert.Equal(1, testEntity1.Id);
            Assert.Equal(2, testEntity2.Id);
            Assert.Equal(3, testEntity3.Id);

            testEntity1.Description = "updated1";
            testEntity2.Description = "updated2";
            testEntity3.Description = "updated3";

            await UnitOfWork.SaveAsync();

            Assert.Equal(1, testEntity1.Id);
            Assert.Equal(2, testEntity2.Id);
            Assert.Equal(3, testEntity3.Id);
        }
    }
}
