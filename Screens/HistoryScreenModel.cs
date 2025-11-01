﻿using CommunityToolkit.Mvvm.ComponentModel;
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
    public partial class HistoryScreenModel : ObservableObject
    {
        private readonly FirebaseAuthClient _authClient;
        private readonly FirebaseClient _firebaseDb;

        private ObservableCollection<HistoryDeliveryInfo> _allDeliveries = new();

        [ObservableProperty]
        private ObservableCollection<HistoryDeliveryInfo> _filteredDeliveries = new();

        [ObservableProperty]
        private string _currentFilter = "delivered";

        [ObservableProperty]
        private bool _isRefreshing = false;

        [ObservableProperty]
        private string _emptyStateMessage = "No delivered items yet";

        // Button colors
        public Color DeliveredBackgroundColor => CurrentFilter == "delivered" ? Color.FromArgb("#2D4A6E") : Color.FromArgb("#F0F8FF");
        public Color CancelledBackgroundColor => CurrentFilter == "cancelled" ? Color.FromArgb("#2D4A6E") : Color.FromArgb("#F0F8FF");
        public Color PendingBackgroundColor => CurrentFilter == "pending" ? Color.FromArgb("#2D4A6E") : Color.FromArgb("#F0F8FF");

        public Color DeliveredTextColor => CurrentFilter == "delivered" ? Colors.White : Colors.Black;
        public Color CancelledTextColor => CurrentFilter == "cancelled" ? Colors.White : Colors.Black;
        public Color PendingTextColor => CurrentFilter == "pending" ? Colors.White : Colors.Black;

        public HistoryScreenModel(FirebaseAuthClient authClient, FirebaseClient firebaseDb)
        {
            _authClient = authClient;
            _firebaseDb = firebaseDb;
            _ = LoadDeliveries();
        }

        [RelayCommand]
        private async Task Refresh()
        {
            IsRefreshing = true;
            await LoadDeliveries();
            IsRefreshing = false;
        }

        [RelayCommand]
        private void FilterDeliveries(string filter)
        {
            CurrentFilter = filter;
            ApplyFilter();

            // Update button colors
            OnPropertyChanged(nameof(DeliveredBackgroundColor));
            OnPropertyChanged(nameof(CancelledBackgroundColor));
            OnPropertyChanged(nameof(PendingBackgroundColor));
            OnPropertyChanged(nameof(DeliveredTextColor));
            OnPropertyChanged(nameof(CancelledTextColor));
            OnPropertyChanged(nameof(PendingTextColor));
        }

        private async Task LoadDeliveries()
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

                _allDeliveries.Clear();

                // Process delivery_requests
                foreach (var delivery in requestsData)
                {
                    var data = delivery.Object;

                    if (data.senderUid == user.Uid || data.receiverUid == user.Uid)
                    {
                        _allDeliveries.Add(CreateHistoryInfo(data, user.Uid));
                    }
                }

                // Process delivery_history
                foreach (var delivery in historyData)
                {
                    var data = delivery.Object;

                    if (data.senderUid == user.Uid || data.receiverUid == user.Uid)
                    {
                        _allDeliveries.Add(CreateHistoryInfo(data, user.Uid));
                    }
                }

                // Sort by date (newest first)
                var sorted = _allDeliveries.OrderByDescending(d => d.CreatedAt).ToList();
                _allDeliveries.Clear();
                foreach (var item in sorted)
                {
                    _allDeliveries.Add(item);
                }

                ApplyFilter();
            }
            catch (Exception ex)
            {
                // Handle error
            }
        }

        private HistoryDeliveryInfo CreateHistoryInfo(DeliveryData data, string currentUserId)
        {
            bool isOutgoing = data.senderUid == currentUserId;

            return new HistoryDeliveryInfo
            {
                Id = data.id,
                Sender = data.sender,
                Receiver = data.receiver,
                Destination = data.destination,
                Status = data.status ?? "pending",
                Message = data.message,
                CreatedAt = DateTime.TryParse(data.createdAt, out var date) ? date : DateTime.Now,
                IsOutgoing = isOutgoing
            };
        }

        private void ApplyFilter()
        {
            FilteredDeliveries.Clear();

            var filtered = _allDeliveries.Where(d =>
            {
                return CurrentFilter switch
                {
                    "delivered" => d.Status == "completed" || d.Status == "delivered",
                    "cancelled" => d.Status == "cancelled",
                    "pending" => d.Status == "pending" || d.Status == "in_progress" || d.Status == "arrived",
                    _ => false
                };
            }).ToList();

            foreach (var item in filtered)
            {
                FilteredDeliveries.Add(item);
            }

            // Update empty state message
            EmptyStateMessage = CurrentFilter switch
            {
                "delivered" => "No delivered items yet",
                "cancelled" => "No cancelled deliveries",
                "pending" => "No pending deliveries",
                _ => "No deliveries found"
            };
        }
    }

    public class HistoryDeliveryInfo
    {
        public string Id { get; set; }
        public string Sender { get; set; }
        public string Receiver { get; set; }
        public int Destination { get; set; }
        public string Status { get; set; }
        public string Message { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsOutgoing { get; set; }

        public string DirectionText => IsOutgoing ? $"To: {Receiver}" : $"From: {Sender}";
        public string DestinationText => $"Destination {Destination}";
        public string DateText => CreatedAt.ToString("MMM dd, yyyy hh:mm tt");
        public string MessagePreview => !string.IsNullOrEmpty(Message) ? $"Message: {Message}" : "";
        public bool HasMessage => !string.IsNullOrEmpty(Message);

        public string StatusBadge => Status switch
        {
            "pending" => "⏳ Pending",
            "in_progress" => "🚚 In Transit",
            "arrived" => "📍 Arrived",
            "completed" => "✅ Delivered",
            "delivered" => "✅ Delivered",
            "cancelled" => "❌ Cancelled",
            _ => Status
        };

        public Color StatusColor => Status switch
        {
            "pending" => Color.FromArgb("#FFF9C4"),
            "in_progress" => Color.FromArgb("#BBDEFB"),
            "arrived" => Color.FromArgb("#C8E6C9"),
            "completed" => Color.FromArgb("#E8F5E9"),
            "delivered" => Color.FromArgb("#E8F5E9"),
            "cancelled" => Color.FromArgb("#FFCDD2"),
            _ => Color.FromArgb("#FFFFFF")
        };
    }
}