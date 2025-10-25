using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CrmContactsApi.Services;

namespace CrmContactsApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class VentasProxyController : ControllerBase
    {
        private readonly IVentasIntegrationService _ventasService;

        public VentasProxyController(IVentasIntegrationService ventasService)
        {
            _ventasService = ventasService;
        }

        /// <summary>
        /// Obtener ventas de un contacto por ID
        /// </summary>
        [HttpGet("contacto/{contactoId}")]
        public async Task<IActionResult> GetVentasContacto(int contactoId)
        {
            try
            {
                var ventas = await _ventasService.GetVentasByContactoAsync(contactoId);
                return Ok(ventas);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Error obteniendo ventas: {ex.Message}" });
            }
        }

        /// <summary>
        /// Obtener top clientes por ventas
        /// </summary>
        [HttpGet("top-clientes")]
        public async Task<IActionResult> GetTopClientes([FromQuery] int limit = 10)
        {
            try
            {
                var topClientes = await _ventasService.GetTopClientesAsync(limit);
                return Ok(topClientes);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Error obteniendo top clientes: {ex.Message}" });
            }
        }

        /// <summary>
        /// Obtener historial de ventas de un cliente
        /// </summary>
        [HttpGet("historial/{clienteId}")]
        public async Task<IActionResult> GetHistorialVentas(int clienteId)
        {
            try
            {
                var historial = await _ventasService.GetHistorialVentasAsync(clienteId);
                return Ok(historial);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Error obteniendo historial: {ex.Message}" });
            }
        }
    }
}