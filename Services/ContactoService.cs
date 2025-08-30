using Microsoft.EntityFrameworkCore;
using CrmContactsApi.Models;

namespace CrmContactsApi.Services
{
    public class ContactoService : IContactoService
    {
        private readonly CrmDbContext _context;

        public ContactoService(CrmDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Contacto>> GetAllContactosAsync(bool incluirInactivos = false)
        {
            var query = _context.Contactos.AsQueryable();

            if (!incluirInactivos)
            {
                query = query.Where(c => c.Activo);
            }

            return await query.OrderBy(c => c.Nombre).ThenBy(c => c.Apellido).ToListAsync();
        }

        public async Task<Contacto?> GetContactoByIdAsync(int id)
        {
            return await _context.Contactos
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<Contacto?> GetContactoByDpiAsync(string dpi)
        {
            return await _context.Contactos
                .FirstOrDefaultAsync(c => c.Dpi == dpi && c.Activo);
        }

        public async Task<Contacto> CreateContactoAsync(Contacto contacto)
        {
            // Verificar que el DPI no exista
            if (await ExistsByDpiAsync(contacto.Dpi))
            {
                throw new InvalidOperationException($"Ya existe un contacto con el DPI {contacto.Dpi}");
            }

            contacto.FechaCreacion = DateTime.Now;
            contacto.FechaActualizacion = DateTime.Now;

            _context.Contactos.Add(contacto);
            await _context.SaveChangesAsync();
            return contacto;
        }

        public async Task<Contacto> UpdateContactoAsync(Contacto contacto)
        {
            // Verificar que el DPI no exista en otro contacto
            if (await ExistsByDpiAsync(contacto.Dpi, contacto.Id))
            {
                throw new InvalidOperationException($"Ya existe otro contacto con el DPI {contacto.Dpi}");
            }

            contacto.FechaActualizacion = DateTime.Now;
            _context.Contactos.Update(contacto);
            await _context.SaveChangesAsync();
            return contacto;
        }

        public async Task<bool> DeleteContactoAsync(int id)
        {
            var contacto = await _context.Contactos.FindAsync(id);
            if (contacto == null)
            {
                return false;
            }

            // Soft delete - marcar como inactivo
            contacto.Activo = false;
            contacto.FechaActualizacion = DateTime.Now;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsByDpiAsync(string dpi, int? excludeId = null)
        {
            var query = _context.Contactos.Where(c => c.Dpi == dpi);

            if (excludeId.HasValue)
            {
                query = query.Where(c => c.Id != excludeId.Value);
            }

            return await query.AnyAsync();
        }

        public async Task<IEnumerable<Contacto>> GetContactosByCategoriaAsync(string categoria)
        {
            return await _context.Contactos
                .Where(c => c.Categoria == categoria && c.Activo)
                .OrderBy(c => c.Nombre)
                .ThenBy(c => c.Apellido)
                .ToListAsync();
        }

        public async Task<IEnumerable<Contacto>> GetContactosByMunicipioAsync(string municipio)
        {
            return await _context.Contactos
                .Where(c => c.Municipio == municipio && c.Activo)
                .OrderBy(c => c.Nombre)
                .ThenBy(c => c.Apellido)
                .ToListAsync();
        }

        public async Task<IEnumerable<Contacto>> SearchContactosAsync(string searchTerm)
        {
            var term = searchTerm.ToLower();

            return await _context.Contactos
                .Where(c => c.Activo && (
                    c.Nombre.ToLower().Contains(term) ||
                    c.Apellido.ToLower().Contains(term) ||
                    c.Email != null && c.Email.ToLower().Contains(term) ||
                    c.Telefono != null && c.Telefono.Contains(term) ||
                    c.Dpi.Contains(term) ||
                    c.Nit != null && c.Nit.Contains(term)
                ))
                .OrderBy(c => c.Nombre)
                .ThenBy(c => c.Apellido)
                .ToListAsync();
        }
    }
}