document.addEventListener("DOMContentLoaded", function () {
    const urlParams = new URLSearchParams(window.location.search);
    const tripId = urlParams.get("tripId"); 
    const jwtToken = localStorage.getItem("jwt_token") || sessionStorage.getItem("jwt_token");

    if (!jwtToken) {
        alert("Nie znaleziono tokenu JWT. Proszę się zalogować.");
        window.location.href = "login.html"; 
        return;
    }

    if (!tripId) {
        alert("Brak ID wyjazdu w adresie URL.");
        window.location.href = "index.html"; 
        return;
    }

    // Funkcja do pobrania danych wyjazdu
    async function fetchTripDetails(tripId) {
        try {
            const response = await fetch(`http://localhost:7235/api/Trip/details/${tripId}`, {
                headers: {
                    "Authorization": `Bearer ${jwtToken}`,
                }
            });
            if (!response.ok) {
                throw new Error("Nie udało się pobrać danych wyjazdu.");
            }
            const data = await response.json();
            return data;
        } catch (error) {
            console.error("Błąd podczas ładowania danych wyjazdu:", error);
            alert("Wystąpił błąd podczas ładowania danych wyjazdu.");
            window.location.href = "index.html"; 
        }
    }

    // Funkcja do wyświetlenia szczegółów wyjazdu
    async function displayTripDetails() {
        const tripData = await fetchTripDetails(tripId);
        if (!tripData) {
            return;
        }

        document.getElementById("trip-name").textContent = tripData.name;
        document.getElementById("description").textContent = tripData.description;
        document.getElementById("secretcode").textContent = tripData.secretCode;
        document.getElementById("start-date").textContent = new Date(tripData.startDate).toLocaleDateString("pl-PL");
        document.getElementById("end-date").textContent = new Date(tripData.endDate).toLocaleDateString("pl-PL");

        const membersList = document.getElementById("members-list");
        membersList.innerHTML = ""; 

        tripData.members.forEach(member => {
            const listItem = document.createElement("li");
            listItem.textContent = `${member.username} (${member.email})`;

            // Dodanie statusu offline, jeśli użytkownik nie zaakceptował zaproszenia
            if (member.isOffline) {
                const statusSpan = document.createElement("span");
                statusSpan.classList.add("status");
                statusSpan.textContent = "(Offline)";
                listItem.appendChild(statusSpan);
            }

            membersList.appendChild(listItem);
        });
    }

    // Funkcja do wysyłania zaproszeń użytkownikom
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

            const result = await response.json();
            console.log(`Odpowiedź z API dla użytkownika ${username}:`, result);

            if (!response.ok) {
                console.warn(`Zaproszenie dla użytkownika ${username} nie powiodło się:`, result.errors);
                alert(`Nie udało się wysłać zaproszenia dla użytkownika ${username}. Szczegóły: ${JSON.stringify(result.errors)}`);
            } else {
                alert(`Zaproszenie dla użytkownika ${username} wysłane pomyślnie!`);
            }
        } catch (error) {
            console.error(`Błąd przy wysyłaniu zaproszenia dla użytkownika ${username}:`, error);
            alert(`Wystąpił problem przy wysyłaniu zaproszenia dla użytkownika ${username}.`);
        }
    }

    // Funkcja obsługująca dodawanie użytkowników
async function addUsersToTrip() {
    const inviteUsersInput = document.getElementById("invite-users");
    const usernames = inviteUsersInput.value.split(",").map(u => u.trim()).filter(u => u);

    if (usernames.length === 0) {
        alert("Podaj przynajmniej jedną nazwę użytkownika.");
        return;
    }

    // Pobierz szczegóły wyjazdu, aby sprawdzić istniejących członków i zaproszenia
    const tripData = await fetchTripDetails(tripId);
    if (!tripData) return;

    const existingMembers = tripData.members.map(member => member.username.toLowerCase());

    for (const username of usernames) {
        if (existingMembers.includes(username.toLowerCase())) {
            alert(`Użytkownik ${username} jest już członkiem wyjazdu.`);
            continue; 
        }

        // Sprawdź, czy istnieje już zaproszenie dla użytkownika
        const invitationExists = await checkExistingInvitation(username);
        if (invitationExists) {
            alert(`Zaproszenie dla użytkownika ${username} już istnieje.`);
            continue; 
        }

        await sendInvitation(tripId, username, jwtToken);
    }

    displayTripDetails();
}

// Funkcja do sprawdzania, czy istnieje zaproszenie dla użytkownika
async function checkExistingInvitation(username) {
    try {
        const response = await fetch(`http://localhost:7235/api/Invitations/check?tripId=${tripId}&username=${username}`, {
            headers: {
                "Authorization": `Bearer ${jwtToken}`,
                "Content-Type": "application/json"
            }
        });

        if (!response.ok) {
            throw new Error("Nie udało się sprawdzić istniejących zaproszeń.");
        }

        const result = await response.json();
        return result.exists; 
    } catch (error) {
        console.error(`Błąd podczas sprawdzania zaproszenia dla użytkownika ${username}:`, error);
        return false; 
    }
}

 // Funkcja do opuszczenia wyjazdu
 async function leaveTrip() {
    try {
        const response = await fetch(`http://localhost:7235/api/Trip/leave/${tripId}`, {
            method: "DELETE",
            headers: {
                "Authorization": `Bearer ${jwtToken}`,
                "Content-Type": "application/json"
            }
        });

        if (!response.ok) {
            throw new Error("Nie udało się opuścić wyjazdu.");
        }

        const result = await response.json();
        alert(result.message || "Wyjazd opuszczony pomyślnie.");
        window.location.href = "index";
    } catch (error) {
        console.error("Błąd podczas opuszczania wyjazdu:", error);
        alert("Wystąpił problem podczas opuszczania wyjazdu.");
    }
}

    displayTripDetails();

    document.getElementById("back-button").addEventListener("click", function () {
        window.location.href = "index.html";
    });

    document.getElementById("create-expense-button").addEventListener("click", function () {
        window.location.href = `create-expense.html?tripId=${tripId}`;
    });

    document.getElementById("expenses-button").addEventListener("click", function () {
        window.location.href = `expenses.html?tripId=${tripId}`;
    });
    
    document.getElementById("summary-button").addEventListener("click", function () {
        window.location.href = `summary.html?tripId=${tripId}`;
    });

    document.getElementById("send-invite-button").addEventListener("click", function () {
        addUsersToTrip();
    });

    document.getElementById("leave-trip-button").addEventListener("click", function () {
        if (confirm("Czy na pewno chcesz opuścić ten wyjazd?")) {
            leaveTrip();
        }
    });
});
