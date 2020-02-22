using System.Linq;
using System.Threading.Tasks;
using DataAccessClient.Configuration;
using DataAccessClient.EntityFrameworkCore.SqlServer.Tests.TestBase;
using DataAccessClient.EntityFrameworkCore.SqlServer.Tests.TestModels;
using DataAccessClient.Providers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DataAccessClient.EntityFrameworkCore.SqlServer.Tests.EntityBehaviors
{
    public class TenantScopableIntegrationTests : DbContextTestBase
    {
        public TenantScopableIntegrationTests() : base(nameof(TenantScopableIntegrationTests))
        {
        }

        [Fact]
        public async Task TenantScopableQueryFilter_WhenCalled_ItShouldApplyQueryFilter()
        {
            // Arrange
            var tenantIdentifierProvider = (TestTenantIdentifierProvider)ServiceProvider.GetRequiredService<ITenantIdentifierProvider<int>>();
            var multiTenancyConfiguration = ServiceProvider.GetRequiredService<IMultiTenancyConfiguration>();
            
            tenantIdentifierProvider.ChangeTenantIdentifier(1);
            var testEntityTenant1 = new TestEntity();
            TestEntityRepository.Add(testEntityTenant1);
            await UnitOfWork.SaveAsync();

            tenantIdentifierProvider.ChangeTenantIdentifier(2);
            var testEntityTenant2 = new TestEntity();
            TestEntityRepository.Add(testEntityTenant2);
            await UnitOfWork.SaveAsync();

            multiTenancyConfiguration.EnableQueryFilter();
            Assert.True(multiTenancyConfiguration.IsQueryFilterEnabled);

            using (multiTenancyConfiguration.DisableQueryFilter())
            {
                Assert.False(multiTenancyConfiguration.IsQueryFilterEnabled);

                var allTenantEntities = await TestEntityRepository.GetReadOnlyQuery().ToListAsync();
                Assert.Equal(2, allTenantEntities.Count);
            }

            Assert.True(multiTenancyConfiguration.IsQueryFilterEnabled);

            tenantIdentifierProvider.ChangeTenantIdentifier(1);
            var tenant1Entities = await TestEntityRepository.GetReadOnlyQuery().ToListAsync();
            Assert.Single(tenant1Entities);
            Assert.Equal(1, tenant1Entities.Single().TenantId);

            tenantIdentifierProvider.ChangeTenantIdentifier(2);
            var tenant2Entities = await TestEntityRepository.GetReadOnlyQuery().ToListAsync();
            Assert.Single(tenant2Entities);
            Assert.Equal(2, tenant2Entities.Single().TenantId);
        }
    }
}
