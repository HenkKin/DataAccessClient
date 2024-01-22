using DataAccessClient.Providers;

namespace DataAccessClientExample.DataLayer
{
    internal class ExampleLocaleIdentifierProvider : ILocaleIdentifierProvider<string>
    {
        public string LocaleId { get; private set; } = "nl-NL";
        public string Execute()
        {
            return LocaleId;
        }

        public void ChangeTentantIdentifier(string localeIdentifier)
        {
            LocaleId = localeIdentifier;
        }
    }
}