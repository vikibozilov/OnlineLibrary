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
    public class RecommendationsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public RecommendationsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetRecommendations()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var knownBookIds = await _context.ReadingList
                .Where(r => r.UserId == userId)
                .Select(r => r.BookId)
                .Union(_context.Favorites
                    .Where(f => f.UserId == userId)
                    .Select(f => f.BookId))
                .Distinct()
                .ToListAsync();

            var categoryIdsFromReading = await _context.ReadingList
                .Where(r => r.UserId == userId && r.Book != null && r.Book.CategoryId != null)
                .Include(r => r.Book)
                .Select(r => r.Book!.CategoryId!.Value)
                .Distinct()
                .ToListAsync();

            var categoryIdsFromFavorites = await _context.Favorites
                .Where(f => f.UserId == userId && f.Book != null && f.Book.CategoryId != null)
                .Include(f => f.Book)
                .Select(f => f.Book!.CategoryId!.Value)
                .Distinct()
                .ToListAsync();

            var categoryIdsFromReviews = await _context.Reviews
                .Where(r => r.UserId == userId && r.Book != null && r.Book.CategoryId != null)
                .Include(r => r.Book)
                .Select(r => r.Book!.CategoryId!.Value)
                .Distinct()
                .ToListAsync();

            var allCategoryIds = categoryIdsFromReading
                .Union(categoryIdsFromFavorites)
                .Union(categoryIdsFromReviews)
                .Distinct()
                .ToList();

            List<BookResponseDTO> recommendations;

            if (allCategoryIds.Any())
            {
                recommendations = await _context.Books
                    .Include(b => b.Author)
                    .Include(b => b.Category)
                    .Include(b => b.Reviews)
                    .Where(b =>
                        b.CategoryId != null &&
                        allCategoryIds.Contains(b.CategoryId.Value) &&
                        !knownBookIds.Contains(b.Id))
                    .OrderByDescending(b => b.Reviews.Any() ? b.Reviews.Average(r => r.Rating) : 0)
                    .Take(10)
                    .Select(b => new BookResponseDTO
                    {
                        Id = b.Id,
                        Title = b.Title,
                        Isbn = b.Isbn,
                        AuthorName = b.Author != null ? b.Author.Name : null,
                        CategoryName = b.Category != null ? b.Category.Name : null,
                        TotalCopies = b.TotalCopies,
                        AvailableCopies = b.AvailableCopies,
                        CoverUrl = b.CoverUrl,
                        Description = b.Description,
                        PublishedYear = b.PublishedYear,
                        AverageRating = b.Reviews.Any() ? b.Reviews.Average(r => r.Rating) : 0,
                        PdfUrl = b.PdfUrl
                    })
                    .ToListAsync();
            }
            else
            {
                recommendations = await _context.Books
                    .Include(b => b.Author)
                    .Include(b => b.Category)
                    .Include(b => b.Reviews)
                    .Where(b => !knownBookIds.Contains(b.Id))
                    .OrderByDescending(b => b.Reviews.Any() ? b.Reviews.Average(r => r.Rating) : 0)
                    .Take(10)
                    .Select(b => new BookResponseDTO
                    {
                        Id = b.Id,
                        Title = b.Title,
                        Isbn = b.Isbn,
                        AuthorName = b.Author != null ? b.Author.Name : null,
                        CategoryName = b.Category != null ? b.Category.Name : null,
                        TotalCopies = b.TotalCopies,
                        AvailableCopies = b.AvailableCopies,
                        CoverUrl = b.CoverUrl,
                        Description = b.Description,
                        PublishedYear = b.PublishedYear,
                        AverageRating = b.Reviews.Any() ? b.Reviews.Average(r => r.Rating) : 0,
                        PdfUrl = b.PdfUrl
                    })
                    .ToListAsync();
            }

            return Ok(recommendations);
        }
    }
}