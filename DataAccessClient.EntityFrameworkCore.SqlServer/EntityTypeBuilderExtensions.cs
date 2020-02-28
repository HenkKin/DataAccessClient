using System;
using System.Linq;
using System.Linq.Expressions;
using DataAccessClient.EntityBehaviors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

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

        internal static EntityTypeBuilder<TEntity> IsSoftDeletable<TEntity, TIdentifierType>(this EntityTypeBuilder<TEntity> entity, Expression<Func<TEntity, bool>> queryFilter)
            where TEntity : class, ISoftDeletable<TIdentifierType>
            where TIdentifierType : struct
        {
            entity.Property(e => e.IsDeleted).IsRequired();
            entity.Property(e => e.DeletedOn).IsRequired(false);
            entity.Property(e => e.DeletedById).IsRequired(false);

            entity.AppendQueryFilter(queryFilter);
            return entity;
        }

        internal static EntityTypeBuilder<TEntity> IsTenantScopable<TEntity, TIdentifierType>(this EntityTypeBuilder<TEntity> entity, Expression<Func<TEntity, bool>> queryFilter)
            where TEntity : class, ITenantScopable<TIdentifierType>
            where TIdentifierType : struct
        {
            entity.Property(e => e.TenantId).IsRequired();

            entity.AppendQueryFilter(queryFilter);

            return entity;
        }

        internal static EntityTypeBuilder<TEntity> IsLocalizable<TEntity, TIdentifierType>(this EntityTypeBuilder<TEntity> entity, Expression<Func<TEntity, bool>> queryFilter)
            where TEntity : class, ILocalizable<TIdentifierType>
            where TIdentifierType : IConvertible
        {
            entity.Property(e => e.LocaleId).IsRequired();

            entity.AppendQueryFilter(queryFilter);

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

        internal static EntityTypeBuilder<TEntity> IsTranslatable<TEntity, TEntityTranslation, TIdentifierType, TLocaleIdentifierType>(this EntityTypeBuilder<TEntity> entity)
            where TEntity : class, ITranslatable<TEntityTranslation, TIdentifierType, TLocaleIdentifierType>
            where TEntityTranslation : class, IEntityTranslation<TEntity, TIdentifierType, TLocaleIdentifierType>
            where TIdentifierType : struct
            where TLocaleIdentifierType : IConvertible
        {
            entity.HasMany(x => x.Translations)
                .WithOne(x => x.TranslatedEntity)
                .HasForeignKey(x => x.TranslatedEntityId)
                .OnDelete(DeleteBehavior.Cascade);
            return entity;
        }

        internal static EntityTypeBuilder<TEntity> IsEntityTranslation<TEntity, TTranslatableEntity, TIdentifierType, TLocaleIdentifierType>(this EntityTypeBuilder<TEntity> entity)
            where TEntity : class, IEntityTranslation<TTranslatableEntity, TIdentifierType, TLocaleIdentifierType>
            where TTranslatableEntity : class, ITranslatable<TEntity, TIdentifierType, TLocaleIdentifierType>
            where TIdentifierType : struct
            where TLocaleIdentifierType : IConvertible
        {
            entity.HasOne(x => x.TranslatedEntity)
                .WithMany(x => x.Translations)
                .HasForeignKey(x => x.TranslatedEntityId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasKey(e => new { e.TranslatedEntityId, Language = e.LocaleId });
            entity.Property(e => e.TranslatedEntityId).IsRequired();
            entity.Property(e => e.LocaleId).IsRequired();

            return entity;
        }

        internal static EntityTypeBuilder<TEntity> HasTranslatedProperties<TEntity>(this EntityTypeBuilder<TEntity> entity)
            where TEntity : class
        {
            var translatedProperties = typeof(TEntity).GetProperties().Where(p => p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(TranslatedProperty<>));
            foreach (var translatedProperty in translatedProperties)
            {
                var localeIdentifierType = translatedProperty.PropertyType.GenericTypeArguments[0];
                var ownsOneType = typeof(TranslatedProperty<>).MakeGenericType(localeIdentifierType);
                entity.OwnsOne(ownsOneType, translatedProperty.Name, translatedPropertyBuilder =>
                {
                    var propertyTranslationType = typeof(PropertyTranslation<>).MakeGenericType(localeIdentifierType);
                    translatedPropertyBuilder.OwnsMany(propertyTranslationType,
                        nameof(TranslatedProperty<int>.Translations), builder =>
                        {
                            builder.ToTable(typeof(TEntity).Name + "_" + translatedProperty.Name + nameof(TranslatedProperty<int>.Translations));
                            builder.WithOwner().HasForeignKey("OwnerId");
                            builder.Property(nameof(PropertyTranslation<int>.LocaleId)).IsRequired();
                            builder.Property(nameof(PropertyTranslation<int>.Translation)).IsRequired();
                        }
                    );
                });
            }

            return entity;
        }

        internal static EntityTypeBuilder<TEntity> HasUtcDateTimeProperties<TEntity>(this EntityTypeBuilder<TEntity> entity, ValueConverter<DateTime?, DateTime?> dateTimeValueConverter)
            where TEntity : class
        {
            var datetimeProperties = typeof(TEntity).GetProperties()
                .Where(property => property.CanWrite && (property.PropertyType == typeof(DateTime) ||  property.PropertyType == typeof(DateTime?))
                )
                .ToList();

            datetimeProperties.ForEach(property =>
            {
                entity.Property(property.Name)
                    .HasConversion(dateTimeValueConverter);
            });

            return entity;
        }

        internal static EntityTypeBuilder<TEntity> AppendQueryFilter<TEntity>(this EntityTypeBuilder<TEntity> entityTypeBuilder, Expression<Func<TEntity, bool>> expression) where TEntity : class
        {
            var parameterType = Expression.Parameter(entityTypeBuilder.Metadata.ClrType);
            var expressionFilter = ReplacingExpressionVisitor.Replace(
                expression.Parameters.Single(), parameterType, expression.Body);

            var currentQueryFilter = entityTypeBuilder.Metadata.GetQueryFilter();

            if (currentQueryFilter != null)
            {
                var currentExpressionFilter = ReplacingExpressionVisitor.Replace(
                    currentQueryFilter.Parameters.Single(), parameterType, currentQueryFilter.Body);
                expressionFilter = Expression.AndAlso(currentExpressionFilter, expressionFilter);
            }

            var lambdaExpression = Expression.Lambda(expressionFilter, parameterType);
            entityTypeBuilder.HasQueryFilter(lambdaExpression);

            return entityTypeBuilder;
        }
    }
}