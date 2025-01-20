using Microsoft.EntityFrameworkCore;
using SimpleApiBackend.Models;

namespace SimpleApiBackend.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Trip> Trips { get; set; }
        public DbSet<UserTrip> UserTrips { get; set; }
        public DbSet<Invitation> Invitations { get; set; }
        public DbSet<Expense> Expenses { get; set; } // Nowa tabela Expenses
        public DbSet<Debt> Debts { get; set; }       // Nowa tabela Debts
        public DbSet<DebtPaymentRequest> DebtPaymentRequests { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Konfiguracja tabeli Trip
            modelBuilder.Entity<Trip>()
                .HasMany(t => t.UserTrips)
                .WithOne(ut => ut.Trip)
                .HasForeignKey(ut => ut.TripId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Trip>()
                .HasIndex(t => t.SecretCode)
                .IsUnique();

            modelBuilder.Entity<Trip>()
                .Property(t => t.Name)
                .IsRequired();

            modelBuilder.Entity<Trip>()
                .Property(t => t.Description)
                .HasMaxLength(1000);

            modelBuilder.Entity<Trip>()
                .Property(t => t.StartDate)
                .IsRequired();

            modelBuilder.Entity<Trip>()
                .Property(t => t.EndDate)
                .IsRequired();

            // Konfiguracja tabeli UserTrip
            modelBuilder.Entity<UserTrip>()
                .HasKey(ut => new { ut.UserId, ut.TripId }); // Klucz złożony

            // Konfiguracja tabeli Invitation
            modelBuilder.Entity<Invitation>()
                .HasOne(i => i.Trip)
                .WithMany(t => t.Invitations)
                .HasForeignKey(i => i.TripId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Invitation>()
                .HasOne(i => i.Sender)
                .WithMany()
                .HasForeignKey(i => i.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Invitation>()
                .HasOne(i => i.Receiver)
                .WithMany()
                .HasForeignKey(i => i.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Invitation>()
                .Property(i => i.Status)
                .IsRequired()
                .HasMaxLength(50);

            // Konfiguracja tabeli Expense
            modelBuilder.Entity<Expense>()
                .HasKey(e => e.ExpenseId);

            modelBuilder.Entity<Expense>()
                .HasOne(e => e.Trip)
                .WithMany(t => t.Expenses)
                .HasForeignKey(e => e.TripId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Expense>()
                .HasOne(e => e.Creator)
                .WithMany(u => u.ExpensesCreated)
                .HasForeignKey(e => e.CreatorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Expense>()
                .Property(e => e.Currency)
                .HasMaxLength(10)
                .IsRequired();

            modelBuilder.Entity<Expense>()
                .Property(e => e.Category)
                .HasMaxLength(100);

            modelBuilder.Entity<Expense>()
                .Property(e => e.Location)
                .HasMaxLength(255)
                .IsRequired(false);

            // Konfiguracja tabeli Debt
            modelBuilder.Entity<Debt>()
                .HasKey(d => d.DebtId);

            modelBuilder.Entity<Debt>()
                .HasOne(d => d.Expense)
                .WithMany(e => e.Debts)
                .HasForeignKey(d => d.ExpenseId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Debt>()
                .HasOne(d => d.User)
                .WithMany(u => u.Debts)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Debt>()
                .Property(d => d.Currency)
                .HasMaxLength(10)
                .IsRequired();

            modelBuilder.Entity<Debt>()
                .Property(d => d.Status)
                .HasMaxLength(50)
                .IsRequired();
        }
    }
}
