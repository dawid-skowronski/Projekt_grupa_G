document.addEventListener("DOMContentLoaded", function () {
    const joinTripForm = document.getElementById("join-trip-form");
    const token = localStorage.getItem("jwt_token") || sessionStorage.getItem("jwt_token");

    if (joinTripForm) {
        joinTripForm.addEventListener("submit", function (e) {
            e.preventDefault(); 

            const tripCode = document.getElementById("trip-code").value;

            if (!tripCode) {
                alert("Proszę podać kod wyjazdu.");
                return;
            }

            // Sprawdzamy, czy token JWT istnieje
            if (!token) {
                alert("Nie jesteś zalogowany. Zaloguj się, aby dołączyć do wyjazdu.");
                return;
            }

            // Funkcja do dekodowania JWT i wyciągania userId
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

            const decodedToken = decodeJwt(token);
            const userId = decodedToken?.['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'];
            
            if (!userId) {
                alert("Brak identyfikatora użytkownika w tokenie.");
                return;
            }

            // Wyślij kod do backendu
            fetch("http://localhost:7235/api/Trip/join", {
                method: "POST",
                headers: {
                    "Authorization": `Bearer ${token}`,
                    "Content-Type": "application/json"
                },
                body: JSON.stringify({ userId, secretCode: tripCode })
            })
            .then(response => {
                if (!response.ok) {
                    return response.json().then(err => {
                        throw new Error(err.message || "Błąd podczas przetwarzania żądania.");
                    });
                }
                return response.json(); 
            })
            .then(data => {
                console.log("Odpowiedź z serwera:", data); 
                if (data.message === "Dołączono do wyjazdu.") {
                    alert("Pomyślnie dołączono do wyjazdu!");
                    
                    setTimeout(function () {
                        window.location.href = "index.html"; 
                    }, 200);
                } else {
                    throw new Error(data.message || "Nie udało się dołączyć do wyjazdu.");
                }
            })
            .catch(error => {
                console.error("Błąd:", error);
                alert(error.message || "Nie udało się dołączyć do wyjazdu. Spróbuj ponownie.");
            });
        });

        // Obsługa przycisku powrotu
        const backButton = document.getElementById("back-button");
        if (backButton) {
            backButton.addEventListener("click", function () {
                window.location.href = "index.html";
            });
        }
    }
});
