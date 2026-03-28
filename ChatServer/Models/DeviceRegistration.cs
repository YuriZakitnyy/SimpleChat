namespace ChatServer.Models
{
    public class DeviceRegistration
    {
        public string ConnectionId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string DeviceId { get; set; } = string.Empty;
        public string DeviceToken { get; set; } = string.Empty;
        public string Platform { get; set; } = "Android";
        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
        public DateTime LastActive { get; set; } = DateTime.UtcNow;
    }
}