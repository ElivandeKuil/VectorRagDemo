using Google.Apis.Auth.OAuth2;
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

        public static async Task<string> GetAuthenticationToken()
        {
            // Probeer eerst volledige JSON-inhoud
            var credentialsJson = _config?["Google:CredentialsJson"]
                ?? Environment.GetEnvironmentVariable("GOOGLE_CREDENTIALS_JSON");

            if (string.IsNullOrEmpty(credentialsJson))
            {
                // Probeer pad naar JSON-bestand
                var credentialsPath = _config?["Google:CredentialsPath"]
                    ?? Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");

                if (string.IsNullOrEmpty(credentialsPath))
                    throw new InvalidOperationException(
                        "Google-credentials niet geconfigureerd. " +
                        "Voeg 'Google:CredentialsJson' of 'Google:CredentialsPath' toe aan " +
                        "appsettings.Development.json (dev) of stel GOOGLE_CREDENTIALS_JSON / " +
                        "GOOGLE_APPLICATION_CREDENTIALS in als omgevingsvariabele (prod).");

                credentialsJson = await File.ReadAllTextAsync(credentialsPath);
            }

            GoogleCredential credential = GoogleCredential.FromJson(credentialsJson);

            if (credential.IsCreateScopedRequired)
                credential = credential.CreateScoped("https://www.googleapis.com/auth/cloud-platform");

            return await credential.UnderlyingCredential.GetAccessTokenForRequestAsync();
        }

        public static string GetProjectId()
        {
            var projectNumber = _config?["Google:ProjectNumber"]
                ?? Environment.GetEnvironmentVariable("GOOGLE_PROJECT_NUMBER");

            if (string.IsNullOrEmpty(projectNumber))
                throw new InvalidOperationException(
                    "Google-projectnummer niet geconfigureerd. " +
                    "Voeg 'Google:ProjectNumber' toe aan appsettings.Development.json (dev) " +
                    "of stel GOOGLE_PROJECT_NUMBER in als omgevingsvariabele (prod).");

            return projectNumber;
        }
    }
}
