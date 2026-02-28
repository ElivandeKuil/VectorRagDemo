using System.ComponentModel.DataAnnotations;
using VectorRagDemo.Models.Entities;
using VectorRagDemo.Models.Entities.Management;

namespace VectorRagDemo.Models.ViewModels
{
    public class GebruikerViewModel
    {
        public int? ID { get; set; }

        [Required(ErrorMessage = "Naam is verplicht.")]
        [Display(Name = "Naam")]
        public string Naam { get; set; } = string.Empty;

        [Required(ErrorMessage = "Gebruikersnaam is verplicht.")]
        [Display(Name = "Gebruikersnaam")]
        public string GebruikersNaam { get; set; } = string.Empty;

        [Display(Name = "Wachtwoord")]
        public string? Wachtwoord { get; set; }

        // Selected IDs (submitted from form checkboxes)
        public List<int> GeselecteerdeRollen { get; set; } = new();
        public List<int> GeselecteerdeProjecten { get; set; } = new();
        public List<int> GeselecteerdeSubProjecten { get; set; } = new();

        // Single VectorRagDemo project (determines which chunks this user's chatbot searches)
        public int? GeselecteerdVectorProject { get; set; }

        // Available options (populated by controller, not bound from form)
        public List<Rol> BeschikbareRollen { get; set; } = new();
        public List<MgmtProject> BeschikbareProjecten { get; set; } = new();
        public List<MgmtSubProject> BeschikbareSubProjecten { get; set; } = new();
        public List<Project> BeschikbareVectorProjecten { get; set; } = new();
    }

    public class GebruikerListItemViewModel
    {
        public Gebruiker Gebruiker { get; set; } = null!;
        public List<string> Rollen { get; set; } = new();
    }
}
