using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using TaskManagement.Application.Interfaces;
using TaskManagement.Application.Options;
using TaskManagement.Domain.Interfaces;
using TaskManagement.Infrastructure.BackgroundServices;
using TaskManagement.Infrastructure.Caching;
using TaskManagement.Infrastructure.Identity;
using TaskManagement.Infrastructure.Persistence;
using TaskManagement.Infrastructure.Persistence.Repositories;
using TaskManagement.Infrastructure.Queue;

namespace TaskManagement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services,IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection("Jwt"));
        services.Configure<RefreshTokenOptions>(configuration.GetSection("RefreshToken"));
        services.Configure<RedisOptions>(configuration.GetSection("Redis"));

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ITaskRepository, TaskRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

        services.AddSingleton<IJwtService, JwtService>();

        var redisConnection = configuration["Redis:ConnectionString"]
            ?? configuration.GetConnectionString("Redis")
            ?? throw new InvalidOperationException("Redis connection string is not configured.");

        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnection));
        services.AddScoped<ICacheService, RedisCacheService>();

        services.AddSingleton(Channel.CreateUnbounded<Guid>());
        services.AddSingleton<ITaskQueue, InMemoryTaskQueue>();
        services.AddHostedService<TaskProcessingService>();

        return services;
    }
}
