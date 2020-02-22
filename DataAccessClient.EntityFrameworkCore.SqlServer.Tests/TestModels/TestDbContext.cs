using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace DataAccessClient.EntityFrameworkCore.SqlServer.Tests.TestModels
{
    public class TestDbContext : SqlServerDbContext<int, int>
    {
        public CascadeTiming CascadeDeleteTiming => ChangeTracker.CascadeDeleteTiming;
        public CascadeTiming DeleteOrphansTiming => ChangeTracker.DeleteOrphansTiming;

        public TestDbContext(DbContextOptions options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TestEntity>();
            base.OnModelCreating(modelBuilder);
        }
    }
}