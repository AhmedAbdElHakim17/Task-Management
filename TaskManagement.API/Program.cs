using Microsoft.EntityFrameworkCore;
using Serilog;
using TaskManagement.API.Extensions;
using TaskManagement.API.Middleware;
using TaskManagement.Infrastructure.Persistence;
using TaskManagement.Infrastructure.Persistence.Seeding;

// ── Serilog bootstrap logger (captures startup errors before host is built) ──
Log.Logger = new LoggerConfiguration()
    .CreateBootstrapLogger();

Log.Information("Starting Task Management API");

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, config) => 
            config.ReadFrom.Configuration(context.Configuration)
                  .ReadFrom.Services(services)
                  .Enrich.FromLogContext());

builder.Services.AddApplicationServices(builder.Configuration);

var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>();

// Expose Swagger in all environments (useful inside Docker)
app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var retries = 10;    
    while (retries > 0)
    {
        try
        {
            Log.Information("Applying EF Core migrations...");
            await db.Database.MigrateAsync();
            Log.Information("Seeding database...");
            await DbSeeder.SeedAsync(db);
            Log.Information("Database ready");
            break;
        }
        catch (Exception ex)
        {
            retries--;
            Log.Warning("DB not ready, retrying... {Retries} left. Error: {Message}", retries, ex.Message);
            await Task.Delay(5000); 
        }
    }
}

app.Run();
