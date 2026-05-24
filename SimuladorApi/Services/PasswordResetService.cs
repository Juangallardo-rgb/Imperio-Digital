using Microsoft.EntityFrameworkCore;
using SimuladorApi.Data;
using SimuladorApi.Models;
using System.Security.Cryptography;

namespace SimuladorApi.Services
{
    public class PasswordResetService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public PasswordResetService(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<(bool Success, string Message, string? ResetUrl)> GenerateResetTokenAsync(string email)
        {
            var normalizedEmail = email.Trim().ToLower();

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);

            // Por seguridad, no revelamos si el correo existe o no.
            if (user == null)
            {
                return (true, "Si el correo existe, se generó una solicitud de recuperación.", null);
            }

            var oldTokens = await _context.PasswordResetTokens
                .Where(t => t.UserId == user.Id && !t.Used)
                .ToListAsync();

            foreach (var oldToken in oldTokens)
            {
                oldToken.Used = true;
                oldToken.UsedAt = DateTime.UtcNow;
            }

            var token = GenerateSecureToken();

            var resetToken = new PasswordResetToken
            {
                UserId = user.Id,
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddMinutes(30),
                Used = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.PasswordResetTokens.Add(resetToken);
            await _context.SaveChangesAsync();

            var frontendUrl = _configuration["Frontend:Url"] ?? "http://localhost:5173";

            var resetUrl = $"{frontendUrl}/reset-password?token={Uri.EscapeDataString(token)}";

            return (true, "Solicitud de recuperación generada correctamente.", resetUrl);
        }

        public async Task<(bool Success, string Message)> ResetPasswordAsync(string token, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(token))
                return (false, "Token inválido.");

            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
                return (false, "La nueva contraseña debe tener al menos 6 caracteres.");

            var resetToken = await _context.PasswordResetTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Token == token);

            if (resetToken == null)
                return (false, "Token inválido.");

            if (resetToken.Used)
                return (false, "Este enlace ya fue utilizado.");

            if (resetToken.ExpiresAt < DateTime.UtcNow)
                return (false, "Este enlace ya expiró.");

            if (resetToken.User == null)
                return (false, "Usuario no encontrado.");

            resetToken.User.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            resetToken.Used = true;
            resetToken.UsedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return (true, "Contraseña actualizada correctamente.");
        }

        private static string GenerateSecureToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(48);
            return Convert.ToBase64String(bytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "");
        }
    }
}