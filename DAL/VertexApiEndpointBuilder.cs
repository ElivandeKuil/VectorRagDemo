using VectorRagDemo.BLL;
using VectorRagDemo.Models.Enums;

namespace VectorRagDemo.DAL
{
    public static class VertexApiEndpointBuilder
    {
        public static string BuildEmbeddingEndpoint()
        {
            string projectId = ConnectionProcessor.GetProjectId();
            return $"https://{Config.Location}-aiplatform.googleapis.com/v1/projects/{projectId}/locations/{Config.Location}/publishers/google/models/{Config.EmbeddingModel}:predict";
        }

        public static string BuildGeminiEndpoint(string model)
        {
            string projectId = ConnectionProcessor.GetProjectId();
            return $"https://{GeminiLocationEnum.Netherlands}-aiplatform.googleapis.com/v1/projects/{projectId}/locations/{GeminiLocationEnum.Netherlands}/publishers/google/models/{model}:generateContent";
        }
    }
}
