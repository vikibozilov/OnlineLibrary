namespace LibraryApp.DTOs
{
    public class ForumTopicCreateDTO
    {
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    public class ForumPostCreateDTO
    {
        public int TopicId { get; set; }
        public string Content { get; set; } = string.Empty;
    }

    public class ForumTopicResponseDTO
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int PostsCount { get; set; }
    }

    public class ForumPostResponseDTO
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}