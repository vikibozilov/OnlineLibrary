namespace LibraryApp.DTOs
{
    public class ReadingListAddDTO
    {
        public int BookId { get; set; }
        public string Status { get; set; } = "want_to_read";
        public string? Notes { get; set; }
    }

    public class ReadingListUpdateDTO
    {
        public string Status { get; set; } = "want_to_read";
        public string? Notes { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? FinishedAt { get; set; }
    }

    public class ReadingListResponseDTO
    {
        public int Id { get; set; }
        public int BookId { get; set; }
        public string BookTitle { get; set; } = string.Empty;
        public string? AuthorName { get; set; }
        public string? CoverUrl { get; set; }
        public string? CategoryName { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateOnly? StartedAt { get; set; }
        public DateOnly? FinishedAt { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? PdfUrl { get; set; }
    }

    public class ReadingStatsDTO
    {
        public int TotalBooks { get; set; }
        public int WantToRead { get; set; }
        public int CurrentlyReading { get; set; }
        public int Read { get; set; }
        public string? FavoriteCategory { get; set; }
        public int BooksReadThisYear { get; set; }
    }
}