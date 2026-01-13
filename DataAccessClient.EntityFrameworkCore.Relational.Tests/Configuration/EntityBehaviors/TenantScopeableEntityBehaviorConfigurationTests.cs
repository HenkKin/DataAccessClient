using DataAccessClient.EntityBehaviors;
using DataAccessClient.EntityFrameworkCore.Relational.Tests.TestModels;
using DataAccessClient.EntityFrameworkCore.Relational.Configuration.EntityBehaviors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Xunit;

namespace DataAccessClient.EntityFrameworkCore.Relational.Tests.Configuration.EntityBehaviors
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "EF1001:Internal EF Core API usage.", Justification = "<Pending>")]
    public class TenantScopeableEntityBehaviorConfigurationTests
    {
        [Fact]
        public void ModelBuilder_IsTenantScopable_WhenNotCalled_ItShouldHaveNoConfiguration()
        {
            // Act
            var result = new ModelBuilder(new ConventionSet());

            // Assert
            Assert.Null(result.Entity<TestEntity>().Metadata.FindProperty(nameof(ITenantScopable<int>.TenantId)));

            Assert.Null(result.Entity<TestEntity>().Metadata.GetQueryFilter());
        }

        [Fact]
        public void ModelBuilder_IsTenantScopable_WhenCalled_ItShouldSetTenantScopableConfiguration()
        {
            // Arrange
            var entityTypeBuilder = new ModelBuilder(new ConventionSet());

            // Act
            var result = TenantScopeableEntityBehaviorConfigurationExtensions.ConfigureEntityBehaviorITenantScopable<TestEntity, int>(entityTypeBuilder, x => x.TenantId == 1);

            // Assert
            var tenantId = result.Entity<TestEntity>().Metadata.FindProperty(nameof(ITenantScopable<int>.TenantId));
            Assert.Equal(nameof(ITenantScopable<int>.TenantId), tenantId.Name);
            Assert.False(tenantId.IsNullable);

            var tenantQueryFilter = result.Entity<TestEntity>().Metadata.GetQueryFilter();
            Assert.Equal("Param_0 => (Param_0.TenantId == 1)", tenantQueryFilter.ToString());
        }

        [Fact]
        public void EntityTypeBuilder_IsTenantScopable_WhenNotCalled_ItShouldHaveNoConfiguration()
        {
            // Arrange
            var entityType = new EntityType(typeof(TestEntity), new Model(new ConventionSet()), false, ConfigurationSource.Explicit);

            // Act
            var result = new EntityTypeBuilder<TestEntity>(entityType);

            // Assert
            Assert.Null(result.Metadata.FindProperty(nameof(ITenantScopable<int>.TenantId)));

            Assert.Null(result.Metadata.GetQueryFilter());
        }

        [Fact]
        public void EntityTypeBuilder_IsTenantScopable_WhenCalled_ItShouldSetTenantScopableConfiguration()
        {
            // Arrange
            var entityType = new EntityType(typeof(TestEntity), new Model(new ConventionSet()), false, ConfigurationSource.Explicit);
            var entityTypeBuilder = new EntityTypeBuilder<TestEntity>(entityType);

            // Act
            var result = entityTypeBuilder.IsTenantScopable<TestEntity, int>(x => x.TenantId == 1);

            // Assert
            var tenantId = result.Metadata.FindProperty(nameof(ITenantScopable<int>.TenantId));
            Assert.Equal(nameof(ITenantScopable<int>.TenantId), tenantId.Name);
            Assert.False(tenantId.IsNullable);

            var tenantQueryFilter = result.Metadata.GetQueryFilter();
            Assert.Equal("Param_0 => (Param_0.TenantId == 1)", tenantQueryFilter.ToString());
        }
    }
}