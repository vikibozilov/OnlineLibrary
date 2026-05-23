using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryApp.Models
{
    [Table("books")]
    public class Book
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("title")]
        [Required]
        public string Title { get; set; } = string.Empty;

        [Column("isbn")]
        public string? Isbn { get; set; }

        [Column("author_id")]
        public int? AuthorId { get; set; }

        [Column("category_id")]
        public int? CategoryId { get; set; }

        [Column("cover_url")]
        public string? CoverUrl { get; set; }

        [Column("description")]
        public string? Description { get; set; }

        [Column("published_year")]
        public int? PublishedYear { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Column("pdf_url")]
        public string? PdfUrl { get; set; }

        [ForeignKey("AuthorId")]
        public Author? Author { get; set; }

        [ForeignKey("CategoryId")]
        public Category? Category { get; set; }

        public ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
}