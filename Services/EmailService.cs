using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CrmContactsApi.Services
{
    public interface IEmailService
    {
        Task<bool> EnviarEmailVerificacionAsync(string emailDestino, string nombreCompleto, string token, int contactoId);
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

        public async Task<bool> EnviarEmailVerificacionAsync(string emailDestino, string nombreCompleto, string token, int contactoId)
        {
            try
            {
                var appUrl = _configuration["AppUrl"];
                var urlVerificacion = $"{appUrl}/api/Contactos/verificar/{contactoId}/{token}";

                var htmlContent = GenerarHtmlVerificacion(nombreCompleto, urlVerificacion);

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

        private string GenerarHtmlVerificacion(string nombreCompleto, string urlVerificacion)
        {
            return $@"
<!DOCTYPE html>
<html lang='es'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; background-color: #f3f4f6; }}
        .container {{ max-width: 600px; margin: 40px auto; background: white; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }}
        .header {{ background: linear-gradient(135deg, #3b82f6 0%, #2563eb 100%); color: white; padding: 40px 20px; text-align: center; }}
        .header h1 {{ margin: 0; font-size: 28px; font-weight: bold; }}
        .content {{ padding: 40px 30px; background: white; }}
        .content h2 {{ color: #1f2937; font-size: 22px; margin-bottom: 20px; }}
        .content p {{ color: #4b5563; font-size: 16px; margin-bottom: 15px; }}
        .button-container {{ text-align: center; margin: 35px 0; }}
        .button {{ display: inline-block; background: #10b981; color: white !important; padding: 16px 32px; text-decoration: none; border-radius: 6px; font-weight: bold; font-size: 16px; box-shadow: 0 4px 6px rgba(16, 185, 129, 0.3); }}
        .divider {{ border: none; border-top: 1px solid #e5e7eb; margin: 30px 0; }}
        .note {{ background: #f9fafb; border-left: 4px solid #3b82f6; padding: 15px 20px; border-radius: 4px; margin: 20px 0; }}
        .note p {{ margin: 5px 0; color: #6b7280; font-size: 14px; }}
        .note strong {{ color: #1f2937; }}
        .url-box {{ background: #f3f4f6; padding: 15px; border-radius: 4px; margin: 20px 0; word-break: break-all; }}
        .url-box p {{ margin: 5px 0; font-size: 12px; color: #6b7280; }}
        .url-box a {{ color: #3b82f6; font-size: 12px; }}
        .footer {{ text-align: center; color: #9ca3af; font-size: 13px; padding: 30px; background: #f9fafb; border-top: 1px solid #e5e7eb; }}
        .footer p {{ margin: 5px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🔐 Verificación de Contacto</h1>
        </div>
        <div class='content'>
            <h2>¡Hola {nombreCompleto}!</h2>
            <p>Has sido registrado exitosamente en nuestro sistema CRM.</p>
            <p>Para completar tu registro y activar tu perfil, necesitamos que verifiques que tus datos son correctos.</p>
            <div class='button-container'>
                <a href='{urlVerificacion}' class='button'>✓ Verificar mis datos ahora</a>
            </div>
            <div class='url-box'>
                <p><strong>¿No puedes hacer clic en el botón?</strong></p>
                <p>Copia y pega este enlace en tu navegador:</p>
                <a href='{urlVerificacion}'>{urlVerificacion}</a>
            </div>
            <hr class='divider'>
            <div class='note'>
                <p><strong>⏰ Importante:</strong></p>
                <p>• Este enlace es válido por 24 horas</p>
                <p>• Si no solicitaste este registro, puedes ignorar este mensaje</p>
                <p>• Tu cuenta se activará automáticamente después de la verificación</p>
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
    }
}