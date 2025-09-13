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

        public ContactosController(IContactoService contactoService, IMapper mapper)
        {
            _contactoService = contactoService;
            _mapper = mapper;
        }

        [HttpGet]
        [Authorize(Roles = "Gerente de Ventas,Administrador,Vendedor")]
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
        [Authorize(Roles = "Gerente de Ventas,Administrador,Vendedor")]
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
        [Authorize(Roles = "Gerente de Ventas,Administrador,Vendedor")]
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
        [Authorize(Roles = "Gerente de Ventas,Administrador")]
        public async Task<ActionResult<ContactoDto>> CreateContacto(CreateContactoRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var contacto = _mapper.Map<Contacto>(request);
                var createdContacto = await _contactoService.CreateContactoAsync(contacto);
                var contactoDto = _mapper.Map<ContactoDto>(createdContacto);

                return CreatedAtAction(nameof(GetContacto), new { id = contactoDto.Id }, contactoDto);
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
        [HttpPost("upload-excel")]
        [Authorize(Roles = "Gerente de Ventas,Administrador")]
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
        [Authorize(Roles = "Gerente de Ventas,Administrador")]
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
        [Authorize(Roles = "Gerente de Ventas,Administrador")]
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
        [Authorize(Roles = "Gerente de Ventas,Administrador")]
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
        [Authorize(Roles = "Gerente de Ventas,Administrador")]
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

       
    }

}
