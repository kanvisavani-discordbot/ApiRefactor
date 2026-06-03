using ApiRefactor.Data;
using ApiRefactor.Data.Repositories;
using ApiRefactor.Domain.Interfaces;

namespace ApiRefactor.Extensions;

public static class DataServiceExtensions
{
    public static IServiceCollection AddDataServices(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddSingleton<IDbConnectionFactory>(
            _ => new SqliteConnectionFactory(connectionString));

        services.AddScoped<IWaveRepository, WaveRepository>();
        services.AddSingleton<DatabaseInitialiser>();

        return services;
    }
}
