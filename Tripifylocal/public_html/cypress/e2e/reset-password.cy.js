describe("Resetowanie hasła", () => {
    const apiUrl = "http://localhost:7235/api/Account/reset-password";

    // Ignorujemy wyjątki, żeby test się nie zatrzymywał na błędach frontendowych
    Cypress.on("uncaught:exception", (err, runnable) => {
        console.warn("⚠️ Ignorowany wyjątek:", err.message);
        return false;
    });

    beforeEach(() => {
        cy.visit("http://127.0.0.1:5500/reset-password.html?token=validToken");
    });

    it("Powinien wyświetlić formularz resetowania hasła", () => {
        cy.get("h1").should("contain", "Resetowanie hasła");
        cy.get("#new-password").should("exist");
        cy.get("#confirm-password").should("exist");
        cy.get("button[type='submit']").should("exist");
    });

    it("Powinien zresetować hasło pomyślnie", () => {
        cy.intercept("POST", apiUrl, {
            statusCode: 200,
            body: { message: "Hasło zostało zresetowane pomyślnie!" }
        }).as("resetPassword");

        cy.get("#new-password").type("NoweHaslo123!");
        cy.get("#confirm-password").type("NoweHaslo123!");
        cy.get("button[type='submit']").click();

        cy.wait("@resetPassword").its("response.statusCode").should("eq", 200);
        cy.get(".message").should("contain", "Hasło zostało zresetowane pomyślnie!")
            .should("have.class", "btn-success");
    });

    it("Powinien wyświetlić błąd, gdy hasła nie są identyczne", () => {
        cy.get("#new-password").type("NoweHaslo123!");
        cy.get("#confirm-password").type("InneHaslo456!");
        cy.get("button[type='submit']").click();

        cy.get(".message")
            .should("contain", "Hasła muszą być takie same.")
            .should("have.class", "highlight");
    });

    it("Powinien obsłużyć błąd serwera przy resetowaniu hasła", () => {
        cy.intercept("POST", apiUrl, {
            statusCode: 500,
            body: { message: "Wystąpił błąd serwera." }
        }).as("resetPasswordError");

        cy.get("#new-password").type("NoweHaslo123!");
        cy.get("#confirm-password").type("NoweHaslo123!");
        cy.get("button[type='submit']").click();

        cy.wait("@resetPasswordError");
        cy.get(".message")
            .should("contain", "Wystąpił błąd serwera.")
            .should("have.class", "btn-danger");
    });

    it("Powinien obsłużyć błąd związany z nieprawidłowym tokenem", () => {
        cy.intercept("POST", apiUrl, {
            statusCode: 400,
            body: { message: "Token wygasł lub jest nieprawidłowy." }
        }).as("resetPasswordInvalidToken");

        cy.get("#new-password").type("NoweHaslo123!");
        cy.get("#confirm-password").type("NoweHaslo123!");
        cy.get("button[type='submit']").click();

        cy.wait("@resetPasswordInvalidToken");
        cy.get(".message")
            .should("contain", "Token wygasł lub jest nieprawidłowy.")
            .should("have.class", "btn-danger");
    });
});
