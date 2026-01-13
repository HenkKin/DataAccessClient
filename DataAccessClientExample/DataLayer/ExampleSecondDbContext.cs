using DataAccessClient.EntityFrameworkCore.Relational;
using Microsoft.EntityFrameworkCore;

namespace DataAccessClientExample.DataLayer
{
    internal class ExampleSecondDbContext : RelationalDbContext
    {
        public ExampleSecondDbContext(DbContextOptions<ExampleSecondDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("dbo");

            modelBuilder.Entity<ExampleSecondEntity>()
                .ToTable("ExampleSecondEntities");
                
            base.OnModelCreating(modelBuilder);
        }
    }
}
