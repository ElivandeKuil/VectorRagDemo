using Newtonsoft.Json;

namespace VectorRagDemo.Models.JsonContracts.MistralAPI
{
    public class MistralStreamChunk
    {
        [JsonProperty("choices")]
        public List<MistralStreamChoice> Choices { get; set; } = new();
    }

    public class MistralStreamChoice
    {
        [JsonProperty("delta")]
        public MistralStreamDelta Delta { get; set; } = new();

        [JsonProperty("finish_reason")]
        public string? FinishReason { get; set; }
    }

    public class MistralStreamDelta
    {
        [JsonProperty("content")]
        public string? Content { get; set; }
    }
}
