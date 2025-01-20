namespace SimpleApiBackend.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string Role { get; set; } = "User"; // Dodaj pole Role
        public bool IsEmailConfirmed { get; set; } = false;
        public ICollection<UserTrip> Trips { get; set; }
        public ICollection<Debt> Debts { get; set; }
        public ICollection<Expense> ExpensesCreated { get; set; }

    }
}
