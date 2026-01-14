using System;
using DataAccessClient.EntityBehaviors;

namespace DataAccessClient.EntityFrameworkCore.SqlServer.Tests.TestModels
{
    public class TestEntityWithWrongIdentifierTypes : IIdentifiable<long>, ICreatable<long>, IModifiable<long>, ISoftDeletable<long>, IRowVersionable<byte[]>, ITenantScopable<long>
    {
        public long Id { get; set; }
        public DateTime CreatedOn { get; set; }
        public long CreatedById { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public long? ModifiedById { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedOn { get; set; }
        public long? DeletedById { get; set; }
        public byte[] RowVersion { get; set; }
        public TranslatedProperty<string> Name { get; set; }
        public long TenantId { get; set; }
        public string Description { get; set; }

    }
}
