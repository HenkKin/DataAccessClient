using Microsoft.EntityFrameworkCore;

namespace DataAccessClient.EntityFrameworkCore.SqlServer.Tests
{
    public class TestDbContext : SqlServerDbContext<int, int>
    {
        public TestDbContext()
        {
            
        }

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