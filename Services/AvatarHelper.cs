using CommunityToolkit.Mvvm.ComponentModel;

namespace LalabotApplication.Services
{
    public static class AvatarHelper
    {
        //Total number of available avatars to choose
        public const int TotalAvatars = 6;

        // Get avatar image from index OR custom URL
        public static string GetAvatarSource(int avatarIndex, string customAvatarUrl = "")
        {
            // If custom URL exists, use it
            if (!string.IsNullOrEmpty(customAvatarUrl))
            {
                return customAvatarUrl;
            }

            // Otherwise use default avatar
            // Ensuring index is within the valid range
            if (avatarIndex < 0 || avatarIndex >= TotalAvatars)
            {
                avatarIndex = 0; // Defaults to first avatar if invalid
            }
            return $"avatar_{avatarIndex}.png";
        }

        // Get all avatar sources as a list (for the picker screen)
        public static List<AvatarOption> GetAllAvatars()
        {
            var avatars = new List<AvatarOption>();

            for (int i = 0; i < TotalAvatars; i++)
            {
                avatars.Add(new AvatarOption
                {
                    Index = i,
                    ImageSource = GetAvatarSource(i)
                });
            }
            return avatars;
        }
    }

    // Helper class for avatar picker
    public partial class AvatarOption : ObservableObject
    {
        public int Index { get; set; }
        public string ImageSource { get; set; } = string.Empty;

        [ObservableProperty]
        private bool _isSelected = false;
    }
}