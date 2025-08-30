using System.ComponentModel.DataAnnotations;

namespace CrmContactsApi.Models.DTOs
{
    public class CreateContactoRequest
    {
        [Required(ErrorMessage = "El nombre es requerido")]
        public string Nom_contact { get; set; } = string.Empty;

        [Required(ErrorMessage = "El apellido es requerido")]
        public string Ape_contact { get; set; } = string.Empty;

        public string? Cel_contact { get; set; }

        [EmailAddress(ErrorMessage = "Email no válido")]
        public string? Correo_contact { get; set; }

        [Required(ErrorMessage = "El DPI es requerido")]
        public string Dpi_contact { get; set; } = string.Empty;

        public string? Nit_contact { get; set; }
        public string? Dire_contact { get; set; }
        public string? Zona_contact { get; set; }
        public string? Muni_contact { get; set; }
        public string? Depto_contact { get; set; }
        public int Dcredito_contact { get; set; } = 0;
        public decimal Lcredito_contact { get; set; } = 0.00m;
        public string? Cate_contact { get; set; }
        public string? Subcat_contact { get; set; }
    }
}

