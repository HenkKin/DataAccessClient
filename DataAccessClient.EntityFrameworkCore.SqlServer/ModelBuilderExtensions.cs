using System;
using System.Linq.Expressions;
using DataAccessClient.EntityBehaviors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

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

        internal static ModelBuilder ConfigureEntityBehaviorISoftDeletable<TEntity, TIdentifierType>(ModelBuilder modelBuilder, Expression<Func<TEntity, bool>> queryFilter)
            where TEntity : class, ISoftDeletable<TIdentifierType>
            where TIdentifierType : struct
        {
            modelBuilder.Entity<TEntity>()
                .IsSoftDeletable<TEntity, TIdentifierType>(queryFilter);

            return modelBuilder;
        }

        internal static ModelBuilder ConfigureEntityBehaviorITenantScopable<TEntity, TIdentifierType>(ModelBuilder modelBuilder, Expression<Func<TEntity, bool>> queryFilter)
            where TEntity : class, ITenantScopable<TIdentifierType>
            where TIdentifierType : struct
        {
            modelBuilder.Entity<TEntity>()
                .IsTenantScopable<TEntity, TIdentifierType>(queryFilter);

            return modelBuilder;
        }

        internal static ModelBuilder ConfigureEntityBehaviorILocalizable<TEntity, TIdentifierType>(ModelBuilder modelBuilder, Expression<Func<TEntity, bool>> queryFilter)
            where TEntity : class, ILocalizable<TIdentifierType>
            where TIdentifierType : IConvertible
        {
            modelBuilder.Entity<TEntity>()
                .IsLocalizable<TEntity, TIdentifierType>(queryFilter);

            return modelBuilder;
        }

        internal static ModelBuilder ConfigureEntityBehaviorIRowVersionable<TEntity>(ModelBuilder modelBuilder)
            where TEntity : class, IRowVersionable
        {
            modelBuilder.Entity<TEntity>()
                .IsRowVersionable();

            return modelBuilder;
        }


        internal static ModelBuilder ConfigureEntityBehaviorITranslatable<TEntity, TEntityTranslation, TIdentifierType, TLocaleIdentifierType>(
            ModelBuilder modelBuilder)
            where TEntity : class, ITranslatable<TEntityTranslation, TIdentifierType, TLocaleIdentifierType>
            where TEntityTranslation : class, IEntityTranslation<TEntity, TIdentifierType, TLocaleIdentifierType>
            where TIdentifierType : struct
            where TLocaleIdentifierType : IConvertible
        {
            modelBuilder.Entity<TEntity>()
                .IsTranslatable<TEntity, TEntityTranslation, TIdentifierType, TLocaleIdentifierType>();

            modelBuilder.Entity<TEntityTranslation>()
                .IsEntityTranslation<TEntityTranslation, TEntity, TIdentifierType, TLocaleIdentifierType>();

            return modelBuilder;
        }

        internal static ModelBuilder ConfigureEntityBehaviorTranslatedProperties<TEntity>(ModelBuilder modelBuilder)
            where TEntity : class
        {
            modelBuilder.Entity<TEntity>()
                .HasTranslatedProperties();

            return modelBuilder;
        }
        
        internal static ModelBuilder ConfigureHasUtcDateTimeProperties<TEntity>(ModelBuilder modelBuilder, ValueConverter<DateTime?, DateTime?> dateTimeValueConverter)
            where TEntity : class
        {
            modelBuilder.Entity<TEntity>()
                .HasUtcDateTimeProperties(dateTimeValueConverter);

            return modelBuilder;
        }
    }
}