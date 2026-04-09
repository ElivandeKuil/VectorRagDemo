namespace VectorRagDemo.DAL
{
    public static class MistralApiEndpointBuilder
    {
        private const string BaseUrl = "https://api.mistral.ai/v1";

        public static string BuildChatEndpoint() => $"{BaseUrl}/chat/completions";

        public static string BuildEmbeddingEndpoint() => $"{BaseUrl}/embeddings";
    }
}
