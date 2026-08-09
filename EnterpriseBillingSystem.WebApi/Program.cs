using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using EnterpriseBillingSystem.Application;
using EnterpriseBillingSystem.Application.Common.Interfaces;
using EnterpriseBillingSystem.Infrastructure;
using EnterpriseBillingSystem.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using EnterpriseBillingSystem.WebApi.Authorization;
using EnterpriseBillingSystem.WebApi.Middleware;
using EnterpriseBillingSystem.WebApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Configurar Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console());

try
{
    Log.Information("Iniciando la base del sistema de facturación empresarial...");

    // Registrar servicios de las capas Application e Infrastructure
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    // Registrar HttpContextAccessor e ICurrentUserService
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

    // Registrar Autorización por Permisos Dinámicos
    builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
    builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

    // Configurar JWT Authentication
    var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>();
    if (jwtSettings == null || string.IsNullOrEmpty(jwtSettings.Secret))
    {
        throw new InvalidOperationException("La sección JwtSettings no está configurada o carece de la clave secreta.");
    }

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
            ClockSkew = TimeSpan.Zero
        };
    });

    // Configurar límites de tamaño de solicitud para subida de archivos (50 MB)
    builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
    {
        options.MultipartBodyLengthLimit = 52_428_800; // 50 MB
        options.ValueLengthLimit = 52_428_800;
        options.MultipartHeadersLengthLimit = 52_428_800;
    });

    builder.WebHost.ConfigureKestrel(options =>
    {
        options.Limits.MaxRequestBodySize = 52_428_800; // 50 MB
    });

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();

    // Configurar Swagger con Bearer Security
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo 
        { 
            Title = "LA UNIÓN API", 
            Version = "v1",
            Description = "Base del API del Sistema de Facturación de LA UNIÓN (.NET 8 Clean Architecture / DDD)"
        });

        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Ingrese 'Bearer' seguido de un espacio y su token JWT.\n\nEjemplo: 'Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...'"
        });

        options.AddSecurityRequirement(new OpenApiSecurityRequirement
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
                Array.Empty<string>()
            }
        });
    });

    // Configurar CORS para soportar WPF y Flutter (móvil y web si fuera el caso)
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll", policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
    });

    var app = builder.Build();

    // Middleware de excepciones global al principio de la cadena
    app.UseMiddleware<ExceptionMiddleware>();

    // Usar Logging de peticiones Serilog
    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "LA UNIÓN API v1");
        });
    }

    // Deshabilitado redireccion forzada a HTTPS para permitir la conexion por HTTP directo en IP 167.99.13.177
    // if (!app.Environment.IsDevelopment())
    // {
    //     app.UseHttpsRedirection();
    // }

    app.UseCors("AllowAll");

    // Permitir la descarga de archivos .apk de Android y archivos por defecto (index.html) para Flutter Web (/app/)
    var provider = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider();
    provider.Mappings[".apk"] = "application/vnd.android.package-archive";

    app.UseDefaultFiles();

    app.UseStaticFiles(new StaticFileOptions
    {
        ContentTypeProvider = provider,
        OnPrepareResponse = ctx =>
        {
            ctx.Context.Response.Headers.Append("Access-Control-Allow-Origin", "*");
            ctx.Context.Response.Headers.Append("Access-Control-Allow-Methods", "GET, OPTIONS");
        }
    });

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    // Sembrar la base de datos al inicio
    try
    {
        using var scope = app.Services.CreateScope();
        var dbInitializer = scope.ServiceProvider.GetRequiredService<IDbInitializer>();
        await dbInitializer.InitializeAsync();
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Ocurrió un error durante la inicialización de la base de datos en el arranque.");
    }

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "El host terminó inesperadamente.");
}
finally
{
    Log.CloseAndFlush();
}
