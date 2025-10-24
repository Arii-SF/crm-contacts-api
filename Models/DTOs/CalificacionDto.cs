using System.ComponentModel.DataAnnotations;

namespace CrmContactsApi.DTOs
{
    public class CalificacionDto
    {
        public int Id { get; set; }
        public int ContactoId { get; set; }
        public decimal Calificacion { get; set; }
        public string Modulo { get; set; }
        public string? Comentario { get; set; }
        public int? UsuarioCalificacion { get; set; }
        public DateTime FechaCalificacion { get; set; }
    }

    public class CreateCalificacionRequest
    {
        [Required(ErrorMessage = "La calificación es requerida")]
        [Range(0, 5, ErrorMessage = "La calificación debe estar entre 0 y 5")]
        public decimal Calificacion { get; set; }

        [Required(ErrorMessage = "El módulo es requerido")]
        [StringLength(50)]
        public string Modulo { get; set; }

        [StringLength(1000)]
        public string? Comentario { get; set; }

        public int? UsuarioCalificacion { get; set; }
    }

    public class ContactoConCalificacionDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Apellido { get; set; } = string.Empty;
        public string Dpi { get; set; } = string.Empty;
        public string? Nit { get; set; }
        public string? Telefono { get; set; }
        public string? Email { get; set; }
        public string? Direccion { get; set; }
        public string? Zona { get; set; }
        public string? Municipio { get; set; }
        public string? Departamento { get; set; }
        public string? Categoria { get; set; }
        public string? Subcategoria { get; set; }
        public int DiasCredito { get; set; }
        public decimal LimiteCredito { get; set; }
        public bool Activo { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime FechaActualizacion { get; set; }

        // Campos de usuario (IDs)
        public int? UsuarioCreacion { get; set; }
        public int? UsuarioActualizacion { get; set; }

        // Campos de usuario (Nombres) - AGREGAR ESTOS
        public string? NombreUsuarioCreacion { get; set; }
        public string? NombreUsuarioActualizacion { get; set; }

        // Campos de calificación
        public double CalificacionPromedio { get; set; }
        public int TotalCalificaciones { get; set; }
        public DateTime? UltimaCalificacion { get; set; }
    }

    public class PerfilContactoDto
    {
        public ContactoConCalificacionDto Contacto { get; set; }
        public List<CalificacionDto> HistorialCalificaciones { get; set; }
        public Dictionary<string, EstadisticasModuloDto> EstadisticasPorModulo { get; set; }
    }

    public class EstadisticasModuloDto
    {
        public string Modulo { get; set; }
        public decimal CalificacionPromedio { get; set; }
        public int TotalCalificaciones { get; set; }
    }
}