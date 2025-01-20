document.addEventListener("DOMContentLoaded", function () {
    const tripForm = document.getElementById("trip-form");
    const jwtToken = localStorage.getItem("jwt_token") || sessionStorage.getItem("jwt_token");
    const currentUsername = (localStorage.getItem("username") || "").trim().toLowerCase();

    console.log("Token JWT podczas ładowania strony:", jwtToken);
    console.log("Aktualna nazwa użytkownika:", currentUsername);

    if (!jwtToken) {
        alert("Nie znaleziono tokenu JWT. Proszę się zalogować.");
        window.location.href = "login.html";
        return;
    }

    if (tripForm) {
        tripForm.addEventListener("submit", async function (event) {
            event.preventDefault();

            const tripName = document.getElementById("trip-name").value.trim();
            const tripDescription = document.getElementById("trip-description").value.trim();
            const tripStartDate = document.getElementById("start-date").value;
            const tripEndDate = document.getElementById("end-date").value;
            const inviteUsers = document.getElementById("invite-users").value.trim();

            if (!tripName || !tripDescription || !tripStartDate || !tripEndDate) {
                alert("Proszę wypełnić wszystkie pola.");
                return;
            }

            if (new Date(tripStartDate) > new Date(tripEndDate)) {
                alert("Data zakończenia nie może być wcześniejsza niż data rozpoczęcia.");
                return;
            }

            let usernames = inviteUsers
                .split(',')
                .map((u) => u.trim().toLowerCase())
                .filter((u) => u);

            console.log("Lista zaproszonych użytkowników przed walidacją:", usernames);

            usernames = usernames.filter((u) => u !== currentUsername);
            console.log("Lista zaproszonych użytkowników po usunięciu siebie:", usernames);

            if (usernames.includes(currentUsername)) {
                console.warn("BŁĄD WALIDACJI: użytkownik nadal jest na liście");
                alert("Nie możesz zaprosić samego siebie. Usuń swoją nazwę użytkownika z listy zaproszonych osób.");
                return;
            }

            const tripData = {
                name: tripName,
                description: tripDescription,
                startDate: tripStartDate,
                endDate: tripEndDate,
            };

            console.log("Dane wyjazdu przed wysłaniem:", tripData);

            try {
                const response = await fetch("http://localhost:7235/api/Trip/create", {
                    method: "POST",
                    headers: {
                        "Content-Type": "application/json",
                        "Authorization": `Bearer ${jwtToken}`,
                    },
                    body: JSON.stringify(tripData),
                });

                const data = await response.json();
                console.log("Odpowiedź z tworzenia wyjazdu:", data);

                if (response.ok && data.tripId) {
                    alert("Wyjazd został pomyślnie utworzony!");

                    if (usernames.length > 0) {
                        for (const username of usernames) {
                            await sendInvitation(data.tripId, username, jwtToken);
                        }
                    }
                    window.location.href = "index.html";
                } else {
                    alert("Wystąpił błąd podczas tworzenia wyjazdu. Spróbuj ponownie.");
                }
            } catch (error) {
                console.error("Błąd podczas tworzenia wyjazdu:", error);
                alert("Wystąpił problem podczas tworzenia wyjazdu. Spróbuj ponownie.");
            }
        });
    }

    async function sendInvitation(tripId, username, jwtToken) {
        const invitationData = {
            tripId: tripId,
            receiverUsername: username,
        };

        console.log("Dane zaproszenia przed wysłaniem:", invitationData);

        try {
            const response = await fetch("http://localhost:7235/api/Invitations/send", {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                    "Authorization": `Bearer ${jwtToken}`,
                },
                body: JSON.stringify(invitationData),
            });

            let result;
            try {
                result = await response.json();
            } catch (parseError) {
                console.warn("Nie udało się sparsować odpowiedzi API jako JSON:", parseError);
                result = { message: "Błąd serwera, brak szczegółowych informacji." };
            }

            console.log(`Odpowiedź z API dla użytkownika ${username}:`, result);

            if (!response.ok) {
                const errorMessage = result.message || JSON.stringify(result) || "Nieznany błąd.";
                console.warn(`Zaproszenie dla użytkownika ${username} nie powiodło się:`, errorMessage);
                alert(`Nie udało się wysłać zaproszenia dla użytkownika ${username}. Szczegóły: ${errorMessage}`);
            } else {
                alert(`Zaproszenie dla użytkownika ${username} wysłane pomyślnie!`);
            }
        } catch (error) {
            console.error(`Błąd przy wysyłaniu zaproszenia dla użytkownika ${username}:`, error);
            alert(`Wystąpił problem przy wysyłaniu zaproszenia dla użytkownika ${username}. Szczegóły: ${error.message}`);
        }
    }
});
