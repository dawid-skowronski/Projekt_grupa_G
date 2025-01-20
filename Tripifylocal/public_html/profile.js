document.addEventListener("DOMContentLoaded", async () => {
    const usernameElement = document.getElementById("profile-username");
    const emailElement = document.getElementById("profile-email");
    const tripContainer = document.getElementById("user-trips"); // Dodaj ten element w HTML

    try {
        // Pobierz token JWT z localStorage lub sessionStorage
        const token = localStorage.getItem("jwt_token") || sessionStorage.getItem("jwt_token");
        if (!token) {
            throw new Error("Brak tokenu uwierzytelniającego. Zaloguj się ponownie.");
        }

        // Wyślij żądanie do API, aby pobrać dane profilu
        const response = await fetch("http://localhost:7235/api/Account/profile", {
            method: "GET",
            headers: {
                "Authorization": `Bearer ${token}`,
                "Content-Type": "application/json",
            },
        });

        if (!response.ok) {
            const errorData = await response.json().catch(() => null);
            throw new Error(errorData?.message || "Nie udało się załadować danych profilu.");
        }

        // Wyświetl dane profilu
        const profileData = await response.json();
        usernameElement.textContent = profileData.username;
        emailElement.textContent = profileData.email;

        // Wyślij żądanie do API, aby pobrać listę wyjazdów
        const tripsResponse = await fetch("http://localhost:7235/api/Trip/my-trips", {
            method: "GET",
            headers: {
                "Authorization": `Bearer ${token}`
            }
        });

        if (!tripsResponse.ok) {
            throw new Error("Błąd podczas pobierania wyjazdów.");
        }

        const trips = await tripsResponse.json();
        tripContainer.innerHTML = ""; // Wyczyść kontener na wyjazdy

        if (trips && trips.length > 0) {
            trips.forEach(trip => {
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
    } catch (error) {
        console.error("Wystąpił błąd:", error.message);
        alert(`Wystąpił problem: ${error.message}`);
        window.location.href = "login.html"; // Przekierowanie na stronę logowania
    }
});
