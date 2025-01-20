document.addEventListener("DOMContentLoaded", async () => {
    const paymentRequestsContainer = document.getElementById("payment-requests-container");
    const token = localStorage.getItem("jwt_token") || sessionStorage.getItem("jwt_token");

    if (!token) {
        alert("Nie jesteś zalogowany. Zaloguj się, aby zobaczyć zgłoszenia płatności.");
        window.location.href = "login.html";
        return;
    }

    // Funkcja pobierania zgłoszeń płatności
    async function fetchPaymentRequests() {
        try {
            console.log("Wysyłanie żądania do API /payment-requests...");
            const response = await fetch("http://localhost:7235/api/Expenses/payment-requests", {
                method: "GET",
                headers: {
                    "Authorization": `Bearer ${token}`,
                    "Content-Type": "application/json",
                },
            });
            

            if (!response.ok) {
                throw new Error("Nie udało się pobrać zgłoszeń płatności.");
            }

            const requests = await response.json();
            console.log("Otrzymano zgłoszenia płatności:", requests);

            // Sprawdzenie, czy są zgłoszenia
            if (!requests || requests.length === 0) {
                paymentRequestsContainer.innerHTML = "<p>Brak zgłoszeń płatności do rozpatrzenia.</p>";
                return;
            }

            // Wyświetlanie zgłoszeń
            paymentRequestsContainer.innerHTML = requests.map(request => `
                <div class="payment-request">
                    <p><strong>Dług:</strong> ${request.debtDescription}</p>
                    <p><strong>Kwota:</strong> ${request.amount} ${request.currency}</p>
                    <p><strong>Zgłoszone przez:</strong> ${request.requestedBy}</p>
                    <p><strong>Metoda płatności:</strong> ${request.paymentMethod}</p>
                    <p><strong>Zgłoszono:</strong> ${new Date(request.requestedAt).toLocaleString()}</p>
                    <button class="btn btn-success" data-id="${request.id}" data-approve="true">Zatwierdź</button>
                    <button class="btn btn-danger" data-id="${request.id}" data-approve="false">Odrzuć</button>
                </div>
            `).join("");

            // Obsługa kliknięć przycisków
            document.querySelectorAll(".btn").forEach(button => {
                button.addEventListener("click", () => handleReviewRequest(button.dataset.id, button.dataset.approve === "true"));
            });

        } catch (error) {
            console.error("Błąd podczas pobierania zgłoszeń płatności:", error);
            paymentRequestsContainer.innerHTML = `<p class="text-danger">${error.message}</p>`;
        }
    }

    // Funkcja obsługi zgłoszenia płatności
    async function handleReviewRequest(requestId, approve) {
        try {
            const response = await fetch(`http://localhost:7235/api/Expenses/review-payment/${requestId}`, {
                method: "PUT",
                headers: {
                    "Authorization": `Bearer ${token}`,
                    "Content-Type": "application/json",
                },
                body: JSON.stringify({ approved: approve }),
            });

            if (!response.ok) {
                throw new Error("Nie udało się rozpatrzyć zgłoszenia.");
            }

            const result = await response.json();
            alert(result.message);

            // Odświeżenie zgłoszeń
            await fetchPaymentRequests();
        } catch (error) {
            console.error("Błąd podczas rozpatrywania zgłoszenia:", error);
            alert(error.message || "Wystąpił problem podczas rozpatrywania zgłoszenia.");
        }
    }

    fetchPaymentRequests();
});
