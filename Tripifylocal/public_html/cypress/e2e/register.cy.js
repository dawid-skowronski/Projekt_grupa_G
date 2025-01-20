describe("Testy rejestracji", () => {
  beforeEach(() => {
      cy.visit("http://127.0.0.1:5500/register.html"); 
  });

  it("Powinien wyświetlić formularz rejestracji", () => {
      cy.get("h1").should("contain", "Zarejestruj się");
      cy.get("#register-email").should("exist");
      cy.get("#register-username").should("exist");
      cy.get("#register-password").should("exist");
      cy.get("#register-confirm-password").should("exist");
      cy.get("button[type='submit']").should("exist");
  });

  it("Powinien zarejestrować nowego użytkownika", () => {
      cy.intercept("POST", "https://localhost:7235/api/Account/register", {
          statusCode: 200,
          body: { message: "Rejestracja zakończona sukcesem. Sprawdź e-mail, aby potwierdzić swoje konto." }
      }).as("registerRequest");

      cy.get("#register-email").type("test@example.com");
      cy.get("#register-username").type("nowyUser");
      cy.get("#register-password").type("Haslo123!");
      cy.get("#register-confirm-password").type("Haslo123!");
      cy.get("button[type='submit']").click();

      

      cy.url().should("include", "/login.html");
  });

  it("Powinien wyświetlić błąd, gdy hasła się nie zgadzają", () => {
      cy.get("#register-email").type("test@example.com");
      cy.get("#register-username").type("nowyUser");
      cy.get("#register-password").type("Haslo123!");
      cy.get("#register-confirm-password").type("Haslo456!");
      cy.get("button[type='submit']").click();

      cy.on("window:alert", (txt) => {
          expect(txt).to.contains("Hasła muszą być takie same!");
      });
  });

  it("Powinien obsłużyć błąd, gdy email już istnieje", () => {
      cy.intercept("POST", "https://localhost:7235/api/Account/register", {
          statusCode: 400,
          body: { message: "Użytkownik o takim loginie lub emailu już istnieje." }
      }).as("registerRequest");

      cy.get("#register-email").type("test@example.com");
      cy.get("#register-username").type("nowyUser");
      cy.get("#register-password").type("Haslo123!");
      cy.get("#register-confirm-password").type("Haslo123!");
      cy.get("button[type='submit']").click();

      

      cy.on("window:alert", (txt) => {
          expect(txt).to.contains("Użytkownik o takim loginie lub emailu już istnieje.");
      });
  });

  it("Powinien wyświetlić błąd, gdy hasło jest za krótkie", () => {
      cy.intercept("POST", "https://localhost:7235/api/Account/register", {
          statusCode: 400,
          body: { message: "Hasło musi zawierać co najmniej 8 znaków, jedną wielką literę, jedną małą literę, cyfrę i znak specjalny." }
      }).as("registerRequest");

      cy.get("#register-email").type("test@example.com");
      cy.get("#register-username").type("nowyUser");
      cy.get("#register-password").type("123");
      cy.get("#register-confirm-password").type("123");
      cy.get("button[type='submit']").click();

      

      cy.on("window:alert", (txt) => {
          expect(txt).to.contains("Hasło musi zawierać co najmniej 8 znaków, jedną wielką literę, jedną małą literę, cyfrę i znak specjalny.");
      });
  });

  it("Powinien wyświetlić błąd, gdy pola są puste", () => {
      cy.get("button[type='submit']").click();

      cy.on("window:alert", (txt) => {
          expect(txt).to.contains("Proszę wypełnić wszystkie wymagane pola");
      });
  });
});
