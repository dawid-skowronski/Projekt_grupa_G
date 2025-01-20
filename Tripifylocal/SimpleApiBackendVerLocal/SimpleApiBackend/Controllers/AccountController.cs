using BCrypt.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SimpleApiBackend.Data;
using SimpleApiBackend.Models;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Mail;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace SimpleApiBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public AccountController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // Rejestracja użytkownika
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            if (model.Password != model.ConfirmPassword)
            {
                return BadRequest(new { message = "Hasła nie pasują do siebie." });
            }

            if (!IsPasswordStrong(model.Password))
            {
                return BadRequest(new { message = "Hasło musi zawierać co najmniej 8 znaków, jedną wielką literę, jedną małą literę, cyfrę i znak specjalny." });
            }

            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == model.Username || u.Email == model.Email);

            if (existingUser != null)
            {
                return BadRequest(new { message = "Użytkownik o takim loginie lub emailu już istnieje." });
            }

            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(model.Password);

            var user = new User
            {
                Username = model.Username,
                Email = model.Email,
                Password = hashedPassword,
                Role = "User",
                IsEmailConfirmed = false // Konto domyślnie nieaktywne
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Generowanie tokenu aktywacyjnego i wysyłanie e-maila
            var token = GenerateEmailConfirmationToken(user);
            SendConfirmationEmail(user.Email, token);

            return Ok(new { message = "Rejestracja zakończona sukcesem. Sprawdź e-mail, aby potwierdzić swoje konto." });
        }

        [HttpGet("confirm-email")]
        public async Task<IActionResult> ConfirmEmail(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return BadRequest(new { message = "Nieprawidłowy token aktywacyjny." });
            }

            try
            {
                var handler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_configuration["Jwt:EmailSecretKey"]);
                var parameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero
                };

                var claimsPrincipal = handler.ValidateToken(token, parameters, out _);
                var userIdClaim = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier);

                if (userIdClaim == null)
                {
                    return BadRequest(new { message = "Token nie zawiera identyfikatora użytkownika." });
                }

                var userId = int.Parse(userIdClaim.Value);
                var user = await _context.Users.FindAsync(userId);

                if (user == null) return BadRequest(new { message = "Nieprawidłowy token aktywacyjny." });

                user.IsEmailConfirmed = true;
                await _context.SaveChangesAsync();

                return Ok(new { message = "Twoje konto zostało potwierdzone." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd potwierdzania emaila: {ex.Message}");
                return BadRequest(new { message = "Nieprawidłowy lub wygasły token aktywacyjny." });
            }
        }


        // Logowanie użytkownika
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == model.Username);

            if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.Password))
            {
                return Unauthorized(new { message = "Nieprawidłowe dane logowania." });
            }

            if (!user.IsEmailConfirmed)
            {
                return Unauthorized(new { message = "Potwierdź swoje konto przed zalogowaniem." });
            }

            var token = GenerateJwtToken(user);

            return Ok(new { message = "Logowanie udane.", token });
        }

        // Sprawdzanie dostępności nazwy użytkownika
        [HttpGet("check-username")]
        public async Task<IActionResult> CheckUsernameAvailability([FromQuery] string username)
        {
            var exists = await _context.Users.AnyAsync(u => u.Username == username);
            return Ok(new { isAvailable = !exists });
        }

        [HttpPut("edit-profile")]
        public async Task<IActionResult> EditProfile([FromBody] EditProfileModel model)
        {
            var userId = GetUserIdFromToken();

            if (!userId.HasValue)
            {
                Console.WriteLine("Błąd: Nie udało się pobrać ID użytkownika z tokenu.");
                return Unauthorized(new { message = "Nieprawidłowy token." });
            }

            var user = await _context.Users.FindAsync(userId.Value);

            if (user == null)
            {
                Console.WriteLine("Błąd: Użytkownik nie znaleziony dla ID: " + userId.Value);
                return NotFound(new { message = "Użytkownik nie znaleziony." });
            }

            var usernameExists = await _context.Users.AnyAsync(u => u.Username == model.NewUsername && u.Id != userId);
            if (usernameExists)
            {
                return BadRequest(new { message = "Nazwa użytkownika jest już zajęta." });
            }

            user.Username = model.NewUsername;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Profil zaktualizowany pomyślnie." });
        }


        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            try
            {
                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                {
                    return Unauthorized(new { message = "Token JWT nie zawiera informacji o użytkowniku." });
                }

                var userId = int.Parse(userIdClaim.Value);
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                {
                    return NotFound(new { message = "Użytkownik nie został znaleziony." });
                }

                return Ok(new
                {
                    username = user.Username,
                    email = user.Email
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd podczas pobierania profilu: {ex.Message}");
                return StatusCode(500, new { message = "Wystąpił błąd serwera." });
            }
        }

        private void SendConfirmationEmail(string email, string token)
        {
            try
            {
                var confirmationLink = $"{_configuration["FrontendUrl"]}/confirm-email.html?token={token}";

                var mailMessage = new MailMessage("noreply@projekt-tripfy.pl", email)
                {
                    Subject = "Potwierdź swoje konto",
                    Body = $"<p>Dziękujemy za rejestrację!</p><p>Kliknij poniższy link, aby potwierdzić swoje konto:</p><a href='{confirmationLink}'>Potwierdź konto</a>",
                    IsBodyHtml = true
                };

                using (var smtpClient = new SmtpClient(_configuration["Email:SmtpServer"], int.Parse(_configuration["Email:SmtpPort"])))
                {
                    smtpClient.Credentials = new System.Net.NetworkCredential(_configuration["Email:Email"], _configuration["Email:Password"]);
                    smtpClient.EnableSsl = true;
                    smtpClient.Send(mailMessage);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd wysyłania maila: {ex.Message}");
            }
        }


        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordModel model)
        {
            if (string.IsNullOrEmpty(model.Email))
            {
                return BadRequest(new { message = "Email nie może być pusty." });
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);

            if (user == null)
            {
                Console.WriteLine("Błąd: Użytkownik nie znaleziony dla emaila: " + model.Email);
                return BadRequest(new { message = "Nie znaleziono użytkownika z podanym adresem e-mail." });
            }

            // Generowanie tokenu
            var token = GenerateResetPasswordToken(user);

            if (string.IsNullOrEmpty(token))
            {
                Console.WriteLine("Błąd: Token resetowania hasła jest pusty!");
                return StatusCode(500, new { message = "Wystąpił błąd podczas generowania tokenu." });
            }

            // Wysyłanie e-maila z linkiem resetującym hasło
            SendResetPasswordEmail(user.Email, token);

            return Ok(new { message = "Link do resetowania hasła został wysłany na podany adres e-mail." });
        }

        private void SendResetPasswordEmail(string email, string token)
        {
            var resetLink = $"{_configuration["FrontendUrl"]}/reset-password.html?token={token}";

            var mailMessage = new MailMessage("noreply@projekt-tripfy.pl", email)
            {
                Subject = "Resetowanie hasła",
                Body = $"<p>Kliknij poniższy link, aby zresetować swoje hasło:</p><a href='{resetLink}'>Resetuj hasło</a>",
                IsBodyHtml = true
            };

            using (var smtpClient = new SmtpClient(_configuration["Email:SmtpServer"], int.Parse(_configuration["Email:SmtpPort"])))
            {
                smtpClient.Credentials = new System.Net.NetworkCredential(_configuration["Email:Email"], _configuration["Email:Password"]);
                smtpClient.EnableSsl = true;
                smtpClient.Send(mailMessage);
            }
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordModel model)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_configuration["Jwt:EmailSecretKey"]);
                var parameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero
                };

                var claimsPrincipal = handler.ValidateToken(model.Token, parameters, out _);
                var userId = int.Parse(claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value);

                var user = await _context.Users.FindAsync(userId);
                if (user == null) return BadRequest(new { message = "Nieprawidłowy token resetujący." });

                // Ustaw nowe hasło
                user.Password = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Hasło zostało zresetowane pomyślnie." });
            }
            catch
            {
                return BadRequest(new { message = "Nieprawidłowy lub wygasły token resetujący." });
            }
        }

        [HttpGet("login-facebook")]
        public IActionResult LoginWithFacebook()
        {
            var redirectUrl = Url.Action(nameof(HandleFacebookLoginCallback));
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
            return Challenge(properties, FacebookDefaults.AuthenticationScheme);
        }


        [HttpGet("signin-facebook")]
        public async Task<IActionResult> HandleFacebookLoginCallback()
        {
            try
            {
                Console.WriteLine("Rozpoczynam uwierzytelnianie przez Facebook.");
                var authenticateResult = await HttpContext.AuthenticateAsync(FacebookDefaults.AuthenticationScheme);

                if (!authenticateResult.Succeeded)
                {
                    Console.WriteLine($"Facebook authentication failed: {authenticateResult.Failure?.Message}");
                    if (authenticateResult.Failure?.InnerException != null)
                    {
                        Console.WriteLine($"Inner Exception: {authenticateResult.Failure.InnerException.Message}");
                    }
                    return Redirect("https://projekt-tripfy.pl/login");
                }

                var facebookId = authenticateResult.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var nameClaim = authenticateResult.Principal.FindFirst(ClaimTypes.Name)?.Value;

                Console.WriteLine($"Pobrano dane: FacebookId = {facebookId}, Name = {nameClaim}");

                if (string.IsNullOrEmpty(facebookId))
                {
                    Console.WriteLine("Brak FacebookId w odpowiedzi.");
                    return Redirect("/error?message=MissingFacebookId");
                }

                User user;

                // Generowanie emaila na podstawie FacebookId
                var generatedEmail = $"user_{facebookId}@facebook.local";

                // Sprawdzenie, czy użytkownik już istnieje na podstawie wygenerowanego emaila
                var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == generatedEmail);
                if (existingUser == null)
                {
                    Console.WriteLine("Tworzenie nowego użytkownika, ponieważ nie znaleziono istniejącego.");

                    // Generowanie losowego hasła
                    var randomPassword = GenerateRandomPassword();
                    var hashedPassword = BCrypt.Net.BCrypt.HashPassword(randomPassword);

                    user = new User
                    {
                        Username = nameClaim, // Imię i nazwisko jako username
                        Email = generatedEmail, // Email generowany na podstawie FacebookId
                        Password = hashedPassword, // Losowe hasło
                        Role = "User",
                        IsEmailConfirmed = true // Użytkownicy Facebooka są automatycznie potwierdzani
                    };

                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();

                    user.Username = $"{nameClaim}_{user.Id}";
                    await _context.SaveChangesAsync();


                    Console.WriteLine($"Utworzono nowego użytkownika: {user.Username}, Email: {user.Email}");
                }
                else
                {
                    Console.WriteLine("Użytkownik istnieje. Logowanie.");
                    user = existingUser;
                }

                // Generowanie tokenu JWT
                var token = GenerateJwtToken(user);

                // Przekierowanie na stronę główną z tokenem w URL
                var redirectUrl = $"https://projekt-tripfy.pl/login.html?token={Uri.EscapeDataString(token)}";
                return Redirect(redirectUrl);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}\nStackTrace: {ex.StackTrace}");
                return StatusCode(500, new { message = "Wystąpił błąd serwera." });
            }
        }


        private string GenerateRandomPassword(int length = 12)
        {
            const string validChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890!@#$%^&*()";
            var random = new Random();
            return new string(Enumerable.Repeat(validChars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }




        private string GenerateEmailConfirmationToken(User user)
        {
            var keyString = _configuration["Jwt:EmailSecretKey"];
            if (string.IsNullOrEmpty(keyString))
            {
                throw new ArgumentNullException(nameof(keyString), "JWT Email Secret Key is null or empty");
            }

            var key = Encoding.UTF8.GetBytes(keyString);
            if (key.Length < 32)
            {
                Console.WriteLine("❌ BŁĄD: Klucz JWT jest za krótki! Długość: " + key.Length);
                throw new InvalidOperationException("JWT SecretKey is too short!");
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var expirationTime = DateTime.UtcNow.AddMinutes(30); // ⏳ Ustal termin wygaśnięcia

            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Email, user.Email),
        new Claim(JwtRegisteredClaimNames.Exp, expirationTime.ToUniversalTime().ToString("o")) // 🔥 **Dodane poprawnie Exp!**
    };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = expirationTime, // ⏳ Upewniamy się, że Exp jest w SecurityToken
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            // ✅ Debugowanie problemu
            Console.WriteLine($"🔎 Token wygenerowany: {tokenString}");
            Console.WriteLine($"✅ Wygenerowany token ma datę wygaśnięcia: {expirationTime}");

            return tokenString;
        }











        // Generowanie tokenu JWT do logowania
        private string GenerateJwtToken(User user)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:SecretKey"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }


        private string GenerateResetPasswordToken(User user)
        {
            var keyString = _configuration["Jwt:EmailSecretKey"];
            if (string.IsNullOrEmpty(keyString))
            {
                throw new ArgumentNullException(nameof(keyString), "JWT Email Secret Key is null or empty");
            }

            var key = Encoding.UTF8.GetBytes(keyString);
            if (key.Length < 32)
            {
                throw new InvalidOperationException("JWT SecretKey is too short!");
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
        }),
                Expires = DateTime.UtcNow.AddHours(1), // ✅ Dodajemy termin wygaśnięcia!**
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }




        // Walidacja wymagań dotyczących hasła
        private bool IsPasswordStrong(string password)
        {
            return password.Length >= 8
                && password.Any(char.IsUpper)
                && password.Any(char.IsLower)
                && password.Any(char.IsDigit)
                && password.Any(ch => "!@#$%^&*()_+-=[]{}|;:'\",.<>?/`~".Contains(ch));
        }

        // Pobieranie ID użytkownika z tokenu
        private int GetUserIdFromToken(string token)
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token.Replace("Bearer ", ""));
            var userIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            return int.Parse(userIdClaim.Value);
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
}
