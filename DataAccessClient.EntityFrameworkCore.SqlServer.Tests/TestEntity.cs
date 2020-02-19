using System;
using System.Collections.Generic;
using DataAccessClient.EntityBehaviors;

namespace DataAccessClient.EntityFrameworkCore.SqlServer.Tests
{
    public class TestEntity : IIdentifiable<int>, ICreatable<int>, IModifiable<int>, ISoftDeletable<int>, IRowVersionable, ITranslatable<TestEntityTranslation, int>, ITenantScopable<int>
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
        public TranslatedProperty Name { get; set; }
        public ICollection<TestEntityTranslation> Translations { get; set; }
        public int TenantId { get; set; }
    }

    public class TestEntityTranslation : IEntityTranslation<TestEntity, int>
    {
        public TestEntity TranslatedEntity { get; set; }
        public int TranslatedEntityId { get; set; }
        public string Language { get; set; }
        public string Description { get; set; }
    }
}
