using LibraryApp.Data;
using LibraryApp.DTOs;
using LibraryApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace LibraryApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;

        public AuthController(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDTO dto)
        {
            var exists = await _context.Users.AnyAsync(u => u.Email == dto.Email);
            if (exists)
                return BadRequest(new { message = "Имейлът вече е зает." });

            var user = new User
            {
                Name = dto.Name,
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = "member"
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Регистрацията е успешна!" });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null)
                return Unauthorized(new { message = "Невалиден имейл или парола." });

            if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                return Unauthorized(new { message = "Невалиден имейл или парола." });

            return Ok(new AuthResponseDTO
            {
                Token = GenerateToken(user),
                Name = user.Name,
                Email = user.Email,
                Role = user.Role
            });
        }

        private string GenerateToken(User user)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Secret"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim(ClaimTypes.Name, user.Name)
            };

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(24),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDTO dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null)
                return BadRequest(new { message = "Няма акаунт с този имейл." });

            var oldTokens = _context.PasswordResetTokens.Where(t => t.UserId == user.Id);
            _context.PasswordResetTokens.RemoveRange(oldTokens);

            var token = Guid.NewGuid().ToString("N");
            _context.PasswordResetTokens.Add(new PasswordResetToken
            {
                UserId = user.Id,
                Token = token,
                ExpiresAt = DateTime.Now.AddMinutes(30),
                Used = false
            });
            await _context.SaveChangesAsync();

            var resetLink = $"https://localhost:7268/index.html?token={token}&email={dto.Email}";
            var client = new SendGrid.SendGridClient(new SendGrid.SendGridClientOptions
            {
                ApiKey = Environment.GetEnvironmentVariable("SENDGRID_API_KEY"),
                HttpErrorAsException = false
            });
            var msg = new SendGrid.Helpers.Mail.SendGridMessage
            {
                From = new SendGrid.Helpers.Mail.EmailAddress(
                    Environment.GetEnvironmentVariable("SENDGRID_FROM_EMAIL"), "Онлайн Библиотека"),
                Subject = "Нулиране на парола",
                HtmlContent = $@"
<h2>Нулиране на парола</h2>
<p>Получихме заявка за нулиране на паролата за вашия акаунт.</p>
<p>Кликнете на бутона по-долу за да нулирате паролата си:</p>
<a href='{resetLink}' style='background:#2c1a0e;color:white;padding:12px 24px;border-radius:8px;text-decoration:none;display:inline-block;margin:16px 0;'>Нулирай паролата</a>
<p>Или копирайте този линк: {resetLink}</p>
<p>Линкът е валиден 30 минути.</p>
<p>Ако не сте поискали нулиране на парола, игнорирайте този имейл.</p>"
            };
            msg.AddTo(new SendGrid.Helpers.Mail.EmailAddress(dto.Email, user.Name));

            try
            {
                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                await client.SendEmailAsync(msg, cts.Token);
                return Ok(new { message = "Имейлът е изпратен!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Грешка при изпращане.", error = ex.Message });
            }
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDTO dto)
        {
            if (dto.NewPassword != dto.ConfirmPassword)
                return BadRequest(new { message = "Паролите не съвпадат." });

            if (dto.NewPassword.Length < 8)
                return BadRequest(new { message = "Паролата трябва да е поне 8 символа." });

            var resetToken = await _context.PasswordResetTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Token == dto.Code && t.User!.Email == dto.Email);

            if (resetToken == null)
                return BadRequest(new { message = "Невалиден линк за възстановяване." });

            if (resetToken.Used)
                return BadRequest(new { message = "Този линк вече е използван." });

            if (resetToken.ExpiresAt < DateTime.Now)
                return BadRequest(new { message = "Линкът е изтекъл. Моля поискайте нов." });

            resetToken.User!.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            resetToken.Used = true;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Паролата е сменена успешно!" });
        }

        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDTO dto)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound(new { message = "Потребителят не е намерен." });

            if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
                return BadRequest(new { message = "Грешна текуща парола." });

            if (dto.NewPassword != dto.ConfirmPassword)
                return BadRequest(new { message = "Новите пароли не съвпадат." });

            if (dto.NewPassword.Length < 8)
                return BadRequest(new { message = "Паролата трябва да е поне 8 символа." });

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Паролата е сменена успешно!" });
        }
    }
}