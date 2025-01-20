describe("Testy formularza dołączania do wyjazdu w TRIPIFY", () => {
    beforeEach(() => {
        // Odwiedzenie strony logowania przed testami
        cy.visit("http://127.0.0.1:5500/login.html");

        // Logowanie użytkownika
        cy.get("input[name='username']").type("nowyUser");
        cy.get("input[name='password']").type("Haslo123!");
        cy.get("button[type='submit']").click();

        // Sprawdzenie, czy użytkownik został przekierowany na stronę główną
        cy.url().should("include", "index.html");

        // Odwiedzenie strony dołączania do wyjazdu po zalogowaniu
        cy.visit("http://127.0.0.1:5500/jointrip.html");
    });

    it("Powinno umożliwiać dołączenie do wyjazdu po podaniu poprawnego kodu", () => {
        // Wypełnianie formularza kodem wyjazdu
        cy.get("#trip-code").type("KOD123");

        // Przechwycenie żądania POST i udawanie odpowiedzi
        cy.intercept("POST", "http://localhost:7235/api/Trip/join", {
            statusCode: 200,
            body: { message: "Dołączono do wyjazdu." }
        }).as("joinTrip");

        // Wysłanie formularza
        cy.get("form#join-trip-form button[type='submit']").click();

        // Oczekiwanie na API i sprawdzenie odpowiedzi
        cy.wait("@joinTrip");

        // Oczekiwanie na alert potwierdzający dołączenie
        cy.on("window:alert", (txt) => {
            expect(txt).to.contains("Pomyślnie dołączono do wyjazdu!");
        });

        // Sprawdzenie, czy użytkownik został przekierowany do strony głównej
        cy.url().should("include", "index.html");
    });

    it("Powinno wyświetlać błąd, jeśli użytkownik nie poda kodu wyjazdu", () => {
        // Wysłanie formularza bez wpisania kodu
        cy.get("form#join-trip-form button[type='submit']").click();

        // Oczekiwanie na alert z komunikatem o błędzie
        cy.on("window:alert", (txt) => {
            expect(txt).to.contains("Proszę podać kod wyjazdu.");
        });
    });

    it("Powinno obsługiwać przypadek, gdy kod wyjazdu jest błędny", () => {
        // Wypełnianie formularza błędnym kodem wyjazdu
        cy.get("#trip-code").type("BLEDNY_KOD");

        // Przechwycenie żądania POST i zasymulowanie błędu API
        cy.intercept("POST", "http://localhost:7235/api/Trip/join", {
            statusCode: 400,
            body: { message: "Niepoprawny kod wyjazdu." }
        }).as("joinTripFail");

        // Wysłanie formularza
        cy.get("form#join-trip-form button[type='submit']").click();

        // Oczekiwanie na API i sprawdzenie błędnej odpowiedzi
        cy.wait("@joinTripFail");

        // Oczekiwanie na alert informujący o błędzie kodu
        cy.on("window:alert", (txt) => {
            expect(txt).to.contains("Niepoprawny kod wyjazdu.");
        });
    });
});
