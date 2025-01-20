namespace SimpleApiBackend.Models
{
    public class RegisterModel
    {
        public string Username { get; set; }   // Login użytkownika
        public string Email { get; set; }      // Email użytkownika
        public string Password { get; set; }   // Hasło
        public string ConfirmPassword { get; set; }  // Potwierdzenie hasła
    }
}
