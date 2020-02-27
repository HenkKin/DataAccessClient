﻿using System;
using System.Threading;
using System.Threading.Tasks;
using DataAccessClient.Configuration;
using DataAccessClient.EntityFrameworkCore.SqlServer.Resolvers;
using DataAccessClient.EntityFrameworkCore.SqlServer.Tests.TestModels;
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

            var options = new DbContextOptionsBuilder<TestDbContext>()
                .WithUserIdentifierType(typeof(int))
                .WithTenantIdentifierType(typeof(int))
                .Options;

            var testDbContextMock = mockRepository.Create<TestDbContext>(options);
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
            var serviceProviderMock = new Mock<IServiceProvider>();

            var testDbContextResolverMock = new Mock<ISqlServerDbContextResolver<TestDbContext>>();

            var options = new DbContextOptionsBuilder<TestDbContext>()
                .WithUserIdentifierType(typeof(int))
                .WithTenantIdentifierType(typeof(int))
                .UseInMemoryDatabase(databaseName: "Reset_WhenCalled_ItShouldCallSaveChangesAsyncOnDbContext")
                .UseApplicationServiceProvider(serviceProviderMock.Object)
                .Options;

            var testDbContext = new TestDbContext(options);
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

            var options = new DbContextOptionsBuilder<TestDbContext>()
                .WithUserIdentifierType(typeof(int))
                .WithTenantIdentifierType(typeof(int))
                .UseInMemoryDatabase(databaseName: "Reset_WhenCalled_ItShouldCallSaveChangesAsyncOnDbContext")
                .UseApplicationServiceProvider(serviceProviderMock.Object)
                .Options;

            var testDbContext = new TestDbContext(options);
            var testUserIdentifierProvider = new TestUserIdentifierProvider();
            var testTenantIdentifierProvider = new TestTenantIdentifierProvider();
            testDbContext.Initialize(()=> testUserIdentifierProvider.Execute(), ()=> testTenantIdentifierProvider.Execute(), new DefaultSoftDeletableConfiguration(), new DefaultMultiTenancyConfiguration());
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
