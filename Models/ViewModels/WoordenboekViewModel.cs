using VectorRagDemo.Models.Entities;

namespace VectorRagDemo.Models.ViewModels
{
    public class WoordenboekViewModel
    {
        public Project Project { get; set; } = null!;
        public List<WoordenboekRijViewModel> Entries { get; set; } = new();
        public int TotaalAantal { get; set; }
        public int Pagina { get; set; } = 1;
        public const int PaginaGrootte = 25;
        public int TotaalPaginas => (int)Math.Ceiling(TotaalAantal / (double)PaginaGrootte);
    }

    public class WoordenboekRijViewModel
    {
        public int ID { get; set; }
        public string Woord { get; set; } = "";
        public string Omschrijving { get; set; } = "";
        public DateTime GemaaktOp { get; set; }
        public DateTime? GewijzigdOp { get; set; }
    }

    public class WoordenboekBewerkenViewModel
    {
        public int ID { get; set; }
        public int ProjectID { get; set; }
        public string Woord { get; set; } = "";
        public string Omschrijving { get; set; } = "";
        public Project Project { get; set; } = null!;
    }

    public class WoordenboekAanmakenViewModel
    {
        public int ProjectID { get; set; }
        public string Woord { get; set; } = "";
        public string Omschrijving { get; set; } = "";
        public Project Project { get; set; } = null!;
    }
}
