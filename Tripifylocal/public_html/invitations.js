document.addEventListener("DOMContentLoaded", function () {
    const invitationsList = document.getElementById("invitations-container");
    const token = localStorage.getItem("jwt_token") || sessionStorage.getItem("jwt_token");

    if (!token) {
        alert("Nie jesteś zalogowany. Zaloguj się, aby zobaczyć swoje zaproszenia.");
        window.location.href = "login.html";
        return;
    }

    // Funkcja pobierania zaproszeń
    async function fetchInvitations() {
        try {
            const response = await fetch("http://localhost:7235/api/Invitations/received", {
                method: "GET",
                headers: {
                    "Authorization": `Bearer ${token}`,
                    "Content-Type": "application/json"
                }
            });

            if (!response.ok) {
                const errorData = await response.json();
                throw new Error(errorData.message || "Błąd podczas pobierania zaproszeń.");
            }

            const invitations = await response.json();

            // Sprawdzenie, czy są zaproszenia
            if (!invitations || invitations.length === 0) {
                invitationsList.innerHTML = "<p>Nie masz żadnych nowych zaproszeń.</p>";
                return;
            }

            // Wyświetlanie zaproszeń
            invitationsList.innerHTML = invitations.map(invitation => `
                <div class="invitation">
                    <p><strong>Wyjazd:</strong> ${invitation.tripName || "Brak danych"}</p>
                    <p><strong>Wysłane przez:</strong> ${invitation.senderUsername || "Nieznany użytkownik"}</p>
                    <p><strong>Data:</strong> ${new Date(invitation.createdAt).toLocaleString()}</p>
                    <p><strong>Status:</strong> ${invitation.status}</p>
                    <button class="btn btn-success" data-id="${invitation.invitationId}" data-status="Accepted">Przyjmij</button>
                    <button class="btn btn-danger" data-id="${invitation.invitationId}" data-status="Rejected">Odrzuć</button>
                </div>
            `).join("");

            // Obsługa kliknięć przycisków
            document.querySelectorAll(".btn").forEach(button => {
                button.addEventListener("click", () => respondToInvitation(button.dataset.id, button.dataset.status));
            });

        } catch (error) {
            invitationsList.innerHTML = `<p class="text-danger">${error.message}</p>`;
        }
    }

    // Funkcja odpowiadania na zaproszenie
    async function respondToInvitation(invitationId, status) {
        try {
            const response = await fetch(`http://localhost:7235/api/Invitations/respond`, {
                method: "PUT",
                headers: {
                    "Authorization": `Bearer ${token}`,
                    "Content-Type": "application/json"
                },
                body: JSON.stringify({
                    invitationId: parseInt(invitationId),
                    status: status
                })
            });

            if (!response.ok) {
                const errorData = await response.json();
                throw new Error(errorData.message || "Błąd podczas przetwarzania zaproszenia.");
            }

            const result = await response.json();
            alert(result.message);

            // Odświeżenie listy zaproszeń 
            await fetchInvitations(); 

        } catch (error) {
            alert(error.message || "Nie udało się przetworzyć zaproszenia. Spróbuj ponownie.");
        }
    }

    // Wywołanie pobierania zaproszeń po załadowaniu strony
    fetchInvitations();

    // Obsługa przycisku "Wróć"
    const backButton = document.getElementById("back-button");
    if (backButton) {
        backButton.addEventListener("click", function () {
            window.location.href = "index.html";
        });
    }
});
