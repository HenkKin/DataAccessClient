using System;
using System.Collections.Generic;
using DataAccessClient;
using DataAccessClient.EntityBehaviors;

namespace DataAccessClientExample.DataLayer
{
    public class ExampleEntity : IIdentifiable<int>, ICreatable<int>, IModifiable<int>, ISoftDeletable<int>, IRowVersionable, ITranslatable<ExampleEntityTranslation, int>, ITenantScopable<int>
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
        public string Name { get; set; }
        public ICollection<ExampleEntityTranslation> Translations { get; set; }
        public int TenantId { get; set; }
    }

    public class ExampleEntityTranslation : IEntityTranslation<ExampleEntity, int>
    {
        public string Description { get; set; }
        public string Language { get; set; }
        public ExampleEntity TranslatedEntity { get; set; }
        public int TranslatedEntityId { get; set; }
    }
}
