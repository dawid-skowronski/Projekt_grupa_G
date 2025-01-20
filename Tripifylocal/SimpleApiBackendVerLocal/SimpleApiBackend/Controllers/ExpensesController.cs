using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimpleApiBackend.Data;
using SimpleApiBackend.Models;
using System.Security.Claims;

namespace SimpleApiBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ExpensesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ExpensesController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateExpense([FromBody] CreateExpenseModel dto)
        {
            if (dto == null)
            {
                return BadRequest(new { message = "Nieprawidłowe dane wydatku." });
            }

            // Pobieranie userId z tokena
            var userId = GetUserIdFromToken();

            if (userId == null)
            {
                return Unauthorized(new { message = "Brak uprawnień. Użytkownik nie jest zalogowany." });
            }

            // Sprawdzenie czy podróż istnieje
            var trip = await _context.Trips
                .Include(t => t.UserTrips)
                .FirstOrDefaultAsync(t => t.TripId == dto.TripId);

            if (trip == null)
            {
                return NotFound(new { message = "Podróż nie istnieje." });
            }

            // Sprawdzamy, czy twórca (użytkownik) należy do tej podróży
            if (!trip.UserTrips.Any(ut => ut.UserId == userId))
            {
                return BadRequest(new { message = "Twórca nie należy do tej podróży." });
            }

            // Sprawdzamy, czy data wydatku mieści się w przedziale dat wyjazdu
            if (dto.Date < trip.StartDate || dto.Date > trip.EndDate)
            {
                return BadRequest(new { message = "Data wydatku musi mieścić się w przedziale dat wyjazdu." });
            }

            // Zapis wydatku
            var expense = new Expense
            {
                TripId = dto.TripId,
                CreatorId = userId.Value,
                Cost = dto.Cost,
                Currency = dto.Currency,
                Description = dto.Description,
                Date = dto.Date,
                Category = dto.Category,
                Location = string.IsNullOrWhiteSpace(dto.Location) ? null : dto.Location
            };

            _context.Expenses.Add(expense);
            await _context.SaveChangesAsync();

            // Naliczanie długu
            var tripMembers = trip.UserTrips.Select(ut => ut.UserId).ToList();
            int numberOfMembers = tripMembers.Count;

            if (numberOfMembers == 0)
            {
                return BadRequest(new { message = "Podróż nie ma członków." });
            }

            // Obliczanie kwoty długu na każdego członka (w tym twórcę)
            decimal debtAmount = expense.Cost / numberOfMembers;

            // Tworzenie długu tylko dla członków podróży, pomijając twórcę w tabeli
            var debts = tripMembers
                .Select(userId => new Debt
                {
                    ExpenseId = expense.ExpenseId,
                    UserId = userId,
                    Amount = Math.Round(debtAmount, 2),
                    Currency = dto.Currency,
                    Status = "unpaid"
                }).ToList();

            _context.Debts.AddRange(debts);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Wydatek i długi zapisane pomyślnie.", expenseId = expense.ExpenseId });
        }


        [HttpPost("addDebt")]
        public async Task<IActionResult> AddDebt([FromBody] CreateDebtModel dto)
        {
            if (dto == null)
            {
                return BadRequest(new { message = "Nieprawidłowe dane długu." });
            }

            var debt = new Debt
            {
                ExpenseId = dto.ExpenseId,
                UserId = dto.UserId,
                Amount = dto.Amount,
                Currency = dto.Currency,
                Status = dto.Status
            };

            _context.Debts.Add(debt);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Dług zapisany pomyślnie.", debtId = debt.DebtId });
        }

        [HttpGet("trip/{tripId}")]
        public async Task<IActionResult> GetExpensesForTrip(int tripId)
        {
            try
            {
                var expenses = await _context.Expenses
                    .Where(e => e.TripId == tripId)
                    .Select(e => new
                    {
                        e.ExpenseId,
                        e.Description,
                        e.Cost,
                        e.Currency,
                        e.Date,
                        e.Category, // Dodano kategorię
                        Debts = e.Debts.Select(d => new
                        {
                            d.DebtId,
                            d.Amount,
                            d.Currency,
                            d.Status,
                            User = new { d.User.Id, d.User.Username }
                        })
                    })
                    .ToListAsync();

                if (expenses == null || expenses.Count == 0)
                {
                    return NotFound(new { message = "Brak wydatków dla podanego wyjazdu." });
                }

                return Ok(expenses);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Wystąpił błąd serwera.", details = ex.Message });
            }
        }


        [HttpPut("updateDebtStatus/{debtId}")]
        public async Task<IActionResult> UpdateDebtStatus(int debtId, [FromBody] string newStatus)
        {
            try
            {
                var debt = await _context.Debts.FindAsync(debtId);
                if (debt == null)
                {
                    return NotFound(new { message = "Dług o podanym ID nie istnieje." });
                }

                // Aktualizacja statusu
                debt.Status = newStatus;
                _context.Debts.Update(debt);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Status długu został zaktualizowany.", debt });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Wystąpił błąd serwera.", details = ex.Message });
            }
        }


        [HttpGet("pending-debts-count")]
        [Authorize]
        public async Task<IActionResult> GetPendingDebtsCount()
        {
            try
            {
                // Pobierz ID zalogowanego użytkownika z tokena
                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new { message = "Brak odpowiednich uprawnień." });
                }

                // Zlicz długi o statusie "unpaid" należące do zalogowanego użytkownika, pomijając długi wobec siebie
                var pendingDebtsCount = await _context.Debts
                    .Where(d => d.Status == "unpaid" && d.UserId == userId && d.Expense.CreatorId != userId)
                    .CountAsync();

                return Ok(new { count = pendingDebtsCount });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Wystąpił błąd serwera.", details = ex.Message });
            }
        }




        [HttpGet("summary")]
        public async Task<IActionResult> GetDebtSummary()
        {
            try
            {
                // Pobierz userId z tokena
                var userId = GetUserIdFromToken();
                if (userId == null)
                {
                    return Unauthorized(new { message = "Brak uprawnień. Użytkownik nie jest zalogowany." });
                }

                // Pobierz wszystkie długi użytkownika (zarówno te, które mu należą, jak i te, które mu są długiem)
                var debtsYouOwe = await _context.Debts
                    .Where(d => d.UserId == userId && d.Expense.CreatorId != userId)
                    .Include(d => d.Expense)
                    .ThenInclude(e => e.Trip) // Dołączamy dane wyjazdu
                    .Select(d => new
                    {
                        d.DebtId,
                        d.Amount,
                        d.Currency,
                        d.Status,
                        d.Expense.Description,
                        d.Expense.TripId,
                        ExpenseDate = d.Expense.Date,
                        TripName = d.Expense.Trip.Name, // Pobieramy nazwę wyjazdu
                        Username = _context.Users.Where(u => u.Id == d.Expense.CreatorId).Select(u => u.Username).FirstOrDefault(),
                        PaymentRequestPending = _context.DebtPaymentRequests
                            .Any(p => p.DebtId == d.DebtId && p.RequestedById == userId && p.Status == "Pending")
                    })
                    .OrderBy(d => d.Status == "paid")
                    .ThenBy(d => d.ExpenseDate)
                    .ToListAsync();

                var debtsOwedToYou = await _context.Debts
                    .Where(d => d.Expense.CreatorId == userId && d.UserId != userId)
                    .Include(d => d.Expense)
                    .ThenInclude(e => e.Trip) // Dołączamy dane wyjazdu
                    .Select(d => new
                    {
                        d.DebtId,
                        d.Amount,
                        d.Currency,
                        d.Status,
                        d.Expense.Description,
                        d.Expense.TripId,
                        ExpenseDate = d.Expense.Date,
                        TripName = d.Expense.Trip.Name, // Pobieramy nazwę wyjazdu
                        Username = _context.Users.Where(u => u.Id == d.UserId).Select(u => u.Username).FirstOrDefault()
                    })
                    .OrderBy(d => d.Status == "paid")
                    .ThenBy(d => d.ExpenseDate)
                    .ToListAsync();

                // Zamiast sprawdzania aktywnych tripów, po prostu zwróć wszystkie długi
                return Ok(new { debtsYouOwe, debtsOwedToYou });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Wystąpił błąd serwera.", details = ex.Message });
            }
        }









        [HttpPost("request-payment")]
        public async Task<IActionResult> RequestPayment([FromBody] PaymentRequestModel model)
        {
            // Pobierz ID użytkownika z tokena
            var userId = GetUserIdFromToken();
            if (userId == null)
            {
                return Unauthorized(new { message = "Brak tokenu uwierzytelniającego." });
            }

            // Sprawdź, czy dług istnieje i należy do użytkownika
            var debt = await _context.Debts.Include(d => d.Expense).FirstOrDefaultAsync(d => d.DebtId == model.DebtId);
            if (debt == null || debt.UserId != userId)
            {
                return BadRequest(new { message = "Nie znaleziono długu lub brak dostępu." });
            }

            // Sprawdź, czy dług nie jest już opłacony
            if (debt.Status == "paid")
            {
                return BadRequest(new { message = "Dług został już zapłacony." });
            }

            // Utwórz nową prośbę o płatność
            var paymentRequest = new DebtPaymentRequest
            {
                DebtId = debt.DebtId,
                RequestedById = userId.Value,
                Status = model.Status ?? "Pending",
                PaymentMethod = model.PaymentMethod,
                RequestedAt = DateTime.UtcNow
            };

            _context.DebtPaymentRequests.Add(paymentRequest);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Zgłoszono płatność." });
        }



        [HttpPut("review-payment/{requestId}")]
        public async Task<IActionResult> ReviewPayment(int requestId, [FromBody] PaymentReviewModel model)
        {
            // Implementacja metody ReviewPayment
            var userId = GetUserIdFromToken();
            if (userId == null)
            {
                return Unauthorized(new { message = "Brak tokenu uwierzytelniającego." });
            }

            var paymentRequest = await _context.DebtPaymentRequests
                .Include(p => p.Debt)
                .ThenInclude(d => d.Expense)
                .FirstOrDefaultAsync(p => p.Id == requestId);

            if (paymentRequest == null || paymentRequest.Debt.Expense.CreatorId != userId)
            {
                return BadRequest(new { message = "Nie znaleziono zgłoszenia lub brak uprawnień." });
            }

            if (paymentRequest.Status != "Pending")
            {
                return BadRequest(new { message = "Zgłoszenie już zostało rozpatrzone." });
            }

            paymentRequest.Status = model.Approved ? "Accepted" : "Rejected";

            if (model.Approved)
            {
                paymentRequest.Debt.Status = "paid";
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Zgłoszenie rozpatrzone pomyślnie." });
        }

        [HttpGet("payment-requests")]
        public async Task<IActionResult> GetPaymentRequests()
        {
            var userId = GetUserIdFromToken();
            if (userId == null)
            {
                return Unauthorized(new { message = "Brak tokenu uwierzytelniającego." });
            }

            var paymentRequests = await _context.DebtPaymentRequests
                .Include(p => p.Debt)
                .ThenInclude(d => d.Expense)
                .Where(p => p.Debt.Expense.CreatorId == userId && p.Status == "Pending")
                .Select(p => new
                {
                    p.Id,
                    p.RequestedAt,
                    Amount = p.Debt.Amount,
                    Currency = p.Debt.Currency,
                    DebtDescription = p.Debt.Expense.Description,
                    RequestedBy = p.RequestedBy.Username,
                    p.PaymentMethod, // Dodaj metodę płatności
                })
                .ToListAsync();

            return Ok(paymentRequests);
        }

        [HttpGet("check-payment-request/{debtId}")]
        public async Task<IActionResult> CheckPaymentRequest(int debtId)
        {
            try
            {
                var exists = await _context.DebtPaymentRequests
                    .AnyAsync(p => p.DebtId == debtId && p.Status == "Pending");

                return Ok(new { exists });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Wystąpił błąd serwera.", details = ex.Message });
            }
        }

        [HttpGet("pending-payment-requests-count")]
        [Authorize]
        public async Task<IActionResult> GetPendingPaymentRequestsCount()
        {
            try
            {
                // Pobierz ID zalogowanego użytkownika z tokena
                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new { message = "Brak odpowiednich uprawnień." });
                }

                // Zlicz żądania płatności o statusie "Pending", które dotyczą użytkownika (jako odbiorcy)
                var pendingCount = await _context.DebtPaymentRequests
                    .Where(pr => pr.Status == "Pending" && pr.Debt.Expense.CreatorId == userId) // Sprawdzamy, czy to użytkownik jest twórcą wydatku
                    .CountAsync();

                return Ok(new { count = pendingCount });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Wystąpił błąd serwera.", details = ex.Message });
            }
        }




        [HttpPut("updateDebts/{expenseId}")]
        [Authorize]
        public async Task<IActionResult> UpdateDebts(int expenseId, [FromBody] UpdateDebtsModel model)
        {
            try
            {
                // Pobierz ID zalogowanego użytkownika z tokena
                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new { message = "Brak odpowiednich uprawnień." });
                }

                // Pobierz wydatek wraz z długami
                var expense = await _context.Expenses
                    .Include(e => e.Debts)
                    .FirstOrDefaultAsync(e => e.ExpenseId == expenseId);

                if (expense == null)
                {
                    return NotFound(new { message = "Wydatek o podanym ID nie istnieje." });
                }

                // Sprawdź, czy zalogowany użytkownik jest twórcą wydatku
                if (expense.CreatorId != userId)
                {
                    return BadRequest(new { message = "Nie masz uprawnień do modyfikacji tego wydatku." });
                }

                // Oblicz całkowitą sumę długu
                var totalAssignedAmount = model.Debts.Sum(d => d.NewAmount);
                var difference = totalAssignedAmount - expense.Cost;

                if (difference != 0)
                {
                    var direction = difference > 0 ? "za dużo" : "za mało";
                    return BadRequest(new { message = $"Podział jest nieprawidłowy. Rozdzielono {Math.Abs(difference):0.00} {expense.Currency} {direction}." });
                }

                // Zaktualizuj długi
                foreach (var debtUpdate in model.Debts)
                {
                    var debt = expense.Debts.FirstOrDefault(d => d.DebtId == debtUpdate.DebtId);
                    if (debt != null)
                    {
                        debt.Amount = debtUpdate.NewAmount;
                    }
                }

                await _context.SaveChangesAsync();

                return Ok(new { message = "Długi zostały zaktualizowane." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Wystąpił błąd serwera.", details = ex.Message });
            }
        }


        [HttpDelete("{expenseId}")]
        [Authorize]
        public async Task<IActionResult> DeleteExpense(int expenseId)
        {
            try
            {
                // Pobierz ID zalogowanego użytkownika z tokena
                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new { message = "Brak odpowiednich uprawnień." });
                }

                // Pobierz wydatek wraz z długami
                var expense = await _context.Expenses
                    .Include(e => e.Debts)
                    .FirstOrDefaultAsync(e => e.ExpenseId == expenseId);

                if (expense == null)
                {
                    return NotFound(new { message = "Wydatek o podanym ID nie istnieje." });
                }

                // Sprawdź, czy zalogowany użytkownik jest twórcą wydatku
                if (expense.CreatorId != userId)
                {
                    return BadRequest(new { message = "Nie masz uprawnień do usunięcia tego wydatku." });
                }

                // Usuń długi związane z wydatkiem
                _context.Debts.RemoveRange(expense.Debts);

                // Usuń wydatek
                _context.Expenses.Remove(expense);

                // Zapisz zmiany w bazie danych
                await _context.SaveChangesAsync();

                return Ok(new { message = "Wydatek został pomyślnie usunięty." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Wystąpił błąd serwera.", details = ex.Message });
            }
        }



        [HttpGet("summary/{tripId}")]
        public async Task<IActionResult> GetTripExpensesSummary(int tripId)
        {
            try
            {
                // Pobranie wydatków dla danego wyjazdu
                var expenses = await _context.Expenses
                    .Where(e => e.TripId == tripId)
                    .ToListAsync();

                if (expenses == null || !expenses.Any())
                {
                    return NotFound(new { message = "Brak wydatków dla podanego wyjazdu." });
                }

                // Obliczenie sumy wydatków
                var totalAmount = expenses.Sum(e => e.Cost);
                var currency = expenses.FirstOrDefault()?.Currency ?? "PLN"; // Domyślnie PLN, jeśli nie znaleziono wydatków z walutą

                // Zwrócenie podsumowania
                return Ok(new
                {
                    tripId,
                    totalAmount,
                    currency,
                    expenseCount = expenses.Count
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Wystąpił błąd serwera.", details = ex.Message });
            }
        }

        [HttpGet("debts-summary")]
        [Authorize]
        public async Task<IActionResult> GetDebtsSummaryByUser()
        {
            try
            {
                // Pobierz ID zalogowanego użytkownika z tokena
                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new { message = "Brak odpowiednich uprawnień." });
                }

                // Pobierz długi użytkownika, gdzie jest dłużnikiem
                var debts = await _context.Debts
                    .Where(d => d.UserId == userId && d.Status != "paid")
                    .Include(d => d.Expense)
                    .ThenInclude(e => e.Creator)
                    .ToListAsync();

                if (!debts.Any())
                {
                    return Ok(new { message = "Nie masz żadnych aktywnych długów." });
                }

                // Grupowanie długów według twórcy (użytkownika), ale wykluczenie siebie
                var groupedDebts = debts
                    .Where(d => d.Expense.CreatorId != userId) // Wykluczanie samego siebie
                    .GroupBy(d => new { d.Expense.CreatorId, d.Currency })
                    .Select(g => new
                    {
                        CreatorId = g.Key.CreatorId,
                        Currency = g.Key.Currency,
                        TotalAmount = g.Sum(d => d.Amount),
                        CreatorName = _context.Users
                            .Where(u => u.Id == g.Key.CreatorId)
                            .Select(u => u.Username)
                            .FirstOrDefault()
                    })
                    .ToList();

                return Ok(groupedDebts);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Wystąpił błąd serwera.", details = ex.Message });
            }
        }




        // Model dla nowej kwoty długu
        public class UpdateDebtModel
        {
            public int DebtId { get; set; } // Identyfikator długu, który jest aktualizowany
            public decimal NewAmount { get; set; }
        }


        private int? GetUserIdFromToken()
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            Console.WriteLine($"UserId from token: {userIdClaim?.Value}"); // Logowanie ID użytkownika
            return userIdClaim != null ? int.Parse(userIdClaim.Value) : (int?)null;
        }
    }
}
