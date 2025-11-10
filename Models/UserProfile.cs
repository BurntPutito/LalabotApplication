namespace LalabotApplication.Models
{
    internal class UserProfile
    {
        public string Uid { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int ProfileAvatarIndex { get; set; } = 0;
        public string CustomAvatarUrl { get; set; } = string.Empty;
    }
}
