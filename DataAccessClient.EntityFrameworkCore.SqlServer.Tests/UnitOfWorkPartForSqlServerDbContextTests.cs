using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace DataAccessClient.EntityFrameworkCore.SqlServer.Tests
{
    public class UnitOfWorkPartForSqlServerDbContextTests
    {
        [Fact]
        public async Task SaveAsync_WhenCalled_ItShouldCallSaveChangesAsyncOnDbContext()
        {
            // Arrange
            var mockRepository = new MockRepository(MockBehavior.Strict);

            var testDbContextMock = mockRepository.Create<TestDbContext>();
            testDbContextMock.Setup(x => x.SaveChangesAsync(CancellationToken.None))
                .ReturnsAsync(1);

            var unitOfWorkPart = new UnitOfWorkPartForSqlServerDbContext<TestDbContext, int>(testDbContextMock.Object);

            // Act
            await unitOfWorkPart.SaveAsync();

            // Assert
            mockRepository.VerifyAll();
        }

        [Theory]
        [InlineData(EntityState.Added, EntityState.Detached)]
        [InlineData(EntityState.Deleted, EntityState.Detached)]
        [InlineData(EntityState.Modified, EntityState.Unchanged)]
        [InlineData(EntityState.Detached, EntityState.Detached)]
        [InlineData(EntityState.Unchanged, EntityState.Unchanged)]
        public void Reset_WhenNewEntityHasState_ItShouldResetEntityToExpectedState(EntityState state, EntityState expectedState)
        {
            // Arrange
            var serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock.Setup(x => x.GetService(typeof(IUserIdentifierProvider<int>)))
                .Returns(new TestUserIdentifierProvider());
            serviceProviderMock.Setup(x => x.GetService(typeof(ITenantIdentifierProvider<int>)))
                .Returns(new TestTenantIdentifierProvider());
            serviceProviderMock.Setup(x => x.GetService(typeof(ISoftDeletableConfiguration)))
                .Returns(new TestSoftDeletableConfiguration());
            serviceProviderMock.Setup(x => x.GetService(typeof(IMultiTenancyConfiguration<int>)))
                .Returns(new TestMultiTenancyConfiguration(new TestTenantIdentifierProvider()));

            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase(databaseName: "Reset_WhenCalled_ItShouldCallSaveChangesAsyncOnDbContext")
                .UseApplicationServiceProvider(serviceProviderMock.Object)
                .Options;

            var testDbContext = new TestDbContext(options);

            var testEntity = new TestEntity();
            testDbContext.Entry(testEntity).State = state;
            var unitOfWorkPart = new UnitOfWorkPartForSqlServerDbContext<TestDbContext, int>(testDbContext);

            // Act
            unitOfWorkPart.Reset();

            // Assert
            Assert.Equal(expectedState, testDbContext.Entry(testEntity).State);
        }

        [Theory]
        [InlineData(EntityState.Added, EntityState.Detached)]
        [InlineData(EntityState.Deleted, EntityState.Unchanged)]
        [InlineData(EntityState.Modified, EntityState.Unchanged)]
        [InlineData(EntityState.Detached, EntityState.Detached)]
        [InlineData(EntityState.Unchanged, EntityState.Unchanged)]
        public void Reset_WhenExstingEntityHasState_ItShouldResetEntityToExpectedState(EntityState state, EntityState expectedState)
        {
            // Arrange
            var serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock.Setup(x => x.GetService(typeof(IUserIdentifierProvider<int>)))
                .Returns(new TestUserIdentifierProvider());
            serviceProviderMock.Setup(x => x.GetService(typeof(ITenantIdentifierProvider<int>)))
                .Returns(new TestTenantIdentifierProvider());
            serviceProviderMock.Setup(x => x.GetService(typeof(ISoftDeletableConfiguration)))
                .Returns(new TestSoftDeletableConfiguration());
            serviceProviderMock.Setup(x => x.GetService(typeof(IMultiTenancyConfiguration<int>)))
                .Returns(new TestMultiTenancyConfiguration(new TestTenantIdentifierProvider()));

            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase(databaseName: "Reset_WhenCalled_ItShouldCallSaveChangesAsyncOnDbContext")
                .UseApplicationServiceProvider(serviceProviderMock.Object)
                .Options;

            var testDbContext = new TestDbContext(options);

            var testEntity = new TestEntity();
            testDbContext.Add(testEntity);
            testDbContext.SaveChanges();

            testDbContext.Entry(testEntity).State = state;
            var unitOfWorkPart = new UnitOfWorkPartForSqlServerDbContext<TestDbContext, int>(testDbContext);

            // Act
            unitOfWorkPart.Reset();

            // Assert
            Assert.Equal(expectedState, testDbContext.Entry(testEntity).State);
        }
    }
}
