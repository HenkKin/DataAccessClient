using System;
using System.Collections.Generic;
using DataAccessClient.EntityBehaviors;

namespace DataAccessClient.EntityFrameworkCore.Relational.Tests.TestModels
{
    public class TestEntity : IIdentifiable<int>, ICreatable<int>, IModifiable<int>, ISoftDeletable<int>, IRowVersionable<byte[]>, ITranslatable<TestEntityTranslation, int, string>, ITenantScopable<int>
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
        public TranslatedProperty<string> Name { get; set; }
        public ICollection<TestEntityTranslation> Translations { get; set; } = new List<TestEntityTranslation>();
        public int TenantId { get; set; }
        public string Description { get; set; }
    }

    public class TestEntityTranslation : IEntityTranslation<TestEntity, int, string>
    {
        public TestEntity TranslatedEntity { get; set; }
        public int TranslatedEntityId { get; set; }
        public string LocaleId { get; set; }
        public string Description { get; set; }
    }
}
