using System.Text;
using System.Text.Json.Serialization;
using MDMS.Application.Security;
using MDMS.Infrastructure;
using MDMS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Enums as strings ("Single", "Open", ...) rather than the framework's numeric default —
// every enum in this API is domain-meaningful and every client (this project's own frontend
// included) reasons about them by name, not by an arbitrary underlying int.
builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddMdmsInfrastructure(builder.Configuration);

// Development-only CORS policy so the Next.js dev server (a different origin) can call this API.
// No policy is registered outside Development — a real deployment needs its own explicit,
// narrower origin list, not this permissive localhost default. AllowCredentials is required for
// the refresh-token cookie to be sent/received cross-origin (localhost:3000 -> localhost:5004);
// it's only valid alongside an explicit origin list, never AllowAnyOrigin — WithOrigins already
// satisfies that.
const string DevCorsPolicy = "DevFrontend";
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddCors(options =>
    {
        options.AddPolicy(DevCorsPolicy, policy =>
        {
            policy.WithOrigins("http://localhost:3000")
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
    });
}

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.AddSingleton<JwtTokenService>();
builder.Services.AddScoped<MDMS.Api.Tenancy.HttpTenantContext>();
builder.Services.AddScoped<MDMS.Application.Common.ITenantContext>(sp => sp.GetRequiredService<MDMS.Api.Tenancy.HttpTenantContext>());
builder.Services.AddHostedService<MDMS.Api.Reporting.DownloadRequestWorker>();

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("Jwt configuration section is missing.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseCors(DevCorsPolicy);

    // Auto-apply pending migrations in Development only — never runs outside Development.
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<MdmsDbContext>();
    db.Database.Migrate();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseMiddleware<MDMS.Api.Tenancy.TenantResolutionMiddleware>();
app.UseAuthorization();

// Every controller requires an authenticated caller by default (fail-closed) — [AllowAnonymous]
// on AuthController's login/claim/refresh is the only opt-out. This is the concrete piece of
// "RBAC enforcement": authentication is now real and mandatory. Per-role authorization beyond
// that is applied endpoint-by-endpoint only where the roles already recorded in the Users module
// (Admin vs. everyone else) map onto an obvious permission boundary — user-account administration.
// A full permission matrix for every field role (ItManager, Nomc, Installer, ...) doesn't exist in
// any spec yet and would be fabricated if invented here; that's tracked as its own follow-up.
app.MapControllers().RequireAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));

app.Run();
