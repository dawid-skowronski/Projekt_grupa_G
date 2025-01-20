describe("Profil użytkownika", () => {
    beforeEach(() => {
        // 🔥 Przechwycenie żądań do API
        cy.intercept("POST", "http://localhost:7235/api/account/login").as("loginRequest");
        cy.intercept("GET", "http://localhost:7235/api/Account/profile").as("getProfile");
        cy.intercept("GET", "http://localhost:7235/api/Trip/my-trips").as("getTrips");

        // 🔥 Odwiedź stronę logowania
        cy.visit("http://127.0.0.1:5500/login.html");

        // 🔥 Wypełnij formularz logowania i zaloguj się
        cy.get("input[name='username']").type("nowyUser");
        cy.get("input[name='password']").type("Haslo123!");
        cy.get("button[type='submit']").click();

        // 🔥 Poczekaj na odpowiedź serwera z tokenem
        cy.wait("@loginRequest").then((interception) => {
            expect(interception.response.statusCode).to.eq(200);
            const token = interception.response.body.token;
            expect(token).to.be.a("string");

            // 🔥 Ręcznie ustawiamy token JWT w localStorage
            cy.window().then((win) => {
                win.localStorage.setItem("jwt_token", token);
            });
        });

        // 🔥 Teraz przechodzimy na stronę profilu
        cy.visit("http://127.0.0.1:5500/profile.html");
    });

    it("Powinien poprawnie załadować profil użytkownika", () => {
        // 🔥 Przechwycenie API profilu użytkownika i jego mockowanie
        cy.intercept("GET", "http://localhost:7235/api/Account/profile", {
            statusCode: 200,
            body: { username: "nowyUser", email: "test@example.com" }
        }).as("getProfile");

        // 🔥 Oczekiwanie na pobranie profilu
        cy.wait("@getProfile");

        // 🔥 Sprawdzenie, czy dane się wyświetliły
        cy.get("#profile-username").should("contain", "nowyUser");
        cy.get("#profile-email").should("contain", "test@example.com");
    });

    it("Powinien poprawnie załadować listę wyjazdów użytkownika", () => {
        // 🔥 Przechwycenie API listy wyjazdów i jego mockowanie
        cy.intercept("GET", "http://localhost:7235/api/Trip/my-trips", {
            statusCode: 200,
            body: [
                { tripId: 1, name: "Wyjazd do Zakopanego", startDate: "2024-07-10", endDate: "2024-07-15" },
                { tripId: 2, name: "Weekend w Krakowie", startDate: "2024-08-01", endDate: "2024-08-03" }
            ]
        }).as("getTrips");

        // 🔥 Oczekiwanie na pobranie wyjazdów
        cy.wait("@getTrips");

        // 🔥 Sprawdzenie, czy wyjazdy się wyświetliły
        cy.get("#user-trips").should("contain", "Wyjazd do Zakopanego");
        cy.get("#user-trips").should("contain", "Weekend w Krakowie");
    });

    it("Powinien obsłużyć brak wyjazdów użytkownika", () => {
        // 🔥 Mockowanie pustej listy wyjazdów
        cy.intercept("GET", "http://localhost:7235/api/Trip/my-trips", {
            statusCode: 200,
            body: []
        }).as("getTrips");

        // 🔥 Oczekiwanie na odpowiedź
        cy.wait("@getTrips");

        // 🔥 Sprawdzenie komunikatu
        cy.get("#user-trips").should("contain", "Nie masz jeszcze żadnych wyjazdów.");
    });

    it("Powinien obsłużyć błąd autoryzacji i przekierować na stronę logowania", () => {
        // 🔥 Przechwytujemy i symulujemy błąd 401
        cy.intercept("GET", "http://localhost:7235/api/Account/profile", {
            statusCode: 401,
            body: { message: "Brak tokenu uwierzytelniającego." }
        }).as("getProfile");
    
        // 🔥 Nasłuchujemy na alert i sprawdzamy jego treść
        cy.on("window:alert", (txt) => {
            expect(txt).to.contain("Brak tokenu uwierzytelniającego");
        });
    
        // 🔥 Odwiedzamy profil użytkownika
        cy.visit("http://127.0.0.1:5500/profile.html");
    
        // 🔥 Oczekujemy na odpowiedź API profilu (401 Unauthorized)
        cy.wait("@getProfile");
    
        // 🔥 Sprawdzamy, czy po zamknięciu alertu następuje przekierowanie na login
        cy.url().should("include", "login.html");
    });

    it("Powinien obsłużyć błąd pobierania listy wyjazdów", () => {
        // 🔥 Symulujemy błąd serwera przy pobieraniu wyjazdów
        cy.intercept("GET", "http://localhost:7235/api/Trip/my-trips", {
            statusCode: 500,
            body: { message: "Błąd podczas pobierania wyjazdów." }
        }).as("getTripsError");
    
        // 🔥 Nasłuchujemy na alert i sprawdzamy jego treść (dokładne dopasowanie)
        cy.on("window:alert", (txt) => {
            expect(txt).to.include("Błąd podczas pobierania wyjazdów");
        });
    
        // 🔥 Odwiedzamy profil użytkownika
        cy.visit("http://127.0.0.1:5500/profile.html");
    
        // 🔥 Czekamy na błąd 500 z API
        cy.wait("@getTripsError");
    });
    
});
