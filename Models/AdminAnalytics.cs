namespace LalabotApplication.Models
{
    public class AdminAnalytics
    {
        // Overview Stats
        public int TotalUsers { get; set; }
        public int TotalDeliveries { get; set; }
        public int ActiveDeliveries { get; set; }
        public int CompletedDeliveries { get; set; }
        public int CancelledDeliveries { get; set; }
        public double OverallSuccessRate { get; set; }

        // Time-based Stats
        public int DeliveriesToday { get; set; }
        public int DeliveriesThisWeek { get; set; }
        public int DeliveriesThisMonth { get; set; }

        // Compartment Usage
        public int Compartment1InUse { get; set; }
        public int Compartment2InUse { get; set; }
        public int Compartment3InUse { get; set; }
        public string CompartmentUsageText { get; set; }

        // Popular Destinations
        public string MostPopularDestination { get; set; }
        public int MostPopularDestinationCount { get; set; }

        // Peak Usage
        public string PeakUsageTime { get; set; }
    }
}
