using MDMS.Infrastructure;
using MDMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddMdmsInfrastructure(builder.Configuration);

// Development-only CORS policy so the Next.js dev server (a different origin) can call this API.
// No policy is registered outside Development — a real deployment needs its own explicit,
// narrower origin list, not this permissive localhost default.
const string DevCorsPolicy = "DevFrontend";
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddCors(options =>
    {
        options.AddPolicy(DevCorsPolicy, policy =>
        {
            policy.WithOrigins("http://localhost:3000")
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
    });
}

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
app.UseAuthorization();

app.MapControllers();

app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));

app.Run();
