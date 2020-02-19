using System.Collections.Generic;

namespace DataAccessClient.EntityBehaviors
{
    public class TranslatedProperty
    {
        public ICollection<PropertyTranslation> Translations { get; set; } = new List<PropertyTranslation>();
    }

    public class PropertyTranslation
    {
        public string Translation { get; set; }
        public string Language { get; set; }
    }
}