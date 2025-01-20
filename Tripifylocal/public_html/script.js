document.addEventListener("DOMContentLoaded", function () {
    console.log('DOM is ready');

    const loginSection = document.getElementById("login-section");
    const welcomeSection = document.getElementById("welcome-section");
    const usernameDisplay = document.getElementById("username");
    const TripSection = document.getElementById("trip-section");
    const tripContainer = document.getElementById("user-trips");
    const loginButton = document.getElementById("login-button");
    const registerButton = document.getElementById("register-button");
    const logoutButton = document.getElementById("logout-button");
    const editProfileButton = document.getElementById("edit-profile-button");
    const createTripButton = document.getElementById("create-trip-button");

    const jwtToken = localStorage.getItem("jwt_token");

    if (jwtToken) {
        loginSection.style.display = "none";
        welcomeSection.style.display = "block";

        const decodedToken = decodeJwt(jwtToken);
        const username = decodedToken?.['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'];

        const storedUsername = localStorage.getItem('username');
        if (storedUsername) {
            usernameDisplay.textContent = `Witaj, ${storedUsername}`;
        } else if (username) {
            usernameDisplay.textContent = `Witaj, ${username}`;
            localStorage.setItem('username', username);
        } else {
            usernameDisplay.textContent = "Niezalogowany użytkownik";
        }

       // Pobieranie wyjazdów użytkownika
fetch("http://localhost:7235/api/Trip/my-trips", {
    method: "GET",
    headers: {
        "Authorization": `Bearer ${jwtToken}`
    }
})
.then(response => {
    if (!response.ok) {
        throw new Error("Błąd podczas pobierania wyjazdów.");
    }
    return response.json();
})
.then(data => {
    tripContainer.innerHTML = ""; 

    if (data && data.length > 0) {
        data.forEach(trip => {
            const tripElement = document.createElement("div");
            tripElement.className = "trip-item";

            tripElement.innerHTML = `
                <h3>${trip.name}</h3>
                <p>Data rozpoczęcia: ${trip.startDate}</p>
                <p>Data zakończenia: ${trip.endDate}</p>
            `;

            // Podświetlanie wyjazdu na hover
            tripElement.addEventListener("mouseover", () => {
                tripElement.classList.add("highlight");
            });
            tripElement.addEventListener("mouseout", () => {
                tripElement.classList.remove("highlight");
            });

            tripElement.addEventListener("click", () => {
                window.location.href = `trip-details.html?tripId=${trip.tripId}`;
            });

            tripContainer.appendChild(tripElement);
        });
    } else {
        tripContainer.innerHTML = "<p>Nie masz jeszcze żadnych wyjazdów.</p>";
    }
})
.catch(error => {
    console.error("Błąd podczas ładowania wyjazdów:", error);
    tripContainer.innerHTML = "<p>Wystąpił błąd podczas ładowania wyjazdów. Spróbuj ponownie później.</p>";
});


        // Obsługa przycisku "Wyloguj się"
        if (logoutButton) {
            logoutButton.addEventListener("click", function () {
                localStorage.removeItem("jwt_token");
                localStorage.removeItem("username");
                window.location.href = "index.html";
            });
        }

        // Obsługa przycisku "Edytuj profil"
        if (editProfileButton) {
            editProfileButton.addEventListener("click", function () {
                window.location.href = "edit-profile.html";
            });
        }

        // Obsługa przycisku "Stwórz wyjazd"
        if (createTripButton) {
            createTripButton.addEventListener("click", function () {
                window.location.href = "create-trip.html";
            });
        }
    } else {
        loginSection.style.display = "block";
        welcomeSection.style.display = "none";
        TripSection.style.display = "none";
    }

    if (loginButton) {
        loginButton.addEventListener("click", function () {
            window.location.href = "login.html";
        });
    }

    if (registerButton) {
        registerButton.addEventListener("click", function () {
            window.location.href = "register.html";
        });
    }
});

function decodeJwt(token) {
    try {
        const base64Url = token.split('.')[1];
        const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
        const jsonPayload = decodeURIComponent(atob(base64).split('').map(c =>
            '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2)
        ).join(''));
        return JSON.parse(jsonPayload);
    } catch (error) {
        console.error("Błąd podczas dekodowania tokenu JWT:", error);
        return null;
    }
}