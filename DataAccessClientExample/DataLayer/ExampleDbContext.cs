using DataAccessClient.EntityBehaviors;
using DataAccessClient.EntityFrameworkCore.SqlServer;
using Microsoft.EntityFrameworkCore;

namespace DataAccessClientExample.DataLayer
{
    internal class ExampleDbContext : SqlServerDbContext<int>
    {
        public ExampleDbContext(DbContextOptions<ExampleDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ExampleEntity>()
                .ToTable("ExampleEntities");
            base.OnModelCreating(modelBuilder);
        }
    }
}
