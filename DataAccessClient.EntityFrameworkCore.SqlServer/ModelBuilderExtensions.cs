using DataAccessClient.EntityBehaviors;
using Microsoft.EntityFrameworkCore;

namespace DataAccessClient.EntityFrameworkCore.SqlServer
{
    internal static class ModelBuilderExtensions
    {
        internal static ModelBuilder ConfigureEntityBehaviorIIdentifiable<TEntity, TIdentifierType>(ModelBuilder modelBuilder)
            where TEntity : class, IIdentifiable<TIdentifierType>
            where TIdentifierType : struct
        {
            modelBuilder.Entity<TEntity>()
                .IsIdentifiable<TEntity, TIdentifierType>();

            return modelBuilder;
        }

        internal static ModelBuilder ConfigureEntityBehaviorICreatable<TEntity, TIdentifierType>(ModelBuilder modelBuilder)
            where TEntity : class, ICreatable<TIdentifierType>
            where TIdentifierType : struct
        {
            modelBuilder.Entity<TEntity>()
                .IsCreatable<TEntity, TIdentifierType>();

            return modelBuilder;
        }

        internal static ModelBuilder ConfigureEntityBehaviorIModifiable<TEntity, TIdentifierType>(ModelBuilder modelBuilder)
            where TEntity : class, IModifiable<TIdentifierType>
            where TIdentifierType : struct
        {
            modelBuilder.Entity<TEntity>()
                .IsModifiable<TEntity, TIdentifierType>();

            return modelBuilder;
        }

        internal static ModelBuilder ConfigureEntityBehaviorISoftDeletable<TEntity, TIdentifierType>(ModelBuilder modelBuilder)
            where TEntity : class, ISoftDeletable<TIdentifierType>
            where TIdentifierType : struct
        {
            modelBuilder.Entity<TEntity>()
                .IsSoftDeletable<TEntity, TIdentifierType>();

            return modelBuilder;
        }

        internal static ModelBuilder ConfigureEntityBehaviorIRowVersionable<TEntity>(ModelBuilder modelBuilder)
            where TEntity : class, IRowVersionable
        {
            modelBuilder.Entity<TEntity>()
                .IsRowVersionable();

            return modelBuilder;
        }


        internal static ModelBuilder ConfigureEntityBehaviorITranslatable<TEntity, TEntityTranslation, TIdentifierType>(
            ModelBuilder modelBuilder)
            where TEntity : class, ITranslatable<TEntityTranslation, TIdentifierType>
            where TEntityTranslation : class, IEntityTranslation<TEntity, TIdentifierType>
            where TIdentifierType : struct
        {
            modelBuilder.Entity<TEntity>()
                .IsTranslatable<TEntity, TEntityTranslation, TIdentifierType>();

            modelBuilder.Entity<TEntityTranslation>()
                .IsEntityTranslation<TEntityTranslation, TEntity, TIdentifierType>();

            return modelBuilder;
        }

        internal static ModelBuilder ConfigureEntityBehaviorTranslatedProperties<TEntity>(ModelBuilder modelBuilder) 
            where TEntity : class
        {
            modelBuilder.Entity<TEntity>()
                .HasTranslatedProperties();

            return modelBuilder;
        }
    }
}