using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Database.Query;
using System;
using System.Threading.Tasks;

namespace LalabotApplication.Screens
{
    public partial class CreateDeliveryScreenModel : ObservableObject
    {
        private readonly FirebaseAuthClient _authClient;
        private readonly FirebaseClient _firebaseDb;

        [ObservableProperty]
        private string _receiver = string.Empty;

        [ObservableProperty]
        private int _selectedDestinationIndex = -1;

        [ObservableProperty]
        private string _message = string.Empty;

        public CreateDeliveryScreenModel(FirebaseAuthClient authClient, FirebaseClient firebaseDb)
        {
            _authClient = authClient;
            _firebaseDb = firebaseDb;
        }

        [RelayCommand]
        private async Task CreateDelivery()
        {
            //Validation
            if (string.IsNullOrWhiteSpace(Receiver))
            {
                await Shell.Current.DisplayAlert("Validation Error", "Please enter the receiver's name.", "OK");
                return;
            }

            if (SelectedDestinationIndex == -1)
            {
                await Shell.Current.DisplayAlert("Validation Error", "Please select a destination.", "OK");
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

                // get sender username from database
                var senderData = await _firebaseDb
                    .Child("users")
                    .Child(user.Uid)
                    .Child("Username")
                    .OnceSingleAsync<string>();

                string senderName = senderData ?? "Unknown";

                // get available compartment
                int compartment = await GetAvailableCompartment();

                if (compartment == -1)
                {
                    await Shell.Current.DisplayAlert("Robot Full", "All compartments are occupied. Please wait for deliveries to complete.", "OK");
                    return;
                }

                // will generete a unique delivery ID and verification code
                string deliveryId = $"del_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
                string verificationCode = GenerateVerificationCode();

                // calculate actual destination number 
                int destination = SelectedDestinationIndex + 1;

                // create delivery object
                var delivery = new
                {
                    id = deliveryId,
                    sender = senderName,
                    senderUid = user.Uid,
                    receiver = Receiver,
                    destination = destination,
                    compartment = compartment,
                    message = Message,
                    verificationCode = verificationCode,
                    status = "pending",
                    createdAt = DateTime.UtcNow.ToString("o"),
                    arrivedAt = (string)null,
                    completedAt = (string)null
                };

                // save to firebase
                await _firebaseDb
                    .Child("delivery_requests")
                    .Child(deliveryId)
                    .PutAsync(delivery);

                // Update robot status - get current compartments first
                var currentCompartments = await _firebaseDb
                    .Child("robot_status")
                    .Child("currentDeliveries")
                    .OnceSingleAsync<CompartmentStatus>();

                // Update the entire currentDeliveries object
                await _firebaseDb
                    .Child("robot_status")
                    .Child("currentDeliveries")
                    .PutAsync(new
                    {
                        compartment1 = compartment == 1 ? deliveryId : (currentCompartments?.Compartment1 ?? ""),
                        compartment2 = compartment == 2 ? deliveryId : (currentCompartments?.Compartment2 ?? ""),
                        compartment3 = compartment == 3 ? deliveryId : (currentCompartments?.Compartment3 ?? "")
                    });

                await Shell.Current.DisplayAlert(
                    "Delivery Created!",
                    $"Verification Code: {verificationCode}\n\nShare this code with {Receiver}. They will need it to receive the delivery.",
                    "OK");

                // navigate back to home
                await Shell.Current.GoToAsync("///MainPage");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", $"Failed to create delivery: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        private async Task Cancel()
        {
            await Shell.Current.GoToAsync("///MainPage");
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
                    // initialize if doesn't exist
                    await _firebaseDb
                        .Child("robot_status")
                        .Child("currentDeliveries")
                        .PutAsync(new
                        {
                            compartment1 = (string)null,
                            compartment2 = (string)null,
                            compartment3 = (string)null,
                        });
                    return 1; // first compartment is available
                }

                if (string.IsNullOrEmpty(compartments.Compartment1)) return 1;
                if (string.IsNullOrEmpty(compartments.Compartment2)) return 2;
                if (string.IsNullOrEmpty(compartments.Compartment3)) return 3;
                return -1; // no compartments available
            }
            catch
            {
                return 1; // Default to first compartment if error
            }
        }

        private string GenerateVerificationCode()
        {
            // Generate 4-digit code
            Random random = new Random();
            return random.Next(1000, 9999).ToString();
        }
    }

    // Helper class to map compartment status
    public class CompartmentStatus
    {
        public string Compartment1 { get; set; }
        public string Compartment2 { get; set; }
        public string Compartment3 { get; set; }
    }
}
