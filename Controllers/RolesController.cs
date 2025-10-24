using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CrmContactsApi.Models;

namespace CrmContactsApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class RolesController : ControllerBase
    {
        private readonly CrmDbContext _context;

        public RolesController(CrmDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Obtener todos los roles
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllRoles()
        {
            var roles = await _context.Roles
                .OrderBy(r => r.Level)
                .Select(r => new
                {
                    r.Id,
                    r.Name,
                    r.Level,
                    r.Description,
                    r.CreatedAt,
                    UsersCount = _context.Users.Count(u => u.RoleId == r.Id)
                })
                .ToListAsync();

            return Ok(roles);
        }

        /// <summary>
        /// Obtener rol por ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetRoleById(int id)
        {
            var role = await _context.Roles
                .Where(r => r.Id == id)
                .Select(r => new
                {
                    r.Id,
                    r.Name,
                    r.Level,
                    r.Description,
                    r.CreatedAt,
                    UsersCount = _context.Users.Count(u => u.RoleId == r.Id)
                })
                .FirstOrDefaultAsync();

            if (role == null)
            {
                return NotFound(new { message = "Rol no encontrado" });
            }

            return Ok(role);
        }

        /// <summary>
        /// Crear nuevo rol (solo nivel 4 - Administrador)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequest request)
        {
            var roleLevel = int.Parse(User.FindFirst("RoleLevel")?.Value ?? "0");
            if (roleLevel < 4)
            {
                return Forbid();
            }

            // Validaciones
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest(new { message = "El nombre del rol es requerido" });
            }

            if (await _context.Roles.AnyAsync(r => r.Name == request.Name))
            {
                return BadRequest(new { message = "Ya existe un rol con ese nombre" });
            }

            if (request.Level < 1 || request.Level > 4)
            {
                return BadRequest(new { message = "El nivel debe estar entre 1 y 4" });
            }

            var newRole = new Role
            {
                Name = request.Name,
                Level = request.Level,
                Description = request.Description ?? string.Empty,
                CreatedAt = DateTime.Now
            };

            _context.Roles.Add(newRole);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetRoleById), new { id = newRole.Id }, new
            {
                newRole.Id,
                newRole.Name,
                newRole.Level,
                newRole.Description,
                newRole.CreatedAt
            });
        }

        /// <summary>
        /// Actualizar rol (solo nivel 4 - Administrador)
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRole(int id, [FromBody] UpdateRoleRequest request)
        {
            var roleLevel = int.Parse(User.FindFirst("RoleLevel")?.Value ?? "0");
            if (roleLevel < 4)
            {
                return Forbid();
            }

            var role = await _context.Roles.FindAsync(id);
            if (role == null)
            {
                return NotFound(new { message = "Rol no encontrado" });
            }

            // Actualizar nombre si se proporciona
            if (!string.IsNullOrWhiteSpace(request.Name) && request.Name != role.Name)
            {
                if (await _context.Roles.AnyAsync(r => r.Name == request.Name && r.Id != id))
                {
                    return BadRequest(new { message = "Ya existe un rol con ese nombre" });
                }
                role.Name = request.Name;
            }

            // Actualizar nivel
            if (request.Level.HasValue)
            {
                if (request.Level.Value < 1 || request.Level.Value > 4)
                {
                    return BadRequest(new { message = "El nivel debe estar entre 1 y 4" });
                }
                role.Level = request.Level.Value;
            }

            // Actualizar descripción
            if (request.Description != null)
            {
                role.Description = request.Description;
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Rol actualizado exitosamente" });
        }

        /// <summary>
        /// Eliminar rol (solo nivel 4 - Administrador)
        /// No se puede eliminar si tiene usuarios asignados
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRole(int id)
        {
            var roleLevel = int.Parse(User.FindFirst("RoleLevel")?.Value ?? "0");
            if (roleLevel < 4)
            {
                return Forbid();
            }

            var role = await _context.Roles.FindAsync(id);
            if (role == null)
            {
                return NotFound(new { message = "Rol no encontrado" });
            }

            // Verificar si hay usuarios con este rol
            var usersWithRole = await _context.Users.CountAsync(u => u.RoleId == id);
            if (usersWithRole > 0)
            {
                return BadRequest(new { message = $"No se puede eliminar el rol porque tiene {usersWithRole} usuario(s) asignado(s)" });
            }

            _context.Roles.Remove(role);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Rol eliminado exitosamente" });
        }
    }

    public class CreateRoleRequest
    {
        public string Name { get; set; } = string.Empty;
        public int Level { get; set; }
        public string? Description { get; set; }
    }

    public class UpdateRoleRequest
    {
        public string? Name { get; set; }
        public int? Level { get; set; }
        public string? Description { get; set; }
    }
}