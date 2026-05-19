using LibraryApp.Data;
using LibraryApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LibraryApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NewBookNotificationsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public NewBookNotificationsController(AppDbContext context)
        {
            _context = context;
        }

        // GET api/newbooknotifications - моите известия
        [HttpGet]
        public async Task<IActionResult> GetMyNotifications()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var notifications = await _context.NewBookNotifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new {
                    id = n.Id,
                    message = n.Message,
                    isRead = n.IsRead,
                    createdAt = n.CreatedAt,
                    bookId = n.BookId
                })
                .ToListAsync();
            return Ok(notifications);
        }

        // GET api/newbooknotifications/unread - брой непрочетени
        [HttpGet("unread")]
        public async Task<IActionResult> GetUnreadCount()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var count = await _context.NewBookNotifications
                .CountAsync(n => n.UserId == userId && !n.IsRead);
            return Ok(new { count = count });
        }

        // PUT api/newbooknotifications/{id}/read - маркирай като прочетено
        [HttpPut("{id}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var notification = await _context.NewBookNotifications
                .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);
            if (notification == null)
                return NotFound(new { message = "Известието не е намерено." });
            notification.IsRead = true;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Маркирано като прочетено." });
        }

        // PUT api/newbooknotifications/read-all - маркирай всички
        [HttpPut("read-all")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var notifications = await _context.NewBookNotifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();
            notifications.ForEach(n => n.IsRead = true);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Всички са маркирани като прочетени." });
        }
    }
}