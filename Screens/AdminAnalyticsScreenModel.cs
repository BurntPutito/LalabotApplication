using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Database.Query;
using LalabotApplication.Models;

namespace LalabotApplication.Screens
{
    public partial class AdminAnalyticsScreenModel : ObservableObject
    {
        private readonly FirebaseAuthClient _authClient;
        private readonly FirebaseClient _firebaseDb;

        [ObservableProperty]
        private bool _isRefreshing = false;

        // Overview Stats
        [ObservableProperty]
        private int _totalUsers = 0;

        [ObservableProperty]
        private int _totalDeliveries = 0;

        [ObservableProperty]
        private int _activeDeliveries = 0;

        [ObservableProperty]
        private int _completedDeliveries = 0;

        [ObservableProperty]
        private int _cancelledDeliveries = 0;

        [ObservableProperty]
        private double _overallSuccessRate = 0;

        // Time-based
        [ObservableProperty]
        private int _deliveriesToday = 0;

        [ObservableProperty]
        private int _deliveriesThisWeek = 0;

        [ObservableProperty]
        private int _deliveriesThisMonth = 0;

        // Compartments
        [ObservableProperty]
        private string _compartmentUsageText = "Loading...";

        [ObservableProperty]
        private string _compartment1Status = "Free";

        [ObservableProperty]
        private string _compartment2Status = "Free";

        [ObservableProperty]
        private string _compartment3Status = "Free";

        [ObservableProperty]
        private Color _compartment1Color = Color.FromArgb("#C8E6C9");

        [ObservableProperty]
        private Color _compartment2Color = Color.FromArgb("#C8E6C9");

        [ObservableProperty]
        private Color _compartment3Color = Color.FromArgb("#C8E6C9");

        // Popular Destination
        [ObservableProperty]
        private string _mostPopularDestination = "N/A";

        [ObservableProperty]
        private int _mostPopularDestinationCount = 0;

        public AdminAnalyticsScreenModel(FirebaseAuthClient authClient, FirebaseClient firebaseDb)
        {
            _authClient = authClient;
            _firebaseDb = firebaseDb;
        }

        [RelayCommand]
        public async Task Refresh()
        {
            IsRefreshing = true;
            await LoadAnalytics();
            IsRefreshing = false;
        }

        public async Task LoadAnalytics()
        {
            try
            {
                // Load all data in parallel
                var usersTask = LoadUserCount();
                var deliveriesTask = LoadDeliveryStats();
                var compartmentsTask = LoadCompartmentStatus();

                await Task.WhenAll(usersTask, deliveriesTask, compartmentsTask);
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", $"Failed to load analytics: {ex.Message}", "OK");
            }
        }

        private async Task LoadUserCount()
        {
            try
            {
                var users = await _firebaseDb
                    .Child("users")
                    .OnceAsync<object>();

                TotalUsers = users?.Count ?? 0;
            }
            catch
            {
                TotalUsers = 0;
            }
        }

        private async Task LoadDeliveryStats()
        {
            try
            {
                // Get all deliveries
                var requestsData = await _firebaseDb
                    .Child("delivery_requests")
                    .OnceAsync<DeliveryData>();

                var historyData = await _firebaseDb
                    .Child("delivery_history")
                    .OnceAsync<DeliveryData>();

                var allDeliveries = new List<DeliveryData>();

                foreach (var delivery in requestsData)
                {
                    allDeliveries.Add(delivery.Object);
                }

                foreach (var delivery in historyData)
                {
                    allDeliveries.Add(delivery.Object);
                }

                // Total deliveries
                TotalDeliveries = allDeliveries.Count;

                // Active deliveries
                ActiveDeliveries = allDeliveries.Count(d =>
                    d.status == "pending" ||
                    d.status == "in_progress" ||
                    d.status == "arrived");

                // Completed deliveries
                CompletedDeliveries = allDeliveries.Count(d =>
                    d.status == "completed" || d.progressStage == 3);

                // Cancelled deliveries
                CancelledDeliveries = allDeliveries.Count(d => d.status == "cancelled");

                // Success rate
                OverallSuccessRate = TotalDeliveries > 0
                    ? Math.Round((double)CompletedDeliveries / TotalDeliveries * 100, 1)
                    : 0;

                // Time-based stats
                var now = DateTime.UtcNow;
                var today = now.Date;
                var weekAgo = now.AddDays(-7);
                var monthAgo = now.AddMonths(-1);

                DeliveriesToday = allDeliveries.Count(d =>
                {
                    if (DateTime.TryParse(d.createdAt, out var date))
                    {
                        return date.Date == today;
                    }
                    return false;
                });

                DeliveriesThisWeek = allDeliveries.Count(d =>
                {
                    if (DateTime.TryParse(d.createdAt, out var date))
                    {
                        return date >= weekAgo;
                    }
                    return false;
                });

                DeliveriesThisMonth = allDeliveries.Count(d =>
                {
                    if (DateTime.TryParse(d.createdAt, out var date))
                    {
                        return date >= monthAgo;
                    }
                    return false;
                });

                // Most popular destination
                var destinationCounts = allDeliveries
                    .GroupBy(d => d.destination)
                    .OrderByDescending(g => g.Count())
                    .FirstOrDefault();

                if (destinationCounts != null)
                {
                    MostPopularDestination = $"Room {destinationCounts.Key}";
                    MostPopularDestinationCount = destinationCounts.Count();
                }
            }
            catch (Exception ex)
            {
                // Handle error
            }
        }

        private async Task LoadCompartmentStatus()
        {
            try
            {
                var compartments = await _firebaseDb
                    .Child("robot_status")
                    .Child("currentDeliveries")
                    .OnceSingleAsync<CompartmentStatus>();

                if (compartments == null)
                {
                    CompartmentUsageText = "All compartments are free";
                    return;
                }

                int inUse = 0;

                // Compartment 1
                if (!string.IsNullOrEmpty(compartments.compartment1))
                {
                    Compartment1Status = "In Use";
                    Compartment1Color = Color.FromArgb("#FFE082");
                    inUse++;
                }
                else
                {
                    Compartment1Status = "Free";
                    Compartment1Color = Color.FromArgb("#C8E6C9");
                }

                // Compartment 2
                if (!string.IsNullOrEmpty(compartments.compartment2))
                {
                    Compartment2Status = "In Use";
                    Compartment2Color = Color.FromArgb("#FFE082");
                    inUse++;
                }
                else
                {
                    Compartment2Status = "Free";
                    Compartment2Color = Color.FromArgb("#C8E6C9");
                }

                // Compartment 3
                if (!string.IsNullOrEmpty(compartments.compartment3))
                {
                    Compartment3Status = "In Use";
                    Compartment3Color = Color.FromArgb("#FFE082");
                    inUse++;
                }
                else
                {
                    Compartment3Status = "Free";
                    Compartment3Color = Color.FromArgb("#C8E6C9");
                }

                CompartmentUsageText = $"{inUse} of 3 compartments in use";
            }
            catch
            {
                CompartmentUsageText = "Unable to load compartment status";
            }
        }
    }
}