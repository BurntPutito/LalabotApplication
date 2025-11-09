using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LalabotApplication.Services;
using System.Collections.ObjectModel;

namespace LalabotApplication.Screens
{
    public partial class AvatarPickerScreenModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<AvatarOption> _avatars;

        [ObservableProperty]
        private AvatarOption _selectedAvatar;

        private int _initialAvatarIndex;

        public AvatarPickerScreenModel()
        {
            // Load all available avatars
            var avatarList = AvatarHelper.GetAllAvatars();
            Avatars = new ObservableCollection<AvatarOption>(avatarList);
        }

        // This will be called from EditProfileScreen to set current avatar
        public void SetCurrentAvatar(int avatarIndex)
        {
            _initialAvatarIndex = avatarIndex;

            // Mark the current avatar as selected
            foreach (var avatar in Avatars)
            {
                avatar.IsSelected = (avatar.Index == avatarIndex);
            }

            // Set the selected avatar
            SelectedAvatar = Avatars.FirstOrDefault(a => a.Index == avatarIndex);
        }

        partial void OnSelectedAvatarChanged(AvatarOption value)
        {
            // Update IsSelected for all avatars
            if (value != null)
            {
                foreach (var avatar in Avatars)
                {
                    avatar.IsSelected = (avatar.Index == value.Index);
                }
            }
        }

        [RelayCommand]
        private async Task ConfirmSelection()
        {
            if (SelectedAvatar != null)
            {
                // Pass the selected avatar index back to EditProfileScreen
                var navigationParameter = new Dictionary<string, object>
                {
                    { "SelectedAvatarIndex", SelectedAvatar.Index }
                };

                await Shell.Current.GoToAsync("..", navigationParameter);
            }
            else
            {
                await Shell.Current.DisplayAlert("No Selection", "Please select an avatar.", "OK");
            }
        }

        [RelayCommand]
        private async Task Cancel()
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}