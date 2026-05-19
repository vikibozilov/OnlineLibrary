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
    public class AuthorsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AuthorsController(AppDbContext context)
        {
            _context = context;
        }

        // GET api/authors - всички автори
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var authors = await _context.Authors
                .Include(a => a.Books)
                .Select(a => new AuthorResponseDTO
                {
                    Id = a.Id,
                    Name = a.Name,
                    Bio = a.Bio,
                    BooksCount = a.Books.Count
                })
                .ToListAsync();

            return Ok(authors);
        }

        // GET api/authors/5 - един автор
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var author = await _context.Authors
                .Include(a => a.Books)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (author == null)
                return NotFound(new { message = "Авторът не е намерен." });

            return Ok(new AuthorResponseDTO
            {
                Id = author.Id,
                Name = author.Name,
                Bio = author.Bio,
                BooksCount = author.Books.Count
            });
        }

        // POST api/authors - добави автор (само admin)
        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Create([FromBody] AuthorCreateDTO dto)
        {
            if (string.IsNullOrEmpty(dto.Name))
                return BadRequest(new { message = "Името е задължително." });

            var author = new Author
            {
                Name = dto.Name,
                Bio = dto.Bio
            };

            _context.Authors.Add(author);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Авторът е добавен успешно!", id = author.Id });
        }

        // PUT api/authors/5 - редактирай автор (само admin)
        [HttpPut("{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Update(int id, [FromBody] AuthorCreateDTO dto)
        {
            var author = await _context.Authors.FindAsync(id);
            if (author == null)
                return NotFound(new { message = "Авторът не е намерен." });

            author.Name = dto.Name;
            author.Bio = dto.Bio;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Авторът е обновен успешно!" });
        }

        // DELETE api/authors/5 - изтрий автор (само admin)
        [HttpDelete("{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var author = await _context.Authors.FindAsync(id);
            if (author == null)
                return NotFound(new { message = "Авторът не е намерен." });

            _context.Authors.Remove(author);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Авторът е изтрит успешно!" });
        }
    }
}