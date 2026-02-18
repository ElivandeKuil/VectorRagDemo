using System.ComponentModel.DataAnnotations;

namespace VectorRagDemo.Models.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Gebruikersnaam is verplicht")]
        [Display(Name = "Gebruikersnaam")]
        public string GebruikersNaam { get; set; } = string.Empty;

        [Required(ErrorMessage = "Wachtwoord is verplicht")]
        [DataType(DataType.Password)]
        [Display(Name = "Wachtwoord")]
        public string Wachtwoord { get; set; } = string.Empty;

        [Display(Name = "Onthoud mij")]
        public bool RememberMe { get; set; }
    }
}
