document.addEventListener("DOMContentLoaded", function () {
    const urlParams = new URLSearchParams(window.location.search);
    const tripId = urlParams.get("tripId");

    if (!tripId) {
        alert("ID podróży nie zostało znalezione w URL. Spróbuj ponownie.");
        window.location.href = "index.html";
        return;
    }

    let tripStartDate, tripEndDate;
    fetch(`http://localhost:7235/api/Trip/details/${tripId}`)
        .then(response => response.json())
        .then(data => {
            if (data && data.startDate && data.endDate) {
                tripStartDate = new Date(data.startDate);
                tripEndDate = new Date(data.endDate);
                tripEndDate.setHours(23, 59, 59, 999);
            } else {
                alert("Nie udało się pobrać daty wyjazdu. Spróbuj ponownie.");
                window.location.href = "index.html";
            }
        })
        .catch(error => {
            console.error("Błąd podczas pobierania daty wyjazdu:", error);
            alert("Wystąpił problem z pobieraniem daty wyjazdu.");
        });

    document.getElementById("cancel-button").addEventListener("click", function () {
        window.location.href = `trip-details.html?tripId=${tripId}`;
    });

    document.getElementById("expense-form").addEventListener("submit", function (event) {
        event.preventDefault();

        const token = localStorage.getItem("jwt_token") || sessionStorage.getItem("jwt_token");

        if (!token) {
            alert("Nie jesteś zalogowany. Zaloguj się, aby dodać wydatek.");
            window.location.href = "login.html";
            return;
        }

        const expenseData = {
            tripId: tripId,
            description: document.getElementById("expense-name").value.trim(),
            category: document.getElementById("expense-category").value.trim(),
            cost: parseFloat(document.getElementById("expense-cost").value),
            currency: document.getElementById("expense-currency").value.trim(),
            date: document.getElementById("expense-date").value,
            location: null 
        };

        // Pobierz lokalizację tylko jeśli użytkownik coś wpisał
        const locationValue = document.getElementById("expense-location").value.trim();
        if (locationValue) {
            expenseData.location = locationValue;
        }

        if (!expenseData.description || !expenseData.category || isNaN(expenseData.cost) || !expenseData.currency || !expenseData.date) {
            alert("Proszę uzupełnić wszystkie pola.");
            return;
        }

        const expenseDate = new Date(expenseData.date);
        if (expenseDate < tripStartDate || expenseDate > tripEndDate) {
            alert(`Data wydatku musi mieścić się w przedziale ${tripStartDate.toISOString().slice(0, 10)} - ${tripEndDate.toISOString().slice(0, 10)}`);
            return;
        }

        fetch("http://localhost:7235/api/Expenses/create", {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
                "Authorization": `Bearer ${token}`,
            },
            body: JSON.stringify(expenseData),
        })
            .then(response => {
                if (!response.ok) {
                    return response.json().then(err => {
                        throw new Error(err.message || "Błąd podczas zapisywania wydatku");
                    });
                }
                return response.json();
            })
            .then(data => {
                alert("Wydatek został zapisany!");
                window.location.href = `trip-details.html?tripId=${tripId}`;
            })
            .catch(error => {
                console.error("Błąd:", error);
                alert("Wystąpił problem. Spróbuj ponownie.");
            });
    });
});