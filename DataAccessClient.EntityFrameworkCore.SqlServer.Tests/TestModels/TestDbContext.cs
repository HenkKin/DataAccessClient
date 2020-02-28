using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace DataAccessClient.EntityFrameworkCore.SqlServer.Tests.TestModels
{
    public class TestDbContext : SqlServerDbContext
    {
        public DbSet<TestEntity> TestEntities { get; set; } 
        public DbSet<TestEntityView> TestEntitiesView { get; set; }

        public CascadeTiming CascadeDeleteTiming => ChangeTracker.CascadeDeleteTiming;
        public CascadeTiming DeleteOrphansTiming => ChangeTracker.DeleteOrphansTiming;

        public TestDbContext(DbContextOptions options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TestEntity>();

            // TODO: create TestEntityView ToQuery expression
            //Expression<Func<IQueryable<TestEntityView>>> testEntityViewQuery = () =>
            //    from testEntity in TestEntities
            //        //join testEntityTranslation in Set<TestEntityTranslation>() on testEntity.Id equals testEntityTranslation.TranslatedEntityId into testEntityTranslations
            //        //from m in testEntityTranslations.DefaultIfEmpty()
            //    select new TestEntityView
            //    {
            //        Id = testEntity.Id,
            //        RowVersion = testEntity.RowVersion,
            //        CreatedOn = testEntity.CreatedOn,
            //        CreatedById = testEntity.CreatedById,
            //        IsDeleted = testEntity.IsDeleted,
            //        DeletedById = testEntity.DeletedById,
            //        DeletedOn = testEntity.DeletedOn,
            //        ModifiedById = testEntity.ModifiedById,
            //        ModifiedOn = testEntity.ModifiedOn,
            //        // Name = testEntity.Name.Translations.SingleOrDefault(t=>t.LocaleId == m.LocaleId).Translation,
            //        TenantId = testEntity.TenantId,
            //        //LocaleId = m.LocaleId,
            //        //Description = m.Description
            //    };


            //modelBuilder
            //    .Entity<TestEntityView>()
            //    .HasNoKey()
            //    .ToQuery(
            //        () => TestEntities
            //            .Select(
            //                testEntity => new TestEntityView
            //                {
            //                    Id = testEntity.Id,
            //                    RowVersion = testEntity.RowVersion,
            //                    CreatedOn = testEntity.CreatedOn,
            //                    CreatedById = testEntity.CreatedById,
            //                    IsDeleted = testEntity.IsDeleted,
            //                    DeletedById = testEntity.DeletedById,
            //                    DeletedOn = testEntity.DeletedOn,
            //                    ModifiedById = testEntity.ModifiedById,
            //                    ModifiedOn = testEntity.ModifiedOn,
            //                    // Name = testEntity.Name.Translations.SingleOrDefault(t=>t.LocaleId == m.LocaleId).Translation,
            //                    TenantId = testEntity.TenantId,
            //                    //LocaleId = m.LocaleId,
            //                    //Description = m.Description
            //                }));

            base.OnModelCreating(modelBuilder);
        }
    }

    //public class TestEntityViewConfiguration : IEntityTypeConfiguration<TestEntityView>
    //{
    //    public void Configure(EntityTypeBuilder<TestEntityView> builder)
    //    {
    //        builder.ToQuery(() => { });
    //    }
    //}
}