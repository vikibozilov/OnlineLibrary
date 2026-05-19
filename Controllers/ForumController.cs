using LibraryApp.Data;
using LibraryApp.DTOs;
using LibraryApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LibraryApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ForumController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ForumController(AppDbContext context)
        {
            _context = context;
        }

        // GET api/forum - всички теми
        [HttpGet]
        public async Task<IActionResult> GetTopics()
        {
            var topics = await _context.ForumTopics
                .Include(t => t.User)
                .Include(t => t.Posts)
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new ForumTopicResponseDTO
                {
                    Id = t.Id,
                    Title = t.Title,
                    Content = t.Content,
                    UserName = t.User != null ? t.User.Name : "Неизвестен",
                    CreatedAt = t.CreatedAt,
                    PostsCount = t.Posts.Count
                })
                .ToListAsync();

            return Ok(topics);
        }

        // GET api/forum/5 - детайли на тема с отговори
        [HttpGet("topic/{id}")]

        public async Task<IActionResult> GetTopic(int id)
        {
            var topic = await _context.ForumTopics
                .Include(t => t.User)
                .Include(t => t.Posts)
                    .ThenInclude(p => p.User)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (topic == null)
                return NotFound(new { message = "Темата не е намерена." });

            var result = new
            {
                id = topic.Id,
                title = topic.Title,
                content = topic.Content,
                userName = topic.User?.Name ?? "Неизвестен",
                createdAt = topic.CreatedAt,
                posts = topic.Posts
                    .OrderBy(p => p.CreatedAt)
                    .Select(p => new ForumPostResponseDTO
                    {
                        Id = p.Id,
                        Content = p.Content,
                        UserName = p.User != null ? p.User.Name : "Неизвестен",
                        CreatedAt = p.CreatedAt
                    }).ToList()
            };

            return Ok(result);
        }

        // POST api/forum - създай тема
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateTopic([FromBody] ForumTopicCreateDTO dto)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            if (string.IsNullOrWhiteSpace(dto.Title) || string.IsNullOrWhiteSpace(dto.Content))
                return BadRequest(new { message = "Заглавието и съдържанието са задължителни." });

            var topic = new ForumTopic
            {
                UserId = userId,
                Title = dto.Title,
                Content = dto.Content
            };

            _context.ForumTopics.Add(topic);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Темата е създадена успешно!", id = topic.Id });
        }

        // POST api/forum/post - отговори на тема
        [HttpPost("post")]
        [Authorize]
        public async Task<IActionResult> CreatePost([FromBody] ForumPostCreateDTO dto)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            if (string.IsNullOrWhiteSpace(dto.Content))
                return BadRequest(new { message = "Съдържанието е задължително." });

            var topic = await _context.ForumTopics.FindAsync(dto.TopicId);
            if (topic == null)
                return NotFound(new { message = "Темата не е намерена." });

            var post = new ForumPost
            {
                TopicId = dto.TopicId,
                UserId = userId,
                Content = dto.Content
            };

            _context.ForumPosts.Add(post);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Отговорът е добавен успешно!" });
        }

        // DELETE api/forum/5 - изтрий тема (само admin)
        [HttpDelete("{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> DeleteTopic(int id)
        {
            var topic = await _context.ForumTopics.FindAsync(id);
            if (topic == null)
                return NotFound(new { message = "Темата не е намерена." });

            _context.ForumTopics.Remove(topic);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Темата е изтрита успешно!" });
        }

        // DELETE api/forum/post/5 - изтрий отговор (само admin)
        [HttpDelete("post/{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> DeletePost(int id)
        {
            var post = await _context.ForumPosts.FindAsync(id);
            if (post == null)
                return NotFound(new { message = "Отговорът не е намерен." });

            _context.ForumPosts.Remove(post);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Отговорът е изтрит успешно!" });
        }
    }
}