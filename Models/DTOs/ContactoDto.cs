using System.ComponentModel.DataAnnotations;

namespace CrmContactsApi.DTOs
{
    public class ContactoDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Apellido { get; set; } = string.Empty;
        public string? Telefono { get; set; }
        public string? Email { get; set; }
        public string Dpi { get; set; } = string.Empty;
        public string? Nit { get; set; }
        public string? Direccion { get; set; }
        public string? Zona { get; set; }
        public string? Municipio { get; set; }
        public string? Departamento { get; set; }
        public int DiasCredito { get; set; }
        public decimal LimiteCredito { get; set; }
        public string? Categoria { get; set; }
        public string? Subcategoria { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime FechaActualizacion { get; set; }
        public int? UsuarioCreacion { get; set; }
        public int? UsuarioActualizacion { get; set; }
        public bool Activo { get; set; }
    }

    public class CreateContactoRequest
    {
        [Required]
        [StringLength(50)]
        public string Nombre { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Apellido { get; set; } = string.Empty;

        [StringLength(15)]
        public string? Telefono { get; set; }

        [StringLength(100)]
        [EmailAddress]
        public string? Email { get; set; }

        [Required]
        [StringLength(20)]
        public string Dpi { get; set; } = string.Empty;

        [StringLength(20)]
        public string? Nit { get; set; }

        [StringLength(255)]
        public string? Direccion { get; set; }

        [StringLength(10)]
        public string? Zona { get; set; }

        [StringLength(50)]
        public string? Municipio { get; set; }

        [StringLength(50)]
        public string? Departamento { get; set; }

        public int DiasCredito { get; set; } = 0;

        public decimal LimiteCredito { get; set; } = 0.00m;

        [StringLength(50)]
        public string? Categoria { get; set; }

        [StringLength(50)]
        public string? Subcategoria { get; set; }

        public int? UsuarioCreacion { get; set; }
    }

    public class UpdateContactoRequest
    {
        [Required]
        [StringLength(50)]
        public string Nombre { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Apellido { get; set; } = string.Empty;

        [StringLength(15)]
        public string? Telefono { get; set; }

        [StringLength(100)]
        [EmailAddress]
        public string? Email { get; set; }

        [Required]
        [StringLength(20)]
        public string Dpi { get; set; } = string.Empty;

        [StringLength(20)]
        public string? Nit { get; set; }

        [StringLength(255)]
        public string? Direccion { get; set; }

        [StringLength(10)]
        public string? Zona { get; set; }

        [StringLength(50)]
        public string? Municipio { get; set; }

        [StringLength(50)]
        public string? Departamento { get; set; }

        public int DiasCredito { get; set; } = 0;

        public decimal LimiteCredito { get; set; } = 0.00m;

        [StringLength(50)]
        public string? Categoria { get; set; }

        [StringLength(50)]
        public string? Subcategoria { get; set; }

        public int? UsuarioActualizacion { get; set; }

        public bool Activo { get; set; } = true;
    }
}