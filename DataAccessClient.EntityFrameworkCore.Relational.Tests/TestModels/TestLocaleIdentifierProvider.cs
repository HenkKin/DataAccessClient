using System;
using DataAccessClient.Providers;

namespace DataAccessClient.EntityFrameworkCore.Relational.Tests.TestModels
{
    public class TestLocaleIdentifierProvider : ILocaleIdentifierProvider<string>
    {
        public Guid InstanceId { get; }
        public TestLocaleIdentifierProvider()
        {
            InstanceId = Guid.NewGuid();
        }

        public string LocaleId { get; private set; } = "nl-NL";

        public string Execute()
        {
            return LocaleId;
        }

        public void ChangeLocaleIdentifier(string localeIdentifier)
        {
            LocaleId = localeIdentifier;
        }
    }
}
