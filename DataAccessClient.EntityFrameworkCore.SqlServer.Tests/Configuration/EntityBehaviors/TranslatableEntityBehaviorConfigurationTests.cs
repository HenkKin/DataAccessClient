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
    public class TranslatableEntityBehaviorConfigurationTests
    {
        [Fact]
        public void ModelBuilder_IsTranslatable_WhenNotCalled_ItShouldHaveNoConfiguration()
        {
            // Act
            var result = new ModelBuilder(new ConventionSet());

            // Assert
            Assert.Null(result.Entity<TestEntity>().Metadata.FindNavigation(nameof(ITranslatable<TestEntityTranslation, int, string>.Translations)));
            Assert.Null(result.Entity<TestEntityTranslation>().Metadata.FindProperty(nameof(IEntityTranslation<int, string>.TranslatedEntityId)));
            Assert.Null(result.Entity<TestEntityTranslation>().Metadata.FindProperty(nameof(IEntityTranslation<int, string>.LocaleId)));
            Assert.Null(result.Entity<TestEntityTranslation>().Metadata.FindPrimaryKey());
        }

        [Fact]
        public void ModelBuilder_IsTranslatable_WhenCalled_ItShouldSetTranslatableConfiguration()
        {
            // Arrange
            var entityTypeBuilder = new ModelBuilder(new ConventionSet());

            // Act
            var result = TranslatableEntityBehaviorConfigurationExtensions.ConfigureEntityBehaviorITranslatable<TestEntity, TestEntityTranslation, int, string>(entityTypeBuilder);

            // Assert
            Assert.NotNull(result.Entity<TestEntity>().Metadata.FindNavigation(nameof(ITranslatable<TestEntityTranslation, int, string>.Translations)));
            Assert.NotNull(result.Entity<TestEntityTranslation>().Metadata.FindProperty(nameof(IEntityTranslation<int, string>.TranslatedEntityId)));
            Assert.NotNull(result.Entity<TestEntityTranslation>().Metadata.FindProperty(nameof(IEntityTranslation<int, string>.LocaleId)));

            Assert.Equal(2, result.Entity<TestEntityTranslation>().Metadata.FindPrimaryKey().Properties.Count);
            Assert.Contains(result.Entity<TestEntityTranslation>().Metadata.FindPrimaryKey().Properties, x => x.Name == nameof(IEntityTranslation<TestEntity, int, string>.TranslatedEntityId));
            Assert.Contains(result.Entity<TestEntityTranslation>().Metadata.FindPrimaryKey().Properties, x => x.Name == nameof(IEntityTranslation<TestEntity, int, string>.LocaleId));
        }

        [Fact]
        public void EntityTypeBuilder_IsTranslatable_WhenNotCalled_ItShouldHaveNoConfiguration()
        {
            // Arrange
            var entityType = new EntityType(typeof(TestEntity), new Model(new ConventionSet()), ConfigurationSource.Explicit);

            // Act
            var result = new EntityTypeBuilder<TestEntity>(entityType);

            // Assert
            Assert.Null(result.Metadata.FindNavigation(nameof(ITranslatable<TestEntityTranslation, int, string>.Translations)));
        }

        [Fact]
        public void EntityTypeBuilder_IsTranslatable_WhenCalled_ItShouldSetTranslatableConfiguration()
        {
            // Arrange
            var entityType = new EntityType(typeof(TestEntity), new Model(new ConventionSet()), ConfigurationSource.Explicit);
            var entityTypeBuilder = new EntityTypeBuilder<TestEntity>(entityType);

            // Act
            var result = entityTypeBuilder.IsTranslatable<TestEntity, TestEntityTranslation, int, string>();

            // Assert
            Assert.NotNull(result.Metadata.FindNavigation(nameof(ITranslatable<TestEntityTranslation, int, string>.Translations)));
        }


        [Fact]
        public void EntityTypeBuilder_IsEntityTranslation_WhenNotCalled_ItShouldHaveNoConfiguration()
        {
            // Arrange
            var entityType = new EntityType(typeof(TestEntityTranslation), new Model(new ConventionSet()), ConfigurationSource.Explicit);

            // Act
            var result = new EntityTypeBuilder<TestEntityTranslation>(entityType);

            // Assert
            Assert.Null(result.Metadata.FindProperty(nameof(IEntityTranslation<int, string>.TranslatedEntityId)));
            Assert.Null(result.Metadata.FindProperty(nameof(IEntityTranslation<int, string>.LocaleId)));
            Assert.Null(result.Metadata.FindPrimaryKey());
        }

        [Fact]
        public void EntityTypeBuilder_IsEntityTranslation_WhenCalled_ItShouldSetTranslatableConfiguration()
        {
            // Arrange
            var entityType = new EntityType(typeof(TestEntityTranslation), new Model(new ConventionSet()), ConfigurationSource.Explicit);
            var entityTypeBuilder = new EntityTypeBuilder<TestEntityTranslation>(entityType);

            // Act
            var result = entityTypeBuilder.IsEntityTranslation<TestEntityTranslation, TestEntity, int, string>();

            // Assert
            Assert.NotNull(result.Metadata.FindProperty(nameof(IEntityTranslation<int, string>.TranslatedEntityId)));
            Assert.NotNull(result.Metadata.FindProperty(nameof(IEntityTranslation<int, string>.LocaleId)));

            Assert.Equal(2, result.Metadata.FindPrimaryKey().Properties.Count);
            Assert.Contains(result.Metadata.FindPrimaryKey().Properties, x => x.Name == nameof(IEntityTranslation<TestEntity, int, string>.TranslatedEntityId));
            Assert.Contains(result.Metadata.FindPrimaryKey().Properties, x => x.Name == nameof(IEntityTranslation<TestEntity, int, string>.LocaleId));
        }
    }
}