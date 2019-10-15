using System;
using DataAccessClient.EntityBehaviors;

namespace DataAccessClientExample.DataLayer
{
    public class ExampleSecondEntity : IIdentifiable<int>, ICreatable<int>, IModifiable<int>, ISoftDeletable<int>, IRowVersionable
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
        public TranslatedProperty Description { get; set; }
        public TranslatedProperty Code { get; set; }
    }
}