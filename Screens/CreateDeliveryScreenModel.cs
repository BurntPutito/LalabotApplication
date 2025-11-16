using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Database.Query;
using System.Collections.ObjectModel;

namespace LalabotApplication.Screens
{
    public partial class CreateDeliveryScreenModel : ObservableObject
    {
        private readonly FirebaseAuthClient _authClient;
        private readonly FirebaseClient _firebaseDb;

        private ObservableCollection<UserInfo> _allUsers = new();

        [ObservableProperty]
        private ObservableCollection<UserInfo> _filteredUsers = new();

        [ObservableProperty]
        private UserInfo _selectedReceiver;

        [ObservableProperty]
        private string _receiverSearchText = string.Empty;

        [ObservableProperty]
        private int _selectedDestinationIndex = -1;

        [ObservableProperty]
        private int _selectedPickupIndex = -1;

        [ObservableProperty]
        private int _selectedCategoryIndex = -1;

        [ObservableProperty]
        private string _message = string.Empty;

        public bool HasFilteredUsers => FilteredUsers?.Count > 0 && SelectedReceiver == null;
        public bool HasSelectedReceiver => SelectedReceiver != null;

        public CreateDeliveryScreenModel(FirebaseAuthClient authClient, FirebaseClient firebaseDb)
        {
            _authClient = authClient;
            _firebaseDb = firebaseDb;
            _ = LoadAvailableUsers();
        }

        partial void OnReceiverSearchTextChanged(string value)
        {
            FilterUsers(value);
        }

        private void FilterUsers(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                FilteredUsers.Clear();
                OnPropertyChanged(nameof(HasFilteredUsers));
                return;
            }

            var filtered = _allUsers
                .Where(u => u.Username.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                           u.Email.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                .Take(5) // Show max 5 suggestions
                .ToList();

            FilteredUsers.Clear();
            foreach (var user in filtered)
            {
                FilteredUsers.Add(user);
            }

            OnPropertyChanged(nameof(HasFilteredUsers));
        }

        [RelayCommand]
        private void SelectReceiver(UserInfo user)
        {
            SelectedReceiver = user;
            ReceiverSearchText = user.Username;
            FilteredUsers.Clear();
            OnPropertyChanged(nameof(HasFilteredUsers));
            OnPropertyChanged(nameof(HasSelectedReceiver));
        }

        [RelayCommand]
        private void ClearReceiver()
        {
            SelectedReceiver = null;
            ReceiverSearchText = string.Empty;
            FilteredUsers.Clear();
            OnPropertyChanged(nameof(HasFilteredUsers));
            OnPropertyChanged(nameof(HasSelectedReceiver));

        }

