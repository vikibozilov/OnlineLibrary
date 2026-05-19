namespace LibraryApp.DTOs
{
    public class UserResponseDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int ActiveLoans { get; set; }
    }

    public class UserUpdateRoleDTO
    {
        public string Role { get; set; } = string.Empty;
    }
}