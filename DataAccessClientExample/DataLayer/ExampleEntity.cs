using System;
using System.Collections.Generic;
using DataAccessClient.EntityBehaviors;

namespace DataAccessClientExample.DataLayer
{
    public interface IEntity : IIdentifiable<int>, ICreatable<int>, IModifiable<int>, ISoftDeletable<int>, IRowVersionable, ITranslatable<ExampleEntityTranslation, int>, ITenantScopable<int>
    {

    }
    public abstract class BaseEntity : IEntity
    {
        public int Id { get; set; }
        public DateTime CreatedOn { get; set; }
        public int CreatedById { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public int? ModifiedById { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedOn { get; set; }
        public int? DeletedById { get; set; }
        public byte[] RowVersion { get; set; }
        public ICollection<ExampleEntityTranslation> Translations { get; set; }
        public int TenantId { get; set; }
    }
    public class ExampleEntity : BaseEntity
    {
        public string Name { get; set; }
    }

    public class ExampleEntityTranslation : IEntityTranslation<ExampleEntity, int>
    {
        public string Description { get; set; }
        public string Language { get; set; }
        public ExampleEntity TranslatedEntity { get; set; }
        public int TranslatedEntityId { get; set; }
    }
}
