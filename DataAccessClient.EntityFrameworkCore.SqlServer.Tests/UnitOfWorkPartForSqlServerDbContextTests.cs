using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DataAccessClient.Configuration;
using DataAccessClient.EntityFrameworkCore.SqlServer.Configuration.EntityBehaviors;
using DataAccessClient.EntityFrameworkCore.SqlServer.Infrastructure;
using DataAccessClient.EntityFrameworkCore.SqlServer.Resolvers;
using DataAccessClient.EntityFrameworkCore.SqlServer.Tests.TestModels;
using DataAccessClient.Providers;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace DataAccessClient.EntityFrameworkCore.SqlServer.Tests
{
    public class UnitOfWorkPartForSqlServerDbContextTests
    {
        private static readonly DbContextOptions Options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: "UnitOfWorkPartForSqlServerDbContextTests")
            .Options.WithExtension(new DataAccessClientOptionsExtension().WithEntityBehaviors(new List<IEntityBehaviorConfiguration>()
            {
                new IdentifiableEntityBehaviorConfiguration(),
                new CreatableEntityBehaviorConfiguration<int>(),
                new ModifiableEntityBehaviorConfiguration<int>(),
                new SoftDeletableEntityBehaviorConfiguration<int>(),
                new RowVersionableEntityBehaviorConfiguration(),
                new LocalizableEntityBehaviorConfiguration<string>(),
                new TenantScopeableEntityBehaviorConfiguration<int>(),
                new TranslatableEntityBehaviorConfiguration()
            }));

        [Fact]
        public async Task SaveAsync_WhenCalled_ItShouldCallSaveChangesAsyncOnDbContext()
        {
            // Arrange
            var mockRepository = new MockRepository(MockBehavior.Strict);

            var testDbContextMock = mockRepository.Create<TestDbContext>(Options);
            var testDbContextResolverMock = mockRepository.Create<ISqlServerDbContextResolver<TestDbContext>>();

            testDbContextResolverMock.Setup(x => x.Execute())
                .Returns(testDbContextMock.Object);

            testDbContextMock.Setup(x => x.SaveChangesAsync(CancellationToken.None))
                .ReturnsAsync(1);

            var unitOfWorkPart = new UnitOfWorkPartForSqlServerDbContext<TestDbContext>(testDbContextResolverMock.Object);

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
            var testDbContextResolverMock = new Mock<ISqlServerDbContextResolver<TestDbContext>>();

            var testDbContext = new TestDbContext(Options);
            testDbContextResolverMock.Setup(x => x.Execute())
                .Returns(testDbContext);

            var testEntity = new TestEntity();
            testDbContext.Entry(testEntity).State = state;

            var unitOfWorkPart = new UnitOfWorkPartForSqlServerDbContext<TestDbContext>(testDbContextResolverMock.Object);

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

            var testDbContextResolverMock = new Mock<ISqlServerDbContextResolver<TestDbContext>>();

            var testDbContext = new TestDbContext(Options);
            var testUserIdentifierProvider = new TestUserIdentifierProvider();
            var testTenantIdentifierProvider = new TestTenantIdentifierProvider();
            var testLocaleIdentifierProvider = new TestLocaleIdentifierProvider();
            
            var executionContext = new SqlServerDbContextExecutionContext(new Dictionary<string, dynamic> {
                {typeof(IUserIdentifierProvider<int>).Name,  testUserIdentifierProvider},
                {typeof(ITenantIdentifierProvider<int>).Name,  testTenantIdentifierProvider},
                {typeof(ILocaleIdentifierProvider<string>).Name,  testLocaleIdentifierProvider},
                {typeof(ISoftDeletableConfiguration).Name, new DefaultSoftDeletableConfiguration()},
                {typeof(IMultiTenancyConfiguration).Name,  new DefaultMultiTenancyConfiguration()},
                {typeof(ILocalizationConfiguration).Name,  new DefaultLocalizationConfiguration()},
            });
            testDbContext.Initialize(executionContext);

            testDbContextResolverMock.Setup(x => x.Execute())
                .Returns(testDbContext);

            var testEntity = new TestEntity();
            testDbContext.Add(testEntity);
            testDbContext.SaveChanges();

            testDbContext.Entry(testEntity).State = state;
            var unitOfWorkPart = new UnitOfWorkPartForSqlServerDbContext<TestDbContext>(testDbContextResolverMock.Object);

            // Act
            unitOfWorkPart.Reset();

            // Assert
            Assert.Equal(expectedState, testDbContext.Entry(testEntity).State);
        }
    }
}
