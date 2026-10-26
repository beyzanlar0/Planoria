using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Planoria.Data;

var builder = WebApplication.CreateBuilder(args);

// 1) İmza anahtarını gizli depodan oku; yoksa API'yi başlatma
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key bulunamadı. dotnet user-secrets ile tanımla.");

builder.Services.AddControllers();
builder.Services.AddOpenApi();
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("ConnectionStrings:Default bulunamadı. appsettings.json dosyasını kontrol et.");

builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseSqlServer(connectionString));

// 2) Gelen kartı (JWT) nasıl kontrol edeceğimizi tanımla
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.MapInboundClaims = false;
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "Planoria.Api",
            ValidateAudience = true,
            ValidAudience = "Planoria.App",
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddCors(o => o.AddPolicy("web-dev", p => p
 .WithOrigins("http://localhost:5500", "http://127.0.0.1:5500")
    .WithHeaders("Content-Type", "Authorization")
    .WithMethods("GET", "POST", "PUT", "DELETE")));

// 3) Giriş ve kayıt denemelerini IP başına dakikada 10 ile sınırla
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddPolicy("auth", ctx => RateLimitPartition.GetFixedWindowLimiter(
        ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        }));
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    // Sadece geliştirmede: veritabanı yoksa oluştur, eksik migration varsa uygula
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (db.Database.IsRelational())   // testlerdeki bellek içi veritabanını atla
        db.Database.Migrate();
}

app.UseHttpsRedirection();
if (app.Environment.IsDevelopment())
    app.UseCors("web-dev");
app.UseRateLimiter();
app.UseAuthentication();   // "sen kimsin?"
app.UseAuthorization();    // "buraya girebilir misin?"
app.MapControllers();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

// 5) Kart zorunlu: kimlik sunucuda karttan okunuyor
app.MapGet("/me", (ClaimsPrincipal user) => new
{
    id = user.FindFirst("sub")?.Value,
    username = user.FindFirst("name")?.Value
}).RequireAuthorization();

app.Run();
public partial class Program { }