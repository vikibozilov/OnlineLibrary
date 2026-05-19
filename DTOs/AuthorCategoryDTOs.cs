namespace LibraryApp.DTOs
{
    public class AuthorCreateDTO
    {
        public string Name { get; set; } = string.Empty;
        public string? Bio { get; set; }
    }

    public class AuthorResponseDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Bio { get; set; }
        public int BooksCount { get; set; }
    }

    public class CategoryCreateDTO
    {
        public string Name { get; set; } = string.Empty;
        public string? Slug { get; set; }
    }

    public class CategoryResponseDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public int BooksCount { get; set; }
    }
}