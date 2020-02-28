using DataAccessClient.EntityBehaviors;
using Xunit;

namespace DataAccessClient.Tests.EntityBehaviors
{
    public class PropertyTranslationTests
    {
        [Fact]
        public void WhenPropertyTranslationIsSet_ItShouldHaveValues()
        {
            // Arrange
            var propertyTranslation = new PropertyTranslation<string> {LocaleId = "nl", Translation = "test"};

            // Act

            // Assert
            Assert.Equal("nl", propertyTranslation.LocaleId);
            Assert.Equal("test", propertyTranslation.Translation);
        }
    }
}
