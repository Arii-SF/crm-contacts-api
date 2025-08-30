using CrmContactsApi.Models;

namespace CrmContactsApi.Services
{
    public interface IContactoService
    {
        Task<IEnumerable<Contacto>> GetAllContactosAsync(bool incluirInactivos = false);
        Task<Contacto?> GetContactoByIdAsync(int id);
        Task<Contacto?> GetContactoByDpiAsync(string dpi);
        Task<Contacto> CreateContactoAsync(Contacto contacto);
        Task<Contacto> UpdateContactoAsync(Contacto contacto);
        Task<bool> DeleteContactoAsync(int id);
        Task<bool> ExistsByDpiAsync(string dpi, int? excludeId = null);
        Task<IEnumerable<Contacto>> GetContactosByCategoriaAsync(string categoria);
        Task<IEnumerable<Contacto>> GetContactosByMunicipioAsync(string municipio);
        Task<IEnumerable<Contacto>> SearchContactosAsync(string searchTerm);
    }
}