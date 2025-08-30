using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CrmContactsApi.Models
{
    public class ContactoHistorial
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ContactoId { get; set; }

        [Required]
        [MaxLength(50)]
        public string TipoAccion { get; set; } // CREATE, UPDATE, DELETE, VIEW, SEARCH, EXPORT

        [MaxLength(255)]
        public string? DescripcionAccion { get; set; }

        [MaxLength(50)]
        public string? CampoModificado { get; set; }

        public string? ValorAnterior { get; set; }

        public string? ValorNuevo { get; set; }

        [MaxLength(50)]
        public string ModuloOrigen { get; set; } = "contactos";

        [MaxLength(100)]
        public string? EndpointUsado { get; set; }

        [MaxLength(10)]
        public string? MetodoHttp { get; set; }

        public int? UsuarioId { get; set; }

        [MaxLength(100)]
        public string? NombreUsuario { get; set; }

        [MaxLength(45)]
        public string? IpAddress { get; set; }

        public string? UserAgent { get; set; }

        public DateTime FechaAccion { get; set; } = DateTime.Now;

        // Navegación
        [ForeignKey("ContactoId")]
        public virtual Contacto Contacto { get; set; } = null!;
    }
}