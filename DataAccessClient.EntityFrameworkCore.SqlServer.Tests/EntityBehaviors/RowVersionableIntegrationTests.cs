﻿using System.Threading.Tasks;
using DataAccessClient.EntityFrameworkCore.SqlServer.Tests.TestBase;
using DataAccessClient.EntityFrameworkCore.SqlServer.Tests.TestModels;
using DataAccessClient.Exceptions;
using DataAccessClient.Providers;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DataAccessClient.EntityFrameworkCore.SqlServer.Tests.EntityBehaviors
{
    public class RowVersionableIntegrationTests : DbContextTestBase
    {
        public RowVersionableIntegrationTests() : base(nameof(RowVersionableIntegrationTests))
        {
        }

        [Fact]
        public async Task RowVersionable_WhenCalled_ItShouldApplyRowVersioning()
        {
            // Arrange
            var userIdentifierProvider = (TestUserIdentifierProvider)ServiceProvider.GetRequiredService<IUserIdentifierProvider<int>>();
            
            userIdentifierProvider.ChangeUserIdentifier(10);
            var testEntity = new TestEntity
            {
                RowVersion = new byte[] { 1 }
            };
            TestEntityRepository.Add(testEntity);
            await UnitOfWork.SaveAsync();

            var rowVersion = testEntity.RowVersion;
            var newRowVersion = new byte[] { 2 };
            Assert.NotEqual(rowVersion, newRowVersion);

            testEntity.Description = $"Updated with rowVersion {newRowVersion}";
            testEntity.RowVersion = newRowVersion;

            await Assert.ThrowsAsync<RowVersioningException>(() => UnitOfWork.SaveAsync());
          
            Assert.NotEqual(rowVersion, testEntity.RowVersion);
            Assert.Equal(newRowVersion, testEntity.RowVersion);
        }
    }
}
