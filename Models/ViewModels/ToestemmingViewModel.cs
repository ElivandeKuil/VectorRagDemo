using VectorRagDemo.Models.Entities;

namespace VectorRagDemo.Models.ViewModels
{
    public class ToestemmingViewModel
    {
        public bool HasActiveConsent { get; set; }
        public bool ConsentRequired { get; set; }
        public List<Toestemming> History { get; set; } = new();
        public string? ReturnUrl { get; set; }
    }
}
