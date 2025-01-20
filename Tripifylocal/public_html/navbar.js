document.addEventListener("DOMContentLoaded", () => {
    const authButton = document.getElementById("auth-button");
    const registerButton = document.getElementById("register-button");
    const profileButton = document.getElementById("profile-button");
    const debtButton = document.getElementById("debt-button");
    const createTripButton = document.getElementById("create-trip-button");
    const adminPanelButton = document.getElementById("admin-panel-button");
    const joinTripButton = document.getElementById("join-trip-button");
    const menuToggle = document.getElementById("menu-toggle");
    const navbar = document.getElementById("navbar");

    // Funkcja do przełączania widoczności menu
    menuToggle.addEventListener("click", () => {
        const menu = navbar.querySelector(".menu");
        menu.classList.toggle("show");
    });

    const token = localStorage.getItem("jwt_token") || sessionStorage.getItem("jwt_token");

    // Funkcja dodająca klasę fade-in z opóźnieniem
    function addFadeInAnimation(element) {
        element.style.display = "inline-block"; 
        element.classList.add("fade-in");
    }

        // Funkcja do sprawdzania roli użytkownika na podstawie tokenu JWT
    function getUserRoleFromToken() {
        if (!token) return null;

        try {
            const payload = JSON.parse(atob(token.split(".")[1]));
            return payload["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] || null;
        } catch (error) {
            console.error("Nie udało się odczytać roli użytkownika z tokenu:", error);
            return null;
        }
    }

    const userRole = getUserRoleFromToken();

    // Funkcja do sprawdzania liczby oczekujących zaproszeń i płatności
    async function updateNotificationsCount() {
        if (!token || !notificationsButton) return;

        try {
            // Pobierz liczbę oczekujących zaproszeń
            const invitationsResponse = await fetch("http://localhost:7235/api/Invitations/pending-count", {
                method: "GET",
                headers: {
                    "Authorization": `Bearer ${token}`,
                    "Content-Type": "application/json",
                },
            });

            const paymentRequestsResponse = await fetch("http://localhost:7235/api/Expenses/pending-payment-requests-count", {
                method: "GET",
                headers: {
                    "Authorization": `Bearer ${token}`,
                    "Content-Type": "application/json",
                },
            });

            if (!invitationsResponse.ok || !paymentRequestsResponse.ok) {
                console.error("Nie udało się pobrać danych powiadomień.");
                return;
            }

            const invitationsData = await invitationsResponse.json();
            const paymentRequestsData = await paymentRequestsResponse.json();

            // Zsumuj liczbę zaproszeń i zgłoszeń płatności
            const totalPending = (invitationsData.count || 0) + (paymentRequestsData.count || 0);

            // Aktualizacja tekstu w przycisku "Powiadomienia"
            const notificationsLink = notificationsButton.querySelector("a");
            if (notificationsLink) {
                notificationsLink.style.color = "white";
                notificationsLink.innerHTML = totalPending > 0
                    ? `Powiadomienia <span style="color: yellow;">(${totalPending})</span>`
                    : "Powiadomienia";
            }
        } catch (error) {
            console.error("Błąd podczas aktualizacji powiadomień:", error);
        }
    }

    // Funkcja do sprawdzania liczby zaległych długów
    async function checkPendingDebts() {
        if (!token || !debtButton) return;

        try {
            const response = await fetch("http://localhost:7235/api/Expenses/pending-debts-count", {
                method: "GET",
                headers: {
                    "Authorization": `Bearer ${token}`,
                    "Content-Type": "application/json",
                },
            });

            if (!response.ok) {
                console.error("Nie udało się sprawdzić liczby zaległych długów.");
                return;
            }

            const data = await response.json();
            const pendingCount = data.count || 0;

            // Aktualizacja tekstu przycisku "Długi"
            const debtLink = debtButton.querySelector("a");
            if (debtLink) {
                debtLink.style.color = "white";
                debtLink.innerHTML = pendingCount > 0
                    ? `Długi <span style="color: yellow;">(${pendingCount})</span>`
                    : "Długi";
            }
        } catch (error) {
            console.error("Błąd podczas sprawdzania liczby zaległych długów:", error);
        }
    }

    // Obsługa logowania i wylogowania
    if (token) {
        authButton.innerHTML = '<a href="#">Wyloguj się</a>';
        authButton.onclick = () => {
            const confirmLogout = confirm("Czy na pewno chcesz się wylogować?");
            if (confirmLogout) {
                localStorage.removeItem("jwt_token");
                sessionStorage.removeItem("jwt_token");
                window.location.href = "login.html";
            }
        };

        if (userRole === "Admin") {
            // Administrator
            if (registerButton) registerButton.style.display = "none";
            if (profileButton) profileButton.style.display = "none";
            if (createTripButton) createTripButton.style.display = "none";
            if (joinTripButton) joinTripButton.style.display = "none";
            if (notificationsButton) notificationsButton.style.display = "none";
            if (debtButton) debtButton.style.display = "none";

            // Wyświetl przycisk do panelu administratora
            if (adminPanelButton) {
                adminPanelButton.style.display = "inline-block";
                adminPanelButton.innerHTML = '<a href="admin">Panel Administratora</a>';
            }
        } else {
            // Zwykły użytkownik
            if (adminPanelButton) adminPanelButton.style.display = "none";

            if (registerButton) registerButton.style.display = "none";
            if (profileButton) profileButton.style.display = "inline-block";

            // Wyświetlenie przycisków z animacją
            if (createTripButton) addFadeInAnimation(createTripButton);
            if (joinTripButton) addFadeInAnimation(joinTripButton);
            if (notificationsButton) notificationsButton.style.display = "inline-block";
            if (debtButton) debtButton.style.display = "inline-block";

            updateNotificationsCount();
            checkPendingDebts();
        }
    } else {
        // Użytkownik niezalogowany
        authButton.innerHTML = '<a href="login.html">Zaloguj</a>';
        authButton.onclick = null;

        if (registerButton) registerButton.style.display = "block";
        if (profileButton) profileButton.style.display = "none";
        if (adminPanelButton) adminPanelButton.style.display = "none";

        // Ukrycie przycisków
        if (createTripButton) createTripButton.style.display = "none";
        if (joinTripButton) joinTripButton.style.display = "none";
        if (notificationsButton) notificationsButton.style.display = "none";
        if (debtButton) debtButton.style.display = "none";
    }
});
