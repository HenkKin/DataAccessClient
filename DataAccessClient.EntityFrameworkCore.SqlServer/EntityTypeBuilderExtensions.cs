using DataAccessClient.EntityBehaviors;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccessClient.EntityFrameworkCore.SqlServer
{
    internal static class EntityTypeBuilderExtensions
    {
        internal static EntityTypeBuilder<TEntity> IsIdentifiable<TEntity, TIdentifierType>(this EntityTypeBuilder<TEntity> entity) 
            where TEntity : class, IIdentifiable<TIdentifierType> 
            where TIdentifierType : struct
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .IsRequired();

            return entity;
        }

        internal static EntityTypeBuilder<TEntity> IsSoftDeletable<TEntity, TIdentifierType>(this EntityTypeBuilder<TEntity> entity)
            where TEntity : class, ISoftDeletable<TIdentifierType> 
            where TIdentifierType : struct
        {
            entity.Property(e => e.IsDeleted).IsRequired();
            entity.Property(e => e.DeletedOn).IsRequired(false);
            entity.Property(e => e.DeletedById).IsRequired(false);

            entity.HasQueryFilter(x => x.IsDeleted == false);
            return entity;
        }

        internal static EntityTypeBuilder<TEntity> IsRowVersionable<TEntity>(this EntityTypeBuilder<TEntity> entity)
            where TEntity : class, IRowVersionable
        {
            entity.Property(e => e.RowVersion).IsRowVersion();
            return entity;
        }

        internal static EntityTypeBuilder<TEntity> IsCreatable<TEntity, TIdentifierType>(this EntityTypeBuilder<TEntity> entity)
            where TEntity : class, ICreatable<TIdentifierType> 
            where TIdentifierType : struct
        {
            entity.Property(e => e.CreatedById).IsRequired();
            entity.Property(e => e.CreatedOn).IsRequired();
            return entity;
        }

        internal static EntityTypeBuilder<TEntity> IsModifiable<TEntity, TIdentifierType>(this EntityTypeBuilder<TEntity> entity)
            where TEntity : class, IModifiable<TIdentifierType> 
            where TIdentifierType : struct
        {
            entity.Property(e => e.ModifiedById).IsRequired(false);
            entity.Property(e => e.ModifiedOn).IsRequired(false);
            return entity;
        }
    }
}