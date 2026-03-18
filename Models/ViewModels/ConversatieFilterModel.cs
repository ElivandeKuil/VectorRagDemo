namespace VectorRagDemo.Models.ViewModels
{
    public class ConversatieFilterModel
    {
        public string? Zoekterm { get; set; }
        public string Kanaal { get; set; } = "alle";   // alle / widget / studio
        public DateTime? DatumVan { get; set; }
        public DateTime? DatumTot { get; set; }
        public string Type { get; set; } = "alle";     // alle / verlaten / betrokken / escalatie
        public string Sortering { get; set; } = "nieuwste";
        public int Pagina { get; set; } = 1;
        public const int PaginaGrootte = 25;
        public bool IsLeeg => string.IsNullOrWhiteSpace(Zoekterm) && Kanaal == "alle"
            && !DatumVan.HasValue && !DatumTot.HasValue && Type == "alle" && Sortering == "nieuwste";
    }
}
