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
    [Authorize]
    public class ReadingListController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ReadingListController(AppDbContext context)
        {
            _context = context;
        }

        // GET api/readinglist - моят книжен дневник
        [HttpGet]
        public async Task<IActionResult> GetMyList()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var list = await _context.ReadingList
                .Include(r => r.Book)
                    .ThenInclude(b => b!.Author)
                .Include(r => r.Book)
                    .ThenInclude(b => b!.Category)
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            var result = list.Select(r => new ReadingListResponseDTO
            {
                Id = r.Id,
                BookId = r.BookId,
                BookTitle = r.Book != null ? r.Book.Title : "",
                AuthorName = r.Book?.Author?.Name,
                CoverUrl = r.Book?.CoverUrl,
                CategoryName = r.Book?.Category?.Name,
                Status = r.Status,
                StartedAt = r.StartedAt.HasValue ? DateOnly.FromDateTime(r.StartedAt.Value) : null,
                FinishedAt = r.FinishedAt.HasValue ? DateOnly.FromDateTime(r.FinishedAt.Value) : null,
                Notes = r.Notes,
                CreatedAt = r.CreatedAt,
                PdfUrl = r.Book != null ? r.Book.PdfUrl : null
            }).ToList();

            return Ok(result);
        }

        // GET api/readinglist/stats - статистика
        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var list = await _context.ReadingList
                .Include(r => r.Book)
                    .ThenInclude(b => b!.Category)
                .Where(r => r.UserId == userId)
                .ToListAsync();

            var currentYear = DateTime.Now.Year;

            var favoriteCategory = list
                .Where(r => r.Status == "read" && r.Book?.Category != null)
                .GroupBy(r => r.Book!.Category!.Name)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault()?.Key;

            var stats = new ReadingStatsDTO
            {
                TotalBooks = list.Count,
                WantToRead = list.Count(r => r.Status == "want_to_read"),
                CurrentlyReading = list.Count(r => r.Status == "reading"),
                Read = list.Count(r => r.Status == "read"),
                FavoriteCategory = favoriteCategory,
                BooksReadThisYear = list.Count(r =>
                    r.Status == "read" &&
                    r.FinishedAt.HasValue &&
                    r.FinishedAt.Value.Year == currentYear)
            };

            return Ok(stats);
        }

        // POST api/readinglist - добави книга
        [HttpPost]
        public async Task<IActionResult> Add([FromBody] ReadingListAddDTO dto)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var book = await _context.Books.FindAsync(dto.BookId);
            if (book == null)
                return NotFound(new { message = "Книгата не е намерена." });

            var exists = await _context.ReadingList.AnyAsync(r =>
                r.UserId == userId && r.BookId == dto.BookId);

            if (exists)
                return BadRequest(new { message = "Книгата вече е в дневника ти." });

            var entry = new ReadingList
            {
                UserId = userId,
                BookId = dto.BookId,
                Status = dto.Status,
                Notes = dto.Notes,
                StartedAt = dto.Status == "reading" ? DateTime.Now : null,
                FinishedAt = dto.Status == "read" ? DateTime.Now : null
            };

            _context.ReadingList.Add(entry);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Книгата е добавена в дневника!" });
        }

        // PUT api/readinglist/5 - обнови статус
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] ReadingListUpdateDTO dto)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var entry = await _context.ReadingList
                .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

            if (entry == null)
                return NotFound(new { message = "Записът не е намерен." });

            entry.Status = dto.Status;
            entry.Notes = dto.Notes;

            if (dto.Status == "reading" && entry.StartedAt == null)
                entry.StartedAt = DateTime.Now;

            if (dto.Status == "read" && entry.FinishedAt == null)
                entry.FinishedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Статусът е обновен!" });
        }

        // DELETE api/readinglist/5 - премахни от дневника
        [HttpDelete("{id}")]
        public async Task<IActionResult> Remove(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var entry = await _context.ReadingList
                .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

            if (entry == null)
                return NotFound(new { message = "Записът не е намерен." });

            _context.ReadingList.Remove(entry);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Книгата е премахната от дневника." });
        }
    }
}