document.addEventListener("DOMContentLoaded", function () {
    const registerForm = document.getElementById("register-form");

    if (!registerForm) {
        console.error("Formularz rejestracji nie został znaleziony.");
        return;
    }

    registerForm.addEventListener("submit", async function (event) {
        event.preventDefault(); 

        // Pobierz dane z formularza
        const email = document.getElementById("register-email").value.trim();
        const username = document.getElementById("register-username").value.trim();
        const password = document.getElementById("register-password").value;
        const confirmPassword = document.getElementById("register-confirm-password").value;

        // Walidacja: sprawdzamy, czy hasła się zgadzają
        if (password !== confirmPassword) {
            alert("Hasła muszą być takie same!");
            return;
        }

        // Tworzenie obiektu z danymi rejestracyjnymi
        const registerData = {
            email,
            username,
            password,
            ConfirmPassword: confirmPassword 
        };

        try {
            // Wysłanie żądania POST do API rejestracji
            const response = await fetch("http://localhost:7235/api/account/register", {
                method: "POST",
                headers: {
                    "Content-Type": "application/json"
                },
                body: JSON.stringify(registerData)
            });

            const result = await response.json();

            if (response.ok) {
                alert("Rejestracja zakończona sukcesem!");
                window.location.href = "login.html"; 
            } else {
                console.error("Błąd rejestracji:", result);
                alert(result.errors?.ConfirmPassword?.[0] || result.message || "Wystąpił błąd podczas rejestracji.");
            }
        } catch (error) {
            console.error("Błąd podczas rejestracji:", error);
            alert("Nie udało się połączyć z serwerem. Spróbuj ponownie.");
        }
    });
});
