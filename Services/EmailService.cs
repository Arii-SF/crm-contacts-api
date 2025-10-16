using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CrmContactsApi.Services
{
    public interface IEmailService
    {
        Task<bool> EnviarEmailVerificacionAsync(string emailDestino, string nombreCompleto, string token, int contactoId, object datosContacto);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;
        private readonly HttpClient _httpClient;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger, HttpClient httpClient)
        {
            _configuration = configuration;
            _logger = logger;
            _httpClient = httpClient;

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_configuration["Resend:ApiKey"]}");
        }

        public async Task<bool> EnviarEmailVerificacionAsync(string emailDestino, string nombreCompleto, string token, int contactoId, object datosContacto)
        {
            try
            {
                var appUrl = _configuration["AppUrl"];
                var urlVerificacion = $"{appUrl}/api/Contactos/verificar/{contactoId}/{token}";
                var urlCorreccion = $"{appUrl}/api/Contactos/corregir/{contactoId}/{token}";

                var htmlContent = GenerarHtmlVerificacionConDatos(nombreCompleto, urlVerificacion, urlCorreccion, datosContacto);

                var payload = new
                {
                    from = "CRM Contactos <onboarding@resend.dev>",
                    to = new[] { emailDestino },
                    subject = "Verificación de datos - CRM Contactos",
                    html = htmlContent
                };

                var jsonPayload = JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                _logger.LogInformation($"Enviando email de verificación a: {emailDestino}");

                var response = await _httpClient.PostAsync("https://api.resend.com/emails", content);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"Email enviado exitosamente a: {emailDestino}");
                    return true;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Error al enviar email. Status: {response.StatusCode}, Response: {errorContent}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Excepción al enviar email: {ex.Message}");
                return false;
            }
        }

        private string GenerarHtmlVerificacionConDatos(string nombreCompleto, string urlVerificacion, string urlCorreccion, object datosContacto)
        {
            // Convertir el objeto a un diccionario para acceder a las propiedades
            var datos = JsonSerializer.Deserialize<Dictionary<string, object>>(
                JsonSerializer.Serialize(datosContacto)
            );

            return $@"
<!DOCTYPE html>
<html lang='es'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; background-color: #f3f4f6; }}
        .container {{ max-width: 650px; margin: 40px auto; background: white; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }}
        .header {{ background: linear-gradient(135deg, #3b82f6 0%, #2563eb 100%); color: white; padding: 40px 20px; text-align: center; }}
        .header h1 {{ margin: 0; font-size: 28px; font-weight: bold; }}
        .content {{ padding: 30px; background: white; }}
        .content h2 {{ color: #1f2937; font-size: 22px; margin-bottom: 20px; }}
        .content p {{ color: #4b5563; font-size: 16px; margin-bottom: 15px; }}
        
        .datos-section {{ background: #f9fafb; border-radius: 8px; padding: 20px; margin: 25px 0; border: 2px solid #e5e7eb; }}
        .datos-section h3 {{ color: #1f2937; font-size: 18px; margin-top: 0; margin-bottom: 15px; border-bottom: 2px solid #3b82f6; padding-bottom: 8px; }}
        .dato-row {{ display: flex; padding: 10px 0; border-bottom: 1px solid #e5e7eb; }}
        .dato-row:last-child {{ border-bottom: none; }}
        .dato-label {{ flex: 0 0 140px; font-weight: 600; color: #6b7280; font-size: 14px; }}
        .dato-value {{ flex: 1; color: #1f2937; font-size: 14px; font-weight: 500; }}
        
        .button-container {{ text-align: center; margin: 35px 0; }}
        .button {{ display: inline-block; background: #10b981; color: white !important; padding: 16px 32px; text-decoration: none; border-radius: 6px; font-weight: bold; font-size: 16px; box-shadow: 0 4px 6px rgba(16, 185, 129, 0.3); margin: 0 5px; }}
        .button-secondary {{ background: #f59e0b; box-shadow: 0 4px 6px rgba(245, 158, 11, 0.3); }}
        
        .alert {{ background: #fef3c7; border-left: 4px solid #f59e0b; padding: 15px 20px; border-radius: 4px; margin: 20px 0; }}
        .alert p {{ margin: 5px 0; color: #92400e; font-size: 14px; }}
        .alert strong {{ color: #78350f; }}
        
        .divider {{ border: none; border-top: 1px solid #e5e7eb; margin: 30px 0; }}
        .note {{ background: #f9fafb; border-left: 4px solid #3b82f6; padding: 15px 20px; border-radius: 4px; margin: 20px 0; }}
        .note p {{ margin: 5px 0; color: #6b7280; font-size: 14px; }}
        .note strong {{ color: #1f2937; }}
        .footer {{ text-align: center; color: #9ca3af; font-size: 13px; padding: 30px; background: #f9fafb; border-top: 1px solid #e5e7eb; }}
        .footer p {{ margin: 5px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🔐 Verificación de Datos</h1>
        </div>
        <div class='content'>
            <h2>¡Hola {nombreCompleto}!</h2>
            <p>Has sido registrado en nuestro sistema CRM. Por favor, <strong>revisa cuidadosamente</strong> que toda la información sea correcta:</p>
            
            <div class='datos-section'>
                <h3>📋 Información Personal</h3>
                <div class='dato-row'>
                    <div class='dato-label'>Nombre:</div>
                    <div class='dato-value'>{GetValue(datos, "Nombre")}</div>
                </div>
                <div class='dato-row'>
                    <div class='dato-label'>Apellido:</div>
                    <div class='dato-value'>{GetValue(datos, "Apellido")}</div>
                </div>
                <div class='dato-row'>
                    <div class='dato-label'>DPI:</div>
                    <div class='dato-value'>{GetValue(datos, "Dpi")}</div>
                </div>
                <div class='dato-row'>
                    <div class='dato-label'>NIT:</div>
                    <div class='dato-value'>{GetValue(datos, "Nit", "No especificado")}</div>
                </div>
                <div class='dato-row'>
                    <div class='dato-label'>Teléfono:</div>
                    <div class='dato-value'>{GetValue(datos, "Telefono", "No especificado")}</div>
                </div>
                <div class='dato-row'>
                    <div class='dato-label'>Email:</div>
                    <div class='dato-value'>{GetValue(datos, "Email")}</div>
                </div>
            </div>

            <div class='datos-section'>
                <h3>📍 Dirección</h3>
                <div class='dato-row'>
                    <div class='dato-label'>Dirección:</div>
                    <div class='dato-value'>{GetValue(datos, "Direccion", "No especificada")}</div>
                </div>
                <div class='dato-row'>
                    <div class='dato-label'>Zona:</div>
                    <div class='dato-value'>{GetValue(datos, "Zona", "No especificada")}</div>
                </div>
                <div class='dato-row'>
                    <div class='dato-label'>Municipio:</div>
                    <div class='dato-value'>{GetValue(datos, "Municipio", "No especificado")}</div>
                </div>
                <div class='dato-row'>
                    <div class='dato-label'>Departamento:</div>
                    <div class='dato-value'>{GetValue(datos, "Departamento", "No especificado")}</div>
                </div>
            </div>

            <div class='datos-section'>
                <h3>💼 Información Comercial</h3>
                <div class='dato-row'>
                    <div class='dato-label'>Categoría:</div>
                    <div class='dato-value'>{GetValue(datos, "Categoria", "No especificada")}</div>
                </div>
                <div class='dato-row'>
                    <div class='dato-label'>Subcategoría:</div>
                    <div class='dato-value'>{GetValue(datos, "Subcategoria", "No especificada")}</div>
                </div>
                <div class='dato-row'>
                    <div class='dato-label'>Días de Crédito:</div>
                    <div class='dato-value'>{GetValue(datos, "DiasCredito", "0")} días</div>
                </div>
                <div class='dato-row'>
                    <div class='dato-label'>Límite de Crédito:</div>
                    <div class='dato-value'>Q {GetValue(datos, "LimiteCredito", "0.00")}</div>
                </div>
            </div>

            <div class='alert'>
                <p><strong>⚠️ ¿Hay algún dato incorrecto?</strong></p>
                <p>Si encuentras algún error en tu información, haz clic en ""Reportar Error"" y nos pondremos en contacto contigo para corregirlo.</p>
            </div>

            <div class='button-container'>
                <a href='{urlVerificacion}' class='button'>
                    ✓ Confirmar - Datos Correctos
                </a>
                <a href='{urlCorreccion}' class='button button-secondary'>
                    ✏️ Reportar Error
                </a>
            </div>

            <hr class='divider'>

            <div class='note'>
                <p><strong>⏰ Importante:</strong></p>
                <p>• Este enlace es válido por 24 horas</p>
                <p>• Si no solicitaste este registro, puedes ignorar este mensaje</p>
                <p>• Tu cuenta se activará solo después de confirmar que los datos son correctos</p>
            </div>
        </div>
        <div class='footer'>
            <p><strong>CRM Contactos</strong></p>
            <p>© 2025 Todos los derechos reservados</p>
            <p style='margin-top: 15px; font-size: 11px;'>Este es un mensaje automático, por favor no respondas a este correo.</p>
        </div>
    </div>
</body>
</html>";
        }

        // Método auxiliar para obtener valores del diccionario
        private string GetValue(Dictionary<string, object> datos, string key, string defaultValue = "N/A")
        {
            if (datos != null && datos.TryGetValue(key, out var value) && value != null)
            {
                // Manejar JsonElement
                if (value is JsonElement element)
                {
                    if (element.ValueKind == JsonValueKind.String)
                    {
                        var strValue = element.GetString();
                        return string.IsNullOrWhiteSpace(strValue) ? defaultValue : strValue;
                    }
                    else if (element.ValueKind == JsonValueKind.Number)
                    {
                        return element.ToString();
                    }
                }

                var strValue2 = value.ToString();
                return string.IsNullOrWhiteSpace(strValue2) ? defaultValue : strValue2;
            }
            return defaultValue;
        }
    }
}