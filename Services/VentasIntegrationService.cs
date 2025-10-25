using CrmContactsApi.DTOs;
using CrmContactsApi.Models;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace CrmContactsApi.Services
{
    public interface IVentasIntegrationService
    {
        Task<VentasClienteResponse> GetVentasByContactoAsync(int contactoId);
        Task<List<TopClienteDto>> GetTopClientesAsync(int limit = 10);
        Task<List<VentaDto>> GetHistorialVentasAsync(int clienteId);
    }

    public class VentasIntegrationService : IVentasIntegrationService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly CrmDbContext _context;
        private string? _cachedToken;
        private DateTime? _tokenExpiry;

        public VentasIntegrationService(
            HttpClient httpClient,
            IConfiguration configuration,
            CrmDbContext context)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _context = context;

            var ventasApiUrl = _configuration["VentasApi:BaseUrl"] ??
                "https://web-service-ventas-api.onrender.com/api";
            _httpClient.BaseAddress = new Uri(ventasApiUrl);
        }

        private async Task<string?> GetTokenAsync()
        {
            // Si tenemos un token válido en caché, usarlo
            if (_cachedToken != null && _tokenExpiry.HasValue && DateTime.Now < _tokenExpiry.Value)
            {
                return _cachedToken;
            }

            try
            {
                var username = _configuration["VentasApi:Username"] ?? "contactos";
                var password = _configuration["VentasApi:Password"] ?? "Contactos123";

                var loginRequest = new
                {
                    username = username,
                    password = password
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(loginRequest),
                    Encoding.UTF8,
                    "application/json"
                );

                var response = await _httpClient.PostAsync("/Auth/login", content);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Error al autenticar con API de Ventas: {response.StatusCode}");
                    return null;
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var loginResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

                if (loginResponse.TryGetProperty("token", out var tokenElement))
                {
                    _cachedToken = tokenElement.GetString();
                    _tokenExpiry = DateTime.Now.AddHours(23); // Token válido por 23 horas
                    return _cachedToken;
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error obteniendo token: {ex.Message}");
                return null;
            }
        }

        private async Task<HttpResponseMessage> MakeAuthenticatedRequestAsync(
            string endpoint,
            HttpMethod method,
            HttpContent? content = null)
        {
            var token = await GetTokenAsync();
            if (token == null)
            {
                throw new Exception("No se pudo obtener token de autenticación");
            }

            var request = new HttpRequestMessage(method, endpoint);

            // CRÍTICO: Agregar "Bearer " antes del token con espacio
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            if (content != null)
            {
                request.Content = content;
            }

            var response = await _httpClient.SendAsync(request);

            // Si el token expiró (401), renovar y reintentar
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                _cachedToken = null;
                _tokenExpiry = null;

                token = await GetTokenAsync();
                if (token != null)
                {
                    request = new HttpRequestMessage(method, endpoint);
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    if (content != null)
                    {
                        request.Content = content;
                    }
                    response = await _httpClient.SendAsync(request);
                }
            }

            return response;
        }

        public async Task<VentasClienteResponse> GetVentasByContactoAsync(int contactoId)
        {
            try
            {
                // Obtener contacto de la base de datos
                var contacto = await _context.Contactos.FindAsync(contactoId);
                if (contacto == null)
                {
                    return new VentasClienteResponse
                    {
                        TotalVentas = 0,
                        CantidadVentas = 0,
                        ValorCliente = 1,
                        CategoriaCliente = "Nuevo"
                    };
                }

                int? clienteId = null;

                // Buscar cliente por DPI
                if (!string.IsNullOrEmpty(contacto.Dpi))
                {
                    var response = await MakeAuthenticatedRequestAsync(
                        $"/Clientes/buscar?dpi={contacto.Dpi}",
                        HttpMethod.Get
                    );

                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        var cliente = JsonSerializer.Deserialize<ClienteVentasDto>(
                            content,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                        );
                        clienteId = cliente?.Id;
                    }
                }

                // Si no encontró por DPI, buscar por Email
                if (clienteId == null && !string.IsNullOrEmpty(contacto.Email))
                {
                    var response = await MakeAuthenticatedRequestAsync(
                        $"/Clientes/buscar?email={contacto.Email}",
                        HttpMethod.Get
                    );

                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        var cliente = JsonSerializer.Deserialize<ClienteVentasDto>(
                            content,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                        );
                        clienteId = cliente?.Id;
                    }
                }

                // Si no se encontró cliente
                if (clienteId == null)
                {
                    return new VentasClienteResponse
                    {
                        TotalVentas = 0,
                        CantidadVentas = 0,
                        ValorCliente = 1,
                        CategoriaCliente = "Nuevo"
                    };
                }

                // Obtener ventas del cliente
                var ventasResponse = await MakeAuthenticatedRequestAsync(
                    $"/Ventas/cliente/{clienteId}",
                    HttpMethod.Get
                );

                if (!ventasResponse.IsSuccessStatusCode)
                {
                    return new VentasClienteResponse
                    {
                        TotalVentas = 0,
                        CantidadVentas = 0,
                        ValorCliente = 1,
                        CategoriaCliente = "Nuevo",
                        ClienteId = clienteId
                    };
                }

                var ventasContent = await ventasResponse.Content.ReadAsStringAsync();
                var ventas = JsonSerializer.Deserialize<List<VentaDto>>(
                    ventasContent,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                ) ?? new List<VentaDto>();

                // Calcular totales
                var totalVentas = ventas.Sum(v => v.Total);
                var cantidadVentas = ventas.Count;
                var ultimaVenta = ventas.Any() ? ventas.Max(v => v.Fecha) : (DateTime?)null;

                // Calcular valoración
                int valorCliente = 1;
                string categoriaCliente = "Nuevo";

                if (totalVentas >= 10000)
                {
                    valorCliente = 5;
                    categoriaCliente = "VIP";
                }
                else if (totalVentas >= 5000)
                {
                    valorCliente = 4;
                    categoriaCliente = "Premium";
                }
                else if (totalVentas >= 2000)
                {
                    valorCliente = 3;
                    categoriaCliente = "Frecuente";
                }
                else if (totalVentas >= 500)
                {
                    valorCliente = 2;
                    categoriaCliente = "Regular";
                }

                return new VentasClienteResponse
                {
                    TotalVentas = totalVentas,
                    CantidadVentas = cantidadVentas,
                    UltimaVenta = ultimaVenta,
                    ValorCliente = valorCliente,
                    CategoriaCliente = categoriaCliente,
                    ClienteId = clienteId
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error obteniendo ventas: {ex.Message}");
                return new VentasClienteResponse
                {
                    TotalVentas = 0,
                    CantidadVentas = 0,
                    ValorCliente = 1,
                    CategoriaCliente = "Nuevo"
                };
            }
        }

        public async Task<List<TopClienteDto>> GetTopClientesAsync(int limit = 10)
        {
            try
            {
                var response = await MakeAuthenticatedRequestAsync(
                    $"/Ventas/top-clientes?limit={limit}",
                    HttpMethod.Get
                );

                if (!response.IsSuccessStatusCode)
                {
                    return new List<TopClienteDto>();
                }

                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<TopClienteDto>>(
                    content,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                ) ?? new List<TopClienteDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error obteniendo top clientes: {ex.Message}");
                return new List<TopClienteDto>();
            }
        }

        public async Task<List<VentaDto>> GetHistorialVentasAsync(int clienteId)
        {
            try
            {
                var response = await MakeAuthenticatedRequestAsync(
                    $"/Ventas/cliente/{clienteId}",
                    HttpMethod.Get
                );

                if (!response.IsSuccessStatusCode)
                {
                    return new List<VentaDto>();
                }

                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<VentaDto>>(
                    content,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                ) ?? new List<VentaDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error obteniendo historial: {ex.Message}");
                return new List<VentaDto>();
            }
        }
    }
}