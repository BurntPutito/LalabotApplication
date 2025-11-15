namespace LalabotApplication.Models
{
    public class UserAnalytics
    {
        public int TotalSent { get; set; }
        public int TotalReceived { get; set; }
        public int ThisWeekSent { get; set; }
        public int ThisWeekReceived { get; set; }
        public int CompletedDeliveries { get; set; }
        public int CancelledDeliveries { get; set; }
        public double SuccessRate { get; set; }
        public string MostUsedDestination { get; set; }
    }
}
