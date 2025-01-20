using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimpleApiBackend.Data;
using SimpleApiBackend.Models;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using SimpleApiBackend.Models;


namespace SimpleApiBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TripController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public TripController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateTrip([FromBody] TripCreateModel model)
        {
            if (model == null)
            {
                return BadRequest(new ErrorResponse { Message = "Dane są niekompletne." });
            }

            var userId = GetUserIdFromToken();
            if (userId == null)
            {
                return Unauthorized(new ErrorResponse { Message = "Nie można znaleźć użytkownika." });
            }

            var secretCode = GenerateSecretCode();

            var trip = new Trip
            {
                CreatorId = userId.Value,
                Name = model.Name,
                Description = model.Description,
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                SecretCode = secretCode,
                UserTrips = new List<UserTrip>
                {
                    new UserTrip { UserId = userId.Value }
                },
                Invitations = new List<Invitation>()
            };

            try
            {
                _context.Trips.Add(trip);
                await _context.SaveChangesAsync();

                return Ok(new CreateTripResponse { TripId = trip.TripId, SecretCode = trip.SecretCode });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd podczas zapisywania wyjazdu: {ex.Message}");
                return StatusCode(500, new ErrorResponse { Message = "Wystąpił błąd podczas tworzenia wyjazdu." });
            }
        }

        [HttpPost("join")]
        public async Task<IActionResult> JoinTrip([FromBody] JoinTripModel model)
        {
            var userId = model.UserId;
            if (userId == null)
            {
                return Unauthorized(new ErrorResponse { Message = "Nie można znaleźć użytkownika." });
            }

            var trip = await _context.Trips
                .Include(t => t.UserTrips)
                .ThenInclude(ut => ut.User)
                .FirstOrDefaultAsync(t => t.SecretCode == model.SecretCode);

            if (trip == null)
            {
                return NotFound(new ErrorResponse { Message = "Wyjazd nie został znaleziony." });
            }

            if (trip.UserTrips.Any(m => m.UserId == userId))
            {
                return BadRequest(new ErrorResponse { Message = "Jesteś już członkiem tego wyjazdu." });
            }

            trip.UserTrips.Add(new UserTrip { UserId = userId });
            await _context.SaveChangesAsync();

            return Ok(new ErrorResponse { Message = "Dołączono do wyjazdu." });
        }

        [HttpGet("details/{tripId}")]
        public async Task<IActionResult> GetTripDetails(int tripId)
        {
            var trip = await _context.Trips
                .Include(t => t.UserTrips)
                .ThenInclude(ut => ut.User)
                .FirstOrDefaultAsync(t => t.TripId == tripId);

            if (trip == null)
            {
                return NotFound(new ErrorResponse { Message = "Wyjazd nie został znaleziony." });
            }

            var members = trip.UserTrips.Select(m => new
            {
                m.User.Id,
                m.User.Username,
                m.User.Email,
                IsOffline = m.IsOffline // Przekazujemy status offline
            }).ToList();

            return Ok(new
            {
                tripId = trip.TripId,
                name = trip.Name,
                description = trip.Description,
                startDate = trip.StartDate,
                endDate = trip.EndDate,
                secretCode = trip.SecretCode,
                members
            });
        }


        [HttpGet("my-trips")]
        public async Task<IActionResult> GetUserTrips(int? UserId)
        {
            var userId = UserId ?? GetUserIdFromToken();

            if (userId == null)
            {
                return Unauthorized(new ErrorResponse { Message = "Nie można znaleźć użytkownika." });
            }

            var trips = await _context.UserTrips
                .Where(ut => ut.UserId == userId.Value && !ut.IsOffline) // Tylko tripy, które użytkownik zaakceptował
                .Select(ut => new
                {
                    tripId = ut.Trip.TripId,
                    name = ut.Trip.Name,
                    startDate = ut.Trip.StartDate.ToString("yyyy-MM-dd"),
                    endDate = ut.Trip.EndDate.ToString("yyyy-MM-dd"),
                    secretCode = ut.Trip.SecretCode
                })
                .ToListAsync();

            if (trips.Count == 0)
            {
                return Ok(new ErrorResponse { Message = "Nie masz żadnych wyjazdów." });
            }

            return Ok(trips);
        }


        [HttpDelete("leave/{tripId}")]
        public async Task<IActionResult> LeaveTrip(int tripId)
        {
            var userId = GetUserIdFromToken();
            if (userId == null)
            {
                return Unauthorized(new ErrorResponse { Message = "Nie znaleziono użytkownika w tokenie." });
            }

            var userTrip = await _context.UserTrips
                .FirstOrDefaultAsync(ut => ut.TripId == tripId && ut.UserId == userId);

            if (userTrip == null)
            {
                return NotFound(new ErrorResponse { Message = "Nie jesteś członkiem tego wyjazdu." });
            }

            _context.UserTrips.Remove(userTrip);
            await _context.SaveChangesAsync();

            return Ok(new ErrorResponse { Message = "Wyjazd opuszczony pomyślnie." });
        }

        private int? GetUserIdFromToken()
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            Console.WriteLine($"UserId from token: {userIdClaim?.Value}");
            return userIdClaim != null ? int.Parse(userIdClaim.Value) : (int?)null;
        }

        private string GenerateSecretCode()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 8).Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}
