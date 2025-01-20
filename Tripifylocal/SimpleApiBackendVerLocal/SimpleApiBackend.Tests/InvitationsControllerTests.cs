using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using SimpleApiBackend.Controllers;
using SimpleApiBackend.Data;
using SimpleApiBackend.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace SimpleApiBackend.Tests
{
    /// <summary>
    /// Testy jednostkowe dla kontrolera InvitationsController.
    /// </summary>
    public class InvitationsControllerTests : IDisposable
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly InvitationsController _controller;

        /// <summary>
        /// Inicjalizuje testy, tworz�c now� instancj� kontrolera oraz baz� danych w pami�ci.
        /// </summary>
        public InvitationsControllerTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _dbContext = new ApplicationDbContext(options);
            _controller = new InvitationsController(_dbContext);
        }

        /// <summary>
        /// Ustawia kontekst u�ytkownika dla test�w wymagaj�cych autoryzacji.
        /// </summary>
        /// <param name="userId">Identyfikator u�ytkownika.</param>
        private void SetUserContext(int userId)
        {
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            };

            var identity = new ClaimsIdentity(userClaims, "TestAuthType");
            var userPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = userPrincipal
                }
            };
        }

        /// <summary>
        /// Sprawdza, czy metoda SendInvitation zwraca Unauthorized, gdy u�ytkownik nie jest zalogowany.
        /// </summary>
        [Fact]
        public async Task SendInvitation_ShouldReturnUnauthorized_IfUserNotLoggedIn()
        {
            // ACT: Wywo�anie metody SendInvitation bez zalogowanego u�ytkownika.
            var result = await _controller.SendInvitation(new InvitationCreateModel());

            // ASSERT: Powinno zwr�ci� UnauthorizedObjectResult z odpowiednim komunikatem.
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.NotNull(unauthorizedResult.Value);

            var response = JObject.FromObject(unauthorizedResult.Value);
            Assert.Equal("Nie znaleziono u�ytkownika w tokenie.", response["message"].ToString());
        }

        /// <summary>
        /// Sprawdza, czy metoda SendInvitation zwraca BadRequest, gdy odbiorca zaproszenia nie istnieje.
        /// </summary>
        [Fact]
        public async Task SendInvitation_ShouldReturnBadRequest_IfReceiverNotFound()
        {
            // ARRANGE: Ustawienie kontekstu u�ytkownika i pr�ba wys�ania zaproszenia do nieistniej�cego u�ytkownika.
            SetUserContext(1);
            var model = new InvitationCreateModel { ReceiverUsername = "NonExistentUser", TripId = 1 };

            // ACT: Wywo�anie metody SendInvitation.
            var result = await _controller.SendInvitation(model);

            // ASSERT: Powinno zwr�ci� BadRequestObjectResult z odpowiednim komunikatem.
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequestResult.Value);

            var response = JObject.FromObject(badRequestResult.Value);
            Assert.Equal("Nieprawid�owy ID u�ytkownika wysy�aj�cego.", response["message"].ToString());
        }

        /// <summary>
        /// Sprawdza, czy metoda SendInvitation poprawnie tworzy zaproszenie, gdy dane s� poprawne.
        /// </summary>
        [Fact]
        public async Task SendInvitation_ShouldCreateInvitation_IfValid()
        {
            // ARRANGE: Ustawienie u�ytkownika nadawcy i odbiorcy.
            SetUserContext(1);
            _dbContext.Users.Add(new User { Id = 1, Username = "Sender", Email = "sender@example.com", Password = "hashed_password" });
            _dbContext.Users.Add(new User { Id = 2, Username = "Receiver", Email = "receiver@example.com", Password = "hashed_password" });
            await _dbContext.SaveChangesAsync();

            var model = new InvitationCreateModel { ReceiverUsername = "Receiver", TripId = 1 };

            // ACT: Wys�anie zaproszenia.
            var result = await _controller.SendInvitation(model);

            // ASSERT: Powinno zwr�ci� OkObjectResult z poprawnym komunikatem.
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
            var response = JObject.FromObject(okResult.Value);
            Assert.Equal("Zaproszenie wys�ane pomy�lnie.", response["message"].ToString());

            // Sprawdzenie, czy zaproszenie zosta�o zapisane w bazie danych.
            var invitation = _dbContext.Invitations.FirstOrDefault();
            Assert.NotNull(invitation);
            Assert.Equal(1, invitation.SenderId);
            Assert.Equal(2, invitation.ReceiverId);
        }

        /// <summary>
        /// Sprawdza, czy metoda GetReceivedInvitations zwraca Unauthorized, gdy u�ytkownik nie jest zalogowany.
        /// </summary>
        [Fact]
        public void GetReceivedInvitations_ShouldReturnUnauthorized_IfUserNotLoggedIn()
        {
            // ACT: Wywo�anie metody GetReceivedInvitations bez zalogowanego u�ytkownika.
            var result = _controller.GetReceivedInvitations();

            // ASSERT: Powinno zwr�ci� UnauthorizedObjectResult z odpowiednim komunikatem.
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.NotNull(unauthorizedResult.Value);
            var response = JObject.FromObject(unauthorizedResult.Value);
            Assert.Equal("Nie znaleziono u�ytkownika w tokenie.", response["message"].ToString());
        }

        /// <summary>
        /// Sprawdza, czy metoda RespondToInvitation zwraca NotFound, gdy zaproszenie nie istnieje.
        /// </summary>
        [Fact]
        public async Task RespondToInvitation_ShouldReturnNotFound_IfInvitationDoesNotExist()
        {
            // ARRANGE: Ustawienie kontekstu u�ytkownika.
            SetUserContext(1);
            var model = new InvitationResponseModel { InvitationId = 999, Status = "Accepted" };

            // ACT: Pr�ba odpowiedzi na nieistniej�ce zaproszenie.
            var result = await _controller.RespondToInvitation(model);

            // ASSERT: Powinno zwr�ci� NotFoundObjectResult.
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.NotNull(notFoundResult.Value);
            var response = JObject.FromObject(notFoundResult.Value);
            Assert.Equal("Zaproszenie nie zosta�o znalezione.", response["message"].ToString());
        }


        /// <summary>
        /// Sprawdza, czy metoda RespondToInvitation poprawnie aktualizuje status zaproszenia, gdy dane s� poprawne.
        /// </summary>
        [Fact]
        public async Task RespondToInvitation_ShouldUpdateStatus_IfValid()
        {
            // ARRANGE: Ustawienie kontekstu u�ytkownika i dodanie zaproszenia do bazy.
            SetUserContext(1);
            var invitation = new Invitation
            {
                InvitationId = 1,
                TripId = 1,
                SenderId = 2,
                ReceiverId = 1,
                Status = "Oczekuj�ce"
            };
            _dbContext.Invitations.Add(invitation);
            await _dbContext.SaveChangesAsync();

            var model = new InvitationResponseModel { InvitationId = 1, Status = "Accepted" };

            // ACT: Aktualizacja statusu zaproszenia.
            var result = await _controller.RespondToInvitation(model);

            // ASSERT: Powinno zwr�ci� OkObjectResult z odpowiednim komunikatem.
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
            var response = JObject.FromObject(okResult.Value);
            Assert.Equal("Status zaproszenia zaktualizowany.", response["message"].ToString());

            // Sprawdzenie, czy status zaproszenia zosta� poprawnie zaktualizowany w bazie.
            var updatedInvitation = _dbContext.Invitations.First();
            Assert.Equal("Accepted", updatedInvitation.Status);
        }

        /// <summary>
        /// Sprawdza, czy metoda GetReceivedInvitations zwraca list� zaprosze�, gdy u�ytkownik ma zaproszenia.
        /// </summary>
        [Fact]
        public async Task GetReceivedInvitations_ShouldReturnList_IfUserHasInvitations()
        {
            // ARRANGE: Ustawienie kontekstu u�ytkownika i dodanie zaproszenia do bazy.
            SetUserContext(1);

            var sender = new User { Id = 2, Username = "Sender", Email = "sender@example.com", Password = "hashed_password" };
            var trip = new Trip
            {
                TripId = 1,
                Name = "Test Trip",
                Description = "Opis wyjazdu",
                SecretCode = "XYZ123"
            };

            _dbContext.Trips.Add(trip);
            _dbContext.Users.Add(sender);
            _dbContext.Invitations.Add(new Invitation
            {
                InvitationId = 1,
                TripId = 1,
                SenderId = 2,
                ReceiverId = 1,
                Status = "Oczekuj�ce",
                CreatedAt = DateTime.Now
            });

            await _dbContext.SaveChangesAsync();

            // ACT: Pobranie listy zaprosze� u�ytkownika.
            var result = _controller.GetReceivedInvitations();

            // ASSERT: Powinno zwr�ci� OkObjectResult zawieraj�cy list� zaprosze�.
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = (okResult.Value as IEnumerable<object>).ToList();
            Assert.NotEmpty(response);

            // Sprawdzenie, czy zwr�cona lista zawiera jedno zaproszenie.
            Assert.Single(response);
        }

        /// <summary>
        /// Sprawdza, czy metoda RespondToInvitation usuwa zaproszenie, gdy u�ytkownik je odrzuci.
        /// </summary>
        [Fact]
        public async Task RespondToInvitation_ShouldRemoveInvitation_IfRejected()
        {
            // ARRANGE: Ustawienie kontekstu u�ytkownika i dodanie zaproszenia do bazy.
            SetUserContext(1);

            var invitation = new Invitation
            {
                InvitationId = 1,
                TripId = 1,
                SenderId = 2,
                ReceiverId = 1,
                Status = "Oczekuj�ce"
            };
            _dbContext.Invitations.Add(invitation);
            _dbContext.UserTrips.Add(new UserTrip { UserId = 1, TripId = 1, IsOffline = true });
            await _dbContext.SaveChangesAsync();

            var model = new InvitationResponseModel { InvitationId = 1, Status = "Rejected" };

            // ACT: Odrzucenie zaproszenia.
            var result = await _controller.RespondToInvitation(model);

            // ASSERT: Powinno zwr�ci� OkObjectResult z odpowiednim komunikatem.
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("Status zaproszenia zaktualizowany.", JObject.FromObject(okResult.Value)["message"].ToString());

            // Sprawdzenie, czy zaproszenie zosta�o usuni�te z bazy danych.
            var deletedInvitation = await _dbContext.Invitations.FindAsync(1);
            Assert.Null(deletedInvitation);

            // Sprawdzenie, czy u�ytkownik zosta� usuni�ty z podr�y.
            var userTrip = await _dbContext.UserTrips.FirstOrDefaultAsync(ut => ut.UserId == 1 && ut.TripId == 1);
            Assert.Null(userTrip);
        }

        /// <summary>
        /// Sprawdza, czy metoda CheckExistingInvitation zwraca True, gdy u�ytkownik jest ju� cz�onkiem podr�y.
        /// </summary>
        [Fact]
        public void CheckExistingInvitation_ShouldReturnTrue_IfUserIsAlreadyMember()
        {
            // ARRANGE: Dodanie u�ytkownika do bazy i przypisanie go do podr�y.
            var existingTrip = _dbContext.Trips.FirstOrDefault(t => t.TripId == 1);
            if (existingTrip != null)
            {
                _dbContext.Entry(existingTrip).State = EntityState.Detached;
            }

            _dbContext.Users.Add(new User { Id = 1, Username = "User1", Email = "user1@example.com", Password = "hashed_password" });

            _dbContext.Trips.Add(new Trip
            {
                TripId = 1,
                Name = "Example Trip",
                Description = "Test description",
                SecretCode = "XYZ123"
            });

            _dbContext.UserTrips.Add(new UserTrip { UserId = 1, TripId = 1 });
            _dbContext.SaveChanges();

            // ACT: Sprawdzenie, czy u�ytkownik ju� nale�y do podr�y.
            var result = _controller.CheckExistingInvitation(1, "User1");

            // ASSERT: Powinno zwr�ci� OkObjectResult z warto�ci� `true`.
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = JObject.FromObject(okResult.Value);
            Assert.True(response["exists"].Value<bool>());
        }

        /// <summary>
        /// Sprawdza, czy metoda GetPendingInvitationsCount zwraca poprawn� liczb� oczekuj�cych zaprosze�.
        /// </summary>
        [Fact]
        public void GetPendingInvitationsCount_ShouldReturnCorrectCount()
        {
            // ARRANGE: Ustawienie kontekstu u�ytkownika i dodanie zaprosze� do bazy.
            SetUserContext(1);

            _dbContext.Invitations.Add(new Invitation { InvitationId = 1, TripId = 1, ReceiverId = 1, Status = "Oczekuj�ce" });
            _dbContext.Invitations.Add(new Invitation { InvitationId = 2, TripId = 1, ReceiverId = 1, Status = "Oczekuj�ce" });
            _dbContext.SaveChanges();

            // ACT: Pobranie liczby oczekuj�cych zaprosze�.
            var result = _controller.GetPendingInvitationsCount();

            // ASSERT: Powinno zwr�ci� OkObjectResult z poprawn� liczb� zaprosze�.
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = JObject.FromObject(okResult.Value);
            Assert.Equal(2, response["count"].Value<int>());
        }

        /// <summary>
        /// Usuwa baz� danych po zako�czeniu test�w.
        /// </summary>
        public void Dispose()
        {
            _dbContext.Database.EnsureDeleted();
            _dbContext.Dispose();
        }
    }
}
