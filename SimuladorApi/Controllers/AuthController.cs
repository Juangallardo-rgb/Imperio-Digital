using Microsoft.AspNetCore.Mvc;
using SimuladorApi.DTOs;
using SimuladorApi.Models;
using SimuladorApi.Data;
using SimuladorApi.Services;

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
                Role = request.Role
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

            return Ok(new
            {
                message = "Login exitoso",
                token = token
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