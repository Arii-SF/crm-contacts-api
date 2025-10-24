using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CrmContactsApi.Models;
using CrmContactsApi.Models.DTOs;
using System.Security.Claims;

namespace CrmContactsApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly CrmDbContext _context;

        public UsersController(CrmDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Obtener todos los usuarios (solo nivel 3+)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllUsers([FromQuery] bool includeInactive = false)
        {
            var roleLevel = int.Parse(User.FindFirst("RoleLevel")?.Value ?? "0");
            if (roleLevel < 3)
            {
                return Forbid();
            }

            var query = _context.Users
                .Include(u => u.Role)
                .AsQueryable();

            if (!includeInactive)
            {
                query = query.Where(u => u.IsActive);
            }

            var users = await query
                .Select(u => new UserListResponse
                {
                    Id = u.Id,
                    Username = u.Username,
                    Email = u.Email,
                    FullName = $"{u.FirstName} {u.LastName}".Trim(),
                    RoleName = u.Role.Name,
                    IsActive = u.IsActive
                })
                .OrderBy(u => u.Username)
                .ToListAsync();

            return Ok(users);
        }

        /// <summary>
        /// Obtener usuario por ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(int id)
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .Where(u => u.Id == id)
                .Select(u => new UserResponse
                {
                    Id = u.Id,
                    Username = u.Username,
                    Email = u.Email,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    RoleId = u.RoleId,
                    RoleName = u.Role.Name,
                    RoleLevel = u.Role.Level,
                    IsActive = u.IsActive,
                    CreatedAt = u.CreatedAt,
                    UpdatedAt = u.UpdatedAt
                })
                .FirstOrDefaultAsync();

            if (user == null)
            {
                return NotFound(new { message = "Usuario no encontrado" });
            }

            return Ok(user);
        }

        /// <summary>
        /// Crear nuevo usuario (solo nivel 3+)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
        {
            var roleLevel = int.Parse(User.FindFirst("RoleLevel")?.Value ?? "0");
            if (roleLevel < 3)
            {
                return Forbid();
            }

            // Validaciones
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { message = "Usuario y contraseña son requeridos" });
            }

            if (await _context.Users.AnyAsync(u => u.Username == request.Username))
            {
                return BadRequest(new { message = "El nombre de usuario ya existe" });
            }

            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
            {
                return BadRequest(new { message = "El email ya está registrado" });
            }

            // Verificar que el rol existe
            var role = await _context.Roles.FindAsync(request.RoleId);
            if (role == null)
            {
                return BadRequest(new { message = "Rol no válido" });
            }

            // Solo admin (nivel 4) puede crear otros admins
            if (role.Level >= 4 && roleLevel < 4)
            {
                return Forbid();
            }

            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            var newUser = new User
            {
                Username = request.Username,
                Email = request.Email,
                Password = request.Password, // NOTA: En producción usar hash
                FirstName = request.FirstName,
                LastName = request.LastName,
                RoleId = request.RoleId,
                IsActive = true,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            var userResponse = new UserResponse
            {
                Id = newUser.Id,
                Username = newUser.Username,
                Email = newUser.Email,
                FirstName = newUser.FirstName,
                LastName = newUser.LastName,
                RoleId = newUser.RoleId,
                RoleName = role.Name,
                RoleLevel = role.Level,
                IsActive = newUser.IsActive,
                CreatedAt = newUser.CreatedAt,
                UpdatedAt = newUser.UpdatedAt
            };

            return CreatedAtAction(nameof(GetUserById), new { id = newUser.Id }, userResponse);
        }

        /// <summary>
        /// Actualizar usuario (solo nivel 3+)
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserRequest request)
        {
            var roleLevel = int.Parse(User.FindFirst("RoleLevel")?.Value ?? "0");
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            // Solo puede editar si es nivel 3+ o es el mismo usuario
            if (roleLevel < 3 && currentUserId != id)
            {
                return Forbid();
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound(new { message = "Usuario no encontrado" });
            }

            // Actualizar email si se proporciona
            if (!string.IsNullOrWhiteSpace(request.Email) && request.Email != user.Email)
            {
                if (await _context.Users.AnyAsync(u => u.Email == request.Email && u.Id != id))
                {
                    return BadRequest(new { message = "El email ya está en uso" });
                }
                user.Email = request.Email;
            }

            // Actualizar nombres
            if (!string.IsNullOrWhiteSpace(request.FirstName))
            {
                user.FirstName = request.FirstName;
            }

            if (!string.IsNullOrWhiteSpace(request.LastName))
            {
                user.LastName = request.LastName;
            }

            // Solo nivel 3+ puede cambiar roles y estado
            if (roleLevel >= 3)
            {
                if (request.RoleId.HasValue)
                {
                    var newRole = await _context.Roles.FindAsync(request.RoleId.Value);
                    if (newRole == null)
                    {
                        return BadRequest(new { message = "Rol no válido" });
                    }

                    // Solo admin puede asignar rol admin
                    if (newRole.Level >= 4 && roleLevel < 4)
                    {
                        return Forbid();
                    }

                    user.RoleId = request.RoleId.Value;
                }

                if (request.IsActive.HasValue)
                {
                    // No puede desactivarse a sí mismo
                    if (currentUserId == id && !request.IsActive.Value)
                    {
                        return BadRequest(new { message = "No puedes desactivar tu propia cuenta" });
                    }
                    user.IsActive = request.IsActive.Value;
                }
            }

            user.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Usuario actualizado exitosamente" });
        }

        /// <summary>
        /// Cambiar rol de usuario (solo nivel 3+)
        /// </summary>
        [HttpPatch("{id}/role")]
        public async Task<IActionResult> ChangeUserRole(int id, [FromBody] ChangeRoleRequest request)
        {
            var roleLevel = int.Parse(User.FindFirst("RoleLevel")?.Value ?? "0");
            if (roleLevel < 3)
            {
                return Forbid();
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound(new { message = "Usuario no encontrado" });
            }

            var newRole = await _context.Roles.FindAsync(request.RoleId);
            if (newRole == null)
            {
                return BadRequest(new { message = "Rol no válido" });
            }

            // Solo admin puede asignar rol admin
            if (newRole.Level >= 4 && roleLevel < 4)
            {
                return Forbid();
            }

            user.RoleId = request.RoleId;
            user.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Rol actualizado exitosamente" });
        }

        /// <summary>
        /// Desactivar usuario (solo nivel 3+)
        /// </summary>
        [HttpPatch("{id}/deactivate")]
        public async Task<IActionResult> DeactivateUser(int id)
        {
            var roleLevel = int.Parse(User.FindFirst("RoleLevel")?.Value ?? "0");
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            if (roleLevel < 3)
            {
                return Forbid();
            }

            if (currentUserId == id)
            {
                return BadRequest(new { message = "No puedes desactivar tu propia cuenta" });
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound(new { message = "Usuario no encontrado" });
            }

            user.IsActive = false;
            user.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Usuario desactivado exitosamente" });
        }

        /// <summary>
        /// Activar usuario (solo nivel 3+)
        /// </summary>
        [HttpPatch("{id}/activate")]
        public async Task<IActionResult> ActivateUser(int id)
        {
            var roleLevel = int.Parse(User.FindFirst("RoleLevel")?.Value ?? "0");
            if (roleLevel < 3)
            {
                return Forbid();
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound(new { message = "Usuario no encontrado" });
            }

            user.IsActive = true;
            user.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Usuario activado exitosamente" });
        }

        /// <summary>
        /// Eliminar usuario permanentemente (solo nivel 4)
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var roleLevel = int.Parse(User.FindFirst("RoleLevel")?.Value ?? "0");
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            if (roleLevel < 4)
            {
                return Forbid();
            }

            if (currentUserId == id)
            {
                return BadRequest(new { message = "No puedes eliminar tu propia cuenta" });
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound(new { message = "Usuario no encontrado" });
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Usuario eliminado exitosamente" });
        }
    }

    public class ChangeRoleRequest
    {
        public int RoleId { get; set; }
    }
}