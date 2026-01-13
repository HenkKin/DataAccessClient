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
    public class LocalizableEntityBehaviorConfigurationTests
    {
        [Fact]
        public void ModelBuilder_IsLocalizable_WhenNotCalled_ItShouldHaveNoConfiguration()
        {
            // Act
            var result = new ModelBuilder(new ConventionSet());

            // Assert
            Assert.Null(result.Entity<TestEntityView>().Metadata.FindProperty(nameof(ILocalizable<string>.LocaleId)));

            Assert.Null(result.Entity<TestEntityView>().Metadata.GetQueryFilter());
        }

        [Fact]
        public void ModelBuilder_IsLocalizable_WhenCalled_ItShouldSetLocalizableConfiguration()
        {
            // Arrange
            var entityTypeBuilder = new ModelBuilder(new ConventionSet());

            // Act
            var result = entityTypeBuilder.ConfigureEntityBehaviorILocalizable<TestEntityView, string>(x => x.LocaleId == "nl-NL");

            // Assert
            var localeId = result.Entity<TestEntityView>().Metadata.FindProperty(nameof(ILocalizable<int>.LocaleId));
            Assert.Equal(nameof(ILocalizable<string>.LocaleId), localeId.Name);
            Assert.False(localeId.IsNullable);

            var localeQueryFilter = result.Entity<TestEntityView>().Metadata.GetQueryFilter();
            Assert.Equal("Param_0 => (Param_0.LocaleId == \"nl-NL\")", localeQueryFilter.ToString());
        }

        [Fact]
        public void EntityTypeBuilder_IsLocalizable_WhenNotCalled_ItShouldHaveNoConfiguration()
        {
            // Arrange
            var entityType = new EntityType(typeof(TestEntity), new Model(new ConventionSet()), false, ConfigurationSource.Explicit);

            // Act
            var result = new EntityTypeBuilder<TestEntityView>(entityType);

            // Assert
            Assert.Null(result.Metadata.FindProperty(nameof(ILocalizable<string>.LocaleId)));

            Assert.Null(result.Metadata.GetQueryFilter());
        }

        [Fact]
        public void EntityTypeBuilder_IsLocalizable_WhenCalled_ItShouldSetLocalizableConfiguration()
        {
            // Arrange
            var entityType = new EntityType(typeof(TestEntityView), new Model(new ConventionSet()), false, ConfigurationSource.Explicit);
            var entityTypeBuilder = new EntityTypeBuilder<TestEntityView>(entityType);

            // Act
            var result = entityTypeBuilder.IsLocalizable<TestEntityView, string>(x => x.LocaleId == "nl-NL");

            // Assert
            var localeId = result.Metadata.FindProperty(nameof(ILocalizable<string>.LocaleId));
            Assert.Equal(nameof(ILocalizable<string>.LocaleId), localeId.Name);
            Assert.False(localeId.IsNullable);

            var localeQueryFilter = result.Metadata.GetQueryFilter();
            Assert.Equal("Param_0 => (Param_0.LocaleId == \"nl-NL\")", localeQueryFilter.ToString());
        }
    }
}