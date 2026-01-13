using System.Linq;
using System.Threading.Tasks;
using DataAccessClient.Configuration;
using DataAccessClient.EntityFrameworkCore.Relational.Tests.TestBase;
using DataAccessClient.EntityFrameworkCore.Relational.Tests.TestModels;
using DataAccessClient.Providers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DataAccessClient.EntityFrameworkCore.Relational.Tests.Configuration.EntityBehaviors.IntegrationsTests
{
    public class SoftDeletableIntegrationTests : DbContextTestBase
    {
        public SoftDeletableIntegrationTests(DatabaseFixture databaseFixture) : base(nameof(SoftDeletableIntegrationTests), databaseFixture)
        {
        }

        [Fact]
        public async Task SoftDeletableQueryFilter_WhenCalled_ItShouldApplyQueryFilter()
        {
            // Arrange
            var userIdentifierProvider = (TestUserIdentifierProvider)DatabaseFixture.ServiceProvider.GetRequiredService<IUserIdentifierProvider<int>>();
            var softDeletableConfiguration = DatabaseFixture.ServiceProvider.GetRequiredService<ISoftDeletableConfiguration>();

            var userIdentifier = 15;
            userIdentifierProvider.ChangeUserIdentifier(userIdentifier);

            var testEntity1 = new TestEntity();
            DatabaseFixture.TestEntityRepository.Add(testEntity1);
            var testEntity2 = new TestEntity();
            DatabaseFixture.TestEntityRepository.Add(testEntity2);
            
            await DatabaseFixture.UnitOfWork.SaveAsync();

            DatabaseFixture.TestEntityRepository.Remove(testEntity1);

            await DatabaseFixture.UnitOfWork.SaveAsync();
            Assert.True(testEntity1.IsDeleted);
            Assert.Equal(userIdentifier, testEntity1.DeletedById);
            Assert.NotNull(testEntity1.DeletedOn);

            softDeletableConfiguration.EnableQueryFilter();
            Assert.True(softDeletableConfiguration.IsQueryFilterEnabled);

            using (softDeletableConfiguration.Disable())
            {
                Assert.False(softDeletableConfiguration.IsEnabled);
                // IsEnabled overrides IsQueryFilterEnabled
                Assert.True(softDeletableConfiguration.IsQueryFilterEnabled);

                var allEntities = await DatabaseFixture.TestEntityRepository.GetReadOnlyQuery().ToListAsync();
                Assert.Equal(2, allEntities.Count);
            }
            Assert.True(softDeletableConfiguration.IsEnabled);
            Assert.True(softDeletableConfiguration.IsQueryFilterEnabled);

            using (softDeletableConfiguration.DisableQueryFilter())
            {
                Assert.False(softDeletableConfiguration.IsQueryFilterEnabled);

                var allEntities = await DatabaseFixture.TestEntityRepository.GetReadOnlyQuery().ToListAsync();
                Assert.Equal(2, allEntities.Count);
            }

            Assert.True(softDeletableConfiguration.IsQueryFilterEnabled);

            var notDeletedEntities = await DatabaseFixture.TestEntityRepository.GetReadOnlyQuery().ToListAsync();
            Assert.Single(notDeletedEntities);
            Assert.False(notDeletedEntities.Single().IsDeleted);

            using (softDeletableConfiguration.DisableQueryFilter())
            {
                Assert.False(softDeletableConfiguration.IsQueryFilterEnabled);

                var deletedEntities = await DatabaseFixture.TestEntityRepository.GetReadOnlyQuery()
                    .Where(x=>x.IsDeleted).ToListAsync();
                Assert.Single(deletedEntities);
                Assert.True(deletedEntities.Single().IsDeleted);
            }
            Assert.True(softDeletableConfiguration.IsQueryFilterEnabled);
        }
    }
}
