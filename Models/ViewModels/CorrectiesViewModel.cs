using VectorRagDemo.Models.Entities;

namespace VectorRagDemo.Models.ViewModels
{
    public class CorrectiesViewModel
    {
        public Project Project { get; set; } = null!;
        public List<CorrectieRijViewModel> Correcties { get; set; } = new();
        public int TotaalAantal { get; set; }
        public int Pagina { get; set; } = 1;
        public const int PaginaGrootte = 25;
        public int TotaalPaginas => (int)Math.Ceiling(TotaalAantal / (double)PaginaGrootte);
    }

    public class CorrectieRijViewModel
    {
        public int ID { get; set; }
        public int ConversatieID { get; set; }
        public int MessageID { get; set; }
        public string OrigineleTekst { get; set; } = "";
        public string GecontextualiseerdeVraag { get; set; } = "";
        public string Correctietekst { get; set; } = "";
        public DateTime GemaaktOp { get; set; }
        public DateTime? GewijzigdOp { get; set; }
    }

    public class CorrectieBewerkenViewModel
    {
        public int ID { get; set; }
        public int ProjectID { get; set; }
        public int ConversatieID { get; set; }
        public string OrigineleTekst { get; set; } = "";
        public string GecontextualiseerdeVraag { get; set; } = "";
        public string Correctietekst { get; set; } = "";
        public Project Project { get; set; } = null!;
    }
}
