namespace VectorRagDemo.Models.ViewModels
{
    public class ConversatieFilterModel
    {
        public string? Zoekterm { get; set; }
        public DateTime? DatumVan { get; set; }
        public DateTime? DatumTot { get; set; }
        public string Sortering { get; set; } = "nieuwste";
        public int Pagina { get; set; } = 1;
        public const int PaginaGrootte = 25;
        public bool IsLeeg => string.IsNullOrWhiteSpace(Zoekterm)
            && !DatumVan.HasValue && !DatumTot.HasValue && Sortering == "nieuwste";
    }
}
