document.addEventListener("DOMContentLoaded", function () {
    const loginForm = document.getElementById("login-form");

    // Funkcja do odczytania tokena z URL
    function getQueryParam(param) {
        const urlParams = new URLSearchParams(window.location.search);
        return urlParams.get(param);
    }

    // Sprawdzanie tokena w URL i zapisywanie w sessionStorage
    const facebookToken = getQueryParam("token");
    if (facebookToken) {
        sessionStorage.setItem("jwt_token", facebookToken);
        console.log("Token z Facebooka zapisany w sessionStorage:", facebookToken);
        alert("Logowanie przez Facebook udane!");
        window.location.href = "index.html"; 
    }

    // Obsługa logowania za pomocą formularza
    if (loginForm) {
        loginForm.addEventListener("submit", async function (event) {
            event.preventDefault();

            const username = document.getElementById("login-username").value;
            const password = document.getElementById("login-password").value;
            const rememberMe = document.getElementById("remember-me").checked; 

            const loginData = { username, password };

            try {
                const response = await fetch("http://localhost:7235/api/account/login", {
                    method: "POST",
                    headers: {
                        "Content-Type": "application/json"
                    },
                    body: JSON.stringify(loginData)
                });

                if (!response.ok) {
                    const errorData = await response.json();
                    alert(errorData.message || "Błąd logowania.");
                    return;
                }

                const result = await response.json();
                const token = result.token;

                // Zapisywanie tokenu w zależności od checkboxa
                if (rememberMe) {
                    localStorage.setItem("jwt_token", token);
                    console.log("Token zapisany w localStorage:", token, facebookToken );
                } else {
                    sessionStorage.setItem("jwt_token", token);
                    console.log("Token zapisany w sessionStorage:", token, facebookToken );
                }

                alert("Logowanie udane!");
                window.location.href = "index.html";
            } catch (error) {
                console.error("Błąd podczas logowania:", error);
                alert("Coś poszło nie tak. Spróbuj ponownie.");
            }
        });
    }
});

// Funkcja do obsługi widoczności hasła
function togglePasswordVisibility(passwordFieldId) {
    const passwordField = document.getElementById(passwordFieldId);
    const passwordToggle = passwordField.nextElementSibling;

    if (passwordField.type === "password") {
        passwordField.type = "text";
        passwordToggle.textContent = "🙈"; 
    } else {
        passwordField.type = "password";
        passwordToggle.textContent = "👁️"; 
    }
}

// Funkcja do obsługi logowania przez Facebooka
function facebookLogin() {
    window.location.href = "http://localhost:7235/api/Account/login-facebook";
}
