using System;

namespace DataAccessClient.Providers
{
    public interface ILocaleIdentifierProvider<TLocaleIdentifierType>
        where TLocaleIdentifierType : IConvertible
    {
        TLocaleIdentifierType Execute();
    }
}
