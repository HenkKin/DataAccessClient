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
            var propertyTranslation = new PropertyTranslation();

            // Act
            propertyTranslation.Language = "nl";
            propertyTranslation.Translation = "test";

            // Assert
            Assert.Equal("nl", propertyTranslation.Language);
            Assert.Equal("test", propertyTranslation.Translation);
        }
    }
}
