using System;
using DataAccessClient.EntityBehaviors;

namespace DataAccessClient.EntityFrameworkCore.SqlServer.Tests.TestModels
{
    public class TestEntityView : ILocalizable<string>//  : ISoftDeletable<int>, IRowVersionable, ITenantScopable<int>, ILocalizable<string>
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
        public int TenantId { get; set; }
        public string LocaleId { get; set; }
        public string Description { get; set; }
    }
}
