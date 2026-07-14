using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SimuladorApi.Data;
using SimuladorApi.Hubs;
using SimuladorApi.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

if (!builder.Environment.IsDevelopment())
{
    var requiredConfiguration = new[]
    {
        "ConnectionStrings:DefaultConnection",
        "Jwt:Key",
        "OpenRouter:ApiKey",
        "OpenRouter:SiteUrl",
        "Frontend:Url"
    };

    var missingConfiguration = requiredConfiguration
        .Where(key => string.IsNullOrWhiteSpace(builder.Configuration[key]))
        .ToList();

    if (missingConfiguration.Any())
    {
        throw new InvalidOperationException(
            $"Faltan variables de produccion: {string.Join(", ", missingConfiguration)}."
        );
    }

    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders =
            ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
    });
}

// =====================================================
// CONTROLADORES, SWAGGER Y SIGNALR
// =====================================================

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSignalR();
builder.Services.AddHealthChecks();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Simulador API",
        Version = "v1"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Ingresa el token JWT así: Bearer {tu token}"
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

// =====================================================
// BASE DE DATOS
// =====================================================

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")
    ));

// =====================================================
// SERVICIOS
// =====================================================

builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<ScenarioPhaseMappingService>();
builder.Services.AddScoped<ScenarioService>();
builder.Services.AddScoped<ScoringService>();
builder.Services.AddScoped<KpiSimulationService>();
builder.Services.AddScoped<AiFeedbackService>();
builder.Services.AddScoped<AiScenarioContentService>();
builder.Services.AddScoped<SimulationService>();
builder.Services.AddScoped<CourseService>();
builder.Services.AddScoped<PasswordResetService>();
builder.Services.AddScoped<MethodologyCatalogService>();
builder.Services.AddScoped<ScenarioOptionTemplateService>();

builder.Services.AddScoped<
    IRealtimeNotificationService,
    RealtimeNotificationService>();

builder.Services.AddHttpClient<OpenRouterService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(180);
});

builder.Services.AddHttpClient<AiScenarioContentService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(180);
});

// =====================================================
// CORS
// =====================================================

var allowedOrigins = new List<string>
{
    "http://localhost:5173",
    "https://imperio-digital-one.vercel.app"
};

var configuredFrontendUrl =
    builder.Configuration["Frontend:Url"]?.TrimEnd('/');

if (!string.IsNullOrWhiteSpace(configuredFrontendUrl) &&
    !allowedOrigins.Contains(
        configuredFrontendUrl,
        StringComparer.OrdinalIgnoreCase))
{
    allowedOrigins.Add(configuredFrontendUrl);
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .WithOrigins(allowedOrigins.ToArray())
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// =====================================================
// JWT
// =====================================================

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,

                ValidIssuer =
                    builder.Configuration["Jwt:Issuer"],

                ValidAudience =
                    builder.Configuration["Jwt:Audience"],

                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(
                            builder.Configuration["Jwt:Key"]!
                        )
                    )
            };

        /*
         * En WebSockets y Server-Sent Events, SignalR puede enviar
         * el token mediante el parámetro access_token.
         */
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken =
                    context.Request.Query["access_token"].ToString();

                var path = context.HttpContext.Request.Path;

                if (!string.IsNullOrWhiteSpace(accessToken) &&
                    path.StartsWithSegments("/hubs/realtime"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// =====================================================
// APLICACIÓN
// =====================================================

var app = builder.Build();

app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler(errorApp =>
    {
        errorApp.Run(async context =>
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(new
            {
                message = "Ocurrio un error inesperado. Intenta nuevamente."
            });
        });
    });
    app.UseHsts();
}

using (var scope = app.Services.CreateScope())
{
    var methodologyCatalogService =
        scope.ServiceProvider
            .GetRequiredService<MethodologyCatalogService>();

    await methodologyCatalogService
        .SeedDefaultMethodologiesAsync();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowFrontend");

app.UseAuthentication();

app.Use(async (context, next) =>
{
    var mustChangePassword =
        context.User.Identity?.IsAuthenticated == true &&
        string.Equals(
            context.User.FindFirst("mustChangePassword")?.Value,
            "true",
            StringComparison.OrdinalIgnoreCase
        );

    if (mustChangePassword)
    {
        var path = context.Request.Path;
        var isAllowedAuthPath =
            path.StartsWithSegments("/api/Auth/change-temporary-password") ||
            path.StartsWithSegments("/api/Auth/login") ||
            path.StartsWithSegments("/api/Auth/forgot-password") ||
            path.StartsWithSegments("/api/Auth/reset-password");

        if (!isAllowedAuthPath)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsync(
                "Debes cambiar tu contraseña temporal antes de continuar."
            );
            return;
        }
    }

    await next();
});

app.UseAuthorization();

app.MapControllers();

app.MapHub<RealtimeHub>("/hubs/realtime");

app.MapHealthChecks("/health").AllowAnonymous();

app.Run();
