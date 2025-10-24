using AutoMapper;
using CrmContactsApi.DTOs;
using CrmContactsApi.Models;
using CrmContactsApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ClosedXML.Excel;
using System.ComponentModel.DataAnnotations;


namespace CrmContactsApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ContactosController : ControllerBase
    {
        private readonly ILogger<ContactosController> _logger;
        private readonly IContactoService _contactoService;
        private readonly IMapper _mapper;
        private readonly IEmailService _emailService;
        private readonly ICalificacionService _calificacionService;


        public ContactosController(
            IContactoService contactoService,
            IMapper mapper,
            IEmailService emailService,
            ICalificacionService calificacionService)
        {
            _contactoService = contactoService;
            _mapper = mapper;
            _emailService = emailService;
            _calificacionService = calificacionService;
        }

        [HttpGet]
        [Authorize(Roles = "Gerente,Administrador,Vendedor")]
        public async Task<ActionResult<IEnumerable<ContactoDto>>> GetContactos([FromQuery] bool incluirInactivos = false)
        {
            try
            {
                var contactos = await _contactoService.GetAllContactosAsync(incluirInactivos);
                var contactosDto = _mapper.Map<IEnumerable<ContactoDto>>(contactos);
                return Ok(contactosDto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno del servidor: {ex.Message}");
            }
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Gerente,Administrador,Vendedor")]
        public async Task<ActionResult<ContactoDto>> GetContacto(int id)
        {
            try
            {
                var contacto = await _contactoService.GetContactoByIdAsync(id);
                if (contacto == null)
                {
                    return NotFound($"Contacto con ID {id} no encontrado");
                }

                var contactoDto = _mapper.Map<ContactoDto>(contacto);
                return Ok(contactoDto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno del servidor: {ex.Message}");
            }
        }

        [HttpGet("dpi/{dpi}")]
        [Authorize(Roles = "Gerente,Administrador,Vendedor")]
        public async Task<ActionResult<ContactoDto>> GetContactoByDpi(string dpi)
        {
            try
            {
                var contacto = await _contactoService.GetContactoByDpiAsync(dpi);
                if (contacto == null)
                {
                    return NotFound($"Contacto con DPI {dpi} no encontrado");
                }

                var contactoDto = _mapper.Map<ContactoDto>(contacto);
                return Ok(contactoDto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno del servidor: {ex.Message}");
            }
        }

        [HttpPost]
        [Authorize(Roles = "Gerente,Administrador")]
        public async Task<ActionResult<ContactoDto>> CreateContacto(CreateContactoRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                if (string.IsNullOrWhiteSpace(request.Email))
                    return BadRequest("El email es requerido para enviar la verificación");

                var contacto = _mapper.Map<Contacto>(request);
                contacto.Activo = false;
                contacto.EmailVerificado = false;
                contacto.TokenVerificacion = Guid.NewGuid().ToString();
                contacto.FechaEnvioVerificacion = DateTime.Now;

                var createdContacto = await _contactoService.CreateContactoAsync(contacto);

                // Enviar email con TODOS los datos
                var emailEnviado = await _emailService.EnviarEmailVerificacionAsync(
                    createdContacto.Email,
                    $"{createdContacto.Nombre} {createdContacto.Apellido}",
                    createdContacto.TokenVerificacion,
                    createdContacto.Id,
                    new
                    {
                        createdContacto.Nombre,
                        createdContacto.Apellido,
                        createdContacto.Dpi,
                        createdContacto.Nit,
                        createdContacto.Telefono,
                        createdContacto.Email,
                        createdContacto.Direccion,
                        createdContacto.Zona,
                        createdContacto.Municipio,
                        createdContacto.Departamento,
                        createdContacto.Categoria,
                        createdContacto.Subcategoria,
                        createdContacto.DiasCredito,
                        createdContacto.LimiteCredito
                    }
                );

                var contactoDto = _mapper.Map<ContactoDto>(createdContacto);

                return CreatedAtAction(nameof(GetContacto), new { id = contactoDto.Id }, new
                {
                    contacto = contactoDto,
                    mensaje = emailEnviado
                        ? "Contacto creado exitosamente. Se ha enviado un email de verificación con todos los datos."
                        : "Contacto creado pero no se pudo enviar el email.",
                    emailEnviado = emailEnviado,
                    requiereVerificacion = true
                });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }


        [HttpPost("upload-excel")]
        [Authorize(Roles = "Gerente,Administrador")]
        public async Task<IActionResult> UploadExcel(IFormFile file)
        {

            if (file == null || file.Length == 0)
                return BadRequest("No se ha proporcionado ningún archivo.");


            if (file == null || file.Length == 0)
                return BadRequest("No se ha proporcionado ningún archivo.");

            if (!IsExcelFile(file))
                return BadRequest("El archivo debe ser un Excel (.xlsx o .xls).");

            try
            {
                var contactos = await ProcessExcelFile(file);
                var result = await ProcessContactosBulk(contactos);

                return Ok(new
                {
                    Message = "Contactos procesados exitosamente",
                    ProcessedCount = result.SuccessCount,
                    ErrorCount = result.ErrorCount,
                    Errors = result.Errors
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error procesando el archivo: {ex.Message}");
            }
        }


        private async Task<List<CreateContactoRequest>> ProcessExcelFile(IFormFile file)
        {
            var contactos = new List<CreateContactoRequest>();

            using (var stream = new MemoryStream())
            {
                await file.CopyToAsync(stream);
                stream.Position = 0; // Importante: resetear posición

                using (var workbook = new XLWorkbook(stream))
                {
                    var worksheet = workbook.Worksheet(1);
                    var lastRow = worksheet.LastRowUsed().RowNumber();

                    // Empezar desde la fila 2 (asumiendo que la fila 1 son headers)
                    for (int row = 2; row <= lastRow; row++)
                    {
                        var contacto = new CreateContactoRequest
                        {
                            Nombre = worksheet.Cell(row, 1).GetString().Trim(),
                            Apellido = worksheet.Cell(row, 2).GetString().Trim(),
                            Telefono = worksheet.Cell(row, 3).GetString().Trim(),
                            Email = worksheet.Cell(row, 4).GetString().Trim(),
                            Dpi = worksheet.Cell(row, 5).GetString().Trim(),
                            Nit = worksheet.Cell(row, 6).GetString().Trim(),
                            Direccion = worksheet.Cell(row, 7).GetString().Trim(),
                            Zona = worksheet.Cell(row, 8).GetString().Trim(),
                            Municipio = worksheet.Cell(row, 9).GetString().Trim(),
                            Departamento = worksheet.Cell(row, 10).GetString().Trim(),
                            DiasCredito = ParseInt(worksheet.Cell(row, 11).GetString()),
                            LimiteCredito = ParseDecimal(worksheet.Cell(row, 12).GetString()),
                            Categoria = worksheet.Cell(row, 13).GetString().Trim(),
                            Subcategoria = worksheet.Cell(row, 14).GetString().Trim(),
                            UsuarioCreacion = GetCurrentUserId()
                        };

                        // Validación básica
                        if (!string.IsNullOrEmpty(contacto.Nombre) &&
                            !string.IsNullOrEmpty(contacto.Apellido) &&
                            !string.IsNullOrEmpty(contacto.Dpi))
                        {
                            contactos.Add(contacto);
                        }
                    }
                }
            }

            return contactos;
        }

        private async Task<BulkCreateResult> ProcessContactosBulk(List<CreateContactoRequest> contactos)
        {
            var result = new BulkCreateResult();

            foreach (var contactoRequest in contactos)
            {
                try
                {
                    // Usar el mismo ModelState validation que tu endpoint existente
                    var validationContext = new ValidationContext(contactoRequest);
                    var validationResults = new List<ValidationResult>();

                    if (!Validator.TryValidateObject(contactoRequest, validationContext, validationResults, true))
                    {
                        var errors = string.Join(", ", validationResults.Select(v => v.ErrorMessage));
                        result.Errors.Add($"Contacto {contactoRequest.Nombre} {contactoRequest.Apellido}: {errors}");
                        result.ErrorCount++;
                        continue;
                    }

                    // Reutilizar tu lógica existente
                    var contacto = _mapper.Map<Contacto>(contactoRequest);
                    var createdContacto = await _contactoService.CreateContactoAsync(contacto);
                    result.SuccessCount++;
                }
                catch (InvalidOperationException ex)
                {
                    result.Errors.Add($"Contacto {contactoRequest.Nombre} {contactoRequest.Apellido}: {ex.Message}");
                    result.ErrorCount++;
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"Error procesando {contactoRequest.Nombre} {contactoRequest.Apellido}: {ex.Message}");
                    result.ErrorCount++;
                }
            }

            return result;
        }

        private bool IsExcelFile(IFormFile file)
        {
            var extension = Path.GetExtension(file.FileName).ToLower();
            return extension == ".xlsx" || extension == ".xls";
        }

        private int ParseInt(string? value)
        {
            return int.TryParse(value, out int result) ? result : 0;
        }

        private decimal ParseDecimal(string? value)
        {
            return decimal.TryParse(value, out decimal result) ? result : 0.00m;
        }

        private int? GetCurrentUserId()
        {
            // Implementa según cómo obtienes el ID del usuario autenticado
            // Por ejemplo, desde el token JWT:
            var userIdClaim = User.FindFirst("userId")?.Value;
            return int.TryParse(userIdClaim, out int userId) ? userId : null;
        }
        [HttpPut("{id}")]
        [Authorize(Roles = "Gerente,Administrador")]
        public async Task<IActionResult> UpdateContacto(int id, UpdateContactoRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var contactoExistente = await _contactoService.GetContactoByIdAsync(id);
                if (contactoExistente == null)
                {
                    return NotFound($"Contacto con ID {id} no encontrado");
                }

                _mapper.Map(request, contactoExistente);
                await _contactoService.UpdateContactoAsync(contactoExistente);

                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno del servidor: {ex.Message}");
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Gerente,Administrador")]
        public async Task<IActionResult> DeleteContacto(int id)
        {
            try
            {
                var contacto = await _contactoService.GetContactoByIdAsync(id);
                if (contacto == null)
                {
                    return NotFound($"Contacto con ID {id} no encontrado");
                }

                await _contactoService.DeleteContactoAsync(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno del servidor: {ex.Message}");
            }
        }

        [HttpPatch("{id}/activar")]
        [Authorize(Roles = "Gerente,Administrador")]
        public async Task<IActionResult> ActivarContacto(int id, [FromBody] int? usuarioActualizacion = null)
        {
            try
            {
                var contacto = await _contactoService.GetContactoByIdAsync(id);
                if (contacto == null)
                {
                    return NotFound($"Contacto con ID {id} no encontrado");
                }

                contacto.Activo = true;
                contacto.UsuarioActualizacion = usuarioActualizacion;
                contacto.FechaActualizacion = DateTime.Now;

                await _contactoService.UpdateContactoAsync(contacto);
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno del servidor: {ex.Message}");
            }
        }

        [HttpPatch("{id}/desactivar")]
        [Authorize(Roles = "Gerente,Administrador")]
        public async Task<IActionResult> DesactivarContacto(int id, [FromBody] int? usuarioActualizacion = null)
        {
            try
            {
                var contacto = await _contactoService.GetContactoByIdAsync(id);
                if (contacto == null)
                {
                    return NotFound($"Contacto con ID {id} no encontrado");
                }

                contacto.Activo = false;
                contacto.UsuarioActualizacion = usuarioActualizacion;
                contacto.FechaActualizacion = DateTime.Now;

                await _contactoService.UpdateContactoAsync(contacto);
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno del servidor: {ex.Message}");
            }
        }

        [HttpGet("verificar/{id}/{token}")]
        [AllowAnonymous]
        public async Task<IActionResult> VerificarContacto(int id, string token)
        {
            try
            {
                var contacto = await _contactoService.GetContactoByIdAsync(id);

                if (contacto == null)
                    return Content(GenerarHtmlError("Contacto no encontrado",
                        "El contacto que intentas verificar no existe en el sistema."), "text/html");

                if (contacto.EmailVerificado)
                    return Content(GenerarHtmlExito("Ya verificado",
                        "Este contacto ya fue verificado anteriormente. Tu cuenta está activa."), "text/html");

                if (contacto.TokenVerificacion != token)
                    return Content(GenerarHtmlError("Token inválido",
                        "El enlace de verificación no es válido. Por favor contacta al administrador."), "text/html");

                if (contacto.FechaEnvioVerificacion.HasValue &&
                    DateTime.Now - contacto.FechaEnvioVerificacion.Value > TimeSpan.FromHours(24))
                {
                    return Content(GenerarHtmlError("Enlace expirado",
                        "El enlace de verificación ha expirado. Por favor contacta al administrador para recibir un nuevo enlace."), "text/html");
                }

                contacto.Activo = true;
                contacto.EmailVerificado = true;
                contacto.FechaVerificacion = DateTime.Now;
                contacto.TokenVerificacion = null;

                await _contactoService.UpdateContactoAsync(contacto);

                return Content(GenerarHtmlExito("¡Verificación Exitosa!",
                    $"Gracias {contacto.Nombre}, tus datos han sido verificados correctamente. Tu contacto está ahora activo en el sistema."), "text/html");
            }
            catch (Exception ex)
            {
                return Content(GenerarHtmlError("Error del servidor",
                    "Ocurrió un error al procesar tu verificación. Por favor intenta nuevamente más tarde."), "text/html");
            }
        }

        [HttpGet("corregir/{id}/{token}")]
        [AllowAnonymous]
        public async Task<IActionResult> ReportarError(int id, string token)
        {
            try
            {
                var contacto = await _contactoService.GetContactoByIdAsync(id);

                if (contacto == null)
                    return Content(GenerarHtmlError("Contacto no encontrado",
                        "El contacto que intentas reportar no existe en el sistema."), "text/html");

                if (contacto.TokenVerificacion != token)
                    return Content(GenerarHtmlError("Token inválido",
                        "El enlace no es válido."), "text/html");

                // Marcar el contacto como que necesita corrección
                contacto.Activo = false;
                contacto.EmailVerificado = false;
                await _contactoService.UpdateContactoAsync(contacto);

                return Content(GenerarHtmlReporteError(contacto.Nombre), "text/html");
            }
            catch (Exception ex)
            {
                return Content(GenerarHtmlError("Error del servidor",
                    "Ocurrió un error. Por favor intenta nuevamente más tarde."), "text/html");
            }
        }

        [HttpGet("{id}/estado-verificacion")]
        [Authorize(Roles = "Gerente,Administrador,Vendedor")]
        public async Task<ActionResult> GetEstadoVerificacion(int id)
        {
            try
            {
                var contacto = await _contactoService.GetContactoByIdAsync(id);
                if (contacto == null)
                    return NotFound();

                return Ok(new
                {
                    id = contacto.Id,
                    nombre = $"{contacto.Nombre} {contacto.Apellido}",
                    email = contacto.Email,
                    emailVerificado = contacto.EmailVerificado,
                    activo = contacto.Activo,
                    fechaEnvioVerificacion = contacto.FechaEnvioVerificacion,
                    fechaVerificacion = contacto.FechaVerificacion
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        [HttpPost("{id}/reenviar-verificacion")]
        [Authorize(Roles = "Gerente,Administrador")]
        public async Task<ActionResult> ReenviarVerificacion(int id)
        {
            try
            {
                var contacto = await _contactoService.GetContactoByIdAsync(id);
                if (contacto == null)
                    return NotFound("Contacto no encontrado");

                if (contacto.EmailVerificado)
                    return BadRequest("Este contacto ya está verificado");

                contacto.TokenVerificacion = Guid.NewGuid().ToString();
                contacto.FechaEnvioVerificacion = DateTime.Now;
                await _contactoService.UpdateContactoAsync(contacto);

                // Reenviar email con todos los datos
                var emailEnviado = await _emailService.EnviarEmailVerificacionAsync(
                    contacto.Email,
                    $"{contacto.Nombre} {contacto.Apellido}",
                    contacto.TokenVerificacion,
                    contacto.Id,
                    new
                    {
                        contacto.Nombre,
                        contacto.Apellido,
                        contacto.Dpi,
                        contacto.Nit,
                        contacto.Telefono,
                        contacto.Email,
                        contacto.Direccion,
                        contacto.Zona,
                        contacto.Municipio,
                        contacto.Departamento,
                        contacto.Categoria,
                        contacto.Subcategoria,
                        contacto.DiasCredito,
                        contacto.LimiteCredito
                    }
                );

                if (emailEnviado)
                    return Ok(new { mensaje = "Email de verificación reenviado exitosamente" });
                else
                    return StatusCode(500, "No se pudo enviar el email");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        [HttpPut("{id}/corregir-datos")]
        [Authorize(Roles = "Gerente,Administrador")]
        public async Task<ActionResult> CorregirYReenviar(int id, UpdateContactoRequest request)
        {
            try
            {
                var contacto = await _contactoService.GetContactoByIdAsync(id);
                if (contacto == null)
                    return NotFound("Contacto no encontrado");

                // Actualizar los datos
                _mapper.Map(request, contacto);

                // Generar nuevo token y resetear verificación
                contacto.Activo = false;
                contacto.EmailVerificado = false;
                contacto.TokenVerificacion = Guid.NewGuid().ToString();
                contacto.FechaEnvioVerificacion = DateTime.Now;
                contacto.FechaActualizacion = DateTime.Now;

                await _contactoService.UpdateContactoAsync(contacto);

                // Reenviar email con los datos corregidos
                var emailEnviado = await _emailService.EnviarEmailVerificacionAsync(
                    contacto.Email,
                    $"{contacto.Nombre} {contacto.Apellido}",
                    contacto.TokenVerificacion,
                    contacto.Id,
                    new
                    {
                        contacto.Nombre,
                        contacto.Apellido,
                        contacto.Dpi,
                        contacto.Nit,
                        contacto.Telefono,
                        contacto.Email,
                        contacto.Direccion,
                        contacto.Zona,
                        contacto.Municipio,
                        contacto.Departamento,
                        contacto.Categoria,
                        contacto.Subcategoria,
                        contacto.DiasCredito,
                        contacto.LimiteCredito
                    }
                );

                if (emailEnviado)
                    return Ok(new { mensaje = "Datos actualizados y email de verificación reenviado exitosamente" });
                else
                    return Ok(new { mensaje = "Datos actualizados pero no se pudo enviar el email" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }
        
        // GET: api/contactos/5/perfil
        [HttpGet("{id}/perfil")]
        [Authorize(Roles = "Gerente,Administrador,Vendedor")]
        public async Task<ActionResult<PerfilContactoDto>> GetPerfilContacto(int id)
        {
            try
            {
                var perfil = await _calificacionService.GetPerfilContactoAsync(id);
                return Ok(perfil);
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

        private string GenerarHtmlExito(string titulo, string mensaje)
        {
            return $@"
<!DOCTYPE html>
<html lang='es'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>{titulo}</title>
    <style>
        body {{ font-family: Arial, sans-serif; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); min-height: 100vh; display: flex; align-items: center; justify-content: center; margin: 0; padding: 20px; }}
        .container {{ background: white; padding: 50px; border-radius: 12px; box-shadow: 0 10px 40px rgba(0,0,0,0.2); text-align: center; max-width: 500px; }}
        .icon {{ width: 80px; height: 80px; background: #10b981; border-radius: 50%; display: flex; align-items: center; justify-content: center; margin: 0 auto 30px; font-size: 40px; }}
        h1 {{ color: #10b981; margin-bottom: 20px; font-size: 28px; }}
        p {{ color: #6b7280; line-height: 1.6; font-size: 16px; }}
        .footer {{ margin-top: 30px; padding-top: 20px; border-top: 1px solid #e5e7eb; color: #9ca3af; font-size: 13px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='icon'>✓</div>
        <h1>{titulo}</h1>
        <p>{mensaje}</p>
        <div class='footer'><p>CRM Contactos © 2025</p></div>
    </div>
</body>
</html>";
        }

        private string GenerarHtmlError(string titulo, string mensaje)
        {
            return $@"
<!DOCTYPE html>
<html lang='es'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>{titulo}</title>
    <style>
        body {{ font-family: Arial, sans-serif; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); min-height: 100vh; display: flex; align-items: center; justify-content: center; margin: 0; padding: 20px; }}
        .container {{ background: white; padding: 50px; border-radius: 12px; box-shadow: 0 10px 40px rgba(0,0,0,0.2); text-align: center; max-width: 500px; }}
        .icon {{ width: 80px; height: 80px; background: #ef4444; border-radius: 50%; display: flex; align-items: center; justify-content: center; margin: 0 auto 30px; font-size: 40px; color: white; }}
        h1 {{ color: #ef4444; margin-bottom: 20px; font-size: 28px; }}
        p {{ color: #6b7280; line-height: 1.6; font-size: 16px; }}
        .footer {{ margin-top: 30px; padding-top: 20px; border-top: 1px solid #e5e7eb; color: #9ca3af; font-size: 13px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='icon'>✕</div>
        <h1>{titulo}</h1>
        <p>{mensaje}</p>
        <div class='footer'><p>CRM Contactos © 2025</p></div>
    </div>
</body>
</html>";
        }

        private string GenerarHtmlReporteError(string nombre)
        {
            return $@"
<!DOCTYPE html>
<html lang='es'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Reporte recibido</title>
    <style>
        body {{ font-family: Arial, sans-serif; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); min-height: 100vh; display: flex; align-items: center; justify-content: center; margin: 0; padding: 20px; }}
        .container {{ background: white; padding: 50px; border-radius: 12px; box-shadow: 0 10px 40px rgba(0,0,0,0.2); text-align: center; max-width: 500px; }}
        .icon {{ width: 80px; height: 80px; background: #f59e0b; border-radius: 50%; display: flex; align-items: center; justify-content: center; margin: 0 auto 30px; font-size: 40px; }}
        h1 {{ color: #f59e0b; margin-bottom: 20px; font-size: 28px; }}
        p {{ color: #6b7280; line-height: 1.6; font-size: 16px; }}
        .footer {{ margin-top: 30px; padding-top: 20px; border-top: 1px solid #e5e7eb; color: #9ca3af; font-size: 13px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='icon'>✏️</div>
        <h1>Reporte Recibido</h1>
        <p>Gracias <strong>{nombre}</strong>,</p>
        <p>Hemos recibido tu reporte de que hay información incorrecta en tu registro.</p>
        <p><strong>Nuestro equipo se pondrá en contacto contigo</strong> en las próximas 24 horas para corregir los datos.</p>
        <div class='footer'><p>CRM Contactos © 2025</p></div>
    </div>
</body>
</html>";
        }
    }
}