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
            var serviceProviderMock = mockRepository.Create<IServiceProvider>();
            var testDbContextResolverMock = mockRepository.Create<ISqlServerDbContextResolver<TestDbContext, int, int>>();

            testDbContextResolverMock.Setup(x => x.Execute(serviceProviderMock.Object))
                .Returns(testDbContextMock.Object);

            testDbContextMock.Setup(x => x.SaveChangesAsync(CancellationToken.None))
                .ReturnsAsync(1);

            var unitOfWorkPart = new UnitOfWorkPartForSqlServerDbContext<TestDbContext, int, int>(testDbContextResolverMock.Object, serviceProviderMock.Object);

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

            var testDbContextResolverMock = new Mock<ISqlServerDbContextResolver<TestDbContext, int, int>>();

            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase(databaseName: "Reset_WhenCalled_ItShouldCallSaveChangesAsyncOnDbContext")
                .UseApplicationServiceProvider(serviceProviderMock.Object)
                .Options;

            var testDbContext = new TestDbContext(options);
            testDbContextResolverMock.Setup(x => x.Execute(serviceProviderMock.Object))
                .Returns(testDbContext);

            var testEntity = new TestEntity();
            testDbContext.Entry(testEntity).State = state;

            var unitOfWorkPart = new UnitOfWorkPartForSqlServerDbContext<TestDbContext, int, int>(testDbContextResolverMock.Object, serviceProviderMock.Object);

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

            serviceProviderMock.Setup(x => x.GetService(typeof(IMultiTenancyConfiguration)))
                .Returns(new DefaultMultiTenancyConfiguration());

            var testDbContextResolverMock = new Mock<ISqlServerDbContextResolver<TestDbContext, int, int>>();

            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase(databaseName: "Reset_WhenCalled_ItShouldCallSaveChangesAsyncOnDbContext")
                .UseApplicationServiceProvider(serviceProviderMock.Object)
                .Options;

            var testDbContext = new TestDbContext(options);
            testDbContext.Initialize(new TestUserIdentifierProvider(), new TestTenantIdentifierProvider(), new DefaultSoftDeletableConfiguration(), new DefaultMultiTenancyConfiguration());
            testDbContextResolverMock.Setup(x => x.Execute(serviceProviderMock.Object))
                .Returns(testDbContext);

            var testEntity = new TestEntity();
            testDbContext.Add(testEntity);
            testDbContext.SaveChanges();

            testDbContext.Entry(testEntity).State = state;
            var unitOfWorkPart = new UnitOfWorkPartForSqlServerDbContext<TestDbContext, int, int>(testDbContextResolverMock.Object, serviceProviderMock.Object);

            // Act
            unitOfWorkPart.Reset();

            // Assert
            Assert.Equal(expectedState, testDbContext.Entry(testEntity).State);
        }
    }
}
