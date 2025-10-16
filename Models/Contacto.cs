using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CrmContactsApi.Models
{
    public class Contacto
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Nombre { get; set; }

        [Required]
        [MaxLength(50)]
        public string Apellido { get; set; }

        [MaxLength(15)]
        public string? Telefono { get; set; }

        [MaxLength(100)]
        public string? Email { get; set; }

        [Required]
        [MaxLength(20)]
        public string Dpi { get; set; }

        [MaxLength(20)]
        public string? Nit { get; set; }

        [MaxLength(255)]
        public string? Direccion { get; set; }

        [MaxLength(10)]
        public string? Zona { get; set; }

        [MaxLength(50)]
        public string? Municipio { get; set; }

        [MaxLength(50)]
        public string? Departamento { get; set; }

        public int DiasCredito { get; set; } = 0;

        [Column(TypeName = "decimal(10,2)")]
        public decimal LimiteCredito { get; set; } = 0.00m;

        [MaxLength(50)]
        public string? Categoria { get; set; }

        [MaxLength(50)]
        public string? Subcategoria { get; set; }

        // Campos de auditoría
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public DateTime FechaActualizacion { get; set; } = DateTime.Now;
        public int? UsuarioCreacion { get; set; }
        public int? UsuarioActualizacion { get; set; }
        public bool Activo { get; set; } = true;

        // Navegación
        public virtual ICollection<ContactoHistorial> Historial { get; set; } = new List<ContactoHistorial>();

        public string? TokenVerificacion { get; set; }
        public DateTime? FechaEnvioVerificacion { get; set; }
        public DateTime? FechaVerificacion { get; set; }
        public bool EmailVerificado { get; set; } = false;
    }
}
