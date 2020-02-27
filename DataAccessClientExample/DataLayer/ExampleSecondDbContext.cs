using DataAccessClient.EntityFrameworkCore.SqlServer;
using Microsoft.EntityFrameworkCore;

namespace DataAccessClientExample.DataLayer
{
    internal class ExampleSecondDbContext : SqlServerDbContext
    {
        public ExampleSecondDbContext(DbContextOptions<ExampleSecondDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ExampleSecondEntity>()
                .ToTable("ExampleSecondEntities");
                
            base.OnModelCreating(modelBuilder);
        }
    }
}
