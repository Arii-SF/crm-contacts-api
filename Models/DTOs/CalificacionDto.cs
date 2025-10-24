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

    public class ContactoConCalificacionDto : ContactoDto
    {
        public decimal CalificacionPromedio { get; set; }
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