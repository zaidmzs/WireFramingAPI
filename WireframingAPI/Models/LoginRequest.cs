namespace WireframingAPI.Models
{
    public class LoginRequest
    {
        public string ICNumber { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Pin { get; set; }
        public string Name { get; set; }
    }
}
