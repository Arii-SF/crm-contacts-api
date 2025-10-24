using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CrmContactsApi.Models;
using CrmContactsApi.Models.DTOs;

namespace CrmContactsApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly CrmDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(CrmDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        /// <summary>
        /// Login - Público
        /// </summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { message = "Usuario y contraseña son requeridos" });
            }

            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Username == request.Username && u.Password == request.Password);

            if (user == null || !user.IsActive)
            {
                return Unauthorized(new { message = "Credenciales inválidas o usuario inactivo" });
            }

            var token = GenerateJwtToken(user);

            return Ok(new
            {
                Token = token,
                User = new
                {
                    user.Id,
                    user.Username,
                    user.Email,
                    user.FirstName,
                    user.LastName,
                    FullName = $"{user.FirstName} {user.LastName}".Trim(),
                    Role = user.Role.Name,
                    RoleLevel = user.Role.Level,
                    RoleId = user.RoleId
                }
            });
        }

        /// <summary>
        /// Registro público - Crea usuario con rol "Usuario" (nivel 1)
        /// </summary>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            // Validaciones
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { message = "Usuario y contraseña son requeridos" });
            }

            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return BadRequest(new { message = "Email es requerido" });
            }

            // Verificar si el usuario ya existe
            if (await _context.Users.AnyAsync(u => u.Username == request.Username))
            {
                return BadRequest(new { message = "El nombre de usuario ya está en uso" });
            }

            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
            {
                return BadRequest(new { message = "El email ya está registrado" });
            }

            // Obtener el rol de "Usuario" (nivel 1)
            var userRole = await _context.Roles.FirstOrDefaultAsync(r => r.Level == 1);
            if (userRole == null)
            {
                return StatusCode(500, new { message = "Error: Rol de usuario no configurado en el sistema" });
            }

            // Crear nuevo usuario
            var newUser = new User
            {
                Username = request.Username,
                Email = request.Email,
                Password = request.Password, // NOTA: En producción, usar hash (BCrypt, etc.)
                FirstName = request.FirstName,
                LastName = request.LastName,
                RoleId = userRole.Id,
                IsActive = true,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            // Generar token para login automático
            var userWithRole = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == newUser.Id);

            var token = GenerateJwtToken(userWithRole!);

            return Ok(new
            {
                message = "Usuario registrado exitosamente",
                Token = token,
                User = new
                {
                    newUser.Id,
                    newUser.Username,
                    newUser.Email,
                    newUser.FirstName,
                    newUser.LastName,
                    FullName = $"{newUser.FirstName} {newUser.LastName}".Trim(),
                    Role = userRole.Name,
                    RoleLevel = userRole.Level,
                    RoleId = newUser.RoleId
                }
            });
        }

        /// <summary>
        /// Obtener usuario actual autenticado
        /// </summary>
        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            var user = await _context.Users
                .Include(u => u.Role)
                .Where(u => u.Id == userId)
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
        /// Cambiar contraseña del usuario actual
        /// </summary>
        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                return NotFound(new { message = "Usuario no encontrado" });
            }

            if (user.Password != request.CurrentPassword)
            {
                return BadRequest(new { message = "Contraseña actual incorrecta" });
            }

            if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 6)
            {
                return BadRequest(new { message = "La nueva contraseña debe tener al menos 6 caracteres" });
            }

            user.Password = request.NewPassword; // NOTA: En producción, usar hash
            user.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Contraseña actualizada exitosamente" });
        }

        private string GenerateJwtToken(User user)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.Name),
                new Claim("RoleLevel", user.Role.Level.ToString()),
                new Claim("FullName", $"{user.FirstName} {user.LastName}".Trim())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                _configuration["Jwt:Key"] ?? "MiClaveSecretaSuperSeguraParaJWT123456789"));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"] ?? "CrmContactsApi",
                audience: _configuration["Jwt:Audience"] ?? "CrmContactsApi",
                claims: claims,
                expires: DateTime.Now.AddHours(24),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }

    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}