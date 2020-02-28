using System;

namespace DataAccessClient.EntityBehaviors
{
    public interface ILocalizable<TLocaleIdentifierType> where TLocaleIdentifierType : IConvertible
    {
        TLocaleIdentifierType LocaleId { get; set; }
    }
}