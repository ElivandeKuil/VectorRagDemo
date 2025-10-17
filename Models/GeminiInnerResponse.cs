using Newtonsoft.Json;

namespace VectorRagDemo.Models
{
    public class GeminiInnerResponse
    {
        [JsonProperty("answer")]
        public string Answer { get; set; }

        [JsonProperty("supplementalInfo")]
        public string SupplementalInfo { get; set; }

        [JsonProperty("questionToUser")]
        public string QuestionToUser { get; set; }

        [JsonProperty("missesInfo")]
        public bool MissesInfo { get; set; }

        [JsonProperty("threatDetected")]
        public bool ThreatDetected { get; set; }

        [JsonProperty("usedChunks")]
        public string UsedChunks { get; set; }
    }
}
