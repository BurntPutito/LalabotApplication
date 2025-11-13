using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Database.Query;
using LalabotApplication.Models;
using LalabotApplication.Services;
using Plugin.Maui.Audio;
using System.Collections.ObjectModel;

namespace LalabotApplication.Screens
{
    public partial class HomeScreenModel : ObservableObject
    {
        private readonly FirebaseAuthClient _authClient;
        private readonly FirebaseClient _firebaseDb;
        private readonly IAudioManager _audioManager;

        [ObservableProperty]
        private string _username = "User";

        [ObservableProperty]
        private ObservableCollection<DeliveryCardInfo> _outgoingDeliveries = new();

        [ObservableProperty]
        private ObservableCollection<DeliveryCardInfo> _incomingDeliveries = new();

        [ObservableProperty]
        private DeliveryCardInfo _currentOutgoingDelivery;

        [ObservableProperty]
        private DeliveryCardInfo _currentIncomingDelivery;

        [ObservableProperty]
        private bool _isRefreshing = false;

        public bool HasOutgoingDeliveries => OutgoingDeliveries?.Count > 0;
        public bool HasIncomingDeliveries => IncomingDeliveries?.Count > 0;
        public bool HasNoDeliveries => !HasOutgoingDeliveries && !HasIncomingDeliveries;

        private HashSet<string> _processedNotifications = new HashSet<string>();

        public HomeScreenModel(FirebaseAuthClient authClient, FirebaseClient firebaseDb, IAudioManager audioManager)
        {
            _authClient = authClient;
            _firebaseDb = firebaseDb;
            _audioManager = audioManager;

            _ = LoadUserInfo();
            _ = LoadDeliveries();
            _ = ListenForNewDeliveries();
        }

        [RelayCommand]
        private async Task TestNotification()
        {
            try
            {
                // Test sound
                await PlayNotificationSound();

                // Test vibration
                Vibrate();

                // Test popup
                var testNotification = new NotificationData
                {
                    deliveryId = "test_123",
                    from = "Test Sender",
                    verificationCode = "9999",
                    destination = 1,
                    timestamp = DateTime.UtcNow.ToString("o"),
                    read = false
                };

                await ShowNotificationPopup(testNotification);
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Test Failed", ex.Message, "OK");
            }
        }

        [RelayCommand]
        private async Task Refresh()
        {
            IsRefreshing = true;

            await LoadUserInfo();
            await LoadDeliveries();

            IsRefreshing = false;
        }

        [ObservableProperty]
        private string _avatarSource = "avatar_0.png";

        private async Task LoadUserInfo()
        {
            try
            {
                var user = _authClient.User;

                if (user != null)
                {
                    // Get username from Firebase Database
                    var userData = await _firebaseDb
                        .Child("users")
                        .Child(user.Uid)
                        .OnceSingleAsync<UserProfile>();

                    if (userData != null)
                    {
                        Username = userData.Username ?? "User";

                        // Handle custom avatar URL
                        AvatarSource = AvatarHelper.GetAvatarSource(
                            userData.ProfileAvatarIndex,
                            userData.CustomAvatarUrl ?? string.Empty);
                    }
                }
            }
            catch (Exception ex)
            {
                // If error, keep defaults
                Username = "User";
                AvatarSource = "avatar_0.png";
            }
        }

        [RelayCommand]
        private async Task NavigateToProfile()
        {
            await Shell.Current.GoToAsync("///ProfileScreen");
        }

