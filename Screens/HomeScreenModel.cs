using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Database.Query;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace LalabotApplication.Screens
{
    public partial class HomeScreenModel : ObservableObject
    {
        private readonly FirebaseAuthClient _authClient;
        private readonly FirebaseClient _firebaseDb;

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

        public HomeScreenModel(FirebaseAuthClient authClient, FirebaseClient firebaseDb)
        {
            _authClient = authClient;
            _firebaseDb = firebaseDb;
            _ = LoadUserInfo();
            _ = LoadDeliveries();
            _ = ListenForNewDeliveries();
        }

        [RelayCommand]
        private async Task Refresh()
        {
            IsRefreshing = true;

            await LoadUserInfo();
            await LoadDeliveries();

            IsRefreshing = false;
        }


        private async Task LoadUserInfo()
        {
            try
            {
                var user = _authClient.User;

                if (user != null)
                {
                    var userData = await _firebaseDb
                        .Child("users")
                        .Child(user.Uid)
                        .Child("Username")
                        .OnceSingleAsync<string>();

                    if (!string.IsNullOrEmpty(userData))
                    {
                        Username = userData;
                    }
                }
            }
            catch (Exception ex)
            {
                Username = "User";
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
                        Destination = data.destination,
                        VerificationCode = data.verificationCode,
                        Status = data.status,
                        Message = data.message
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

                // Listen for new notifications
                _firebaseDb
                    .Child("notifications")
                    .Child(user.Uid)
                    .AsObservable<NotificationData>()
                    .Subscribe(async notification =>
                    {
                        if (notification.Object != null && !notification.Object.read)
                        {
                            // Show notification popup
                            await ShowNotificationPopup(notification.Object);

                            // Mark as read
                            await _firebaseDb
                                .Child("notifications")
                                .Child(user.Uid)
                                .Child(notification.Key)
                                .Child("read")
                                .PutAsync(true);

                            // Reload deliveries
                            await LoadDeliveries();
                        }
                    });
            }
            catch (Exception ex)
            {
                // Handle error
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
                    "?? New Delivery!",
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

        public string SenderText => $"From: {Sender}";
        public string ReceiverText => $"To: {Receiver}";
        public string VerificationText => $"Code: {VerificationCode}";
        public string DestinationText => $"Destination {Destination}";
        public string StatusText => Status switch
        {
            "pending" => "Status: Pending",
            "in_progress" => "Status: In Transit",
            "arrived" => "Status: Arrived - Awaiting Verification",
            "delivered" => "Status: Delivered",
            _ => Status
        };

        public Color StatusColor => Status switch
        {
            "pending" => Color.FromArgb("#FFF9C4"),      // Light Yellow
            "in_progress" => Color.FromArgb("#BBDEFB"),  // Light Blue
            "arrived" => Color.FromArgb("#C8E6C9"),      // Light Green
            "delivered" => Color.FromArgb("#F5F5F5"),    // Light Gray
            _ => Color.FromArgb("#FFFFFF")
        };
    }

    public class DeliveryData
    {
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