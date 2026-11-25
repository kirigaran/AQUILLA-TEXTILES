using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Security.Cryptography;
using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using AquillaTextiles.Data;
using AquillaTextiles.Models;
using AquillaTextiles.DTOs;

var builder = WebApplication.CreateBuilder(args);

var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL")
    ?? builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Set DATABASE_URL or ConnectionStrings:DefaultConnection.");

var jwtKey = Environment.GetEnvironmentVariable("JWT_SECRET")
    ?? builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Set a strong JWT_SECRET environment variable.");

if (jwtKey.Length < 32)
    throw new InvalidOperationException("JWT_SECRET must be at least 32 characters.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins(
                Environment.GetEnvironmentVariable("FRONTEND_ORIGIN") ?? "http://localhost:5000")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? "AquillaTextiles",
            ValidAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? "AquillaTextilesUsers",
            IssuerSigningKey = key,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/api/health", () => Results.Ok(new { status = "healthy", app = "AQUILLA TEXTILES" }));

app.MapPost("/api/auth/signup", async (SignupRequest request, AppDbContext db) =>
{
    var username = request.Username?.Trim();
    if (string.IsNullOrWhiteSpace(username) || username.Length < 3 || username.Length > 40)
        return Results.BadRequest(new { message = "Username must be between 3 and 40 characters." });
    if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
        return Results.BadRequest(new { message = "Password must contain at least 8 characters." });
    if (request.Password != request.ConfirmPassword)
        return Results.BadRequest(new { message = "Passwords do not match." });

    var email = request.Email?.Trim();
    if (string.IsNullOrWhiteSpace(email) || !System.Net.Mail.MailAddress.TryCreate(email, out var parsedEmail) || parsedEmail.Address != email)
        return Results.BadRequest(new { message = "Enter a valid email address." });

    var normalizedEmail = email.ToLowerInvariant();
    if (await db.Users.AnyAsync(u => u.NormalizedEmail == normalizedEmail))
        return Results.Conflict(new { message = "That email address is already registered." });

    var normalized = username.ToLowerInvariant();
    if (await db.Users.AnyAsync(u => u.NormalizedUsername == normalized))
        return Results.Conflict(new { message = "That username is already registered." });

    var user = new User
    {
        Username = username,
        NormalizedUsername = normalized,
        Email = email,
        NormalizedEmail = normalizedEmail,
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
        CreatedAtUtc = DateTime.UtcNow
    };
    db.Users.Add(user);
    await db.SaveChangesAsync();
    return Results.Created("/api/auth/login", new { message = "Account created. You can now log in." });
});

app.MapPost("/api/auth/login", async (LoginRequest request, AppDbContext db) =>
{
    var username = request.Username?.Trim().ToLowerInvariant();
    if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(request.Password))
        return Results.BadRequest(new { message = "Enter your username and password." });

    var user = await db.Users.SingleOrDefaultAsync(u => u.NormalizedUsername == username);
    if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        return Results.Unauthorized();

    var claims = new[]
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Name, user.Username)
    };
    var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
    var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
    var issuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? "AquillaTextiles";
    var audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? "AquillaTextilesUsers";
    var token = new JwtSecurityToken(issuer, audience, claims,
        expires: DateTime.UtcNow.AddHours(2), signingCredentials: credentials);

    return Results.Ok(new
    {
        token = new JwtSecurityTokenHandler().WriteToken(token),
        username = user.Username,
        expiresInSeconds = 7200
    });
});

app.MapPost("/api/auth/forgot-password", async (ForgotPasswordRequest request, AppDbContext db, IConfiguration config) =>
{
    var email = request.Email?.Trim();
    if (string.IsNullOrWhiteSpace(email) || !MailAddress.TryCreate(email, out _))
        return Results.BadRequest(new { message = "Enter a valid registered email address." });

    var smtpHost = Environment.GetEnvironmentVariable("EMAIL_SMTP_HOST") ?? config["Email:SmtpHost"];
    var smtpPortText = Environment.GetEnvironmentVariable("EMAIL_SMTP_PORT") ?? config["Email:SmtpPort"] ?? "587";
    var smtpUser = Environment.GetEnvironmentVariable("EMAIL_SMTP_USER") ?? config["Email:SmtpUser"];
    var smtpPassword = Environment.GetEnvironmentVariable("EMAIL_SMTP_PASSWORD") ?? config["Email:SmtpPassword"];
    var fromAddress = Environment.GetEnvironmentVariable("EMAIL_FROM") ?? config["Email:From"] ?? smtpUser;

    if (string.IsNullOrWhiteSpace(smtpHost) || string.IsNullOrWhiteSpace(smtpUser) ||
        string.IsNullOrWhiteSpace(smtpPassword) || string.IsNullOrWhiteSpace(fromAddress) ||
        !int.TryParse(smtpPortText, out var smtpPort))
        return Results.Problem("Email delivery is not configured yet. Set the SMTP environment variables before using password recovery.", statusCode: 503);

    var normalizedEmail = email.ToLowerInvariant();
    var user = await db.Users.SingleOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail);
    // Do not reveal whether an email address is registered.
    if (user is null)
        return Results.Ok(new { message = "If the email is registered, a verification code will be sent." });

    var code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
    user.PasswordResetTokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(code)));
    user.PasswordResetExpiresUtc = DateTime.UtcNow.AddMinutes(10);
    await db.SaveChangesAsync();

    try
    {
        using var message = new MailMessage(fromAddress, email)
        {
            Subject = "AQUILLA TEXTILES - Password reset code",
            Body = $"Hello {user.Username},\n\nYour password reset verification code is: {code}\n\nThis code expires in 10 minutes. If you did not request this, please ignore this email.\n\nAQUILLA TEXTILES",
            IsBodyHtml = false
        };
        using var client = new SmtpClient(smtpHost, smtpPort)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(smtpUser, smtpPassword)
        };
        await client.SendMailAsync(message);
    }
    catch
    {
        user.PasswordResetTokenHash = null;
        user.PasswordResetExpiresUtc = null;
        await db.SaveChangesAsync();
        return Results.Problem("We could not send the email. Check your SMTP settings and try again.", statusCode: 502);
    }

    return Results.Ok(new { message = "If the email is registered, a verification code will be sent." });
});

app.MapPost("/api/auth/reset-password", async (ResetPasswordRequest request, AppDbContext db) =>
{
    var email = request.Email?.Trim().ToLowerInvariant();
    if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(request.Code) ||
        string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 8)
        return Results.BadRequest(new { message = "Enter your email, verification code, and a password of at least 8 characters." });

    var codeHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(request.Code.Trim())));
    var user = await db.Users.SingleOrDefaultAsync(u =>
        u.NormalizedEmail == email && u.PasswordResetTokenHash == codeHash &&
        u.PasswordResetExpiresUtc > DateTime.UtcNow);

    if (user is null)
        return Results.BadRequest(new { message = "The verification code is invalid or expired." });

    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
    user.PasswordResetTokenHash = null;
    user.PasswordResetExpiresUtc = null;
    await db.SaveChangesAsync();
    return Results.Ok(new { message = "Password updated successfully. Please log in." });
});

app.MapGet("/api/account/me", async (ClaimsPrincipal principal, AppDbContext db) =>
{
    var idValue = principal.FindFirstValue(ClaimTypes.NameIdentifier);
    if (!int.TryParse(idValue, out var id)) return Results.Unauthorized();
    var user = await db.Users.FindAsync(id);
    return user is null ? Results.NotFound() : Results.Ok(new { username = user.Username });
}).RequireAuthorization();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.MapFallbackToFile("index.html");
app.Run();
