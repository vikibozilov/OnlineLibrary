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
    public class FavoritesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public FavoritesController(AppDbContext context)
        {
            _context = context;
        }

        // GET api/favorites - моите любими книги
        [HttpGet]
        public async Task<IActionResult> GetFavorites()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var favorites = await _context.Favorites
                .Where(f => f.UserId == userId)
                .Include(f => f.Book)
                    .ThenInclude(b => b!.Author)
                .Include(f => f.Book)
                    .ThenInclude(b => b!.Category)
                .OrderByDescending(f => f.CreatedAt)
                .Select(f => new BookResponseDTO
                {
                    Id = f.Book!.Id,
                    Title = f.Book.Title,
                    AuthorName = f.Book.Author != null ? f.Book.Author.Name : null,
                    CategoryName = f.Book.Category != null ? f.Book.Category.Name : null,
                    CoverUrl = f.Book.CoverUrl,
                    PdfUrl = f.Book.PdfUrl,
                    PublishedYear = f.Book.PublishedYear,
                    AvailableCopies = f.Book.AvailableCopies,
                    TotalCopies = f.Book.TotalCopies
                })
                .ToListAsync();

            return Ok(favorites);
        }

        // POST api/favorites - добави в любими
        [HttpPost]
        public async Task<IActionResult> AddFavorite([FromBody] FavoriteDTO dto)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var book = await _context.Books.FindAsync(dto.BookId);
            if (book == null)
                return NotFound(new { message = "Книгата не е намерена." });

            var exists = await _context.Favorites
                .AnyAsync(f => f.UserId == userId && f.BookId == dto.BookId);
            if (exists)
                return BadRequest(new { message = "Книгата вече е в любимите." });

            _context.Favorites.Add(new Favorite
            {
                UserId = userId,
                BookId = dto.BookId
            });
            await _context.SaveChangesAsync();

            return Ok(new { message = "Книгата е добавена в любимите!" });
        }

        // DELETE api/favorites/5 - премахни от любими
        [HttpDelete("{bookId}")]
        public async Task<IActionResult> RemoveFavorite(int bookId)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var favorite = await _context.Favorites
                .FirstOrDefaultAsync(f => f.UserId == userId && f.BookId == bookId);
            if (favorite == null)
                return NotFound(new { message = "Книгата не е в любимите." });

            _context.Favorites.Remove(favorite);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Книгата е премахната от любимите!" });
        }

        // GET api/favorites/check/5 - провери дали книгата е в любими
        [HttpGet("check/{bookId}")]
        public async Task<IActionResult> CheckFavorite(int bookId)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var exists = await _context.Favorites
                .AnyAsync(f => f.UserId == userId && f.BookId == bookId);
            return Ok(new { isFavorite = exists });
        }
    }
}