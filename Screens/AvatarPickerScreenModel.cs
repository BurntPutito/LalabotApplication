using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace LalabotApplication.Screens
{
    [QueryProperty(nameof(CurrentAvatarIndex), "CurrentAvatarIndex")]
    public partial class AvatarPickerScreenModel : ObservableObject
    {
        [ObservableProperty]
        private int _selectedAvatarIndex = 0;

        private int _initialAvatarIndex = 0;

        public int CurrentAvatarIndex
        {
            set
            {
                _initialAvatarIndex = value;
                SelectedAvatarIndex = value;
            }
        }

        public int GetCurrentAvatarIndex() => _initialAvatarIndex;

        public void SetSelectedIndex(int index)
        {
            SelectedAvatarIndex = index;
        }

        [RelayCommand]
        private async Task ConfirmSelection()
        {
            var navigationParameter = new Dictionary<string, object>
            {
                { "SelectedAvatarIndex", SelectedAvatarIndex }
            };

            await Shell.Current.GoToAsync("..", navigationParameter);
        }

        [RelayCommand]
        private async Task Cancel()
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}