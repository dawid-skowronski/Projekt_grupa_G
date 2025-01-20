describe("Powiadomienia użytkownika", () => {
    let jwtToken;

    beforeEach(() => {
        cy.intercept("POST", "http://localhost:7235/api/account/login").as("loginRequest");
        cy.intercept("GET", "http://localhost:7235/api/Expenses/payment-requests").as("getPaymentRequests");
        cy.intercept("GET", "http://localhost:7235/api/Invitations/received").as("getInvitations");

        cy.visit("http://127.0.0.1:5500/login.html");

        cy.get("input[name='username']").type("nowyUser");
        cy.get("input[name='password']").type("Haslo123!");
        cy.get("button[type='submit']").click();

        cy.wait("@loginRequest").then((interception) => {
            expect(interception.response.statusCode).to.eq(200);
            jwtToken = interception.response.body.token;
            cy.window().then((win) => {
                win.localStorage.setItem("jwt_token", jwtToken);
            });
        });
    });

    it("Powinien załadować zaproszenia", () => {
        cy.intercept("GET", "http://localhost:7235/api/Invitations/received", {
            statusCode: 200,
            body: [
                {
                    invitationId: 1,
                    tripName: "Wyjazd do Zakopanego",
                    senderUsername: "test",
                    createdAt: "2025-01-19T19:41:24",
                    status: "Oczekujące"
                }
            ]
        }).as("getInvitations");

        cy.visit("http://127.0.0.1:5500/notifications.html");
        cy.wait("@getInvitations");

        cy.get("#invitations-container").should("contain", "Wyjazd do Zakopanego");
        cy.get("#invitations-container").should("contain", "test");
        cy.get("#invitations-container").should("contain", "Oczekujące");
    });

    it("Powinien załadować zgłoszenie płatności", () => {
        cy.intercept("GET", "http://localhost:7235/api/Expenses/payment-requests", {
            statusCode: 200,
            body: [
                {
                    debtDescription: "test",
                    amount: 66.67,
                    currency: "PLN",
                    paymentMethod: "Blik",
                    requestedBy: "test",
                    requestedAt: "2025-01-19T18:43:04",
                    id: 1
                }
            ]
        }).as("getPaymentRequests");

        cy.visit("http://127.0.0.1:5500/notifications.html");
        cy.wait("@getPaymentRequests");

        cy.get("#payment-requests-container").should("contain", "test");
        cy.get("#payment-requests-container").should("contain", "66.67 PLN");
        cy.get("#payment-requests-container").should("contain", "test");
        cy.get("#payment-requests-container").should("contain", "Blik");
    });

    it("Powinien zaakceptować zaproszenie", () => {
        cy.intercept("GET", "http://localhost:7235/api/Invitations/received", {
            statusCode: 200,
            body: [
                {
                    invitationId: 1,
                    tripName: "Wyjazd do Zakopanego",
                    senderUsername: "test",
                    createdAt: "2025-01-19T19:41:24",
                    status: "Oczekujące"
                }
            ]
        }).as("getInvitations");

        cy.visit("http://127.0.0.1:5500/notifications.html");
        cy.wait("@getInvitations");

        // Mockujemy odpowiedź po kliknięciu przycisku "Przyjmij"
        cy.intercept("PUT", "http://localhost:7235/api/Invitations/respond", {
            statusCode: 200,
            body: { message: "Zaproszenie zaakceptowane." }
        }).as("acceptInvitation");

        cy.get("button[data-status='Accepted']").should('be.visible').click();
        cy.wait("@acceptInvitation").then((interception) => {
            expect(interception.response.statusCode).to.eq(200);
            expect(interception.response.body.message).to.include("Zaproszenie zaakceptowane.");
        });
    });

    it("Powinien anulować zaproszenie", () => {
        cy.intercept("GET", "http://localhost:7235/api/Invitations/received", {
            statusCode: 200,
            body: [
                {
                    invitationId: 1,
                    tripName: "Wyjazd do Zakopanego",
                    senderUsername: "test",
                    createdAt: "2025-01-19T19:41:24",
                    status: "Oczekujące"
                }
            ]
        }).as("getInvitations");

        cy.visit("http://127.0.0.1:5500/notifications.html");
        cy.wait("@getInvitations");

        // Mockujemy odpowiedź po kliknięciu przycisku "Odrzuć"
        cy.intercept("PUT", "http://localhost:7235/api/Invitations/respond", {
            statusCode: 200,
            body: { message: "Zaproszenie odrzucone." }
        }).as("rejectInvitation");

        cy.get("button[data-status='Rejected']").should('be.visible').click();
        cy.wait("@rejectInvitation").then((interception) => {
            expect(interception.response.statusCode).to.eq(200);
            expect(interception.response.body.message).to.include("Zaproszenie odrzucone.");
        });
    });

    it("Powinien zaakceptować płatność", () => {
        cy.intercept("GET", "http://localhost:7235/api/Expenses/payment-requests", {
            statusCode: 200,
            body: [
                {
                    debtDescription: "test",
                    amount: 66.67,
                    currency: "PLN",
                    paymentMethod: "Blik",
                    requestedBy: "test",
                    requestedAt: "2025-01-19T18:43:04",
                    id: 1
                }
            ]
        }).as("getPaymentRequests");

        cy.visit("http://127.0.0.1:5500/notifications.html");
        cy.wait("@getPaymentRequests");

        // Mockujemy odpowiedź po kliknięciu przycisku "Zatwierdź"
        cy.intercept("PUT", "http://localhost:7235/api/Expenses/review-payment/1", {
            statusCode: 200,
            body: { message: "Płatność zatwierdzona." }
        }).as("approvePayment");

        cy.get("button[data-approve='true']").should('be.visible').click();
        cy.wait("@approvePayment").then((interception) => {
            expect(interception.response.statusCode).to.eq(200);
            expect(interception.response.body.message).to.include("Płatność zatwierdzona.");
        });
    });

    it("Powinien anulować płatność", () => {
        cy.intercept("GET", "http://localhost:7235/api/Expenses/payment-requests", {
            statusCode: 200,
            body: [
                {
                    debtDescription: "test",
                    amount: 66.67,
                    currency: "PLN",
                    paymentMethod: "Blik",
                    requestedBy: "test",
                    requestedAt: "2025-01-19T18:43:04",
                    id: 1
                }
            ]
        }).as("getPaymentRequests");

        cy.visit("http://127.0.0.1:5500/notifications.html");
        cy.wait("@getPaymentRequests");

        // Mockujemy odpowiedź po kliknięciu przycisku "Odrzuć"
        cy.intercept("PUT", "http://localhost:7235/api/Expenses/review-payment/1", {
            statusCode: 200,
            body: { message: "Płatność odrzucona." }
        }).as("rejectPayment");

        cy.get("button[data-approve='false']").should('be.visible').click();
        cy.wait("@rejectPayment").then((interception) => {
            expect(interception.response.statusCode).to.eq(200);
            expect(interception.response.body.message).to.include("Płatność odrzucona.");
        });
    });
});
