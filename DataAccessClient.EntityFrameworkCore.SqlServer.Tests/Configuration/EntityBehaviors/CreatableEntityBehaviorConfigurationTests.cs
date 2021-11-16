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
    public class CreatableEntityBehaviorConfigurationTests
    {
        [Fact]
        public void ModelBuilder_IsCreatable_WhenNotCalled_ItShouldHaveNoConfiguration()
        {
            // Act
            var result = new ModelBuilder(new ConventionSet());

            // Assert
            Assert.Null(result.Entity<TestEntity>().Metadata.FindProperty(nameof(ICreatable<int>.CreatedOn)));
            Assert.Null(result.Entity<TestEntity>().Metadata.FindProperty(nameof(ICreatable<int>.CreatedById)));
        }

        [Fact]
        public void ModelBuilder_IsCreatable_WhenCalled_ItShouldSetIdentifiableConfiguration()
        {
            // Arrange
            var entityTypeBuilder = new ModelBuilder(new ConventionSet());

            // Act
            var result = CreatableEntityBehaviorConfigurationExtensions.ConfigureEntityBehaviorICreatable<TestEntity, int>(entityTypeBuilder);

            // Assert
            var createdOn = result.Entity<TestEntity>().Metadata.FindProperty(nameof(ICreatable<int>.CreatedOn));
            Assert.Equal(nameof(ICreatable<int>.CreatedOn), createdOn.Name);
            Assert.False(createdOn.IsNullable);

            var createdById = result.Entity<TestEntity>().Metadata.FindProperty(nameof(ICreatable<int>.CreatedById));
            Assert.Equal(nameof(ICreatable<int>.CreatedById), createdById.Name);
            Assert.False(createdById.IsNullable);
        }

        [Fact]
        public void EntityTypeBuilder_IsCreatable_WhenNotCalled_ItShouldHaveNoConfiguration()
        {
            // Arrange
            var entityType = new EntityType(typeof(TestEntity), new Model(new ConventionSet()), false, ConfigurationSource.Explicit);

            // Act
            var result = new EntityTypeBuilder<TestEntity>(entityType);

            // Assert
            Assert.Null(result.Metadata.FindProperty(nameof(ICreatable<int>.CreatedOn)));
            Assert.Null(result.Metadata.FindProperty(nameof(ICreatable<int>.CreatedById)));
        }

        [Fact]
        public void EntityTypeBuilder_IsCreatable_WhenCalled_ItShouldSetIdentifiableConfiguration()
        {
            // Arrange
            var entityType = new EntityType(typeof(TestEntity), new Model(new ConventionSet()), false, ConfigurationSource.Explicit);
            var entityTypeBuilder = new EntityTypeBuilder<TestEntity>(entityType);

            // Act
            var result = entityTypeBuilder.IsCreatable<TestEntity, int>();

            // Assert
            var createdOn = result.Metadata.FindProperty(nameof(ICreatable<int>.CreatedOn));
            Assert.Equal(nameof(ICreatable<int>.CreatedOn), createdOn.Name);
            Assert.False(createdOn.IsNullable);

            var createdById = result.Metadata.FindProperty(nameof(ICreatable<int>.CreatedById));
            Assert.Equal(nameof(ICreatable<int>.CreatedById), createdById.Name);
            Assert.False(createdById.IsNullable);
        }
    }
}
