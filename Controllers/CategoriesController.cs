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
    public class CategoriesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CategoriesController(AppDbContext context)
        {
            _context = context;
        }

        // GET api/categories - всички категории
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var categories = await _context.Categories
                .Include(c => c.Books)
                .Select(c => new CategoryResponseDTO
                {
                    Id = c.Id,
                    Name = c.Name,
                    Slug = c.Slug,
                    BooksCount = c.Books.Count
                })
                .ToListAsync();

            return Ok(categories);
        }

        // POST api/categories - добави категория (само admin)
        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Create([FromBody] CategoryCreateDTO dto)
        {
            if (string.IsNullOrEmpty(dto.Name))
                return BadRequest(new { message = "Името е задължително." });

            // Автоматично генериране на slug от името
            var slug = string.IsNullOrEmpty(dto.Slug)
                ? dto.Name.ToLower()
                    .Replace(" ", "-")
                    .Replace("а", "a").Replace("б", "b").Replace("в", "v")
                    .Replace("г", "g").Replace("д", "d").Replace("е", "e")
                    .Replace("ж", "zh").Replace("з", "z").Replace("и", "i")
                    .Replace("й", "y").Replace("к", "k").Replace("л", "l")
                    .Replace("м", "m").Replace("н", "n").Replace("о", "o")
                    .Replace("п", "p").Replace("р", "r").Replace("с", "s")
                    .Replace("т", "t").Replace("у", "u").Replace("ф", "f")
                    .Replace("х", "h").Replace("ц", "ts").Replace("ч", "ch")
                    .Replace("ш", "sh").Replace("щ", "sht").Replace("ъ", "a")
                    .Replace("ь", "y").Replace("ю", "yu").Replace("я", "ya")
                : dto.Slug.ToLower().Replace(" ", "-");

            var exists = await _context.Categories.AnyAsync(c => c.Slug == slug);
            if (exists)
                return BadRequest(new { message = "Категория с това име вече съществува." });

            var category = new Category { Name = dto.Name, Slug = slug };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Категорията е добавена успешно!", id = category.Id });
        }

        // PUT api/categories/5 - редактирай категория (само admin)
        [HttpPut("{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Update(int id, [FromBody] CategoryCreateDTO dto)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
                return NotFound(new { message = "Категорията не е намерена." });

            category.Name = dto.Name;
            if (!string.IsNullOrEmpty(dto.Slug))
                category.Slug = dto.Slug.ToLower().Replace(" ", "-");

            await _context.SaveChangesAsync();

            return Ok(new { message = "Категорията е обновена успешно!" });
        }

        // DELETE api/categories/5 - изтрий категория (само admin)
        [HttpDelete("{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
                return NotFound(new { message = "Категорията не е намерена." });

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Категорията е изтрита успешно!" });
        }
    }
}