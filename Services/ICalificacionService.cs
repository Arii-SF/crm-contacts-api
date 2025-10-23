using CrmContactsApi.DTOs;
using CrmContactsApi.Models;

namespace CrmContactsApi.Services
{
    public interface ICalificacionService
    {
        Task<CalificacionContacto> CreateCalificacionAsync(int contactoId, CreateCalificacionRequest request);
        Task<PerfilContactoDto> GetPerfilContactoAsync(int contactoId);
        Task<IEnumerable<CalificacionDto>> GetHistorialCalificacionesAsync(int contactoId, int limit = 10);
        Task<bool> DeleteCalificacionAsync(int id);
    }
}