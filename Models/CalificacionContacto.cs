using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CrmContactsApi.Models
{
    [Table("calificaciones_contacto")]
    public class CalificacionContacto
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("contacto_id")]
        public int ContactoId { get; set; }

        [Required]
        [Range(0, 5)]
        [Column("calificacion")]
        public decimal Calificacion { get; set; }

        [Required]
        [StringLength(50)]
        [Column("modulo")]
        public string Modulo { get; set; }

        [Column("comentario")]
        public string? Comentario { get; set; }

        [Column("usuario_calificacion")]
        public int? UsuarioCalificacion { get; set; }

        [Column("fecha_calificacion")]
        public DateTime FechaCalificacion { get; set; } = DateTime.Now;

        // Navegación
        [ForeignKey("ContactoId")]
        public virtual Contacto? Contacto { get; set; }
    }
}