describe('Testy wydatków w aplikacji TRIPIFY', () => {
    beforeEach(() => {
        // Odwiedź stronę logowania przed każdym testem
        cy.visit('http://127.0.0.1:5500/login.html');
        
        // Logowanie użytkownika
        cy.get("input[name='username']").type("nowyUser");
        cy.get("input[name='password']").type("Haslo123!");
        cy.get("button[type='submit']").click();
        
        // Sprawdzenie czy użytkownik został przekierowany na stronę główną
        cy.url().should('include', 'index.html');
    });

    it('Powinno umożliwiać stworzenie nowego wydatku', () => {
        // Przejdź do strony tworzenia wydatku
        cy.visit('http://127.0.0.1:5500/create-expense.html?tripId=39');
    
        // Wypełnij formularz
        cy.get("input[id='expense-name']").type("Kolacja w restauracji");
        cy.get("select[id='expense-category']").select("jedzenie");
        cy.get("input[id='expense-cost']").type("100");
        cy.get("select[id='expense-currency']").select("PLN");
        cy.get("input[id='expense-date']").type("2025-02-25");
        cy.get("input[id='expense-location']").type("Warszawa");
    
        // Przechwycenie alertu przed wysłaniem formularza
        cy.on('window:alert', (txt) => {
            expect(txt).to.contains("Wydatek został zapisany!");
        });
    
        // Prześlij formularz
        cy.get("form[id='expense-form'] button[type='submit']").click();
    
        // Poczekaj na zakończenie obsługi alertu, zanim sprawdzisz przekierowanie
        cy.wait(1000);
    
        // Sprawdzenie, czy użytkownik został przekierowany do szczegółów podróży
        cy.url().should('include', 'trip-details.html?tripId=39');
    });
    

    it('Powinno wyświetlać błąd, jeśli dane formularza są niepełne', () => {
        cy.visit('http://127.0.0.1:5500/create-expense.html?tripId=39');
        
        // Wypełnij tylko część formularza
        cy.get("input[id='expense-name']").type("Przejazd taxi");
        cy.get("select[id='expense-category']").select("transport");
        cy.get("input[id='expense-cost']").type("50");
        
        // Nie wypełniaj waluty i daty, spróbuj wysłać formularz
        cy.get("form[id='expense-form'] button[type='submit']").click();
        
        // Sprawdź, czy użytkownik otrzymuje alert o brakujących polach
        cy.on('window:alert', (txt) => {
            expect(txt).to.contains("Proszę uzupełnić wszystkie pola.");
        });
    });
    
    
    
   it('Nie powinno pozwolić na zapisanie wydatku bez wyboru kategorii', () => {
        cy.visit('http://127.0.0.1:5500/create-expense.html?tripId=39');
        cy.get("input[id='expense-name']").type("Bilet na koncert");
        cy.get("input[id='expense-cost']").type("150");
        cy.get("select[id='expense-currency']").select("PLN");
        cy.get("input[id='expense-date']").type("2025-02-25");
        cy.get("input[id='expense-location']").type("Gdańsk");

        cy.get("form[id='expense-form'] button[type='submit']").click();
        cy.on('window:alert', (txt) => {
            expect(txt).to.contains("Proszę wybrać kategorię.");
        });
    });


    it('Powinno pozwolić na zapisanie wydatku bez lokalizacji', () => {
        cy.visit('http://127.0.0.1:5500/create-expense.html?tripId=35');
        cy.get("input[id='expense-name']").type("Testowy wydatek");
        cy.get("select[id='expense-category']").select("zakwaterowanie");
        cy.get("input[id='expense-cost']").type("250");
        cy.get("select[id='expense-currency']").select("PLN");
        cy.get("input[id='expense-date']").type("2024-03-17");

        cy.get("form[id='expense-form'] button[type='submit']").click();
       
    });
    it('Nie powinno pozwolić na zapisanie wydatku, jeśli wszystkie pola są puste', () => {
        cy.visit('http://127.0.0.1:5500/create-expense.html?tripId=39');
        cy.get("form[id='expense-form'] button[type='submit']").click();
        cy.on('window:alert', (txt) => {
            expect(txt).to.contains("Proszę uzupełnić wszystkie pola.");
        });
    });

    it('Nie powinno pozwolić na zapisanie wydatku poza zakresem dat podróży', () => {
        cy.visit('http://127.0.0.1:5500/create-expense.html?tripId=39');
        cy.get("input[id='expense-name']").type("Nieprawidłowa data");
        cy.get("select[id='expense-category']").select("rozrywka");
        cy.get("input[id='expense-cost']").type("200");
        cy.get("select[id='expense-currency']").select("PLN");
        cy.get("input[id='expense-date']").type("2026-01-01"); // Data poza zakresem podróży
        cy.get("input[id='expense-location']").type("Gdańsk");
    
        cy.get("form[id='expense-form'] button[type='submit']").click();
        cy.on('window:alert', (txt) => {
            expect(txt).to.contains("Data wydatku musi mieścić się w przedziale");
        });
    });

    it('Nie powinno pozwolić na dodanie wydatku bez zalogowania', () => {
        cy.visit('http://127.0.0.1:5500/create-expense.html?tripId=39');
        
        // Wyczyszczenie tokena (symulacja niezalogowanego użytkownika)
        cy.window().then((win) => {
            win.localStorage.removeItem("jwt_token");
            win.sessionStorage.removeItem("jwt_token");
        });
    
        cy.get("input[id='expense-name']").type("Bez autoryzacji");
        cy.get("select[id='expense-category']").select("jedzenie");
        cy.get("input[id='expense-cost']").type("75");
        cy.get("select[id='expense-currency']").select("PLN");
        cy.get("input[id='expense-date']").type("2025-02-25");
        cy.get("input[id='expense-location']").type("Wrocław");
    
        cy.get("form[id='expense-form'] button[type='submit']").click();
        cy.on('window:alert', (txt) => {
            expect(txt).to.contains("Nie jesteś zalogowany. Zaloguj się, aby dodać wydatek.");
        });
    });
    
    
    
    


    
});
