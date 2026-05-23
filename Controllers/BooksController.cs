using LibraryApp.Data;
using LibraryApp.DTOs;
using LibraryApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BooksController : ControllerBase
    {
        private readonly AppDbContext _context;

        public BooksController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? search, [FromQuery] int? categoryId, [FromQuery] int? authorId)
        {
            var query = _context.Books
                .Include(b => b.Author)
                .Include(b => b.Category)
                .Include(b => b.Reviews)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
                query = query.Where(b =>
                    b.Title.Contains(search) ||
                    (b.Author != null && b.Author.Name.Contains(search)));

            if (categoryId.HasValue)
                query = query.Where(b => b.CategoryId == categoryId);

            if (authorId.HasValue)
                query = query.Where(b => b.AuthorId == authorId);

            var books = await query.Select(b => new BookResponseDTO
            {
                Id = b.Id,
                Title = b.Title,
                Isbn = b.Isbn,
                AuthorName = b.Author != null ? b.Author.Name : null,
                CategoryName = b.Category != null ? b.Category.Name : null,
                CoverUrl = b.CoverUrl,
                Description = b.Description,
                PublishedYear = b.PublishedYear,
                AverageRating = b.Reviews.Any() ? b.Reviews.Average(r => r.Rating) : 0,
                PdfUrl = b.PdfUrl
            }).ToListAsync();

            return Ok(books);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var book = await _context.Books
                .Include(b => b.Author)
                .Include(b => b.Category)
                .Include(b => b.Reviews)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (book == null)
                return NotFound(new { message = "Книгата не е намерена." });

            var result = new BookResponseDTO
            {
                Id = book.Id,
                Title = book.Title,
                Isbn = book.Isbn,
                AuthorName = book.Author?.Name,
                AuthorBio = book.Author?.Bio,
                CategoryName = book.Category?.Name,
                CoverUrl = book.CoverUrl,
                Description = book.Description,
                PublishedYear = book.PublishedYear,
                AverageRating = book.Reviews.Any() ? book.Reviews.Average(r => r.Rating) : 0,
                PdfUrl = book.PdfUrl
                
            };

            return Ok(result);
        }

        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Create([FromBody] BookCreateDTO dto)
        {
            var book = new Book
            {
                Title = dto.Title,
                Isbn = dto.Isbn,
                AuthorId = dto.AuthorId,
                CategoryId = dto.CategoryId,
                CoverUrl = dto.CoverUrl,
                Description = dto.Description,
                PublishedYear = dto.PublishedYear,
                PdfUrl = dto.PdfUrl
            };

            _context.Books.Add(book);
            await _context.SaveChangesAsync();

            // Изпрати известие до всички потребители
            var allUsers = await _context.Users
                .Where(u => u.Role == "member")
                .ToListAsync();

            foreach (var user in allUsers)
            {
                _context.NewBookNotifications.Add(new NewBookNotification
                {
                    UserId = user.Id,
                    BookId = book.Id,
                    Message = $"Нова книга добавена: \"{book.Title}\""
                });
            }
            await _context.SaveChangesAsync();

            return Ok(new { message = "Книгата е добавена успешно!", id = book.Id });
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Update(int id, [FromBody] BookCreateDTO dto)
        {
            var book = await _context.Books.FindAsync(id);
            if (book == null)
                return NotFound(new { message = "Книгата не е намерена." });

            book.Title = dto.Title;
            book.Isbn = dto.Isbn;
            book.AuthorId = dto.AuthorId;
            book.CategoryId = dto.CategoryId;
            book.CoverUrl = dto.CoverUrl;
            book.Description = dto.Description;
            book.PublishedYear = dto.PublishedYear;
            book.PdfUrl = dto.PdfUrl;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Книгата е обновена успешно!" });
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var book = await _context.Books.FindAsync(id);
            if (book == null)
                return NotFound(new { message = "Книгата не е намерена." });

            _context.Books.Remove(book);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Книгата е изтрита успешно!" });
        }

        [HttpPost("upload-cover")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> UploadCover(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Моля изберете файл." });

            var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/webp" };
            if (!allowedTypes.Contains(file.ContentType.ToLower()))
                return BadRequest(new { message = "Позволени са само JPEG, PNG и WebP файлове." });

            var extension = Path.GetExtension(file.FileName);
            var fileName = $"{Guid.NewGuid()}{extension}";
            var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");

            if (!Directory.Exists(uploadsPath))
                Directory.CreateDirectory(uploadsPath);

            var filePath = Path.Combine(uploadsPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return Ok(new { url = $"/images/{fileName}" });
        }

        [HttpPost("upload-pdf")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> UploadPdf(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Моля изберете файл." });

            if (file.ContentType.ToLower() != "application/pdf")
                return BadRequest(new { message = "Позволени са само PDF файлове." });

            var fileName = Guid.NewGuid().ToString() + ".pdf";
            var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "pdfs");

            if (!Directory.Exists(uploadsPath))
                Directory.CreateDirectory(uploadsPath);

            var filePath = Path.Combine(uploadsPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return Ok(new { url = "/pdfs/" + fileName });
        }
    }
}