using VectorRagDemo.BLL;
using VectorRagDemo.BLL.Processors;
using VectorRagDemo.Models.Enums;

namespace VectorRagDemo.DAL
{
    public static class VertexApiEndpointBuilder
    {
        public static string BuildEmbeddingEndpoint()
        {
            string projectId = ConnectionProcessor.GetProjectId();
            return $"https://aiplatform.googleapis.com/v1/projects/{projectId}/locations/global/publishers/google/models/{Config.EmbeddingModel}:predict";
        }

        public static string BuildGeminiEndpoint(string model)
        {
            string projectId = ConnectionProcessor.GetProjectId();
            return $"https://aiplatform.googleapis.com/v1/projects/{projectId}/locations/global/publishers/google/models/{model}:generateContent";
        }
    }
}
