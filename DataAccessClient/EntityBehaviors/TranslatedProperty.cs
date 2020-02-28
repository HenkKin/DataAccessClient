using System;
using System.Collections.Generic;

namespace DataAccessClient.EntityBehaviors
{
    public class TranslatedProperty<TLocaleIdentifierType>
        where TLocaleIdentifierType : IConvertible
    {
        public ICollection<PropertyTranslation<TLocaleIdentifierType>> Translations { get; set; } = new List<PropertyTranslation<TLocaleIdentifierType>>();
    }

    public class PropertyTranslation<TLocaleIdentifierType>
        where TLocaleIdentifierType : IConvertible

    {
        public string Translation { get; set; }
        public TLocaleIdentifierType LocaleId { get; set; }
    }
}