using Microsoft.Maui.Controls.Shapes;

namespace LalabotApplication.Screens;

public partial class AvatarPickerScreen : ContentPage
{
    private readonly AvatarPickerScreenModel _viewModel;
    private int _currentSelectedIndex = 0;

    public AvatarPickerScreen(AvatarPickerScreenModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;

        // Subscribe to property changes
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    private void OnViewModelPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        // When custom photo is uploaded, clear preset selections
        if (e.PropertyName == nameof(_viewModel.SelectedAvatarUrl))
        {
            if (!string.IsNullOrEmpty(_viewModel.SelectedAvatarUrl))
            {
                // Custom photo selected, hide all preset selections
                ClearAllSelections();
            }
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Check if custom photo is selected
        if (!string.IsNullOrEmpty(_viewModel.GetCurrentAvatarUrl()))
        {
            // Custom photo is active, don't select any preset
            ClearAllSelections();
        }
        else
        {
            // Set initial selection based on current avatar
            _currentSelectedIndex = _viewModel.GetCurrentAvatarIndex();
            UpdateSelection(_currentSelectedIndex);
        }
    }

    private void OnAvatarTapped(object sender, TappedEventArgs e)
    {
        if (sender is Grid grid)
        {
            // Find which avatar was tapped based on Grid position
            var row = Grid.GetRow(grid);
            var col = Grid.GetColumn(grid);
            int index = (row * 3) + col; // Calculate index from grid position

            _currentSelectedIndex = index;
            _viewModel.SetSelectedIndex(index);
            UpdateSelection(index);
        }
    }

    private void UpdateSelection(int selectedIndex)
    {
        // Hide all selections first
        ClearAllSelections();

        // Show selected one
        var selectedBorderToShow = this.FindByName<RoundRectangle>($"SelectedBorder{selectedIndex}");
        var checkToShow = this.FindByName<Grid>($"Check{selectedIndex}");

        if (selectedBorderToShow != null) selectedBorderToShow.IsVisible = true;
        if (checkToShow != null) checkToShow.IsVisible = true;
    }

    private void ClearAllSelections()
    {
        // Hide all preset avatar selections
        for (int i = 0; i < 6; i++)
        {
            var selectedBorder = this.FindByName<RoundRectangle>($"SelectedBorder{i}");
            var check = this.FindByName<Grid>($"Check{i}");

            if (selectedBorder != null) selectedBorder.IsVisible = false;
            if (check != null) check.IsVisible = false;
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
    }
}