describe("Test rejestracji na serwerze produkcyjnym", () => {
    it("Powinno poprawnie rejestrować nowego użytkownika", () => {
        cy.visit("/register.html"); // Cypress automatycznie użyje `baseUrl`
        
        cy.get("input[name='email']").type("testuser@email.com");
        cy.get("input[name='username']").type("testowyUżytkownik");
        cy.get("input[name='password']").type("Haslo123!");
        cy.get("input[name='confirmPassword']").type("Haslo123!");
        cy.get("button[type='submit']").click();
        
        cy.url().should("include", "login.html");
    });

    it("Backend powinien poprawnie obsługiwać API rejestracji", () => {
        cy.request({
            method: "POST",
            url: Cypress.env("backendUrl") + "/api/Account/register", // Poprawiona składnia
            body: {
                email: "testuser@email.com",
                username: "testowyUżytkownik",
                password: "Haslo123!",
                confirmPassword: "Haslo123!"
            }
        }).then((response) => {
            expect(response.status).to.eq(201);
        });
    });
});
