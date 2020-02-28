//using System.Linq;
//using System.Threading.Tasks;
//using DataAccessClient.Configuration;
//using DataAccessClient.EntityFrameworkCore.SqlServer.Tests.TestBase;
//using DataAccessClient.EntityFrameworkCore.SqlServer.Tests.TestModels;
//using DataAccessClient.Providers;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.DependencyInjection;
//using Xunit;

//namespace DataAccessClient.EntityFrameworkCore.SqlServer.Tests.EntityBehaviors
//{
//    public class LocalizableIntegrationTests : DbContextTestBase
//    {
//        public LocalizableIntegrationTests() : base(nameof(LocalizableIntegrationTests))
//        {
//        }

//        [Fact]
//        public async Task LocalizableQueryFilter_WhenCalled_ItShouldApplyQueryFilter()
//        {
//            // Arrange
//            var localeIdentifierProvider = (TestLocaleIdentifierProvider)ServiceProvider.GetRequiredService<ILocaleIdentifierProvider<string>>();
//            var localizationConfiguration = ServiceProvider.GetRequiredService<ILocalizationConfiguration>();

//            localeIdentifierProvider.ChangeLocaleIdentifier("nl-NL");
//            var testEntityLocale1 = new TestEntity();
//            testEntityLocale1.Translations.Add(new TestEntityTranslation{Description = "Uitproberen", LocaleId = "nl-NL"});
//            testEntityLocale1.Translations.Add(new TestEntityTranslation{Description = "test", LocaleId = "en-GB" });
//            TestEntityRepository.Add(testEntityLocale1);
//            await UnitOfWork.SaveAsync();

//            localeIdentifierProvider.ChangeLocaleIdentifier("en-GB");
//            var testEntityLocale2 = new TestEntity();
//            testEntityLocale2.Translations.Add(new TestEntityTranslation { Description = "Uitproberen2", LocaleId = "nl-NL" });
//            testEntityLocale2.Translations.Add(new TestEntityTranslation { Description = "test2", LocaleId = "en-GB" });
//            TestEntityRepository.Add(testEntityLocale2);
//            await UnitOfWork.SaveAsync();

//            localizationConfiguration.EnableQueryFilter();
//            Assert.True(localizationConfiguration.IsQueryFilterEnabled);

//            using (localizationConfiguration.DisableQueryFilter())
//            {
//                Assert.False(localizationConfiguration.IsQueryFilterEnabled);

//                var allLocaleEntities = await TestEntityRepository.GetReadOnlyQuery().Include(x=>x.Translations).ToListAsync();
//                Assert.Equal(2, allLocaleEntities.Count);
//            }

//            Assert.True(localizationConfiguration.IsQueryFilterEnabled);

//            localeIdentifierProvider.ChangeLocaleIdentifier("nl-NL");
//            var locale1Entities = await TestDbContext.TestEntitiesView.ToListAsync();
//            locale1Entities = await TestEntityViewRepository.GetReadOnlyQuery().ToListAsync();
//            Assert.Single(locale1Entities);
//            Assert.Equal("nl-NL", locale1Entities.Single().LocaleId);

//            localeIdentifierProvider.ChangeLocaleIdentifier("en-GB");
//            var locale2Entities = await TestEntityViewRepository.GetReadOnlyQuery().ToListAsync();
//            Assert.Single(locale2Entities);
//            Assert.Equal("en-GB", locale2Entities.Single().LocaleId);
//        }
//    }
//}