        private async Task LoadDeliveries()
        {
            try
            {
                var user = _authClient.User;
                if (user == null) return;

                // Get all active delivery requests
                var deliveriesData = await _firebaseDb
                    .Child("delivery_requests")
                    .OnceAsync<DeliveryData>();

                OutgoingDeliveries.Clear();
                IncomingDeliveries.Clear();

                foreach (var delivery in deliveriesData)
                {
                    var data = delivery.Object;

                    // Skip completed deliveries
                    if (data.status == "completed") continue;

                    var cardInfo = new DeliveryCardInfo
                    {
                        Id = data.id,
                        Sender = data.sender,
                        Receiver = data.receiver,
                        Pickup = data.pickup,
                        Destination = data.destination,
                        Category = data.category ?? "Files",
                        VerificationCode = data.verificationCode,
                        Status = data.status ?? "pending",
                        Message = data.message,
                        CurrentLocation = data.currentLocation,      // NEW
                        ProgressStage = data.progressStage           // NEW
                    };

                    // Categorize as outgoing or incoming
                    if (data.senderUid == user.Uid)
                    {
                        OutgoingDeliveries.Add(cardInfo);
                    }
                    else if (data.receiverUid == user.Uid)
                    {
                        IncomingDeliveries.Add(cardInfo);
                    }
                }

                OnPropertyChanged(nameof(HasOutgoingDeliveries));
                OnPropertyChanged(nameof(HasIncomingDeliveries));
                OnPropertyChanged(nameof(HasNoDeliveries));
            }
            catch (Exception ex)
            {
                // Handle error silently or show message
            }
        }

        private async Task ListenForNewDeliveries()
        {
            try
            {
                var user = _authClient.User;
                if (user == null) return;

                // Check for notifications every 3 seconds
                Device.StartTimer(TimeSpan.FromSeconds(3), () =>
                {
                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        try
                        {
                            var notifications = await _firebaseDb
                                .Child("notifications")
                                .Child(user.Uid)
                                .OnceAsync<NotificationData>();

                            foreach (var notification in notifications)
                            {
                                // Check if notification is unread AND we haven't processed it yet
                                if (notification.Object != null &&
                                    !notification.Object.read &&
                                    !_processedNotifications.Contains(notification.Key))
                                {
                                    // Mark as processed immediately to prevent duplicates
                                    _processedNotifications.Add(notification.Key);

                                    // Play notification sound & vibrate
                                    await PlayNotificationSound();
                                    Vibrate();

                                    // Show notification popup
                                    await ShowNotificationPopup(notification.Object);

                                    // Mark as read in Firebase
                                    await _firebaseDb
                                        .Child("notifications")
                                        .Child(user.Uid)
                                        .Child(notification.Key)
                                        .Child("read")
                                        .PutAsync(true);

                                    // Reload deliveries
                                    await LoadDeliveries();
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            // Ignore errors in timer
                        }
                    });

                    return true; // Keep timer running
                });
            }
            catch (Exception ex)
            {
                // Handle error
            }
        }

        private async Task PlayNotificationSound()
        {
            try
            {
                // put a notification.mp3 to Resources/Raw:
                var audioStream = await FileSystem.OpenAppPackageFileAsync("notification.mp3");
                var player = _audioManager.CreatePlayer(audioStream);
                player.Play();
            }
            catch (Exception ex)
            {
                // Sound failed, continue anyway
            }
        }

        private void Vibrate()
        {
            try
            {
                // Vibrate for 500ms
                var duration = TimeSpan.FromMilliseconds(300);
                Vibration.Default.Vibrate(duration);
            }
            catch (Exception ex)
            {
                // Vibration not supported or failed
            }
        }

        private async Task ShowNotificationPopup(NotificationData notification)
        {
            try
            {
                // Play notification sound (we'll implement this next)
                // await PlayNotificationSound();

                // Show popup
                bool viewDetails = await Shell.Current.DisplayAlert(
                    "🔔 New Delivery!",
                    $"From: {notification.from}\n" +
                    $"Verification Code: {notification.verificationCode}\n" +
                    $"Destination: Room {notification.destination}\n\n" +
                    $"Tap to copy verification code",
                    "Copy Code",
                    "Dismiss");

                if (viewDetails)
                {
                    // Copy to clipboard
                    await Clipboard.SetTextAsync(notification.verificationCode);
                    await Shell.Current.DisplayAlert("Copied!", "Verification code copied to clipboard", "OK");
                }
            }
            catch (Exception ex)
            {
                // Handle error
            }
        }

        [RelayCommand]
        private async Task NewDeliveryTap()
        {
            await Shell.Current.GoToAsync("///CreateDeliveryScreen");
        }

