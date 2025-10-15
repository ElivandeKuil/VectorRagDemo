using System.Runtime.Serialization;

namespace VectorRagDemo.Models
{
    [DataContract]
    public class GeminiEndpoints
    {
        [DataMember(Order = 1)]
        public string ProjectID { get; set; }

        [DataMember(Order = 2)]
        public string IndexID { get; set; }

        [DataMember(Order = 3)]
        public string EndpointID { get; set; }

        [DataMember(Order = 4)]
        public string EndpointPublicDomain { get; set; }

        [DataMember(Order = 5)]
        public string DeploymentID { get; set; }
    }
}
