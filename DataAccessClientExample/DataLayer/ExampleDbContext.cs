using DataAccessClient.EntityFrameworkCore.SqlServer;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace DataAccessClientExample.DataLayer
{
    internal class ExampleDbContext : SqlServerDbContext
    {
        public ExampleDbContext(DbContextOptions<ExampleDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ExampleEntity>()
                .ToTable("ExampleEntities");

            modelBuilder
                .Entity<ExampleEntityView>()
                .HasNoKey()
                .ToQuery(() =>
                    // TODO: Query does not work without query side evaluation, thows invalidoperationexception, needs investigation
                    (from exampleEntity in Set<ExampleEntity>()
                     from exampleEntityTranslation in Set<ExampleEntityTranslation>().Where(st => st.TranslatedEntityId == exampleEntity.Id)
                     select new ExampleEntityView
                     {
                         Id = exampleEntity.Id,
                         LocaleId = exampleEntityTranslation.LocaleId,
                         Name = exampleEntity.Name,
                         Description = exampleEntityTranslation.Description
                     }));

            base.OnModelCreating(modelBuilder);
        }
    }
}
