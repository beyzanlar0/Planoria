using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Planoria.Data;
using AppUser = Planoria.Models.User;

namespace Planoria.Api.Controllers;

public record RegisterRequest(string Username, string Email, string Password);
public record LoginRequest(string UsernameOrEmail, string Password);
public record AuthResponse(string Token, DateTime ExpiresAtUtc);

[ApiController]
[Route("api/auth")]
[EnableRateLimiting("auth")]
public class AuthController : ControllerBase
{
    // Kullanıcı yokken de aynı sürede hash kontrolü yapmak için
    private static readonly string DummyHash = BCrypt.Net.BCrypt.HashPassword("planoria-dummy-password");

    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public AuthController(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest req)
    {
        string username = req.Username?.Trim() ?? "";
        string email = req.Email?.Trim() ?? "";
        string password = req.Password ?? "";

        if (username.Length < 3 || username.Length > 50 || username.Contains('@'))
            return BadRequest(new { error = "Kullanıcı adı 3-50 karakter olmalı ve @ içeremez." });

        if (email.Length > 100 || !System.Net.Mail.MailAddress.TryCreate(email, out var parsed) || parsed.Address != email)
            return BadRequest(new { error = "Geçerli bir e-posta adresi gir." });

        if (password.Length < 8 || Encoding.UTF8.GetByteCount(password) > 72)
            return BadRequest(new { error = "Şifre en az 8 karakter olmalı ve 72 bayttan uzun olamaz." });

        if (await _db.Users.AnyAsync(u => u.Username == username || u.Email == email))
            return Conflict(new { error = "Bu kullanıcı adı veya e-posta zaten kullanılıyor." });

        var user = new AppUser
        {
            Username = username,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password)
        };

        _db.Users.Add(user);
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Conflict(new { error = "Bu kullanıcı adı veya e-posta zaten kullanılıyor." });
        }

        return Ok(CreateToken(user));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest req)
    {
        string id = req.UsernameOrEmail?.Trim() ?? "";
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == id || u.Email == id);

        bool passwordOk = BCrypt.Net.BCrypt.Verify(req.Password ?? "", user?.PasswordHash ?? DummyHash);

        if (user == null || !passwordOk)
            return Unauthorized(new { error = "Kullanıcı adı/e-posta veya şifre hatalı." });

        return Ok(CreateToken(user));
    }

    private AuthResponse CreateToken(AppUser user)
    {
        var key = _config["Jwt:Key"]!;
        var expires = DateTime.UtcNow.AddMinutes(60);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = "Planoria.Api",
            Audience = "Planoria.App",
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("sub", user.Id.ToString()),
                new Claim("name", user.Username)
            }),
            Expires = expires,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                SecurityAlgorithms.HmacSha256)
        };

        string token = new JsonWebTokenHandler().CreateToken(descriptor);
        return new AuthResponse(token, expires);
    }
}