﻿using System.Threading.Tasks;
using DataAccessClient.EntityFrameworkCore.SqlServer.Tests.TestBase;
using DataAccessClient.EntityFrameworkCore.SqlServer.Tests.TestModels;
using DataAccessClient.Providers;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DataAccessClient.EntityFrameworkCore.SqlServer.Tests.Configuration.EntityBehaviors.IntegrationsTests
{
    public class ModifiableIntegrationTests : DbContextTestBase
    {
        public ModifiableIntegrationTests(DatabaseFixture databaseFixture) : base(nameof(ModifiableIntegrationTests), databaseFixture)
        {
        }

        [Fact]
        public async Task Modifiable_WhenCalled_ItShouldSetModifiablePropertiesOnUpdate()
        {
            // Arrange
            var userIdentifierProvider = (TestUserIdentifierProvider)DatabaseFixture.ServiceProvider.GetRequiredService<IUserIdentifierProvider<int>>();

            var userIdentifier = 15;
            userIdentifierProvider.ChangeUserIdentifier(userIdentifier);
            var testEntity = new TestEntity();
            DatabaseFixture.TestEntityRepository.Add(testEntity);
            await DatabaseFixture.UnitOfWork.SaveAsync();

            Assert.Null(testEntity.ModifiedById);
            Assert.Null(testEntity.ModifiedOn);

            testEntity.Description = "Updated";
            await DatabaseFixture.UnitOfWork.SaveAsync();

            Assert.Equal(userIdentifier, testEntity.ModifiedById);
            Assert.NotNull(testEntity.ModifiedOn);
        }
    }
}
