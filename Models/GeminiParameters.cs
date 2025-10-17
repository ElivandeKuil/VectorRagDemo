namespace VectorRagDemo.Models
{
    public class GeminiParameters
    {
        public int MaxOutputTokens { get; set; } = 2048;
        public float Temperature { get; set; } = 0.2f;
        public float TopP { get; set; } = 0.8f;
        public int TopK { get; set; } = 40;
    }
}
