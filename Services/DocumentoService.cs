using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using System.Data;
// Ajusta este using según tu namespace del modelo
// using TuNamespace.Models; // Cambia por tu namespace real

namespace CrmContactsApi.Services
{
    public class DocumentoService : IDocumentoService
    {
        private readonly string _rutaBase;
        private readonly string _connectionString;
        private readonly IConfiguration _configuration;

        public DocumentoService(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? throw new ArgumentNullException("Connection string not found");
            _rutaBase = Path.Combine(Directory.GetCurrentDirectory(), "uploads", "documentos");

            // Crear directorio si no existe
            if (!Directory.Exists(_rutaBase))
                Directory.CreateDirectory(_rutaBase);
        }

        public async Task<ContactoDocumento> SubirDocumentoAsync(int contactoId, IFormFile archivo, string usuarioSubida, string descripcion = null)
        {
            if (archivo == null || archivo.Length == 0)
                throw new ArgumentException("El archivo es requerido");

            // Generar nombre único para el archivo
            var extension = Path.GetExtension(archivo.FileName);
            var nombreArchivo = $"{Guid.NewGuid()}{extension}";
            var rutaCompleta = Path.Combine(_rutaBase, nombreArchivo);

            // Guardar archivo físicamente
            using (var stream = new FileStream(rutaCompleta, FileMode.Create))
            {
                await archivo.CopyToAsync(stream);
            }

            // Crear objeto documento usando tu modelo ContactoDocumento
            var documento = new ContactoDocumento
            {
                ContactoId = contactoId,
                NombreArchivo = nombreArchivo,
                NombreOriginal = archivo.FileName,
                RutaArchivo = rutaCompleta,
                TipoArchivo = archivo.ContentType ?? "application/octet-stream",
                TamañoArchivo = archivo.Length,
                UsuarioSubida = usuarioSubida,
                FechaSubida = DateTime.Now,
                Descripcion = descripcion
            };

            // Insertar en base de datos
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            var sql = @"INSERT INTO DocumentosContacto 
                       (ContactoId, NombreArchivo, NombreOriginal, RutaArchivo, TipoArchivo, 
                        TamañoArchivo, UsuarioSubida, FechaSubida, Descripcion) 
                       VALUES (@ContactoId, @NombreArchivo, @NombreOriginal, @RutaArchivo, 
                               @TipoArchivo, @TamañoArchivo, @UsuarioSubida, @FechaSubida, @Descripcion);
                       SELECT LAST_INSERT_ID();";

            using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@ContactoId", documento.ContactoId);
            command.Parameters.AddWithValue("@NombreArchivo", documento.NombreArchivo);
            command.Parameters.AddWithValue("@NombreOriginal", documento.NombreOriginal);
            command.Parameters.AddWithValue("@RutaArchivo", documento.RutaArchivo);
            command.Parameters.AddWithValue("@TipoArchivo", documento.TipoArchivo);
            command.Parameters.AddWithValue("@TamañoArchivo", documento.TamañoArchivo);
            command.Parameters.AddWithValue("@UsuarioSubida", documento.UsuarioSubida);
            command.Parameters.AddWithValue("@FechaSubida", documento.FechaSubida);
            command.Parameters.AddWithValue("@Descripcion", descripcion ?? (object)DBNull.Value);

            var result = await command.ExecuteScalarAsync();
            documento.Id = Convert.ToInt32(result);

            return documento;
        }

        public async Task<List<ContactoDocumento>> ObtenerDocumentosPorContactoAsync(int contactoId)
        {
            var documentos = new List<ContactoDocumento>();

            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            var sql = "SELECT * FROM DocumentosContacto WHERE ContactoId = @ContactoId ORDER BY FechaSubida DESC";
            using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@ContactoId", contactoId);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                documentos.Add(new ContactoDocumento
                {
                    Id = reader.GetInt32("Id"),
                    ContactoId = reader.GetInt32("ContactoId"),
                    NombreArchivo = reader.GetString("NombreArchivo"),
                    NombreOriginal = reader.GetString("NombreOriginal"),
                    RutaArchivo = reader.GetString("RutaArchivo"),
                    TipoArchivo = reader.GetString("TipoArchivo"),
                    TamañoArchivo = reader.GetInt64("TamañoArchivo"),
                    UsuarioSubida = reader.GetString("UsuarioSubida"),
                    FechaSubida = reader.GetDateTime("FechaSubida"),
                    Descripcion = reader.IsDBNull("Descripcion") ? null : reader.GetString("Descripcion")
                });
            }

            return documentos;
        }

        public async Task<bool> EliminarDocumentoAsync(int documentoId, string usuario)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            // Primero obtener la información del documento
            var selectSql = "SELECT * FROM DocumentosContacto WHERE Id = @Id";
            using var selectCommand = new MySqlCommand(selectSql, connection);
            selectCommand.Parameters.AddWithValue("@Id", documentoId);

            ContactoDocumento? documento = null;
            using var reader = await selectCommand.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                documento = new ContactoDocumento
                {
                    Id = reader.GetInt32("Id"),
                    ContactoId = reader.GetInt32("ContactoId"),
                    NombreArchivo = reader.GetString("NombreArchivo"),
                    NombreOriginal = reader.GetString("NombreOriginal"),
                    RutaArchivo = reader.GetString("RutaArchivo"),
                    TipoArchivo = reader.GetString("TipoArchivo"),
                    TamañoArchivo = reader.GetInt64("TamañoArchivo"),
                    UsuarioSubida = reader.GetString("UsuarioSubida"),
                    FechaSubida = reader.GetDateTime("FechaSubida"),
                    Descripcion = reader.IsDBNull("Descripcion") ? null : reader.GetString("Descripcion")
                };
            }
            reader.Close();

            if (documento == null)
                return false;

            // Eliminar archivo físico si existe
            if (File.Exists(documento.RutaArchivo))
            {
                File.Delete(documento.RutaArchivo);
            }

            // Eliminar registro de base de datos
            var deleteSql = "DELETE FROM DocumentosContacto WHERE Id = @Id";
            using var deleteCommand = new MySqlCommand(deleteSql, connection);
            deleteCommand.Parameters.AddWithValue("@Id", documentoId);

            var filasAfectadas = await deleteCommand.ExecuteNonQueryAsync();

            return filasAfectadas > 0;
        }

        public async Task<FileResult> DescargarDocumentoAsync(int documentoId)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            var sql = "SELECT * FROM DocumentosContacto WHERE Id = @Id";
            using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", documentoId);

            using var reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                throw new FileNotFoundException("Documento no encontrado");

            var documento = new ContactoDocumento
            {
                Id = reader.GetInt32("Id"),
                NombreOriginal = reader.GetString("NombreOriginal"),
                RutaArchivo = reader.GetString("RutaArchivo"),
                TipoArchivo = reader.GetString("TipoArchivo")
            };

            reader.Close();

            if (!File.Exists(documento.RutaArchivo))
                throw new FileNotFoundException("Archivo físico no encontrado");

            var bytes = await File.ReadAllBytesAsync(documento.RutaArchivo);

            return new FileContentResult(bytes, documento.TipoArchivo)
            {
                FileDownloadName = documento.NombreOriginal
            };
        }
    }
}