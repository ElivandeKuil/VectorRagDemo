using Newtonsoft.Json;

namespace VectorRagDemo.Models.JsonContracts.MistralAPI
{
    public class MistralChatResponse
    {
        [JsonProperty("choices")]
        public List<MistralChoice> Choices { get; set; } = new();
    }

    public class MistralChoice
    {
        [JsonProperty("message")]
        public MistralMessage Message { get; set; } = new();
    }

    public class MistralMessage
    {
        [JsonProperty("role")]
        public string Role { get; set; } = string.Empty;

        [JsonProperty("content")]
        public string Content { get; set; } = string.Empty;
    }
}
