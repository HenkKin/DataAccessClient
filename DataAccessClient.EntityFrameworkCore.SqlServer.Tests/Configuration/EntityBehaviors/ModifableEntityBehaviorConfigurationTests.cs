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
    public class ModifableEntityBehaviorConfigurationTests
    {
        [Fact]
        public void ModelBuilder_IsModifiable_WhenNotCalled_ItShouldHaveNoConfiguration()
        {
            // Act
            var result = new ModelBuilder(new ConventionSet());

            // Assert
            Assert.Null(result.Entity<TestEntity>().Metadata.FindProperty(nameof(IModifiable<int>.ModifiedOn)));
            Assert.Null(result.Entity<TestEntity>().Metadata.FindProperty(nameof(IModifiable<int>.ModifiedById)));
        }

        [Fact]
        public void ModelBuilder_IsModifiable_WhenCalled_ItShouldSetModifiableConfiguration()
        {
            // Arrange
            var entityTypeBuilder = new ModelBuilder(new ConventionSet());

            // Act
            var result = ModifiableEntityBehaviorConfigurationExtensions.ConfigureEntityBehaviorIModifiable<TestEntity, int>(entityTypeBuilder);

            // Assert
            var modifiedOn = result.Entity<TestEntity>().Metadata.FindProperty(nameof(IModifiable<int>.ModifiedOn));
            Assert.Equal(nameof(IModifiable<int>.ModifiedOn), modifiedOn.Name);
            Assert.True(modifiedOn.IsNullable);

            var modifiedById = result.Entity<TestEntity>().Metadata.FindProperty(nameof(IModifiable<int>.ModifiedById));
            Assert.Equal(nameof(IModifiable<int>.ModifiedById), modifiedById.Name);
            Assert.True(modifiedById.IsNullable);
        }

        [Fact]
        public void EntityTypeBuilder_IsModifiable_WhenNotCalled_ItShouldHaveNoConfiguration()
        {
            // Arrange
            var entityType = new EntityType(typeof(TestEntity), new Model(new ConventionSet()), false, ConfigurationSource.Explicit);

            // Act
            var result = new EntityTypeBuilder<TestEntity>(entityType);

            // Assert
            Assert.Null(result.Metadata.FindProperty(nameof(IModifiable<int>.ModifiedOn)));
            Assert.Null(result.Metadata.FindProperty(nameof(IModifiable<int>.ModifiedById)));
        }

        [Fact]
        public void EntityTypeBuilder_IsModifiable_WhenCalled_ItShouldSetModifiableConfiguration()
        {
            // Arrange
            var entityType = new EntityType(typeof(TestEntity), new Model(new ConventionSet()), false, ConfigurationSource.Explicit);
            var entityTypeBuilder = new EntityTypeBuilder<TestEntity>(entityType);

            // Act
            var result = entityTypeBuilder.IsModifiable<TestEntity, int>();

            // Assert
            var modifiedOn = result.Metadata.FindProperty(nameof(IModifiable<int>.ModifiedOn));
            Assert.Equal(nameof(IModifiable<int>.ModifiedOn), modifiedOn.Name);
            Assert.True(modifiedOn.IsNullable);

            var modifiedById = result.Metadata.FindProperty(nameof(IModifiable<int>.ModifiedById));
            Assert.Equal(nameof(IModifiable<int>.ModifiedById), modifiedById.Name);
            Assert.True(modifiedById.IsNullable);
        }
    }
}