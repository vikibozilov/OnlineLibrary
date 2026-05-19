using LibraryApp.Data;
using LibraryApp.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LibraryApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UsersController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetMe()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                return NotFound(new { message = "Потребителят не е намерен." });
            var readCount = await _context.ReadingList
                .CountAsync(r => r.UserId == userId && r.Status == "read");
            return Ok(new UserResponseDTO
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Role = user.Role,
                CreatedAt = user.CreatedAt,
                ActiveLoans = readCount
            });
        }

        [HttpGet]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> GetAll()
        {
            var users = await _context.Users.ToListAsync();
            var result = new List<UserResponseDTO>();
            foreach (var u in users)
            {
                var readCount = await _context.ReadingList
                    .CountAsync(r => r.UserId == u.Id && r.Status == "read");
                result.Add(new UserResponseDTO
                {
                    Id = u.Id,
                    Name = u.Name,
                    Email = u.Email,
                    Role = u.Role,
                    CreatedAt = u.CreatedAt,
                    ActiveLoans = readCount
                });
            }
            return Ok(result);
        }

        // PUT api/users/5/role - смени роля (само admin)
        [HttpPut("{id}/role")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> UpdateRole(int id, [FromBody] UserUpdateRoleDTO dto)
        {
            if (dto.Role != "member" && dto.Role != "admin")
                return BadRequest(new { message = "Ролята може да е само 'member' или 'admin'." });

            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound(new { message = "Потребителят не е намерен." });

            user.Role = dto.Role;
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Ролята е сменена на '{dto.Role}' успешно!" });
        }

        // DELETE api/users/5 - изтрий потребител (само admin)
        [HttpDelete("{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            if (id == userId)
                return BadRequest(new { message = "Не можете да изтриете собствения си акаунт." });

            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound(new { message = "Потребителят не е намерен." });

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Потребителят е изтрит успешно!" });
        }
    }
}