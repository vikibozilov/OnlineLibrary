using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryApp.Models
{
    [Table("forum_posts")]
    public class ForumPost
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("topic_id")]
        public int TopicId { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [Column("content")]
        public string Content { get; set; } = string.Empty;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("TopicId")]
        public ForumTopic? Topic { get; set; }

        [ForeignKey("UserId")]
        public User? User { get; set; }
    }
}