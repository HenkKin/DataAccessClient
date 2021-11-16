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
    public class UtcDateTimePropertyEntityBehaviorConfigurationTests
    {
        [Fact]
        public void ModelBuilder_ConfigureHasUtcDateTimeProperties_WhenNotCalled_ItShouldHaveNoConfiguration()
        {
            // Act
            var result = new ModelBuilder(new ConventionSet());

            // Assert
            Assert.Null(result.Entity<TestEntity>().Property(nameof(TestEntity.CreatedOn)).Metadata.GetValueConverter());
            Assert.Null(result.Entity<TestEntity>().Property(nameof(TestEntity.ModifiedOn)).Metadata.GetValueConverter());
            Assert.Null(result.Entity<TestEntity>().Property(nameof(TestEntity.DeletedOn)).Metadata.GetValueConverter());
        }

        [Fact]
        public void ModelBuilder_ConfigureHasUtcDateTimeProperties_WhenCalled_ItShouldSetValueConverterForDateTimePropertiesConfiguration()
        {
            // Arrange
            var entityTypeBuilder = new ModelBuilder(new ConventionSet());

            // Act
            var result = UtcDateTimePropertyEntityBehaviorConfigurationExtensions.ConfigureHasUtcDateTimeProperties<TestEntity>(entityTypeBuilder, new UtcDateTimeValueConverter());

            // Assert
            Assert.NotNull(result.Entity<TestEntity>().Property(nameof(TestEntity.CreatedOn)).Metadata.GetValueConverter());
            Assert.NotNull(result.Entity<TestEntity>().Property(nameof(TestEntity.ModifiedOn)).Metadata.GetValueConverter());
            Assert.NotNull(result.Entity<TestEntity>().Property(nameof(TestEntity.DeletedOn)).Metadata.GetValueConverter());

            Assert.IsType<UtcDateTimeValueConverter>(result.Entity<TestEntity>().Property(nameof(TestEntity.CreatedOn)).Metadata.GetValueConverter());
            Assert.IsType<UtcDateTimeValueConverter>(result.Entity<TestEntity>().Property(nameof(TestEntity.ModifiedOn)).Metadata.GetValueConverter());
            Assert.IsType<UtcDateTimeValueConverter>(result.Entity<TestEntity>().Property(nameof(TestEntity.DeletedOn)).Metadata.GetValueConverter());
        }

        [Fact]
        public void EntityTypeBuilder_HasUtcDateTimeProperties_WhenNotCalled_ItShouldHaveNoConfiguration()
        {
            // Arrange
            var entityType = new EntityType(typeof(TestEntity), new Model(new ConventionSet()), false, ConfigurationSource.Explicit);

            // Act
            var result = new EntityTypeBuilder<TestEntity>(entityType);

            // Assert
            Assert.Null(result.Property(nameof(TestEntity.CreatedOn)).Metadata.GetValueConverter());
            Assert.Null(result.Property(nameof(TestEntity.ModifiedOn)).Metadata.GetValueConverter());
            Assert.Null(result.Property(nameof(TestEntity.DeletedOn)).Metadata.GetValueConverter());
        }
    }
}