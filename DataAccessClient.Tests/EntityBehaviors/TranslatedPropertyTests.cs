using System.Collections.Generic;
using System.Linq;
using DataAccessClient.EntityBehaviors;
using Xunit;

namespace DataAccessClient.Tests.EntityBehaviors
{
    public class TranslatedPropertyTests
    {
        [Fact]
        public void WhenPropertyTranslationIsSet_ItShouldHaveValues()
        {
            // Arrange
            var translatedProperty = new TranslatedProperty<string>();
            var propertyTranslation = new PropertyTranslation<string>();
            
            // Act
            translatedProperty.Translations = new List<PropertyTranslation<string>>{ propertyTranslation };

            // Assert
            Assert.Same(propertyTranslation, translatedProperty.Translations.Single());
        }
    }
}