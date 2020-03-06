using System.Linq;
using System.Threading.Tasks;
using DataAccessClient.Configuration;
using DataAccessClient.EntityFrameworkCore.SqlServer.Tests.TestBase;
using DataAccessClient.EntityFrameworkCore.SqlServer.Tests.TestModels;
using DataAccessClient.Providers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DataAccessClient.EntityFrameworkCore.SqlServer.Tests.Configuration.EntityBehaviors.IntegrationsTests
{
    public class SoftDeletableIntegrationTests : DbContextTestBase
    {
        public SoftDeletableIntegrationTests() : base(nameof(SoftDeletableIntegrationTests))
        {
        }

        [Fact]
        public async Task SoftDeletableQueryFilter_WhenCalled_ItShouldApplyQueryFilter()
        {
            // Arrange
            var userIdentifierProvider = (TestUserIdentifierProvider)ServiceProvider.GetRequiredService<IUserIdentifierProvider<int>>();
            var softDeletableConfiguration = ServiceProvider.GetRequiredService<ISoftDeletableConfiguration>();

            var userIdentifier = 15;
            userIdentifierProvider.ChangeUserIdentifier(userIdentifier);

            var testEntity1 = new TestEntity();
            TestEntityRepository.Add(testEntity1);
            var testEntity2 = new TestEntity();
            TestEntityRepository.Add(testEntity2);
            
            await UnitOfWork.SaveAsync();

            TestEntityRepository.Remove(testEntity1);

            await UnitOfWork.SaveAsync();
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

                var allEntities = await TestEntityRepository.GetReadOnlyQuery().ToListAsync();
                Assert.Equal(2, allEntities.Count);
            }
            Assert.True(softDeletableConfiguration.IsEnabled);
            Assert.True(softDeletableConfiguration.IsQueryFilterEnabled);

            using (softDeletableConfiguration.DisableQueryFilter())
            {
                Assert.False(softDeletableConfiguration.IsQueryFilterEnabled);

                var allEntities = await TestEntityRepository.GetReadOnlyQuery().ToListAsync();
                Assert.Equal(2, allEntities.Count);
            }

            Assert.True(softDeletableConfiguration.IsQueryFilterEnabled);

            var notDeletedEntities = await TestEntityRepository.GetReadOnlyQuery().ToListAsync();
            Assert.Single(notDeletedEntities);
            Assert.False(notDeletedEntities.Single().IsDeleted);

            using (softDeletableConfiguration.DisableQueryFilter())
            {
                Assert.False(softDeletableConfiguration.IsQueryFilterEnabled);

                var deletedEntities = await TestEntityRepository.GetReadOnlyQuery()
                    .Where(x=>x.IsDeleted).ToListAsync();
                Assert.Single(deletedEntities);
                Assert.True(deletedEntities.Single().IsDeleted);
            }
            Assert.True(softDeletableConfiguration.IsQueryFilterEnabled);
        }
    }
}
