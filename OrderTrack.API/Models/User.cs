namespace OrderTrack.API.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty; // Password is hashed
        public bool IsLockedOut { get; set; } = false;
        public int FailedLoginAttempts { get; set; } = 0;
        public DateTime? LockoutEndTime { get; set; }
    }
}
