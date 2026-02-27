using System.ComponentModel.DataAnnotations;

namespace VectorRagDemo.Models.ViewModels
{
    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Huidig wachtwoord is verplicht")]
        [DataType(DataType.Password)]
        [Display(Name = "Huidig wachtwoord")]
        public string HuidigWachtwoord { get; set; } = string.Empty;

        [Required(ErrorMessage = "Nieuw wachtwoord is verplicht")]
        [DataType(DataType.Password)]
        [MinLength(8, ErrorMessage = "Wachtwoord moet minimaal 8 tekens bevatten")]
        [Display(Name = "Nieuw wachtwoord")]
        public string NieuwWachtwoord { get; set; } = string.Empty;

        [Required(ErrorMessage = "Bevestig het nieuwe wachtwoord")]
        [DataType(DataType.Password)]
        [Compare("NieuwWachtwoord", ErrorMessage = "Wachtwoorden komen niet overeen")]
        [Display(Name = "Bevestig wachtwoord")]
        public string BevestigWachtwoord { get; set; } = string.Empty;
    }
}
