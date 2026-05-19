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
    public class ReviewsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ReviewsController(AppDbContext context)
        {
            _context = context;
        }

        // GET api/reviews/book/5 - рецензии за книга
        [HttpGet("book/{bookId}")]
        public async Task<IActionResult> GetByBook(int bookId)
        {
            var reviews = await _context.Reviews
                .Include(r => r.User)
                .Include(r => r.Book)
                .Where(r => r.BookId == bookId)
                .Select(r => new ReviewResponseDTO
                {
                    Id = r.Id,
                    UserName = r.User != null ? r.User.Name : "",
                    BookTitle = r.Book != null ? r.Book.Title : "",
                    Rating = r.Rating,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAt
                })
                .ToListAsync();

            return Ok(reviews);
        }

        // POST api/reviews - добави рецензия
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create([FromBody] ReviewCreateDTO dto)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            // Провери дали книгата съществува
            var book = await _context.Books.FindAsync(dto.BookId);
            if (book == null)
                return NotFound(new { message = "Книгата не е намерена." });

            // Провери дали рейтингът е валиден
            if (dto.Rating < 1 || dto.Rating > 5)
                return BadRequest(new { message = "Оценката трябва да е между 1 и 5." });

            // Провери дали потребителят вече е писал рецензия
            var alreadyReviewed = await _context.Reviews.AnyAsync(r =>
                r.UserId == userId && r.BookId == dto.BookId);

            if (alreadyReviewed)
                return BadRequest(new { message = "Вече сте написали рецензия за тази книга." });

            var review = new Review
            {
                UserId = userId,
                BookId = dto.BookId,
                Rating = dto.Rating,
                Comment = dto.Comment,
                CreatedAt = DateTime.Now
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Рецензията е добавена успешно!" });
        }

        // DELETE api/reviews/5 - изтрий рецензия
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            var review = await _context.Reviews.FindAsync(id);
            if (review == null)
                return NotFound(new { message = "Рецензията не е намерена." });

            // Само авторът или admin може да изтрие
            if (review.UserId != userId && userRole != "admin")
                return Forbid();

            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Рецензията е изтрита успешно!" });
        }
    }
}