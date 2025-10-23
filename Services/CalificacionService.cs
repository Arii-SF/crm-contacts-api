using AutoMapper;
using CrmContactsApi.DTOs;
using CrmContactsApi.Models;
using Microsoft.EntityFrameworkCore;

namespace CrmContactsApi.Services
{
    public class CalificacionService : ICalificacionService
    {
        private readonly CrmDbContext _context;
        private readonly IContactoService _contactoService;
        private readonly IMapper _mapper;

        public CalificacionService(
            CrmDbContext context,
            IContactoService contactoService,
            IMapper mapper)
        {
            _context = context;
            _contactoService = contactoService;
            _mapper = mapper;
        }

        public async Task<CalificacionContacto> CreateCalificacionAsync(int contactoId, CreateCalificacionRequest request)
        {
            // Verificar que el contacto existe
            var contacto = await _contactoService.GetContactoByIdAsync(contactoId);
            if (contacto == null)
                throw new InvalidOperationException($"El contacto con ID {contactoId} no existe");

            var calificacion = new CalificacionContacto
            {
                ContactoId = contactoId,
                Calificacion = request.Calificacion,
                Modulo = request.Modulo,
                Comentario = request.Comentario,
                UsuarioCalificacion = request.UsuarioCalificacion,
                FechaCalificacion = DateTime.Now
            };

            _context.CalificacionesContacto.Add(calificacion);
            await _context.SaveChangesAsync();

            return calificacion;
        }

        public async Task<PerfilContactoDto> GetPerfilContactoAsync(int contactoId)
        {
            var contacto = await _contactoService.GetContactoByIdAsync(contactoId);
            if (contacto == null)
                throw new InvalidOperationException($"El contacto con ID {contactoId} no existe");

            // Obtener todas las calificaciones del contacto
            var calificaciones = await _context.CalificacionesContacto
                .Where(c => c.ContactoId == contactoId)
                .OrderByDescending(c => c.FechaCalificacion)
                .Take(10)
                .ToListAsync();

            // Calcular promedio
            var promedio = calificaciones.Any()
                ? Math.Round(calificaciones.Average(c => c.Calificacion), 2)
                : 0;

            // Estadísticas por módulo
            var estadisticasPorModulo = await _context.CalificacionesContacto
                .Where(c => c.ContactoId == contactoId)
                .GroupBy(c => c.Modulo)
                .Select(g => new EstadisticasModuloDto
                {
                    Modulo = g.Key,
                    CalificacionPromedio = Math.Round(g.Average(c => c.Calificacion), 2),
                    TotalCalificaciones = g.Count()
                })
                .ToListAsync();

            var contactoConCalificacion = _mapper.Map<ContactoConCalificacionDto>(contacto);
            contactoConCalificacion.CalificacionPromedio = promedio;
            contactoConCalificacion.TotalCalificaciones = calificaciones.Count;
            contactoConCalificacion.UltimaCalificacion = calificaciones.FirstOrDefault()?.FechaCalificacion;

            return new PerfilContactoDto
            {
                Contacto = contactoConCalificacion,
                HistorialCalificaciones = _mapper.Map<List<CalificacionDto>>(calificaciones),
                EstadisticasPorModulo = estadisticasPorModulo.ToDictionary(e => e.Modulo, e => e)
            };
        }

        public async Task<IEnumerable<CalificacionDto>> GetHistorialCalificacionesAsync(int contactoId, int limit = 10)
        {
            var calificaciones = await _context.CalificacionesContacto
                .Where(c => c.ContactoId == contactoId)
                .OrderByDescending(c => c.FechaCalificacion)
                .Take(limit)
                .ToListAsync();

            return _mapper.Map<IEnumerable<CalificacionDto>>(calificaciones);
        }

        public async Task<bool> DeleteCalificacionAsync(int id)
        {
            var calificacion = await _context.CalificacionesContacto.FindAsync(id);
            if (calificacion == null)
                return false;

            _context.CalificacionesContacto.Remove(calificacion);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}