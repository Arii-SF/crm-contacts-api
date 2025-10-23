using AutoMapper;
using CrmContactsApi.DTOs;
using CrmContactsApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrmContactsApi.Controllers
{
    [ApiController]
    [Route("api/contactos/{contactoId}/calificaciones")]
    public class CalificacionesController : ControllerBase
    {
        private readonly ICalificacionService _calificacionService;
        private readonly IMapper _mapper;

        public CalificacionesController(
            ICalificacionService calificacionService,
            IMapper mapper)
        {
            _calificacionService = calificacionService;
            _mapper = mapper;
        }

        // POST: api/contactos/5/calificaciones
        [HttpPost]
        [Authorize(Roles = "Gerente de Ventas,Administrador,Vendedor")]
        public async Task<ActionResult<CalificacionDto>> CreateCalificacion(
            int contactoId,
            [FromBody] CreateCalificacionRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var calificacion = await _calificacionService.CreateCalificacionAsync(contactoId, request);
                var calificacionDto = _mapper.Map<CalificacionDto>(calificacion);

                return Ok(new
                {
                    calificacion = calificacionDto,
                    mensaje = "Calificación registrada exitosamente"
                });
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Error interno: {ex.Message}" });
            }
        }

        // GET: api/contactos/5/calificaciones
        [HttpGet]
        [Authorize(Roles = "Gerente de Ventas,Administrador,Vendedor")]
        public async Task<ActionResult<IEnumerable<CalificacionDto>>> GetHistorialCalificaciones(
            int contactoId,
            [FromQuery] int limit = 10)
        {
            try
            {
                var calificaciones = await _calificacionService.GetHistorialCalificacionesAsync(contactoId, limit);
                return Ok(calificaciones);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Error interno: {ex.Message}" });
            }
        }

        // DELETE: api/contactos/5/calificaciones/3
        [HttpDelete("{calificacionId}")]
        [Authorize(Roles = "Gerente de Ventas,Administrador")]
        public async Task<ActionResult> DeleteCalificacion(int contactoId, int calificacionId)
        {
            try
            {
                var eliminado = await _calificacionService.DeleteCalificacionAsync(calificacionId);
                if (!eliminado)
                    return NotFound(new { error = "Calificación no encontrada" });

                return Ok(new { mensaje = "Calificación eliminada exitosamente" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Error interno: {ex.Message}" });
            }
        }
    }
}