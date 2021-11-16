using DataAccessClient.EntityBehaviors;
using DataAccessClient.EntityFrameworkCore.SqlServer.Configuration.EntityBehaviors;
using DataAccessClient.EntityFrameworkCore.SqlServer.Tests.TestModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Xunit;

namespace DataAccessClient.EntityFrameworkCore.SqlServer.Tests.Configuration.EntityBehaviors
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "EF1001:Internal EF Core API usage.", Justification = "<Pending>")]
    public class RowVersionableEntityBehaviorConfigurationTests
    {
        [Fact]
        public void ModelBuilder_IsRowVersionable_WhenNotCalled_ItShouldHaveNoConfiguration()
        {
            // Act
            var result = new ModelBuilder(new ConventionSet());

            // Assert
            Assert.Null(result.Entity<TestEntity>().Metadata.FindProperty(nameof(IRowVersionable.RowVersion)));
        }

        [Fact]
        public void ModelBuilder_IsRowVersionable_WhenCalled_ItShouldSetIdentifiableConfiguration()
        {
            // Arrange
            var entityTypeBuilder = new ModelBuilder(new ConventionSet());

            // Act
            var result = RowVersionableEntityBehaviorConfigurationExtensions.ConfigureEntityBehaviorIRowVersionable<TestEntity>(entityTypeBuilder);

            // Assert
            var rowVersion = result.Entity<TestEntity>().Metadata.FindProperty(nameof(IRowVersionable.RowVersion));
            Assert.Equal(nameof(IRowVersionable.RowVersion), rowVersion.Name);
            Assert.True(rowVersion.IsNullable);
            Assert.True(rowVersion.IsConcurrencyToken);
        }

        [Fact]
        public void EntityTypeBuilder_IsRowVersionable_WhenNotCalled_ItShouldHaveNoConfiguration()
        {
            // Arrange
            var entityType = new EntityType(typeof(TestEntity), new Model(new ConventionSet()), false, ConfigurationSource.Explicit);

            // Act
            var result = new EntityTypeBuilder<TestEntity>(entityType);

            // Assert
            Assert.Null(result.Metadata.FindProperty(nameof(IRowVersionable.RowVersion)));
        }

        [Fact]
        public void EntityTypeBuilder_IsRowVersionable_WhenCalled_ItShouldSetIdentifiableConfiguration()
        {
            // Arrange
            var entityType = new EntityType(typeof(TestEntity), new Model(new ConventionSet()), false, ConfigurationSource.Explicit);
            var entityTypeBuilder = new EntityTypeBuilder<TestEntity>(entityType);

            // Act
            var result = entityTypeBuilder.IsRowVersionable();

            // Assert
            var rowVersion = result.Metadata.FindProperty(nameof(IRowVersionable.RowVersion));
            Assert.Equal(nameof(IRowVersionable.RowVersion), rowVersion.Name);
            Assert.True(rowVersion.IsNullable);
            Assert.True(rowVersion.IsConcurrencyToken);
        }
    }
}