        private async Task LoadAvailableUsers()
        {
            try
            {
                var currentUser = _authClient.User;
                if (currentUser == null) return;

                // Get all users from Firebase
                var usersData = await _firebaseDb
                    .Child("users")
                    .OnceAsync<UserData>();

                _allUsers.Clear();

                foreach (var user in usersData)
                {
                    // Don't include current user
                    if (user.Key != currentUser.Uid)
                    {
                        _allUsers.Add(new UserInfo
                        {
                            Uid = user.Key,
                            Username = user.Object.Username ?? "Unknown",
                            Email = user.Object.Email ?? ""
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", $"Failed to load users: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        private async Task CreateDelivery()
        {
            // Validation
            if (SelectedReceiver == null)
            {
                await Shell.Current.DisplayAlert("Validation Error", "Please select a receiver.", "OK");
                return;
            }

            if (SelectedDestinationIndex == -1)
            {
                await Shell.Current.DisplayAlert("Validation Error", "Please select a destination.", "OK");
                return;
            }

            if (SelectedCategoryIndex == -1)
            {
                await Shell.Current.DisplayAlert("Validation Error", "Please select a file category.", "OK");
                return;
            }

            if (SelectedPickupIndex == -1)
            {
                await Shell.Current.DisplayAlert("Validation Error", "Please select a pickup location.", "OK");
                return;
            }

            try
            {
                var user = _authClient.User;
                if (user == null)
                {
                    await Shell.Current.DisplayAlert("Error", "You must be logged in to create a delivery.", "OK");
                    return;
                }

                // Get sender username
                var senderData = await _firebaseDb
                    .Child("users")
                    .Child(user.Uid)
                    .Child("Username")
                    .OnceSingleAsync<string>();

                string senderName = senderData ?? "Unknown";

                // Get available compartment
                int compartment = await GetAvailableCompartment();

                if (compartment == -1)
                {
                    await Shell.Current.DisplayAlert("Robot Full", "All compartments are occupied. Please wait for deliveries to complete.", "OK");
                    return;
                }

                // Generate unique delivery ID and verification code
                string deliveryId = $"del_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
                string verificationCode = GenerateVerificationCode();
                int destination = SelectedDestinationIndex + 1;

                // Adjust destination number if it's >= pickup room
                int pickupRoom = SelectedPickupIndex + 1;
                if (destination >= pickupRoom)
                {
                    destination++; // Skip the pickup room number
                }

                // Define category
                string[] categories = { "Documents", "Forms", "Reports", "Confidentials", "Exam Questionnaire", "Answer Sheet", "Files", "Other" };
                string category = categories[SelectedCategoryIndex];

                // ph timezone date variable settings
                var philippineTime = TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.UtcNow,
                TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time") // Philippines uses this timezone ID
                );
                // Create delivery object
                var delivery = new
                {
                    id = deliveryId,
                    sender = senderName,
                    senderUid = user.Uid,
                    receiver = SelectedReceiver.Username,
                    receiverUid = SelectedReceiver.Uid,
                    pickup = SelectedPickupIndex + 1,
                    destination = destination,
                    compartment = compartment,
                    category = category,
                    message = Message,
                    verificationCode = verificationCode,
                    status = "pending",
                    currentLocation = 0,
                    progressStage = 0,
                    createdAt = philippineTime.ToString("o"),
                    arrivedAt = (string)null,
                    completedAt = (string)null,

                    // for delivery status tracking
                    filesConfirmed = false,
                    confirmationDeadline = (string)null,
                    readyForPickup = false,
                    filesReceived = false
                };

                // Save delivery
                await _firebaseDb
                    .Child("delivery_requests")
                    .Child(deliveryId)
                    .PutAsync(delivery);

                // Update robot status
                var currentCompartments = await _firebaseDb
                    .Child("robot_status")
                    .Child("currentDeliveries")
                    .OnceSingleAsync<CompartmentStatus>();

                await _firebaseDb
                    .Child("robot_status")
                    .Child("currentDeliveries")
                    .PutAsync(new
                    {
                        compartment1 = compartment == 1 ? deliveryId : (currentCompartments?.compartment1 ?? ""),
                        compartment2 = compartment == 2 ? deliveryId : (currentCompartments?.compartment2 ?? ""),
                        compartment3 = compartment == 3 ? deliveryId : (currentCompartments?.compartment3 ?? "")
                    });

                // Create notification for receiver
                await _firebaseDb
                    .Child("notifications")
                    .Child(SelectedReceiver.Uid)
                    .Child(deliveryId)
                    .PutAsync(new
                    {
                        deliveryId = deliveryId,
                        from = senderName,
                        message = $"New delivery from {senderName}",
                        verificationCode = verificationCode,
                        destination = destination,
                        timestamp = DateTime.UtcNow.ToString("o"),
                        read = false
                    });

                // Show success
                await Shell.Current.DisplayAlert(
                    "Delivery Created!",
                    $"Delivery to {SelectedReceiver.Username}\n\n" +
                    $"Verification Code: {verificationCode}\n\n" +
                    $"The receiver will be notified automatically.",
                    "OK");

                // Navigate back
                await Shell.Current.GoToAsync("//HomeScreen");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", $"Failed to create delivery: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        private async Task Cancel()
        {
            await Shell.Current.GoToAsync("//HomeScreen");
        }

        private async Task<int> GetAvailableCompartment()
        {
            try
            {
                var compartments = await _firebaseDb
                    .Child("robot_status")
                    .Child("currentDeliveries")
                    .OnceSingleAsync<CompartmentStatus>();

                if (compartments == null)
                {
                    await _firebaseDb
                        .Child("robot_status")
                        .Child("currentDeliveries")
                        .PutAsync(new
                        {
                            compartment1 = "",
                            compartment2 = "",
                            compartment3 = ""
                        });
                    return 1;
                }

                if (string.IsNullOrEmpty(compartments.compartment1)) return 1;
                if (string.IsNullOrEmpty(compartments.compartment2)) return 2;
                if (string.IsNullOrEmpty(compartments.compartment3)) return 3;

                return -1;
            }
            catch
            {
                return 1;
            }
        }

        private string GenerateVerificationCode()
        {
            Random random = new Random();
            return random.Next(1000, 9999).ToString();
        }

        public List<string> AvailableDestinations
        {
            get
            {
                var allRooms = new List<string> { "Room 1", "Room 2", "Room 3", "Room 4" };

                if (SelectedPickupIndex == -1)
                {
                    return allRooms; // Show all if no pickup selected yet
                }

                // Remove the selected pickup room from destinations
                allRooms.RemoveAt(SelectedPickupIndex);
                return allRooms;
            }
        }

        partial void OnSelectedPickupIndexChanged(int value)
        {
            // Reset destination when pickup changes
            SelectedDestinationIndex = -1;
            OnPropertyChanged(nameof(AvailableDestinations));
        }
    }

    // Helper classes
    public class UserInfo
    {
        public string Uid { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
    }

    public class UserData
    {
        public string Username { get; set; }
        public string Email { get; set; }
    }

    public class CompartmentStatus
    {
        public string compartment1 { get; set; }
        public string compartment2 { get; set; }
        public string compartment3 { get; set; }
    }

}
