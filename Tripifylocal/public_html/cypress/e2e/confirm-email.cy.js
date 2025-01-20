describe('Potwierdzenie e-maila', () => {
    const backendUrl = "http://localhost:7235/api/Account/confirm-email";
    const fakeToken = 'testowy_token_potwierdzenia';
    const invalidToken = 'niepoprawny_token';

    beforeEach(() => {
        cy.visit(`http://127.0.0.1:5500/confirm-email.html?token=${fakeToken}`);
    });

    it('powinien potwierdzić konto i przekierować do logowania', () => {
        cy.intercept('GET', `${backendUrl}?token=${fakeToken}`, {
            statusCode: 200,
            body: { message: "Twoje konto zostało potwierdzone." }
        }).as('confirmEmail');

        cy.reload(); // Wymuszenie odświeżenia, by frontend zainicjował request

        cy.wait('@confirmEmail', { timeout: 10000 });

        cy.get('.message')
          .should('contain', 'Twoje konto zostało pomyślnie potwierdzone!')
          .should('have.class', 'btn-success');

        cy.location('pathname', { timeout: 5000 }).should('eq', '/login.html');
    });

    it('powinien pokazać błąd dla nieprawidłowego tokenu', () => {
        cy.visit(`http://127.0.0.1:5500/confirm-email.html?token=${invalidToken}`);

        cy.intercept('GET', `${backendUrl}?token=${invalidToken}`, {
            statusCode: 400,
            body: { message: "Nieprawidłowy lub wygasły token aktywacyjny." }
        }).as('confirmEmailError');

        cy.reload();

        cy.wait('@confirmEmailError', { timeout: 10000 });

        cy.get('.message')
          .should('contain', 'Nie udało się potwierdzić konta. Token może być nieprawidłowy lub wygasły.')
          .should('have.class', 'btn-danger');
    });

    it('powinien pokazać błąd, jeśli token nie jest podany', () => {
        cy.visit('http://127.0.0.1:5500/confirm-email.html');
        
        cy.get('.message')
          .should('contain', 'Brak tokenu w URL. Nie można potwierdzić konta.')
          .should('have.class', 'btn-danger');
    });
});