        [RelayCommand]
        private async Task CopyVerificationCode(DeliveryCardInfo delivery)
        {
            try
            {
                await Clipboard.SetTextAsync(delivery.VerificationCode);
                await Shell.Current.DisplayAlert("Copied!", $"Verification code {delivery.VerificationCode} copied to clipboard", "OK");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", "Failed to copy code", "OK");
            }
        }
    }

    // Helper classes
    public class DeliveryCardInfo
    {
        public string Id { get; set; }
        public string Sender { get; set; }
        public string Receiver { get; set; }
        public int Destination { get; set; }
        public string VerificationCode { get; set; }
        public string Status { get; set; }
        public string Message { get; set; }
        public string Category { get; set; }
        public int CurrentLocation { get; set; }
        public int ProgressStage { get; set; }
        public int Pickup { get; set; }

        public bool ShowProgressTracker =>
        Status == "pending" ||
        Status == "in_progress" ||
        Status == "arrived" ||
        ProgressStage > 0;

        public string CategoryText => $"📁 {Category}";
        public string SenderText => $"From: {Sender}";
        public string ReceiverText => $"To: {Receiver}";
        public string VerificationText => $"🔐 {VerificationCode}";
        public string DestinationText => $"📍 {Destination}";
        public string PickupText => $"📦 Pickup: Room {Pickup}";
        public string StatusText => ProgressStage switch
        {
            0 => "📦 Processing",
            1 => "🚚 In Transit",
            2 => $"📍 Approaching Room {Destination}",
            3 => "✅ Arrived - Awaiting Verification",
            _ => Status switch
            {
                "completed" => "✅ Delivered",
                "cancelled" => "❌ Cancelled",
                _ => "📦 Pending"
            }
        };

        public bool HasMessage => !string.IsNullOrWhiteSpace(Message);

        public Color StatusColor => Status switch
        {
            "pending" => Color.FromArgb("#FFF9C4"),      // Light Yellow
            "in_progress" => Color.FromArgb("#BBDEFB"),  // Light Blue
            "arrived" => Color.FromArgb("#C8E6C9"),      // Light Green
            "delivered" => Color.FromArgb("#F5F5F5"),    // Light Gray
            _ => Color.FromArgb("#FFFFFF")
        };

        public string ProgressText => ProgressStage switch
        {
            0 => "📦 Processing your delivery...",
            1 => "🚚 Robot is on the way",
            2 => $"📍 Approaching Room {Destination}",
            3 => "✅ Arrived! Ready for pickup",
            _ => ""
        };

        public Color Stage0Color => ProgressStage >= 0 ? Color.FromArgb("#4CAF50") : Color.FromArgb("#E0E0E0");
        public Color Stage1Color => ProgressStage >= 1 ? Color.FromArgb("#4CAF50") : Color.FromArgb("#E0E0E0");
        public Color Stage2Color => ProgressStage >= 2 ? Color.FromArgb("#4CAF50") : Color.FromArgb("#E0E0E0");
        public Color Stage3Color => ProgressStage >= 3 ? Color.FromArgb("#4CAF50") : Color.FromArgb("#E0E0E0");

        public string Stage0Icon => ProgressStage >= 0 ? "✓" : "";
        public string Stage1Icon => ProgressStage >= 1 ? "✓" : "";
        public string Stage2Icon => ProgressStage >= 2 ? "✓" : "";
        public string Stage3Icon => ProgressStage >= 3 ? "✓" : "";
    }

    public class DeliveryData
    {
        public int pickup { get; set; }
        public string category { get; set; }
        public string id { get; set; }
        public string sender { get; set; }
        public string senderUid { get; set; }
        public string receiver { get; set; }
        public string receiverUid { get; set; }
        public int destination { get; set; }
        public int compartment { get; set; }
        public string message { get; set; }
        public string verificationCode { get; set; }
        public string status { get; set; }
        public int currentLocation { get; set; }    // NEW
        public int progressStage { get; set; }      // NEW
        public string createdAt { get; set; }
        public string arrivedAt { get; set; }
        public string completedAt { get; set; }
    }

    public class NotificationData
    {
        public string deliveryId { get; set; }
        public string from { get; set; }
        public string message { get; set; }
        public string verificationCode { get; set; }
        public int destination { get; set; }
        public string timestamp { get; set; }
        public bool read { get; set; }
    }
}