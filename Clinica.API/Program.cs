using Clinica.API.Extensions;
using Clinica.API.Middlewares;
using Clinica.IoC;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Reflection;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, config) => config
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Warning)
    .WriteTo.Console());

// Domain / Application / Infrastructure registrados via IoC
Bootstrap.AddIoC(builder.Services, builder.Configuration);

builder.Services.AddControllers();

// Erros customizados não tratados viram JSON padronizado (GlobalExceptionHandler)
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Clínica API - CP5",
        Version = "v1",
        Description = "Pacientes e consultas com Clean Architecture, Operation Result, erros customizados, ViaCEP, PageResult, HATEOAS e JWT."
    });

    var xml = Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");
    if (File.Exists(xml)) c.IncludeXmlComments(xml);

    var scheme = new OpenApiSecurityScheme
    {
        Description = "Informe o token JWT (Authorization: Bearer {token}).",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
    };

    c.AddSecurityDefinition("Bearer", scheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement { { scheme, Array.Empty<string>() } });
});

// Otimização: compressão das respostas
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});

// Autenticação JWT
var key = Encoding.UTF8.GetBytes(builder.Configuration["Secretkey"]
    ?? throw new InvalidOperationException("Configure 'Secretkey' no appsettings."));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.MapInboundClaims = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromMinutes(1),
        NameClaimType = "name",
        RoleClaimType = "role"
    };

    // Respostas 401/403 em JSON, no mesmo formato dos outros erros
    options.Events = new JwtBearerEvents
    {
        OnChallenge = async context =>
        {
            context.HandleResponse();
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new ErrorResponse(
                401, "NAO_AUTENTICADO", "Token ausente, inválido ou expirado.", context.Request.Path));
        },
        OnForbidden = async context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new ErrorResponse(
                403, "ACESSO_NEGADO", "Seu perfil não tem permissão para este recurso.", context.Request.Path));
        }
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();

try
{
    await Bootstrap.InitializeDatabaseAsync(app.Services);
}
catch (Exception ex)
{
    app.Logger.LogError(ex, "Não foi possível inicializar o banco de dados. Verifique a connection string.");
}

app.UseExceptionHandler();

app.UseSwagger();
app.UseSwaggerUI();

app.UseResponseCompression();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }
