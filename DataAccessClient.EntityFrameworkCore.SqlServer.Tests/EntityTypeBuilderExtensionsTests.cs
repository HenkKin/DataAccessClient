using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Linq;
using DataAccessClient.EntityBehaviors;
using DataAccessClient.EntityFrameworkCore.SqlServer.Tests.TestModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Xunit;

namespace DataAccessClient.EntityFrameworkCore.SqlServer.Tests
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "EF1001:Internal EF Core API usage.", Justification = "<Pending>")]
    public class EntityTypeBuilderExtensionsTests
    {
        [Fact]
        public void IsIdentifiable_WhenNotCalled_ItShouldHaveNoConfiguration()
        {
            // Arrange
            var entityType = new EntityType(typeof(TestEntity), new Model(new ConventionSet()), ConfigurationSource.Explicit);

            // Act
            var result = new EntityTypeBuilder<TestEntity>(entityType);

            // Assert
            Assert.Null(result.Metadata.FindPrimaryKey());
        }

        [Fact]
        public void IsIdentifiable_WhenCalled_ItShouldSetIdentifiableConfiguration()
        {
            // Arrange
            var entityType = new EntityType(typeof(TestEntity), new Model(new ConventionSet()), ConfigurationSource.Explicit);
            var entityTypeBuilder = new EntityTypeBuilder<TestEntity>(entityType);

            // Act
            var result = EntityTypeBuilderExtensions.IsIdentifiable<TestEntity, int>(entityTypeBuilder);

            // Assert
            var primaryKey = result.Metadata.FindPrimaryKey().Properties.Single();
            Assert.Equal(nameof(IIdentifiable<int>.Id), primaryKey.Name);
            Assert.False(primaryKey.IsNullable);
        }


        [Fact]
        public void IsSoftDeletable_WhenNotCalled_ItShouldHaveNoConfiguration()
        {
            // Arrange
            var entityType = new EntityType(typeof(TestEntity), new Model(new ConventionSet()), ConfigurationSource.Explicit);

            // Act
            var result = new EntityTypeBuilder<TestEntity>(entityType);

            // Assert
            Assert.Null(result.Metadata.FindProperty(nameof(ISoftDeletable<int>.IsDeleted)));
            Assert.Null(result.Metadata.FindProperty(nameof(ISoftDeletable<int>.DeletedOn)));
            Assert.Null(result.Metadata.FindProperty(nameof(ISoftDeletable<int>.DeletedById)));

            Assert.Null(result.Metadata.GetQueryFilter());
        }

        [Fact]
        public void IsSoftDeletable_WhenCalled_ItShouldSetSoftDeletableConfiguration()
        {
            // Arrange
            var entityType = new EntityType(typeof(TestEntity), new Model(new ConventionSet()), ConfigurationSource.Explicit);
            var entityTypeBuilder = new EntityTypeBuilder<TestEntity>(entityType);

            // Act
            var result = EntityTypeBuilderExtensions.IsSoftDeletable<TestEntity, int>(entityTypeBuilder, x=>x.IsDeleted == false);

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

            var deletedQueryFilter = result.Metadata.GetQueryFilter();
            Assert.Equal("Param_0 => (Param_0.IsDeleted == False)", deletedQueryFilter.ToString());
        }


        [Fact]
        public void IsTenantScopable_WhenNotCalled_ItShouldHaveNoConfiguration()
        {
            // Arrange
            var entityType = new EntityType(typeof(TestEntity), new Model(new ConventionSet()), ConfigurationSource.Explicit);

            // Act
            var result = new EntityTypeBuilder<TestEntity>(entityType);

            // Assert
            Assert.Null(result.Metadata.FindProperty(nameof(ITenantScopable<int>.TenantId)));

            Assert.Null(result.Metadata.GetQueryFilter());
        }

        [Fact]
        public void IsTenantScopable_WhenCalled_ItShouldSetTenantScopableConfiguration()
        {
            // Arrange
            var entityType = new EntityType(typeof(TestEntity), new Model(new ConventionSet()), ConfigurationSource.Explicit);
            var entityTypeBuilder = new EntityTypeBuilder<TestEntity>(entityType);

            // Act
            var result = EntityTypeBuilderExtensions.IsTenantScopable<TestEntity, int>(entityTypeBuilder, x=> x.TenantId == 1);

            // Assert
            var tenantId = result.Metadata.FindProperty(nameof(ITenantScopable<int>.TenantId));
            Assert.Equal(nameof(ITenantScopable<int>.TenantId), tenantId.Name);
            Assert.False(tenantId.IsNullable);

            var tenantQueryFilter = result.Metadata.GetQueryFilter();
            Assert.Equal("Param_0 => (Param_0.TenantId == 1)", tenantQueryFilter.ToString());
        }

        [Fact]
        public void IsCreatable_WhenNotCalled_ItShouldHaveNoConfiguration()
        {
            // Arrange
            var entityType = new EntityType(typeof(TestEntity), new Model(new ConventionSet()), ConfigurationSource.Explicit);
            
            // Act
            var result = new EntityTypeBuilder<TestEntity>(entityType);

            // Assert
            Assert.Null(result.Metadata.FindProperty(nameof(ICreatable<int>.CreatedOn)));
            Assert.Null(result.Metadata.FindProperty(nameof(ICreatable<int>.CreatedById)));
        }

        [Fact]
        public void IsCreatable_WhenCalled_ItShouldSetIdentifiableConfiguration()
        {
            // Arrange
            var entityType = new EntityType(typeof(TestEntity), new Model(new ConventionSet()), ConfigurationSource.Explicit);
            var entityTypeBuilder = new EntityTypeBuilder<TestEntity>(entityType);

            // Act
            var result = EntityTypeBuilderExtensions.IsCreatable<TestEntity, int>(entityTypeBuilder);

            // Assert
            var createdOn = result.Metadata.FindProperty(nameof(ICreatable<int>.CreatedOn));
            Assert.Equal(nameof(ICreatable<int>.CreatedOn), createdOn.Name);
            Assert.False(createdOn.IsNullable);

            var createdById = result.Metadata.FindProperty(nameof(ICreatable<int>.CreatedById));
            Assert.Equal(nameof(ICreatable<int>.CreatedById), createdById.Name);
            Assert.False(createdById.IsNullable);
        }

        [Fact]
        public void IsModifiable_WhenNotCalled_ItShouldHaveNoConfiguration()
        {
            // Arrange
            var entityType = new EntityType(typeof(TestEntity), new Model(new ConventionSet()), ConfigurationSource.Explicit);

            // Act
            var result = new EntityTypeBuilder<TestEntity>(entityType);

            // Assert
            Assert.Null(result.Metadata.FindProperty(nameof(IModifiable<int>.ModifiedOn)));
            Assert.Null(result.Metadata.FindProperty(nameof(IModifiable<int>.ModifiedById)));
        }

        [Fact]
        public void IsModifiable_WhenCalled_ItShouldSetIdentifiableConfiguration()
        {
            // Arrange
            var entityType = new EntityType(typeof(TestEntity), new Model(new ConventionSet()), ConfigurationSource.Explicit);
            var entityTypeBuilder = new EntityTypeBuilder<TestEntity>(entityType);

            // Act
            var result = EntityTypeBuilderExtensions.IsModifiable<TestEntity, int>(entityTypeBuilder);

            // Assert
            var modifiedOn = result.Metadata.FindProperty(nameof(IModifiable<int>.ModifiedOn));
            Assert.Equal(nameof(IModifiable<int>.ModifiedOn), modifiedOn.Name);
            Assert.True(modifiedOn.IsNullable);

            var modifiedById = result.Metadata.FindProperty(nameof(IModifiable<int>.ModifiedById));
            Assert.Equal(nameof(IModifiable<int>.ModifiedById), modifiedById.Name);
            Assert.True(modifiedById.IsNullable);
        }

        [Fact]
        public void IsRowVersionable_WhenNotCalled_ItShouldHaveNoConfiguration()
        {
            // Arrange
            var entityType = new EntityType(typeof(TestEntity), new Model(new ConventionSet()), ConfigurationSource.Explicit);

            // Act
            var result = new EntityTypeBuilder<TestEntity>(entityType);

            // Assert
            Assert.Null(result.Metadata.FindProperty(nameof(IRowVersionable.RowVersion)));
        }

        [Fact]
        public void IsRowVersionable_WhenCalled_ItShouldSetIdentifiableConfiguration()
        {
            // Arrange
            var entityType = new EntityType(typeof(TestEntity), new Model(new ConventionSet()), ConfigurationSource.Explicit);
            var entityTypeBuilder = new EntityTypeBuilder<TestEntity>(entityType);

            // Act
            var result = EntityTypeBuilderExtensions.IsRowVersionable(entityTypeBuilder);

            // Assert
            var rowVersion = result.Metadata.FindProperty(nameof(IRowVersionable.RowVersion));
            Assert.Equal(nameof(IRowVersionable.RowVersion), rowVersion.Name);
            Assert.True(rowVersion.IsNullable);
            Assert.True(rowVersion.IsConcurrencyToken);
        }

        [Fact]
        public void IsTranslatable_WhenNotCalled_ItShouldHaveNoConfiguration()
        {
            // Arrange
            var entityType = new EntityType(typeof(TestEntity), new Model(new ConventionSet()), ConfigurationSource.Explicit);

            // Act
            var result = new EntityTypeBuilder<TestEntity>(entityType);

            // Assert
            Assert.Null(result.Metadata.FindNavigation(nameof(ITranslatable<TestEntityTranslation, int>.Translations)));
        }

        [Fact]
        public void IsTranslatable_WhenCalled_ItShouldSetTranslatableConfiguration()
        {
            // Arrange
            var entityType = new EntityType(typeof(TestEntity), new Model(new ConventionSet()), ConfigurationSource.Explicit);
            var entityTypeBuilder = new EntityTypeBuilder<TestEntity>(entityType);

            // Act
            var result = EntityTypeBuilderExtensions.IsTranslatable<TestEntity, TestEntityTranslation, int>(entityTypeBuilder);

            // Assert
            Assert.NotNull(result.Metadata.FindNavigation(nameof(ITranslatable<TestEntityTranslation, int>.Translations)));
        }


        [Fact]
        public void IsEntityTranslation_WhenNotCalled_ItShouldHaveNoConfiguration()
        {
            // Arrange
            var entityType = new EntityType(typeof(TestEntityTranslation), new Model(new ConventionSet()), ConfigurationSource.Explicit);

            // Act
            var result = new EntityTypeBuilder<TestEntityTranslation>(entityType);

            // Assert
            Assert.Null(result.Metadata.FindProperty(nameof(IEntityTranslation<int>.TranslatedEntityId)));
            Assert.Null(result.Metadata.FindProperty(nameof(IEntityTranslation<int>.Language)));
            Assert.Null(result.Metadata.FindPrimaryKey());
        }

        [Fact]
        public void IsEntityTranslation_WhenCalled_ItShouldSetTranslatableConfiguration()
        {
            // Arrange
            var entityType = new EntityType(typeof(TestEntityTranslation), new Model(new ConventionSet()), ConfigurationSource.Explicit);
            var entityTypeBuilder = new EntityTypeBuilder<TestEntityTranslation>(entityType);

            // Act
            var result = EntityTypeBuilderExtensions.IsEntityTranslation<TestEntityTranslation, TestEntity, int>(entityTypeBuilder);

            // Assert
            Assert.NotNull(result.Metadata.FindProperty(nameof(IEntityTranslation<int>.TranslatedEntityId)));
            Assert.NotNull(result.Metadata.FindProperty(nameof(IEntityTranslation<int>.Language)));

            Assert.Equal(2, result.Metadata.FindPrimaryKey().Properties.Count);
            Assert.Contains(result.Metadata.FindPrimaryKey().Properties, x => x.Name == nameof(IEntityTranslation<TestEntity, int>.TranslatedEntityId));
            Assert.Contains(result.Metadata.FindPrimaryKey().Properties, x => x.Name == nameof(IEntityTranslation<TestEntity, int>.Language));
        }



        [Fact]
        public void HasTranslatedProperties_WhenNotCalled_ItShouldHaveNoConfiguration()
        {
            // Arrange
            var entityType = new EntityType(typeof(TestEntity), new Model(new ConventionSet()), ConfigurationSource.Explicit);

            // Act
            var result = new EntityTypeBuilder<TestEntity>(entityType);

            // Assert
            Assert.Null(result.Metadata.FindNavigation(nameof(TestEntity.Name)));
        }

        [Fact]
        public void HasTranslatedProperties_WhenCalled_ItShouldConfigureTranslatedProperties()
        {
            // Arrange
            var entityType = new EntityType(typeof(TestEntity), new Model(new ConventionSet()), ConfigurationSource.Explicit);
            var entityTypeBuilder = new EntityTypeBuilder<TestEntity>(entityType);

            // Act
            var result = EntityTypeBuilderExtensions.HasTranslatedProperties(entityTypeBuilder);

            // Assert
            Assert.NotNull(result.Metadata.FindNavigation(nameof(TestEntity.Name)));
            Assert.Equal(typeof(TranslatedProperty),result.Metadata.FindNavigation(nameof(TestEntity.Name)).ForeignKey.PrincipalToDependent.ClrType);
        }

        [Fact]
        public void HasUtcDateTimeProperties_WhenNotCalled_ItShouldHaveNoConfiguration()
        {
            // Arrange
            var entityType = new EntityType(typeof(TestEntity), new Model(new ConventionSet()), ConfigurationSource.Explicit);

            // Act
            var result = new EntityTypeBuilder<TestEntity>(entityType);

            // Assert
            Assert.Null(result.Property(nameof(TestEntity.CreatedOn)).Metadata.GetValueConverter());
            Assert.Null(result.Property(nameof(TestEntity.ModifiedOn)).Metadata.GetValueConverter());
            Assert.Null(result.Property(nameof(TestEntity.DeletedOn)).Metadata.GetValueConverter());
        }


    }
}
