using MDMS.Application.Common;
using MDMS.Application.MeterData;
using MDMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MDMS.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddMdmsInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Mdms")
            ?? throw new InvalidOperationException("Missing 'ConnectionStrings:Mdms' configuration.");

        services.AddDbContext<MdmsDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IMdmsDbContext>(sp => sp.GetRequiredService<MdmsDbContext>());

        services.AddScoped<LoadSurveyIngestionService>();
        services.AddScoped<DailyLoadProfileIngestionService>();
        services.AddScoped<OutOfRangeValidationService>();
        services.AddScoped<MissingIntervalEstimationService>();

        return services;
    }
}
