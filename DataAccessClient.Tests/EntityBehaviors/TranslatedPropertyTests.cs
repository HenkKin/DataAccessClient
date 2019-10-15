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
            var translatedProperty = new TranslatedProperty();
            var propertyTranslation = new PropertyTranslation();
            
            // Act
            translatedProperty.Translations = new List<PropertyTranslation>{ propertyTranslation };

            // Assert
            Assert.Same(propertyTranslation, translatedProperty.Translations.Single());
        }
    }
}