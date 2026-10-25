using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Planoria.Data;

namespace Planoria.Api.Tests;

public class ApiFactory : WebApplicationFactory<Program>
{
    public ApiFactory()
    {
        // Program.cs anahtarı daha başlarken okuyor, bu yüzden ortam değişkeniyle veriyoruz
        Environment.SetEnvironmentVariable("Jwt__Key", "test-only-signing-key-0123456789-abcdefghij");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // SQL Server kaydını kaldır
            var toRemove = services.Where(d =>
                d.ServiceType == typeof(DbContextOptions<AppDbContext>) ||
                d.ServiceType == typeof(DbContextOptions) ||
                (d.ServiceType.IsGenericType &&
                 d.ServiceType.GetGenericArguments().Contains(typeof(AppDbContext)) &&
                 d.ServiceType.Name.StartsWith("IDbContextOptionsConfiguration")))
                .ToList();
            foreach (var d in toRemove) services.Remove(d);

            // Yerine bellek içi veritabanı
            var dbName = "tests-" + Guid.NewGuid();
            services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase(dbName));
        });
    }
}