namespace LibraryApp.DTOs
{
    public class BookCreateDTO
    {
        public string Title { get; set; } = string.Empty;
        public string? Isbn { get; set; }
        public int? AuthorId { get; set; }
        public int? CategoryId { get; set; }
        public int TotalCopies { get; set; } = 1;
        public string? CoverUrl { get; set; }
        public string? Description { get; set; }
        public int? PublishedYear { get; set; }
        public string? PdfUrl { get; set; }
    }

    public class BookResponseDTO
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Isbn { get; set; }
        public string? AuthorName { get; set; }
        public string? CategoryName { get; set; }
        public int TotalCopies { get; set; }
        public int AvailableCopies { get; set; }
        public string? CoverUrl { get; set; }
        public string? Description { get; set; }
        public int? PublishedYear { get; set; }
        public double AverageRating { get; set; }
        public string? PdfUrl { get; set; }
        public string? AuthorBio { get; set; }

    }
}