using System;
using System.Collections.Generic;

namespace DataAccessClient.EntityBehaviors
{

    [Obsolete("This is in research phase, this can be subject of change without notification")]
    public class TranslatedProperty
    {
        public ICollection<PropertyTranslation> Translations { get; set; } = new List<PropertyTranslation>();
    }

    [Obsolete("This is in research phase, this can be subject of change without notification")]
    public class PropertyTranslation
    {
        public string Translation { get; set; }
        public string Language { get; set; }
    }
}