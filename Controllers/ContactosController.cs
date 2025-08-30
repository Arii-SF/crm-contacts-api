using AutoMapper;
using CrmContactsApi.DTOs;
using CrmContactsApi.Models;
using CrmContactsApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrmContactsApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ContactosController : ControllerBase
    {
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