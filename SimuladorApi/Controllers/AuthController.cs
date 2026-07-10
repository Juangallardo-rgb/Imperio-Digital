using Microsoft.AspNetCore.Mvc;
using SimuladorApi.DTOs;
using SimuladorApi.Models;
using SimuladorApi.Data;
using SimuladorApi.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace SimuladorApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly TokenService _tokenService;
        private readonly PasswordResetService _passwordResetService;

        public AuthController(
            AppDbContext context,
            TokenService tokenService,
            PasswordResetService passwordResetService)
        {
            _context = context;
            _tokenService = tokenService;
            _passwordResetService = passwordResetService;
        }

        [HttpPost("register")]
        public IActionResult Register(RegisterDto request)
        {
            if (_context.Users.Any(u => u.Email == request.Email))
                return BadRequest("El usuario ya existe");

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            var user = new User
            {
                Name = request.Name,
                Email = request.Email,
                PasswordHash = passwordHash,
                Role = request.Role,
                MustChangePassword = false
            };

            _context.Users.Add(user);
            _context.SaveChanges();

            return Ok("Usuario registrado correctamente");
        }

        [HttpPost("login")]
        public IActionResult Login(LoginDto request)
        {
            var user = _context.Users.FirstOrDefault(u => u.Email == request.Email);

            if (user == null)
                return Unauthorized("Usuario no encontrado");

            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                return Unauthorized("Contraseña incorrecta");

            var token = _tokenService.CreateToken(user);

            return Ok(new LoginResponseDto
            {
                Message = "Login exitoso",
                Token = token,
                MustChangePassword = user.MustChangePassword
            });
        }

        [Authorize]
        [HttpPost("change-temporary-password")]
        public IActionResult ChangeTemporaryPassword(ChangeTemporaryPasswordDto request)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var user = _context.Users.FirstOrDefault(u => u.Id == userId);

            if (user == null)
                return NotFound("Usuario no encontrado.");

            if (!user.MustChangePassword)
                return BadRequest("No tienes un cambio de contraseña pendiente.");

            if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
                return BadRequest("La contraseña temporal no es correcta.");

            if (string.IsNullOrWhiteSpace(request.NewPassword) ||
                request.NewPassword.Length < 6)
            {
                return BadRequest("La nueva contraseña debe tener al menos 6 caracteres.");
            }

            if (request.NewPassword != request.ConfirmNewPassword)
                return BadRequest("La confirmación de contraseña no coincide.");

            if (request.CurrentPassword == request.NewPassword)
                return BadRequest("La nueva contraseña no puede ser igual a la temporal.");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            user.MustChangePassword = false;

            _context.SaveChanges();

            var token = _tokenService.CreateToken(user);

            return Ok(new LoginResponseDto
            {
                Message = "Contraseña actualizada correctamente.",
                Token = token,
                MustChangePassword = false
            });
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordRequestDto request)
        {
            var result = await _passwordResetService.GenerateResetTokenAsync(request.Email);

            return Ok(new
            {
                message = result.Message,
                resetUrl = result.ResetUrl
            });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordRequestDto request)
        {
            var result = await _passwordResetService.ResetPasswordAsync(
                request.Token,
                request.NewPassword
            );

            if (!result.Success)
                return BadRequest(result.Message);

            return Ok(result.Message);
        }
    }
}
