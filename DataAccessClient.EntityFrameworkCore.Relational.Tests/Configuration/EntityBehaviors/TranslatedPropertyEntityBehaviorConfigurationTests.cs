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
    public class TranslatedPropertyEntityBehaviorConfigurationTests
    {
        [Fact]
        public void ModelBuilder_ConfigureEntityBehaviorTranslatedProperties_WhenNotCalled_ItShouldHaveNoConfiguration()
        {
            // Act
            var result = new ModelBuilder(new ConventionSet());

            // Assert
            Assert.Null(result.Entity<TestEntity>().Metadata.FindNavigation(nameof(TestEntity.Name)));
        }

        [Fact]
        public void ModelBuilder_ConfigureEntityBehaviorTranslatedProperties_WhenCalled_ItShouldConfigureTranslatedProperties()
        {
            // Arrange
            var entityTypeBuilder = new ModelBuilder(new ConventionSet());

            // Act
            var result = TranslatedPropertyEntityBehaviorConfigurationExtensions.ConfigureEntityBehaviorTranslatedProperties<TestEntity>(entityTypeBuilder);

            // Assert
            Assert.NotNull(result.Entity<TestEntity>().Metadata.FindNavigation(nameof(TestEntity.Name)));
            Assert.Equal(typeof(TranslatedProperty<string>), result.Entity<TestEntity>().Metadata.FindNavigation(nameof(TestEntity.Name)).ForeignKey.PrincipalToDependent.ClrType);
        }

        [Fact]
        public void EntityTypeBuilder_HasTranslatedProperties_WhenNotCalled_ItShouldHaveNoConfiguration()
        {
            // Arrange
            var entityType = new EntityType(typeof(TestEntity), new Model(new ConventionSet()), false, ConfigurationSource.Explicit);

            // Act
            var result = new EntityTypeBuilder<TestEntity>(entityType);

            // Assert
            Assert.Null(result.Metadata.FindNavigation(nameof(TestEntity.Name)));
        }

        [Fact]
        public void EntityTypeBuilder_HasTranslatedProperties_WhenCalled_ItShouldConfigureTranslatedProperties()
        {
            // Arrange
            var entityType = new EntityType(typeof(TestEntity), new Model(new ConventionSet()), false, ConfigurationSource.Explicit);
            var entityTypeBuilder = new EntityTypeBuilder<TestEntity>(entityType);

            // Act
            var result = entityTypeBuilder.HasTranslatedProperties<TestEntity>();

            // Assert
            Assert.NotNull(result.Metadata.FindNavigation(nameof(TestEntity.Name)));
            Assert.Equal(typeof(TranslatedProperty<string>), result.Metadata.FindNavigation(nameof(TestEntity.Name)).ForeignKey.PrincipalToDependent.ClrType);
        }
    }
}