using VectorRagDemo.Models.Entities;

namespace VectorRagDemo.Models.ViewModels
{
    public class ConversatiesViewModel
    {
        public Project Project { get; set; } = null!;
        public ConversatieFilterModel Filter { get; set; } = new();
        public List<ConversatieRijViewModel> Gesprekken { get; set; } = new();
        public int TotaalAantal { get; set; }
        public bool HeeftEscalatieFunctie { get; set; }
        public int TotaalPaginas => (int)Math.Ceiling(TotaalAantal / (double)ConversatieFilterModel.PaginaGrootte);
    }

    public class ConversatieRijViewModel
    {
        public int ID { get; set; }
        public DateTime GemaaktOp { get; set; }
        public string BronType { get; set; } = "";
        public int TotaalBerichten { get; set; }
        public int GebruikerBerichten { get; set; }
        public double DuurMinuten { get; set; }
        public string? EersteBerichtPreview { get; set; }
        public bool HeeftEscalatie { get; set; }
        public string? EscalatieKanalen { get; set; }
        public bool IsVerlaten => GebruikerBerichten == 1;
        public bool IsBetrokken => TotaalBerichten >= 5;
    }
}
