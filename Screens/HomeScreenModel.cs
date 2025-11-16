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
        private IDisposable _deliveriesSubscription;
        private bool _isListening = false;

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

        //Analytics properties
        [ObservableProperty]
        private int _totalSent = 0;

        [ObservableProperty]
        private int _totalReceived = 0;

        [ObservableProperty]
        private int _thisWeekTotal = 0;

        [ObservableProperty]
        private double _successRate = 0;

        [ObservableProperty]
        private bool _showAnalytics = false;

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
            _ = LoadAnalytics();
            StartDeliveryListener();
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
        public async Task Refresh()
        {
            IsRefreshing = true;

            await LoadUserInfo();
            await LoadDeliveries();
            await LoadAnalytics();

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

        [RelayCommand]
        private async Task ConfirmFilesPlaced(DeliveryCardInfo delivery)
        {
            try
            {
                bool confirm = await Shell.Current.DisplayAlert(
                    "Confirm Files Placed",
                    "Have you placed the files in the robot's compartment?",
                    "Yes, Start Delivery",
                    "Not Yet");

                if (!confirm) return;

                // Update delivery in Firebase
                var deliveryData = await _firebaseDb
                    .Child("delivery_requests")
                    .Child(delivery.Id)
                    .OnceSingleAsync<DeliveryData>();

                if (deliveryData != null)
                {
                    deliveryData.filesConfirmed = true;
                    deliveryData.progressStage = 1; // Move to "In Transit"

                    await _firebaseDb
                        .Child("delivery_requests")
                        .Child(delivery.Id)
                        .PutAsync(deliveryData);

                    await Shell.Current.DisplayAlert(
                        "Delivery Started!",
                        "The robot will now proceed to deliver your files.",
                        "OK");

                    await LoadDeliveries();
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", $"Failed to confirm: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        private async Task EnterVerificationCode(DeliveryCardInfo delivery)
        {
            try
            {
                string code = await Shell.Current.DisplayPromptAsync(
                    "Enter Verification Code",
                    $"Enter the 4-digit code for this delivery:",
                    placeholder: "0000",
                    maxLength: 4,
                    keyboard: Keyboard.Numeric);

                if (string.IsNullOrWhiteSpace(code)) return;

                // Verify the code
                if (code == delivery.VerificationCode)
                {
                    // Correct code - update database
                    var deliveryData = await _firebaseDb
                        .Child("delivery_requests")
                        .Child(delivery.Id)
                        .OnceSingleAsync<DeliveryData>();

                    if (deliveryData != null)
                    {
                        deliveryData.filesReceived = true; // Mark that compartment is now open

                        await _firebaseDb
                            .Child("delivery_requests")
                            .Child(delivery.Id)
                            .PutAsync(deliveryData);

                        // Play success sound
                        await PlayNotificationSound();

                        await Shell.Current.DisplayAlert(
                            "✅ Verified!",
                            "Compartment is now open. Please retrieve your files and confirm receipt.",
                            "OK");

                        await LoadDeliveries();
                    }
                }
                else
                {
                    // Incorrect code
                    Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(500));
                    await Shell.Current.DisplayAlert(
                        "❌ Incorrect Code",
                        "The verification code you entered is incorrect. Please try again.",
                        "OK");
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", $"Failed to verify: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        private async Task ConfirmReceipt(DeliveryCardInfo delivery)
        {
            try
            {
                bool confirm = await Shell.Current.DisplayAlert(
                    "Confirm Receipt",
                    "Have you retrieved the files from the compartment?",
                    "Yes, I Got Them",
                    "Not Yet");

                if (!confirm) return;

                // Get delivery data
                var deliveryData = await _firebaseDb
                    .Child("delivery_requests")
                    .Child(delivery.Id)
                    .OnceSingleAsync<DeliveryData>();

                if (deliveryData != null)
                {
                    deliveryData.status = "completed";
                    deliveryData.progressStage = 4; // Optional: add stage 4 for completed
                    deliveryData.completedAt = DateTime.UtcNow.ToString("o");

                    // Move to history
                    await _firebaseDb
                        .Child("delivery_history")
                        .Child(delivery.Id)
                        .PutAsync(deliveryData);

                    // Remove from active requests
                    await _firebaseDb
                        .Child("delivery_requests")
                        .Child(delivery.Id)
                        .DeleteAsync();

                    // Free the compartment
                    await FreeCompartment(delivery.Id);

                    await Shell.Current.DisplayAlert(
                        "✅ Delivery Complete!",
                        "Thank you for confirming. The delivery has been completed.",
                        "OK");

                    await LoadDeliveries();
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", $"Failed to complete: {ex.Message}", "OK");
            }
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
                        CurrentLocation = data.currentLocation,
                        ProgressStage = data.progressStage,

                        // delivery status properties
                        FilesConfirmed = data.filesConfirmed,
                        ConfirmationDeadline = data.confirmationDeadline,
                        ReadyForPickup = data.readyForPickup,
                        FilesReceived = data.filesReceived
                    };

                    // Categorize as outgoing or incoming
                    if (data.senderUid == user.Uid)
                    {
                        OutgoingDeliveries.Add(cardInfo);
                        cardInfo.IsOutgoing = true;
                    }
                    else if (data.receiverUid == user.Uid)
                    {
                        IncomingDeliveries.Add(cardInfo);
                        cardInfo.IsOutgoing = false;
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

        private async Task LoadAnalytics()
        {
            try
            {
                var user = _authClient.User;
                if (user == null) return;

                // Get all deliveries from both delivery_requests and delivery_history
                var requestsData = await _firebaseDb
                    .Child("delivery_requests")
                    .OnceAsync<DeliveryData>();

                var historyData = await _firebaseDb
                    .Child("delivery_history")
                    .OnceAsync<DeliveryData>();

                // Combine all deliveries
                var allDeliveries = new List<DeliveryData>();

                foreach (var delivery in requestsData)
                {
                    allDeliveries.Add(delivery.Object);
                }

                foreach (var delivery in historyData)
                {
                    allDeliveries.Add(delivery.Object);
                }

                // Filter user's deliveries
                var userDeliveries = allDeliveries.Where(d =>
                    d.senderUid == user.Uid || d.receiverUid == user.Uid).ToList();

                // Calculate stats
                TotalSent = userDeliveries.Count(d => d.senderUid == user.Uid);
                TotalReceived = userDeliveries.Count(d => d.receiverUid == user.Uid);

                // This week's deliveries
                var oneWeekAgo = DateTime.UtcNow.AddDays(-7);
                ThisWeekTotal = userDeliveries.Count(d =>
                {
                    if (DateTime.TryParse(d.createdAt, out var date))
                    {
                        return date >= oneWeekAgo;
                    }
                    return false;
                });

                // Success rate calculation
                var totalDeliveries = userDeliveries.Count;
                var completedDeliveries = userDeliveries.Count(d =>
                    d.status == "completed" || d.progressStage == 3);

                SuccessRate = totalDeliveries > 0
                    ? Math.Round((double)completedDeliveries / totalDeliveries * 100, 1)
                    : 0;

                ShowAnalytics = totalDeliveries > 0; // Only show if user has deliveries
            }
            catch (Exception ex)
            {
                // Silent fail - analytics is not critical
                ShowAnalytics = false;
            }
        }

        //Real-time listener
        private void StartDeliveryListener()
        {
            if (_isListening) return;

            var user = _authClient.User;
            if (user == null) return;

            _isListening = true;

            // Listen for changes in delivery_requests
            _deliveriesSubscription = _firebaseDb
                .Child("delivery_requests")
                .AsObservable<DeliveryData>()
                .Subscribe(change =>
                {
                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        await LoadDeliveries();
                    });
                });
        }

        //Stop listener when leaving screen
        public void StopListening()
        {
            _isListening = false;
            _deliveriesSubscription?.Dispose();
        }

        [RelayCommand]
        private async Task CancelDelivery(DeliveryCardInfo delivery)
        {
            bool confirm = await Shell.Current.DisplayAlert(
                "Cancel Delivery?",
                $"Are you sure you want to cancel this delivery to {delivery.Receiver}?\n\n" +
                $"The delivery will be marked as cancelled and the compartment will be freed.",
                "Yes, Cancel",
                "No");

            if (!confirm) return;

            try
            {
                var user = _authClient.User;
                if (user == null) return;

                // Update delivery status to cancelled
                await _firebaseDb
                    .Child("delivery_requests")
                    .Child(delivery.Id)
                    .PutAsync(new
                    {
                        id = delivery.Id,
                        sender = delivery.Sender,
                        senderUid = user.Uid,
                        receiver = delivery.Receiver,
                        receiverUid = await GetReceiverUid(delivery.Receiver),
                        pickup = delivery.Pickup,
                        destination = delivery.Destination,
                        compartment = await GetDeliveryCompartment(delivery.Id),
                        category = delivery.Category,
                        message = delivery.Message,
                        verificationCode = delivery.VerificationCode,
                        status = "cancelled",
                        currentLocation = 0,
                        progressStage = 0,
                        createdAt = DateTime.Now.ToString("o"),
                        cancelledAt = DateTime.Now.ToString("o"),
                        arrivedAt = (string)null,
                        completedAt = (string)null
                    });

                // Free up the compartment
                await FreeCompartment(delivery.Id);

                // Move to history
                await MoveToHistory(delivery.Id);

                // Notify receiver about cancellation
                var receiverUid = await GetReceiverUid(delivery.Receiver);
                if (!string.IsNullOrEmpty(receiverUid))
                {
                    await _firebaseDb
                        .Child("notifications")
                        .Child(receiverUid)
                        .Child($"{delivery.Id}_cancelled")
                        .PutAsync(new
                        {
                            deliveryId = delivery.Id,
                            from = delivery.Sender,
                            message = $"Delivery from {delivery.Sender} has been cancelled",
                            verificationCode = delivery.VerificationCode,
                            destination = delivery.Destination,
                            timestamp = DateTime.Now.ToString("o"),
                            read = false
                        });
                }

                await Shell.Current.DisplayAlert(
                    "Delivery Cancelled",
                    "The delivery has been cancelled successfully.",
                    "OK");

                // Reload deliveries
                await LoadDeliveries();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert(
                    "Error",
                    $"Failed to cancel delivery: {ex.Message}",
                    "OK");
            }
        }

        // Helper methods
        private async Task<string> GetReceiverUid(string receiverName)
        {
            try
            {
                var users = await _firebaseDb
                    .Child("users")
                    .OnceAsync<UserProfile>();

                var receiver = users.FirstOrDefault(u => u.Object.Username == receiverName);
                return receiver?.Key ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private async Task<int> GetDeliveryCompartment(string deliveryId)
        {
            try
            {
                var compartments = await _firebaseDb
                    .Child("robot_status")
                    .Child("currentDeliveries")
                    .OnceSingleAsync<CompartmentStatus>();

                if (compartments == null) return 0;

                if (compartments.compartment1 == deliveryId) return 1;
                if (compartments.compartment2 == deliveryId) return 2;
                if (compartments.compartment3 == deliveryId) return 3;

                return 0;
            }
            catch
            {
                return 0;
            }
        }

        private async Task FreeCompartment(string deliveryId)
        {
            try
            {
                var compartments = await _firebaseDb
                    .Child("robot_status")
                    .Child("currentDeliveries")
                    .OnceSingleAsync<CompartmentStatus>();

                if (compartments == null) return;

                await _firebaseDb
                    .Child("robot_status")
                    .Child("currentDeliveries")
                    .PutAsync(new
                    {
                        compartment1 = compartments.compartment1 == deliveryId ? "" : compartments.compartment1,
                        compartment2 = compartments.compartment2 == deliveryId ? "" : compartments.compartment2,
                        compartment3 = compartments.compartment3 == deliveryId ? "" : compartments.compartment3
                    });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error freeing compartment: {ex.Message}");
            }
        }

        private async Task MoveToHistory(string deliveryId)
        {
            try
            {
                // Get the delivery data
                var delivery = await _firebaseDb
                    .Child("delivery_requests")
                    .Child(deliveryId)
                    .OnceSingleAsync<DeliveryData>();

                if (delivery != null)
                {
                    // Copy to history
                    await _firebaseDb
                        .Child("delivery_history")
                        .Child(deliveryId)
                        .PutAsync(delivery);

                    // Remove from active requests
                    await _firebaseDb
                        .Child("delivery_requests")
                        .Child(deliveryId)
                        .DeleteAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error moving to history: {ex.Message}");
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
        public bool CanBeCancelled =>
            Status == "pending" && ProgressStage == 0;
        public bool IsOutgoing { get; set; }

        /*
        public bool ShowProgressTracker =>
        Status == "pending" ||
        Status == "in_progress" ||
        Status == "arrived" ||
        ProgressStage > 0;
        */

        public string CategoryText => $"📁 {Category}";
        public string SenderText => $"From: {Sender}";
        public string ReceiverText => $"To: {Receiver}";
        public string VerificationText => $"🔐 Code: {VerificationCode}";
        public string DestinationText => $"📍Destination: {Destination}";
        public string PickupText => $"📦 Pickup: Room {Pickup}";
        public string StatusText => ProgressStage switch
        {
            0 => "📦 Processing",
            1 => "🚚 In Transit",
            2 => $"📍 Approaching Room {Destination}",
            3 => "✅ Arrived",
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
            1 => "🚚 Lalabot is on the way",
            2 => $"📍 Approaching Room {Destination}",
            3 => "✅ Arrived! Waiting for pickup",
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


        // properties for delivery status
        public bool FilesConfirmed { get; set; }
        public string ConfirmationDeadline { get; set; }
        public bool ReadyForPickup { get; set; }
        public bool FilesReceived { get; set; }

        // For SENDER - show confirm button when robot at pickup and files not confirmed
        public bool ShowConfirmFilesButton =>
            IsOutgoing &&
            CurrentLocation == Pickup &&
            !FilesConfirmed &&
            ProgressStage == 0;

        // Show progress tracker after files confirmed for sender, or for receiver before arrival
        public bool ShowProgressTracker =>
            (IsOutgoing && FilesConfirmed) ||
            (!IsOutgoing && !ReadyForPickup && ProgressStage < 3);

        // For RECEIVER - show verification input when robot arrived
        public bool ShowVerificationInput =>
            !IsOutgoing &&
            ReadyForPickup &&
            !FilesReceived;

        // For RECEIVER - show confirm receipt button after successful verification
        public bool ShowConfirmReceiptButton =>
            !IsOutgoing &&
            ReadyForPickup &&
            FilesReceived;

        // Show regular info when no special buttons are shown
        public bool ShowRegularInfo =>
            !ShowConfirmFilesButton &&
            !ShowVerificationInput &&
            !ShowConfirmReceiptButton;

        // Add this temporarily for debugging
        public string DebugInfo =>
            $"IsOutgoing:{IsOutgoing}, FilesConfirmed:{FilesConfirmed}, " +
            $"CurrentLoc:{CurrentLocation}, Pickup:{Pickup}, " +
            $"Stage:{ProgressStage}, ReadyForPickup:{ReadyForPickup}";
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