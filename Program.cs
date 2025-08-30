using CrmContactsApi.Mappings;
using CrmContactsApi.Models;
using CrmContactsApi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Configuraci�n de Entity Framework - USAR MYSQL_URL de Railway
var connectionString = Environment.GetEnvironmentVariable("MYSQL_URL") ??
                      builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<CrmDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// Configuraci�n de AutoMapper
builder.Services.AddAutoMapper(typeof(ContactoProfile));

// Registro de servicios
builder.Services.AddScoped<IContactoService, ContactoService>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "CRM Contactos API",
        Version = "v1",
        Description = "API para gesti�n de contactos del CRM"
    });

    // Configuraci�n JWT corregida
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header usando Bearer scheme. Ejemplo: 'Bearer {token}'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

// Configuraci�n de CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "CrmContactsApi",
            ValidAudience = "CrmContactsApi",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("MiClaveSecretaSuperSeguraParaJWT123456789"))
        };
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "CRM Contactos API v1");
    c.RoutePrefix = string.Empty; // Hacer Swagger disponible en la ra�z "/"
});

// Comentar HTTPS redirect para Railway (Railway maneja HTTPS autom�ticamente)
// app.UseHttpsRedirection();

app.UseCors();
app.UseAuthentication(); // Agregar esta l�nea que faltaba
app.UseAuthorization();

app.MapControllers();

// Rutas de diagn�stico
app.MapGet("/", () => "CRM Contacts API est� funcionando!");
app.MapGet("/health", () => "OK");

// Ruta para debug de conexi�n
app.MapGet("/debug-connection", async (CrmDbContext context) =>
{
    try
    {
        var canConnect = await context.Database.CanConnectAsync();
        var connectionStr = Environment.GetEnvironmentVariable("MYSQL_URL") ?? "No MYSQL_URL found";

        return new
        {
            CanConnect = canConnect,
            HasMySqlUrl = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("MYSQL_URL")),
            Environment = app.Environment.EnvironmentName
        };
    }
    catch (Exception ex)
    {
        return new
        {
            Error = ex.Message,
            InnerError = ex.InnerException?.Message,
            HasMySqlUrl = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("MYSQL_URL"))
        };
    }
});

app.Run();