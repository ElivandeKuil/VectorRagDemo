using VectorRagDemo.Models.Entities;

namespace VectorRagDemo.Models.ViewModels
{
    public class ConversatieDetailViewModel
    {
        public int ID { get; set; }
        public int ProjectID { get; set; }
        public DateTime GemaaktOp { get; set; }
        public string BronType { get; set; } = "";
        public double DuurMinuten { get; set; }
        public List<BerichtViewModel> Berichten { get; set; } = new();
        public List<EscalatieEventRij> Escalaties { get; set; } = new();
        public Project Project { get; set; } = null!;
    }

    public class BerichtViewModel
    {
        public int ID { get; set; }
        public string Tekst { get; set; } = "";
        public DateTime GemaaktOp { get; set; }
        public bool IsBot { get; set; }
    }

    public class EscalatieEventRij
    {
        public DateTime GemaaktOp { get; set; }
        public string Kanaal { get; set; } = "";
    }
}
