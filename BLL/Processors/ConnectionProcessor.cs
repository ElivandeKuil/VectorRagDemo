using Microsoft.Extensions.Configuration;

namespace VectorRagDemo.BLL.Processors
{
    // Leesolgorde voor elke waarde:
    //   1. appsettings.{Environment}.json  (via IConfiguration, nooit committen)
    //   2. Omgevingsvariabele
    //
    // Dev-machine:  waarden in appsettings.Development.json (staat in .gitignore)
    // Prod-machine: waarden als omgevingsvariabele of in appsettings.Production.json
    public static class ConnectionProcessor
    {
        private static IConfiguration? _config;

        // Aanroepen vanuit Program.cs zodat appsettings beschikbaar zijn.
        public static void Configure(IConfiguration config) => _config = config;

        public static string GetApiKey()
        {
            var apiKey = _config?["Mistral:ApiKey"]
                ?? Environment.GetEnvironmentVariable("MISTRAL_API_KEY");

            if (string.IsNullOrEmpty(apiKey))
                throw new InvalidOperationException(
                    "Mistral API-sleutel niet geconfigureerd. " +
                    "Voeg 'Mistral:ApiKey' toe aan appsettings.Development.json (dev) " +
                    "of stel MISTRAL_API_KEY in als omgevingsvariabele (prod).");

            return apiKey;
        }

        // Kept for backwards compatibility — returns the API key as a bearer token value
        public static Task<string> GetAuthenticationToken() => Task.FromResult(GetApiKey());
    }
}
