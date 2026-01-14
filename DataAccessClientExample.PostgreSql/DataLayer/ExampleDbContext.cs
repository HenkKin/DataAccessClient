using DataAccessClient.EntityFrameworkCore.Relational;
using Microsoft.EntityFrameworkCore;

namespace DataAccessClientExample.DataLayer
{
    internal class ExampleDbContext : RelationalDbContext
    {
        public ExampleDbContext(DbContextOptions<ExampleDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
           modelBuilder.HasDefaultSchema("dbo");

           modelBuilder.Entity<ExampleEntity>()
                .ToTable("ExampleEntities");

            modelBuilder
                .Entity<ExampleEntityView>()
                .HasNoKey()
                .ToSqlQuery("SELECT e.\"Id\", et.\"LocaleId\", e.\"Name\", et.\"Description\" FROM dbo.\"ExampleEntities\" e INNER JOIN dbo.\"ExampleEntityTranslation\" et ON e.\"Id\" = et.\"TranslatedEntityId\"");
            
            base.OnModelCreating(modelBuilder);
        }
    }
}
