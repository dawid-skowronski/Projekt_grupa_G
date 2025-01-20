using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimpleApiBackend.Data;
using SimpleApiBackend.Models;
using System.Security.Claims;

[Route("api/[controller]")]
[ApiController]
public class InvitationsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public InvitationsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpPost("send")]
    public async Task<IActionResult> SendInvitation([FromBody] InvitationCreateModel model)
    {
        Console.WriteLine($"Otrzymane dane zaproszenia: ReceiverUsername={model.ReceiverUsername}, TripId={model.TripId}");

        var senderId = GetUserIdFromToken();
        if (senderId == null)
        {
            return Unauthorized(new { message = "Nie znaleziono użytkownika w tokenie." });
        }

        var senderUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == senderId.Value);
        if (senderUser == null)
        {
            return BadRequest(new { message = "Nieprawidłowy ID użytkownika wysyłającego." });
        }

        if (model.ReceiverUsername.ToLower() == senderUser.Username.ToLower())
        {
            Console.WriteLine("❌ Użytkownik próbował zaprosić samego siebie. Blokujemy żądanie.");
            return BadRequest(new { message = "Nie możesz zaprosić samego siebie." });
        }

        var receiverUser = await _context.Users
                                          .FirstOrDefaultAsync(u => u.Username.ToLower() == model.ReceiverUsername.ToLower());

        if (receiverUser == null)
        {
            return BadRequest(new { message = "Użytkownik o podanym username nie istnieje." });
        }

        // ✅ Sprawdzamy czy zaproszenie już istnieje
        var existingInvitation = await _context.Invitations
            .FirstOrDefaultAsync(i => i.TripId == model.TripId && i.ReceiverId == receiverUser.Id && i.Status == "Oczekujące");

        if (existingInvitation != null)
        {
            Console.WriteLine("⚠️ Zaproszenie dla tego użytkownika już istnieje!");
            return BadRequest(new { message = "Ten użytkownik już otrzymał zaproszenie do tego wyjazdu." });
        }

        var invitation = new Invitation
        {
            TripId = model.TripId,
            SenderId = senderId.Value,
            ReceiverId = receiverUser.Id,
            Status = "Oczekujące",
            CreatedAt = DateTime.Now
        };

        _context.Invitations.Add(invitation);
        await _context.SaveChangesAsync();

        var userTrip = new UserTrip
        {
            UserId = receiverUser.Id,
            TripId = model.TripId,
            IsOffline = true
        };

        _context.UserTrips.Add(userTrip);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Zaproszenie wysłane pomyślnie." });
    }



    [HttpGet("received")]
    public IActionResult GetReceivedInvitations()
    {
        var receiverId = GetUserIdFromToken();
        if (receiverId == null)
        {
            return Unauthorized(new { message = "Nie znaleziono użytkownika w tokenie." });
        }

        try
        {
            var invitations = _context.Invitations
                .Include(i => i.Trip) // Załaduj dane wyjazdu
                .Include(i => i.Sender) // Załaduj dane wysyłającego
                .Where(i => i.ReceiverId == receiverId && i.Status == "Oczekujące")
                .ToList();

            // Logowanie każdej zaproszenia
            foreach (var invitation in invitations)
            {
                Console.WriteLine($"Zaproszenie: ID={invitation.InvitationId}, TripName={invitation.Trip?.Name}, SenderUsername={invitation.Sender?.Username}, Status={invitation.Status}");
            }

            var result = invitations.Select(i => new
            {
                i.InvitationId,
                TripName = i.Trip?.Name ?? "Brak nazwy wyjazdu",
                i.Status,
                SenderUsername = i.Sender?.Username ?? "Nieznany użytkownik",
                CreatedAt = i.CreatedAt
            }).ToList();

            return Ok(result);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd: {ex.Message}");
            return StatusCode(500, new { message = "Wystąpił problem podczas pobierania zaproszeń." });
        }
    }

    [HttpPut("respond")]
    public async Task<IActionResult> RespondToInvitation([FromBody] InvitationResponseModel model)
    {
        var receiverId = GetUserIdFromToken();
        if (receiverId == null)
        {
            return Unauthorized(new { message = "Nie znaleziono użytkownika w tokenie." });
        }

        var invitation = await _context.Invitations.FindAsync(model.InvitationId);

        if (invitation == null)
        {
            return NotFound(new { message = "Zaproszenie nie zostało znalezione." });
        }

        if (invitation.ReceiverId != receiverId)
        {
            return Unauthorized(new { message = "Nie masz dostępu do tego zaproszenia." });
        }

        if (!model.Status.Equals("Accepted", StringComparison.OrdinalIgnoreCase) &&
            !model.Status.Equals("Rejected", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = "Nieznana akcja." });
        }

        if (model.Status.Equals("Accepted", StringComparison.OrdinalIgnoreCase))
        {
            // Zmiana statusu użytkownika na online
            var userTrip = await _context.UserTrips
                                          .Where(ut => ut.UserId == receiverId.Value && ut.TripId == invitation.TripId)
                                          .FirstOrDefaultAsync();
            if (userTrip != null)
            {
                userTrip.IsOffline = false;  // Użytkownik zaakceptował, zmiana na online
                _context.UserTrips.Update(userTrip);
            }

            // Zaktualizowanie zaproszenia
            invitation.Status = "Accepted";  // Zmiana statusu zaproszenia na zaakceptowane
        }
        else if (model.Status.Equals("Rejected", StringComparison.OrdinalIgnoreCase))
        {
            // Usunięcie użytkownika z tabeli UserTrips, ponieważ odrzucił zaproszenie
            var userTrip = await _context.UserTrips
                                          .Where(ut => ut.UserId == receiverId.Value && ut.TripId == invitation.TripId)
                                          .FirstOrDefaultAsync();
            if (userTrip != null)
            {
                _context.UserTrips.Remove(userTrip);
            }

            // Usunięcie zaproszenia
            _context.Invitations.Remove(invitation);
        }

        await _context.SaveChangesAsync();

        return Ok(new { message = "Status zaproszenia zaktualizowany." });
    }


    [HttpGet("check")]
    public IActionResult CheckExistingInvitation(int tripId, string username)
    {
        var user = _context.Users.FirstOrDefault(u => u.Username.ToLower() == username.ToLower());
        if (user == null)
        {
            return NotFound(new { message = "Użytkownik nie istnieje." });
        }

        // Sprawdź, czy użytkownik jest już członkiem wyjazdu
        var isMember = _context.UserTrips.Any(ut => ut.TripId == tripId && ut.UserId == user.Id);
        if (isMember)
        {
            return Ok(new { exists = true });
        }

        // Sprawdź, czy istnieje już zaproszenie dla tego użytkownika
        var invitationExists = _context.Invitations.Any(i => i.TripId == tripId && i.ReceiverId == user.Id && i.Status == "Oczekujące");
        return Ok(new { exists = invitationExists });
    }

    [HttpGet("pending-count")]
    public IActionResult GetPendingInvitationsCount()
    {
        var receiverId = GetUserIdFromToken();
        if (receiverId == null)
        {
            return Unauthorized(new { message = "Brak autoryzacji." });
        }

        var pendingCount = _context.Invitations
            .Count(i => i.ReceiverId == receiverId && i.Status == "Oczekujące");

        return Ok(new { count = pendingCount });
    }

    private int? GetUserIdFromToken()
    {
        try
        {
            var userIdClaim = User?.Claims?.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            Console.WriteLine($"UserId from token: {userIdClaim?.Value}");
            return userIdClaim != null ? int.Parse(userIdClaim.Value) : (int?)null;
        }
        catch
        {
            Console.WriteLine("Error retrieving UserId from token");
            return null;
        }
    }
}
