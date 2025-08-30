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
// Configuración de Entity Framework
builder.Services.AddDbContext<CrmDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        new MySqlServerVersion(new Version(8, 0, 21))
    ));
// Configuración de AutoMapper
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
        Description = "API para gestión de contactos del CRM"
    });
    // Configuración JWT corregida
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
// Configuración de CORS
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
    c.RoutePrefix = string.Empty; // Hacer Swagger disponible en la raíz "/"
});
app.UseHttpsRedirection();
app.UseCors();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/", () => "CRM Contacts API está funcionando!");
app.MapGet("/health", () => "OK");
app.Run();