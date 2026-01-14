using DataAccessClient.EntityBehaviors;
using DataAccessClient.EntityFrameworkCore.Relational.Tests.TestModels;
using DataAccessClient.EntityFrameworkCore.Relational.Configuration.EntityBehaviors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Xunit;
using System.Linq;

namespace DataAccessClient.EntityFrameworkCore.Relational.Tests.Configuration.EntityBehaviors
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "EF1001:Internal EF Core API usage.", Justification = "<Pending>")]
    public class SoftDeletableEntityBehaviorConfigurationTests
    {
        [Fact]
        public void ModelBuilder_IsSoftDeletable_WhenNotCalled_ItShouldHaveNoConfiguration()
        {
            // Act
            var result = new ModelBuilder(new ConventionSet());

            // Assert
            Assert.Null(result.Entity<TestEntity>().Metadata.FindProperty(nameof(ISoftDeletable<int>.IsDeleted)));
            Assert.Null(result.Entity<TestEntity>().Metadata.FindProperty(nameof(ISoftDeletable<int>.DeletedOn)));
            Assert.Null(result.Entity<TestEntity>().Metadata.FindProperty(nameof(ISoftDeletable<int>.DeletedById)));

#if NET10_0_OR_GREATER
            var queryFilter = result.Entity<TestEntity>().Metadata.GetDeclaredQueryFilters().Where(x => x.Key == "SoftDeletableEntityBehaviorQueryFilter").FirstOrDefault()?.Expression;
#else
            var queryFilter = result.Entity<TestEntity>().Metadata.GetQueryFilter();
#endif
            Assert.Null(queryFilter);
        }

        [Fact]
        public void ModelBuilder_IsSoftDeletable_WhenCalled_ItShouldSetSoftDeletableConfiguration()
        {
            // Arrange
            var entityTypeBuilder = new ModelBuilder(new ConventionSet());

            // Act
            var result = SoftDeletableEntityBehaviorConfigurationExtensions.ConfigureEntityBehaviorISoftDeletable<TestEntity, int>(entityTypeBuilder, Param_0 => Param_0.IsDeleted == false);

            // Assert
            var isDeleted = result.Entity<TestEntity>().Metadata.FindProperty(nameof(ISoftDeletable<int>.IsDeleted));
            Assert.Equal(nameof(ISoftDeletable<int>.IsDeleted), isDeleted.Name);
            Assert.False(isDeleted.IsNullable);


            var deletedOn = result.Entity<TestEntity>().Metadata.FindProperty(nameof(ISoftDeletable<int>.DeletedOn));
            Assert.Equal(nameof(ISoftDeletable<int>.DeletedOn), deletedOn.Name);
            Assert.True(deletedOn.IsNullable);

            var deletedById = result.Entity<TestEntity>().Metadata.FindProperty(nameof(ISoftDeletable<int>.DeletedById));
            Assert.Equal(nameof(ISoftDeletable<int>.DeletedById), deletedById.Name);
            Assert.True(deletedById.IsNullable);

#if NET10_0_OR_GREATER
            var deletedQueryFilter = result.Entity<TestEntity>().Metadata.GetDeclaredQueryFilters().Where(x => x.Key == "SoftDeletableEntityBehaviorQueryFilter").FirstOrDefault()?.Expression;
#else
            var deletedQueryFilter = result.Entity<TestEntity>().Metadata.GetQueryFilter();
#endif
            Assert.Equal("Param_0 => (Param_0.IsDeleted == False)", deletedQueryFilter.ToString());
        }

        [Fact]
        public void EntityTypeBuilder_IsSoftDeletable_WhenNotCalled_ItShouldHaveNoConfiguration()
        {
            // Arrange
            var entityType = new EntityType(typeof(TestEntity), new Model(new ConventionSet()), false, ConfigurationSource.Explicit);

            // Act
            var result = new EntityTypeBuilder<TestEntity>(entityType);

            // Assert
            Assert.Null(result.Metadata.FindProperty(nameof(ISoftDeletable<int>.IsDeleted)));
            Assert.Null(result.Metadata.FindProperty(nameof(ISoftDeletable<int>.DeletedOn)));
            Assert.Null(result.Metadata.FindProperty(nameof(ISoftDeletable<int>.DeletedById)));

#if NET10_0_OR_GREATER
            var queryFilter = result.Metadata.GetDeclaredQueryFilters().Where(x => x.Key == "SoftDeletableEntityBehaviorQueryFilter").FirstOrDefault()?.Expression;
#else
            var queryFilter = result.Metadata.GetQueryFilter();
#endif
            Assert.Null(queryFilter);
        }

        [Fact]
        public void EntityTypeBuilder_IsSoftDeletable_WhenCalled_ItShouldSetSoftDeletableConfiguration()
        {
            // Arrange
            var entityType = new EntityType(typeof(TestEntity), new Model(new ConventionSet()), false, ConfigurationSource.Explicit);
            var entityTypeBuilder = new EntityTypeBuilder<TestEntity>(entityType);

            // Act
            var result = entityTypeBuilder.IsSoftDeletable<TestEntity, int>(Param_0 => Param_0.IsDeleted == false);

            // Assert
            var isDeleted = result.Metadata.FindProperty(nameof(ISoftDeletable<int>.IsDeleted));
            Assert.Equal(nameof(ISoftDeletable<int>.IsDeleted), isDeleted.Name);
            Assert.False(isDeleted.IsNullable);


            var deletedOn = result.Metadata.FindProperty(nameof(ISoftDeletable<int>.DeletedOn));
            Assert.Equal(nameof(ISoftDeletable<int>.DeletedOn), deletedOn.Name);
            Assert.True(deletedOn.IsNullable);

            var deletedById = result.Metadata.FindProperty(nameof(ISoftDeletable<int>.DeletedById));
            Assert.Equal(nameof(ISoftDeletable<int>.DeletedById), deletedById.Name);
            Assert.True(deletedById.IsNullable);

#if NET10_0_OR_GREATER
            var deletedQueryFilter = result.Metadata.GetDeclaredQueryFilters().Where(x => x.Key == "SoftDeletableEntityBehaviorQueryFilter").FirstOrDefault()?.Expression;
#else
            var deletedQueryFilter = result.Metadata.GetQueryFilter();
#endif
            Assert.Equal("Param_0 => (Param_0.IsDeleted == False)", deletedQueryFilter.ToString());
        }
    }
}