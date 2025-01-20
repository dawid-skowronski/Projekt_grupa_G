using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimpleApiBackend.Controllers;
using SimpleApiBackend.Data;
using SimpleApiBackend.Models;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace SimpleApiBackend.Tests
{
    public class TripControllerTests : IDisposable
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly TripController _controller;

        public TripControllerTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _dbContext = new ApplicationDbContext(options);
            _controller = new TripController(_dbContext);
        }

        private void SetUserContext(int userId)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            };
            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };
        }

        /// <summary>
        /// Sprawdza, czy metoda CreateTrip zwraca OK i poprawnie tworzy wyjazd, gdy dane s¹ prawid³owe.
        /// </summary>
        [Fact]
        public async Task CreateTrip_ShouldReturnOk_IfValid()
        {
            // ARRANGE: Ustawienie kontekstu u¿ytkownika.
            SetUserContext(1);

            var model = new TripCreateModel
            {
                Name = "Test Trip",
                Description = "Description",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(1)
            };

            // ACT: Tworzenie nowego wyjazdu.
            var result = await _controller.CreateTrip(model);

            // ASSERT: Powinno zwróciæ OkObjectResult i poprawne dane wyjazdu.
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);

            var response = Assert.IsType<CreateTripResponse>(okResult.Value);
            Assert.True(response.TripId > 0); // Sprawdza, czy ID wyjazdu zosta³o nadane
            Assert.False(string.IsNullOrEmpty(response.SecretCode)); // Sprawdza, czy wygenerowano kod wyjazdu
        }

        /// <summary>
        /// Sprawdza, czy metoda JoinTrip zwraca NotFound, gdy wyjazd o podanym kodzie nie istnieje.
        /// </summary>
        [Fact]
        public async Task JoinTrip_ShouldReturnNotFound_IfTripDoesNotExist()
        {
            // ARRANGE: Ustawienie kontekstu u¿ytkownika i przygotowanie b³êdnego kodu wyjazdu.
            SetUserContext(1);
            var model = new JoinTripModel { SecretCode = "INVALID_CODE", UserId = 1 };

            // ACT: Próba do³¹czenia do nieistniej¹cego wyjazdu.
            var result = await _controller.JoinTrip(model);

            // ASSERT: Powinno zwróciæ NotFoundObjectResult z odpowiednim komunikatem.
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("Wyjazd nie zosta³ znaleziony.", ((ErrorResponse)notFoundResult.Value).Message);
        }


        /// <summary>
        /// Sprawdza, czy metoda GetTripDetails zwraca NotFound, gdy wyjazd o podanym ID nie istnieje.
        /// </summary>
        [Fact]
        public async Task GetTripDetails_ShouldReturnNotFound_IfTripDoesNotExist()
        {
            // ACT: Próba pobrania szczegó³ów nieistniej¹cego wyjazdu.
            var result = await _controller.GetTripDetails(999);

            // ASSERT: Powinno zwróciæ NotFoundObjectResult z odpowiednim komunikatem.
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("Wyjazd nie zosta³ znaleziony.", ((ErrorResponse)notFoundResult.Value).Message);
        }

        /// <summary>
        /// Sprawdza, czy metoda GetUserTrips zwraca pust¹ listê, gdy u¿ytkownik nie ma ¿adnych wyjazdów.
        /// </summary>
        [Fact]
        public async Task GetUserTrips_ShouldReturnEmpty_IfNoTrips()
        {
            // ARRANGE: Ustawienie kontekstu u¿ytkownika.
            SetUserContext(1);

            // ACT: Pobranie listy wyjazdów u¿ytkownika.
            var result = await _controller.GetUserTrips(null);

            // ASSERT: Powinno zwróciæ OkObjectResult z informacj¹ o braku wyjazdów.
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("Nie masz ¿adnych wyjazdów.", ((ErrorResponse)okResult.Value).Message);
        }

        /// <summary>
        /// Sprawdza, czy metoda LeaveTrip zwraca NotFound, gdy u¿ytkownik nie jest cz³onkiem wyjazdu.
        /// </summary>
        [Fact]
        public async Task LeaveTrip_ShouldReturnNotFound_IfNotMember()
        {
            // ARRANGE: Ustawienie kontekstu u¿ytkownika.
            SetUserContext(1);

            // ACT: Próba opuszczenia wyjazdu, do którego u¿ytkownik nie nale¿y.
            var result = await _controller.LeaveTrip(1);

            // ASSERT: Powinno zwróciæ NotFoundObjectResult z odpowiednim komunikatem.
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("Nie jesteœ cz³onkiem tego wyjazdu.", ((ErrorResponse)notFoundResult.Value).Message);
        }


        /// <summary>
        /// Sprawdza, czy metoda LeaveTrip poprawnie usuwa u¿ytkownika z wyjazdu, gdy jest cz³onkiem.
        /// </summary>
        [Fact]
        public async Task LeaveTrip_ShouldReturnOk_IfMember()
        {
            // ARRANGE: Ustawienie kontekstu u¿ytkownika oraz dodanie go do wyjazdu.
            SetUserContext(1);

            var trip = new Trip
            {
                TripId = 1,
                Name = "Test Trip",
                Description = "Opis wyjazdu",
                SecretCode = "XYZ123"
            };

            _dbContext.Trips.Add(trip);
            _dbContext.UserTrips.Add(new UserTrip { UserId = 1, TripId = 1 });
            await _dbContext.SaveChangesAsync();

            // ACT: Opuszczenie wyjazdu przez u¿ytkownika.
            var result = await _controller.LeaveTrip(1);

            // ASSERT: Powinno zwróciæ OkObjectResult z odpowiednim komunikatem.
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);

            var response = Assert.IsType<ErrorResponse>(okResult.Value);
            Assert.Equal("Wyjazd opuszczony pomyœlnie.", response.Message);

            // Sprawdzenie, czy u¿ytkownik zosta³ usuniêty z bazy danych.
            var userTrip = await _dbContext.UserTrips.FirstOrDefaultAsync(ut => ut.UserId == 1 && ut.TripId == 1);
            Assert.Null(userTrip);
        }

        /// <summary>
        /// Usuwa bazê danych po zakoñczeniu testów.
        /// </summary>
        public void Dispose()
        {
            _dbContext.Database.EnsureDeleted();
            _dbContext.Dispose();
        }
    }
}