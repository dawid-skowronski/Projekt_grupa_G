using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SimpleApiBackend.Data;
using Microsoft.EntityFrameworkCore;
using SimpleApiBackend.Models;


[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")] // Wymagaj roli Admin dla całego kontrolera
public class AdminController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public AdminController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _context.Users.ToListAsync();
        return Ok(users);
    }

    [HttpPut("user/{id}")]
    public async Task<IActionResult> UpdateUserRole(int id, [FromBody] string role)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
            return NotFound("User not found.");

        user.Role = role;
        _context.Users.Update(user);
        await _context.SaveChangesAsync();

        return Ok("Role updated successfully.");
    }
    [HttpDelete("user/{id}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
            return NotFound("User not found.");

        // 🔥 1️⃣ Usunięcie długów użytkownika
        var debts = _context.Debts.Where(d => d.UserId == id);
        _context.Debts.RemoveRange(debts);

        // 🔥 2️⃣ Usunięcie wszystkich zaproszeń związanych z użytkownikiem
        var invitations = _context.Invitations.Where(i => i.SenderId == id || i.ReceiverId == id);
        _context.Invitations.RemoveRange(invitations);

        // 🔥 3️⃣ Usunięcie wszystkich wydatków dodanych przez użytkownika
        var expenses = _context.Expenses.Where(e => e.CreatorId == id);
        _context.Expenses.RemoveRange(expenses);

        // 🔥 4️⃣ Usunięcie użytkownika z wyjazdów
        var userTrips = _context.UserTrips.Where(ut => ut.UserId == id);
        _context.UserTrips.RemoveRange(userTrips);

        // 🔥 5️⃣ Usunięcie samego użytkownika
        _context.Users.Remove(user);

        await _context.SaveChangesAsync();

        return Ok(new { message = "User deleted successfully" });
    }


    [HttpPut("trip/{id}")]
    public async Task<IActionResult> EditTrip(int id, [FromBody] TripCreateModel model)
    {
        var trip = await _context.Trips.FindAsync(id);
        if (trip == null)
        {
            return NotFound("Trip not found.");
        }

        // 🔹 Aktualizujemy tylko podane wartości, inne zostają bez zmian
        if (!string.IsNullOrWhiteSpace(model.Name))
            trip.Name = model.Name;

        if (!string.IsNullOrWhiteSpace(model.Description))
            trip.Description = model.Description;

        if (model.StartDate != default(DateTime))
            trip.StartDate = model.StartDate;

        if (model.EndDate != default(DateTime))
            trip.EndDate = model.EndDate;

        _context.Trips.Update(trip);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Trip updated successfully.", updatedTrip = trip });
    }


    [HttpGet("trip/{id}")]
    public async Task<IActionResult> GetTrip(int id)
    {
        var trip = await _context.Trips.FindAsync(id);
        if (trip == null)
        {
            return NotFound("Trip not found.");
        }
        return Ok(trip);
    }


    [HttpDelete("trip/{id}")]
    public async Task<IActionResult> DeleteTrip(int id)
    {
        var trip = await _context.Trips.FindAsync(id);
        if (trip == null)
        {
            return NotFound("Trip not found.");
        }

        _context.Trips.Remove(trip);
        await _context.SaveChangesAsync();

        return Ok("Trip deleted successfully.");
    }

    [HttpGet("user/{userId}/trips")]
    public async Task<IActionResult> GetUserTrips(int userId)
    {
        Console.WriteLine($"[DEBUG] Pobieranie podróży dla użytkownika {userId}...");

        var userTrips = await _context.UserTrips
            .Where(ut => ut.UserId == userId)
            .Include(ut => ut.Trip)
            .ToListAsync();

        Console.WriteLine($"[DEBUG] Znalezione UserTrips: {userTrips.Count}");
        foreach (var ut in userTrips)
        {
            Console.WriteLine($"[DEBUG] UserTrip: UserId={ut.UserId}, TripId={ut.TripId}, TripName={ut.Trip?.Name ?? "NULL"}");
        }

        if (!userTrips.Any())
        {
            Console.WriteLine("[DEBUG] Brak podróży dla tego użytkownika.");
            return NotFound("No trips found for the specified user.");
        }

        var trips = userTrips.Select(ut => new
        {
            ut.Trip.TripId,
            ut.Trip.Name,
            ut.Trip.Description,
            ut.Trip.StartDate,
            ut.Trip.EndDate
        }).ToList();

        Console.WriteLine($"[DEBUG] Liczba podróży zwróconych klientowi: {trips.Count}");

        return Ok(trips);
    }

}