using Microsoft.AspNetCore.Mvc;
using CrmContactsApi.Models;
// Ajusta este using según tu namespace del modelo
// using TuNamespace.Models; // Cambia por tu namespace real

namespace CrmContactsApi.Services
{
    public interface IDocumentoService
    {
        Task<ContactoDocumento> SubirDocumentoAsync(int contactoId, IFormFile archivo, string usuarioSubida, string descripcion = null);
        Task<List<ContactoDocumento>> ObtenerDocumentosPorContactoAsync(int contactoId);
        Task<bool> EliminarDocumentoAsync(int documentoId, string usuario);
        Task<FileResult> DescargarDocumentoAsync(int documentoId);
    }
}