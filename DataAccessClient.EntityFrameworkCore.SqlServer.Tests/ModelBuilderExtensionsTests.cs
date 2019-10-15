using System.Linq;
using DataAccessClient.EntityBehaviors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Xunit;

namespace DataAccessClient.EntityFrameworkCore.SqlServer.Tests
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "EF1001:Internal EF Core API usage.", Justification =
        "<Pending>")]
    public class ModelBuilderExtensionsTests
    {
        [Fact]
        public void IsIdentifiable_WhenNotCalled_ItShouldHaveNoConfiguration()
        {
            // Act
            var result = new ModelBuilder(new ConventionSet());

            // Assert
            Assert.Null(result.Entity<TestEntity>().Metadata.FindPrimaryKey());
        }

        [Fact]
        public void IsIdentifiable_WhenCalled_ItShouldSetIdentifiableConfiguration()
        {
            // Arrange
            var entityTypeBuilder = new ModelBuilder(new ConventionSet());

            // Act
            var result = ModelBuilderExtensions.ConfigureEntityBehaviorIIdentifiable<TestEntity, int>(entityTypeBuilder);

            // Assert
            var primaryKey = result.Entity<TestEntity>().Metadata.FindPrimaryKey().Properties.Single();
            Assert.Equal(nameof(IIdentifiable<int>.Id), primaryKey.Name);
            Assert.False(primaryKey.IsNullable);
        }
        
        [Fact]
        public void IsSoftDeletable_WhenNotCalled_ItShouldHaveNoConfiguration()
        {
            // Act
            var result = new ModelBuilder(new ConventionSet());

            // Assert
            Assert.Null(result.Entity<TestEntity>().Metadata.FindProperty(nameof(ISoftDeletable<int>.IsDeleted)));
            Assert.Null(result.Entity<TestEntity>().Metadata.FindProperty(nameof(ISoftDeletable<int>.DeletedOn)));
            Assert.Null(result.Entity<TestEntity>().Metadata.FindProperty(nameof(ISoftDeletable<int>.DeletedById)));

            Assert.Null(result.Entity<TestEntity>().Metadata.GetQueryFilter());
        }

        [Fact]
        public void IsSoftDeletable_WhenCalled_ItShouldSetIdentifiableConfiguration()
        {
            // Arrange
            var entityTypeBuilder = new ModelBuilder(new ConventionSet());

            // Act
            var result = ModelBuilderExtensions.ConfigureEntityBehaviorISoftDeletable<TestEntity, int>(entityTypeBuilder);

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

            var deletedQueryFilter = result.Entity<TestEntity>().Metadata.GetQueryFilter();
            Assert.Equal("x => (x.IsDeleted == False)", deletedQueryFilter.ToString());
        }

        [Fact]
        public void IsCreatable_WhenNotCalled_ItShouldHaveNoConfiguration()
        {
            // Act
            var result = new ModelBuilder(new ConventionSet());

            // Assert
            Assert.Null(result.Entity<TestEntity>().Metadata.FindProperty(nameof(ICreatable<int>.CreatedOn)));
            Assert.Null(result.Entity<TestEntity>().Metadata.FindProperty(nameof(ICreatable<int>.CreatedById)));
        }

        [Fact]
        public void IsCreatable_WhenCalled_ItShouldSetIdentifiableConfiguration()
        {
            // Arrange
            var entityTypeBuilder = new ModelBuilder(new ConventionSet());

            // Act
            var result = ModelBuilderExtensions.ConfigureEntityBehaviorICreatable<TestEntity, int>(entityTypeBuilder);

            // Assert
            var createdOn = result.Entity<TestEntity>().Metadata.FindProperty(nameof(ICreatable<int>.CreatedOn));
            Assert.Equal(nameof(ICreatable<int>.CreatedOn), createdOn.Name);
            Assert.False(createdOn.IsNullable);

            var createdById = result.Entity<TestEntity>().Metadata.FindProperty(nameof(ICreatable<int>.CreatedById));
            Assert.Equal(nameof(ICreatable<int>.CreatedById), createdById.Name);
            Assert.False(createdById.IsNullable);
        }

        [Fact]
        public void IsModifiable_WhenNotCalled_ItShouldHaveNoConfiguration()
        {
            // Act
            var result = new ModelBuilder(new ConventionSet());

            // Assert
            Assert.Null(result.Entity<TestEntity>().Metadata.FindProperty(nameof(IModifiable<int>.ModifiedOn)));
            Assert.Null(result.Entity<TestEntity>().Metadata.FindProperty(nameof(IModifiable<int>.ModifiedById)));
        }

        [Fact]
        public void IsModifiable_WhenCalled_ItShouldSetIdentifiableConfiguration()
        {
            // Arrange
            var entityTypeBuilder = new ModelBuilder(new ConventionSet());

            // Act
            var result = ModelBuilderExtensions.ConfigureEntityBehaviorIModifiable<TestEntity, int>(entityTypeBuilder);

            // Assert
            var modifiedOn = result.Entity<TestEntity>().Metadata.FindProperty(nameof(IModifiable<int>.ModifiedOn));
            Assert.Equal(nameof(IModifiable<int>.ModifiedOn), modifiedOn.Name);
            Assert.True(modifiedOn.IsNullable);

            var modifiedById = result.Entity<TestEntity>().Metadata.FindProperty(nameof(IModifiable<int>.ModifiedById));
            Assert.Equal(nameof(IModifiable<int>.ModifiedById), modifiedById.Name);
            Assert.True(modifiedById.IsNullable);
        }

        [Fact]
        public void IsRowVersionable_WhenNotCalled_ItShouldHaveNoConfiguration()
        {
            // Act
            var result = new ModelBuilder(new ConventionSet());

            // Assert
            Assert.Null(result.Entity<TestEntity>().Metadata.FindProperty(nameof(IRowVersionable.RowVersion)));
        }

        [Fact]
        public void IsRowVersionable_WhenCalled_ItShouldSetIdentifiableConfiguration()
        {
            // Arrange
            var entityTypeBuilder = new ModelBuilder(new ConventionSet());

            // Act
            var result = ModelBuilderExtensions.ConfigureEntityBehaviorIRowVersionable<TestEntity>(entityTypeBuilder);

            // Assert
            var rowVersion = result.Entity<TestEntity>().Metadata.FindProperty(nameof(IRowVersionable.RowVersion));
            Assert.Equal(nameof(IRowVersionable.RowVersion), rowVersion.Name);
            Assert.True(rowVersion.IsNullable);
            Assert.True(rowVersion.IsConcurrencyToken);
        }
    }
}