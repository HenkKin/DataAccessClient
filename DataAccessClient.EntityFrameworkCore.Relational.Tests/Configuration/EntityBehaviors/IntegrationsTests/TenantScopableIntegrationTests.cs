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
    public class TenantScopableIntegrationTests : DbContextTestBase
    {
        public TenantScopableIntegrationTests(DatabaseFixture databaseFixture) : base(nameof(TenantScopableIntegrationTests), databaseFixture)
        {
        }

        [Fact]
        public async Task TenantScopableQueryFilter_WhenCalled_ItShouldApplyQueryFilter()
        {
            // Arrange
            var tenantIdentifierProvider = (TestTenantIdentifierProvider)DatabaseFixture.ServiceProvider.GetRequiredService<ITenantIdentifierProvider<int>>();
            var multiTenancyConfiguration = DatabaseFixture.ServiceProvider.GetRequiredService<IMultiTenancyConfiguration>();
            
            tenantIdentifierProvider.ChangeTenantIdentifier(1);
            var testEntityTenant1 = new TestEntity();
            DatabaseFixture.TestEntityRepository.Add(testEntityTenant1);
            await DatabaseFixture.UnitOfWork.SaveAsync();

            tenantIdentifierProvider.ChangeTenantIdentifier(2);
            var testEntityTenant2 = new TestEntity();
            DatabaseFixture.TestEntityRepository.Add(testEntityTenant2);
            await DatabaseFixture.UnitOfWork.SaveAsync();

            multiTenancyConfiguration.EnableQueryFilter();
            Assert.True(multiTenancyConfiguration.IsQueryFilterEnabled);

            using (multiTenancyConfiguration.DisableQueryFilter())
            {
                Assert.False(multiTenancyConfiguration.IsQueryFilterEnabled);

                var allTenantEntities = await DatabaseFixture.TestEntityRepository.GetReadOnlyQuery().ToListAsync();
                Assert.Equal(2, allTenantEntities.Count);
            }

            Assert.True(multiTenancyConfiguration.IsQueryFilterEnabled);

            tenantIdentifierProvider.ChangeTenantIdentifier(1);
            var tenant1Entities = await DatabaseFixture.TestEntityRepository.GetReadOnlyQuery().ToListAsync();
            Assert.Single(tenant1Entities);
            Assert.Equal(1, tenant1Entities.Single().TenantId);

            tenantIdentifierProvider.ChangeTenantIdentifier(2);
            var tenant2Entities = await DatabaseFixture.TestEntityRepository.GetReadOnlyQuery().ToListAsync();
            Assert.Single(tenant2Entities);
            Assert.Equal(2, tenant2Entities.Single().TenantId);
        }
    }
}
