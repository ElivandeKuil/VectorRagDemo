namespace VectorRagDemo.BLL
{
    public static class Config
    {
        public const string Location = "europe-west4";
        public const string EmbeddingModel = "text-embedding-004";
        public const string GeminiModelID = "gemini-2.5-flash-lite";

        //Gemini parameters
        public const int MaxTokens = 4000;
        public const double Temperature = 0.4;
        public const double TopP = 1.0;
        public const int TopK = 32;

        //Vector search parameters
        public const int BatchSize = 100;
        public const int VectorQueryTopK = 5;

        //Embedder
        public const int MaxWords = 400;
        public const int OverlapWords = 40;

        // Versie van de privacyverklaring. Verhoog dit als de verklaring wijzigt
        // zodat gebruikers opnieuw om toestemming wordt gevraagd.
        public const string ConsentVersion = "v1-2026-03";
    }
}
