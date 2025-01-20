describe('Tworzenie wyjazdu w TRIPIFY', () => {
    beforeEach(() => {
        cy.visit('http://127.0.0.1:5500/login.html');
        
        cy.get("input[name='username']").type("nowyUser");
        cy.get("input[name='password']").type("Haslo123!");
        cy.get("button[type='submit']").click();
        
        cy.url().should('include', 'index.html');
    });

    it('Powinno umożliwić przejście do formularza tworzenia wyjazdu', () => {
        cy.get('#create-trip-button').should('be.visible').click();
        cy.url().should('include', 'create-trip.html');
    });

    it('Powinno umożliwić utworzenie nowego wyjazdu', () => {
        cy.get('#create-trip-button').click();
        
        cy.get('#trip-name').type('Weekendowy wypad w góry');
        cy.get('#trip-description').type('Wyjazd na weekend do Zakopanego.');
        cy.get('#start-date').type('2024-03-15');
        cy.get('#end-date').type('2024-03-17');
        cy.get('#invite-users').type('user1, user2');
        
        cy.get('button[type="submit"]').click();
        
        cy.wait(2000); // Oczekiwanie na odpowiedź serwera
        cy.url().should('include', 'index.html');
    });

    it('Powinno pokazać błąd, jeśli data zakończenia jest wcześniejsza niż rozpoczęcia', () => {
        cy.get('#create-trip-button').click();
        
        cy.get('#trip-name').type('Niepoprawny wyjazd');
        cy.get('#trip-description').type('Test błędnej daty.');
        cy.get('#start-date').type('2024-03-15');
        cy.get('#end-date').type('2024-03-10');
        
        cy.get('button[type="submit"]').click();
        
        cy.on('window:alert', (alertText) => {
            expect(alertText).to.equal('Data zakończenia nie może być wcześniejsza niż data rozpoczęcia.');
        });
    });

    it('Powinno wysłać zaproszenia do użytkowników', () => {
        cy.get('#create-trip-button').click();
        
        cy.get('#trip-name').type('Testowy wyjazd');
        cy.get('#trip-description').type('Test wysyłania zaproszeń.');
        cy.get('#start-date').type('2024-04-10');
        cy.get('#end-date').type('2024-04-15');
        cy.get('#invite-users').type('Kozub');
        
        cy.intercept('POST', 'http://localhost:7235/api/Trip/create').as('createTrip');
        cy.get('button[type="submit"]').click();
        
        cy.wait('@createTrip').its('response.statusCode').should('eq', 200);
        cy.url().should('include', 'index.html');
    });
});
