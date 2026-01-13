using System.Linq;
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
    public class IdentifiableEntityBehaviorConfigurationTests
    {
        [Fact]
        public void ModelBuilder_IsIdentifiable_WhenNotCalled_ItShouldHaveNoConfiguration()
        {
            // Act
            var result = new ModelBuilder(new ConventionSet());

            // Assert
            Assert.Null(result.Entity<TestEntity>().Metadata.FindPrimaryKey());
        }

        [Fact]
        public void ModelBuilder_IsIdentifiable_WhenCalled_ItShouldSetIdentifiableConfiguration()
        {
            // Arrange
            var entityTypeBuilder = new ModelBuilder(new ConventionSet());

            // Act
            var result = IdentifiableEntityBehaviorConfigurationExtensions.ConfigureEntityBehaviorIIdentifiable<TestEntity, int>(entityTypeBuilder);

            // Assert
            var primaryKey = result.Entity<TestEntity>().Metadata.FindPrimaryKey().Properties.Single();
            Assert.Equal(nameof(IIdentifiable<int>.Id), primaryKey.Name);
            Assert.False(primaryKey.IsNullable);
        }

        [Fact]
        public void EntityTypeBuilder_IsIdentifiable_WhenNotCalled_ItShouldHaveNoConfiguration()
        {
            // Arrange
            var entityType = new EntityType(typeof(TestEntity), new Model(new ConventionSet()), false, ConfigurationSource.Explicit);

            // Act
            var result = new EntityTypeBuilder<TestEntity>(entityType);

            // Assert
            Assert.Null(result.Metadata.FindPrimaryKey());
        }

        [Fact]
        public void EntityTypeBuilder_IsIdentifiable_WhenCalled_ItShouldSetIdentifiableConfiguration()
        {
            // Arrange
            var entityType = new EntityType(typeof(TestEntity), new Model(new ConventionSet()), false, ConfigurationSource.Explicit);
            var entityTypeBuilder = new EntityTypeBuilder<TestEntity>(entityType);

            // Act
            var result = entityTypeBuilder.IsIdentifiable<TestEntity, int>();

            // Assert
            var primaryKey = result.Metadata.FindPrimaryKey().Properties.Single();
            Assert.Equal(nameof(IIdentifiable<int>.Id), primaryKey.Name);
            Assert.False(primaryKey.IsNullable);
        }
    }
}
