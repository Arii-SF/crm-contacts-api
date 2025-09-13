using Microsoft.AspNetCore.Mvc;
using CrmContactsApi.Services;

[ApiController]
[Route("api/[controller]")]
public class DocumentosController : ControllerBase
{
    private readonly IDocumentoService _documentoService;

    public DocumentosController(IDocumentoService documentoService)
    {
        _documentoService = documentoService;
    }

    [HttpPost("subir/{contactoId}")]
    public async Task<IActionResult> SubirDocumento(
        int contactoId,
        IFormFile archivo,
        [FromForm] string usuarioSubida,
        [FromForm] string descripcion = null)
    {
        try
        {
            if (archivo == null || archivo.Length == 0)
                return BadRequest("Debe seleccionar un archivo");

            // Validar tamaño (ejemplo: 10MB máximo)
            if (archivo.Length > 10 * 1024 * 1024)
                return BadRequest("El archivo es demasiado grande (máximo 10MB)");

            // Validar tipos de archivo permitidos
            var tiposPermitidos = new[] { ".pdf", ".doc", ".docx", ".jpg", ".jpeg", ".png", ".txt" };
            var extension = Path.GetExtension(archivo.FileName).ToLower();

            if (!tiposPermitidos.Contains(extension))
                return BadRequest("Tipo de archivo no permitido");

            var documento = await _documentoService.SubirDocumentoAsync(
                contactoId, archivo, usuarioSubida, descripcion);

            return Ok(new
            {
                mensaje = "Documento subido exitosamente",
                documento = new
                {
                    documento.Id,
                    documento.NombreOriginal,
                    documento.TipoArchivo,
                    documento.TamañoArchivo,
                    documento.FechaSubida,
                    documento.UsuarioSubida,
                    documento.Descripcion
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error interno: {ex.Message}");
        }
    }

    [HttpGet("contacto/{contactoId}")]
    public async Task<IActionResult> ObtenerDocumentosPorContacto(int contactoId)
    {
        try
        {
            var documentos = await _documentoService.ObtenerDocumentosPorContactoAsync(contactoId);
            return Ok(documentos);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error interno: {ex.Message}");
        }
    }

    [HttpDelete("{documentoId}")]
    public async Task<IActionResult> EliminarDocumento(int documentoId, [FromQuery] string usuario)
    {
        try
        {
            var resultado = await _documentoService.EliminarDocumentoAsync(documentoId, usuario);

            if (resultado)
                return Ok(new { mensaje = "Documento eliminado exitosamente" });
            else
                return NotFound("Documento no encontrado");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error interno: {ex.Message}");
        }
    }

    [HttpGet("descargar/{documentoId}")]
    public async Task<IActionResult> DescargarDocumento(int documentoId)
    {
        try
        {
            // Implementar lógica para descargar archivo
            // Retornar File() con el contenido del archivo
            return Ok();
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error interno: {ex.Message}");
        }
    }

}