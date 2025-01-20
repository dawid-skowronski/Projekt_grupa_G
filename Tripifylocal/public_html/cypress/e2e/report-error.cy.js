describe("Testy formularza zgłaszania błędów w TRIPIFY", () => {
    beforeEach(() => {
        // Odwiedzenie strony zgłaszania błędów przed każdym testem
        cy.visit("http://127.0.0.1:5500/report-error.html");
    });

    it("Powinno umożliwiać wysłanie zgłoszenia błędu po wypełnieniu wszystkich pól", () => {
        // Wypełnianie formularza
        cy.get("#user-email").type("testowy.uzytkownik@example.com");
        cy.get("#error-subject").type("Problem z logowaniem");
        cy.get("#error-description").type("Nie mogę się zalogować po wpisaniu poprawnych danych.");

        // Przechwycenie żądania POST do API i udawanie odpowiedzi
        cy.intercept("POST", "http://localhost:7235/api/ErrorReport/error-report", {
            statusCode: 200,
            body: { message: "Zgłoszenie zostało wysłane pomyślnie!" }
        }).as("sendError");

        // Wysłanie formularza
        cy.get("form#error-form button[type='submit']").click();

        // Oczekiwanie na API i sprawdzenie czy zwróciło poprawną odpowiedź
        cy.wait("@sendError");

        // Oczekiwanie na alert z potwierdzeniem wysyłki
        cy.on("window:alert", (txt) => {
            expect(txt).to.contains("Zgłoszenie zostało wysłane pomyślnie!");
        });
    });

    it("Powinno wyświetlać komunikat, jeśli użytkownik nie uzupełni wszystkich pól", () => {
        // Wypełnienie tylko części formularza
        cy.get("#user-email").type("testowy.uzytkownik@example.com");
        cy.get("#error-subject").type("Problem z logowaniem");

        // Wysłanie formularza bez uzupełnienia opisu błędu
        cy.get("form#error-form button[type='submit']").click();

        // Oczekiwanie na alert o brakujących polach
        cy.on("window:alert", (txt) => {
            expect(txt).to.contains("Wypełnij wszystkie pola!");
        });
    });

    it("Powinno obsługiwać błąd serwera podczas wysyłania zgłoszenia", () => {
        // Wypełnianie formularza
        cy.get("#user-email").type("testowy.uzytkownik@example.com");
        cy.get("#error-subject").type("Problem z logowaniem");
        cy.get("#error-description").type("Nie mogę się zalogować po wpisaniu poprawnych danych.");

        // Przechwycenie żądania POST i zasymulowanie błędu serwera
        cy.intercept("POST", "http://localhost:7235/api/ErrorReport/error-report", {
            statusCode: 500,
            body: { message: "Błąd serwera. Nie udało się wysłać zgłoszenia." }
        }).as("sendErrorFail");

        // Wysłanie formularza
        cy.get("form#error-form button[type='submit']").click();

        // Oczekiwanie na API i sprawdzenie czy zwróciło błąd
        cy.wait("@sendErrorFail");

        // Oczekiwanie na alert informujący o błędzie
        cy.on("window:alert", (txt) => {
            expect(txt).to.contains("Wystąpił błąd. Spróbuj ponownie.");
        });
    });
});
