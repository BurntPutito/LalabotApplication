namespace LalabotApplication.Models
{
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
        public int currentLocation { get; set; }
        public int progressStage { get; set; }
        public string createdAt { get; set; }
        public string arrivedAt { get; set; }
        public string completedAt { get; set; }
    }
}
