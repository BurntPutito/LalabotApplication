namespace LalabotApplication.Services
{
    public static class AdminService
    {
        // Add admin emails here
        private static readonly HashSet<string> AdminEmails = new()
        {
            "admin@lalabot.com",
            "admin@gmail.com",
            "caitan@gmail.com"
            // Add more admin emails as needed
        };

        public static bool IsAdmin(string email)
        {
            if (string.IsNullOrEmpty(email))
                return false;

            return AdminEmails.Contains(email.ToLower());
        }
    }
}
