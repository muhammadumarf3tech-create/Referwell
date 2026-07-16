using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using FluentValidation.AspNetCore;
using ReferWell.Api.Logging;
using ReferWell.Api.Middleware;
using ReferWell.Api.Services;
using ReferWell.Application;
using ReferWell.Application.Common.Interfaces;
using ReferWell.Infrastructure.Data;
using ReferWell.Infrastructure.Hubs;
using ReferWell.Infrastructure.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

const long MaxPdfBytes = 20L * 1024 * 1024; // 20 MB

// ── Database ──────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsAssembly("ReferWell.Infrastructure")));

builder.Services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<AppDbContext>());

// ── JWT Authentication ────────────────────────────────────────────────────────
var jwtConfig = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtConfig["Key"];
if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.StartsWith("CHANGE_ME", StringComparison.OrdinalIgnoreCase))
    throw new InvalidOperationException(
        "JWT signing key is not configured. Set Jwt:Key via User Secrets, environment variable Jwt__Key, or appsettings.Development.json (never commit production secrets).");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = true,
            ValidIssuer = jwtConfig["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwtConfig["Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        // Allow JWT in SignalR query string (hub path only)
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var accessToken = ctx.Request.Query["access_token"];
                var path = ctx.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                    ctx.Token = accessToken;
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<SecurityAuditService>();
builder.Services.AddScoped<ISecurityAuditLogger>(sp => sp.GetRequiredService<SecurityAuditService>());
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
builder.Services.AddScoped<IQueueNotifier, QueueNotifier>();
builder.Services.AddScoped<IAttachmentStorage, AttachmentStorage>();
builder.Services.AddScoped<IMenuAccessChecker, MenuAccessChecker>();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddSingleton<RequestFileLogger>();

// ── SignalR ───────────────────────────────────────────────────────────────────
builder.Services.AddSignalR();

// ── Background Services ───────────────────────────────────────────────────────
builder.Services.AddSingleton<MassCommChannel>();
builder.Services.AddSingleton<IMassCommQueue>(sp => sp.GetRequiredService<MassCommChannel>());
builder.Services.AddHostedService<MassCommBackgroundService>();
builder.Services.AddHostedService<SlaBreachBackgroundService>();
builder.Services.AddHostedService<PriorityScoreRefreshBackgroundService>();

// ── Application layer ─────────────────────────────────────────────────────────
builder.Services.AddApplication();

// ── Upload limits (PDF attachments ≤ 20 MB) ───────────────────────────────────
builder.Services.Configure<FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = MaxPdfBytes;
});
builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestBodySize = MaxPdfBytes);

// ── CORS ─────────────────────────────────────────────────────────────────────
builder.Services.AddCors(opts =>
    opts.AddPolicy("FrontendPolicy", policy =>
        policy.WithOrigins("http://localhost:4000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()));

// ── Controllers + FluentValidation ────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });
builder.Services.AddFluentValidationAutoValidation();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "ReferWell API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Enter JWT token"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// ── Migrate & Seed ────────────────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

// ── Middleware Pipeline ───────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("FrontendPolicy");
app.UseAuthentication();
app.UseAuthorization();

// Request logging (method/path/status/duration/user — no bodies or secrets)
app.UseMiddleware<RequestLoggingMiddleware>();

// OWASP: Security headers
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    await next();
});

app.MapControllers();
app.MapHub<QueueHub>("/hubs/queue");

app.Run();
