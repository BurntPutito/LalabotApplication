using CommunityToolkit.Mvvm.ComponentModel;

namespace LalabotApplication.Services
{
    public static class AvatarHelper
    {
        //Total number of available avatars to choose
        public const int TotalAvatars = 6;

        // get avatar image from index
        public static string GetAvatarSource(int avatarIndex)
        {
            // ensuring index is within the valid range
            if (avatarIndex < 0 || avatarIndex >= TotalAvatars)
            {
                avatarIndex = 0; //defaults to first avatar if invalid
            }
            return $"avatar_{avatarIndex}.png";
        }

        // get all avatar sources as a list (for the picker screen)
        public static List<AvatarOption> GetAllAvatars()
        {
            var avatars = new List<AvatarOption>();

            for (int i = 0; i < TotalAvatars; i++)
            {
                avatars.Add(new AvatarOption
                {
                    Index = 1,
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
