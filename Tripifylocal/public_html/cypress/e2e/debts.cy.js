describe('Testy zarządzania długami w aplikacji TRIPIFY', () => {
    beforeEach(() => {
        cy.visit('http://127.0.0.1:5500/login.html');

        // Logowanie użytkownika
        cy.get("input[name='username']").type("nowyUser");
        cy.get("input[name='password']").type("Haslo123!");
        cy.get("button[type='submit']").click();

        // Sprawdzenie czy użytkownik został przekierowany na stronę główną
        cy.url().should('include', 'index.html');
    });

    it('Powinno poprawnie załadować listę długów', () => {
        cy.visit('http://127.0.0.1:5500/debts.html');

        // Sprawdzenie, czy tabela "Twoje Długi" zawiera dane
        cy.get("#debts-you-owe tbody tr").should("exist");

        // Sprawdzenie, czy tabela "Twoi Wierzyciele" zawiera dane
        cy.get("#debts-owed-to-you tbody tr").should("exist");
    });

    it('Powinno umożliwiać zgłoszenie płatności za dług', () => {
        cy.visit('http://127.0.0.1:5500/debts.html');
    
        cy.get("#debts-you-owe tbody tr").should("have.length.at.least", 5); // Poczekaj na załadowanie tabeli
    
        cy.get("#debts-you-owe tbody tr").eq(5).within(() => { // Piąty wiersz (indeks 4)
            cy.get("td").eq(7).within(() => { // 8 kolumna (indeks 7)
                cy.get("button.btn-success").should("exist").click(); // Kliknięcie "Zgłoś płatność"
    
                // Poczekaj na alert potwierdzający zgłoszenie
                cy.on('window:alert', (txt) => {
                    expect(txt).to.contains("Płatność została zgłoszona pomyślnie.");
                });
    
                // **Najważniejsze:** Poczekaj, aż przycisk zmieni stan na "Oczekujące"
                cy.get("button.btn-warning")
                    .should("exist")
                    .and("contain", "Oczekujące");
            });
        });
    });

    it('Powinno umożliwiać odpuszczenie długu przez wierzyciela', () => {
        cy.visit('http://127.0.0.1:5500/debts.html');

        // Znalezienie pierwszego długu do odpuszczenia
        cy.get("#debts-owed-to-you tbody tr").first().within(() => {
            cy.get("button.btn-danger").click(); // Kliknięcie przycisku "Odpuść"
        });

        // Oczekiwanie na alert potwierdzający odpuszczenie
        cy.on('window:confirm', (txt) => {
            expect(txt).to.contains("Czy na pewno chcesz odpuścić ten dług?");
            return true; // Potwierdzenie
        });

        cy.on('window:alert', (txt) => {
            expect(txt).to.contains("Dług został odpuszczony.");
        });

        // Sprawdzenie, czy dług zmienił status na "Opłacone"
        cy.get("#debts-owed-to-you tbody tr").first().within(() => {
            cy.get("button.btn-secondary").should("contain", "Opłacone");
        });
    });

    it('Powinno umożliwiać filtrowanie opłaconych długów', () => {
        cy.visit('http://127.0.0.1:5500/debts.html');

        // Kliknięcie przycisku "Pokaż opłacone"
        cy.contains("Pokaż opłacone").click();

        // Sprawdzenie, czy opłacone długi się pojawiły
        cy.get("#debts-you-owe tbody tr.paid").should("be.visible");

        // Kliknięcie ponownie, aby ukryć opłacone
        cy.contains("Ukryj opłacone").click();

        // Sprawdzenie, czy opłacone długi zniknęły
        cy.get("#debts-you-owe tbody tr.paid").should("not.be.visible");
    });
});
