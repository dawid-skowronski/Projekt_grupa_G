describe("Testy logowania", () => {
  beforeEach(() => {
    cy.visit("http://127.0.0.1:5500/login.html"); // Podmień na właściwy adres
  });

  it("Powinien wyświetlić formularz logowania", () => {
    cy.get("h1").should("contain", "Zaloguj się");
    cy.get("input[name='username']").should("exist");
    cy.get("input[name='password']").should("exist");
    cy.get("button[type='submit']").should("exist");
  });

  it("Powinien zalogować użytkownika poprawnymi danymi", () => {
    cy.intercept("POST", "/api/account/login").as("loginRequest");

    cy.get("input[name='username']").type("Kozub");
    cy.get("input[name='password']").type("Qwerty123!");
    cy.get("button[type='submit']").click();

    cy.wait("@loginRequest").its("response.statusCode").should("eq", 200);

    cy.url().should("include", "/index.html");
    cy.window().its("sessionStorage").invoke("getItem", "jwt_token").should("not.be.null");
  });

  it("Powinien obsłużyć błędne dane logowania", () => {
    cy.intercept("POST", "/api/account/login", {
      statusCode: 401,
      body: { message: "Nieprawidłowe dane logowania" },
    }).as("loginRequest");

    cy.get("input[name='username']").type("niepoprawny");
    cy.get("input[name='password']").type("blednehaslo");
    cy.get("button[type='submit']").click();

    cy.wait("@loginRequest");
    cy.on("window:alert", (txt) => {
      expect(txt).to.contains("Nieprawidłowe dane logowania");
    });
  });

  it("Powinien zapisywać token w localStorage, gdy 'Zapamiętaj mnie' jest zaznaczone", () => {
    cy.intercept("POST", "/api/account/login", {
      statusCode: 200,
      body: { token: "testowyToken" },
    }).as("loginRequest");

    cy.get("input[name='username']").type("testuser");
    cy.get("input[name='password']").type("password123");
    cy.get("#remember-me").check({ force: true });
    cy.get("button[type='submit']").click();

    cy.wait("@loginRequest");

    cy.window().its("localStorage").invoke("getItem", "jwt_token").should("equal", "testowyToken");
  });
});
