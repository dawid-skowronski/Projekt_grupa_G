document.addEventListener("DOMContentLoaded", async () => {
    const summaryTable = document.getElementById("summary-table");
    const currencyFilter = document.getElementById("currency-filter");
    const tripId = new URLSearchParams(window.location.search).get("tripId");
    const jwtToken = localStorage.getItem("jwt_token") || sessionStorage.getItem("jwt_token");

    if (!jwtToken || !tripId) {
        alert("Brak tokenu JWT lub ID wyjazdu.");
        return;
    }

    let expenses = []; // Przechowywanie danych o wydatkach

    // Pobierz wydatki z API
    async function fetchExpenses() {
        try {
            const response = await fetch(`http://localhost:7235/api/Expenses/trip/${tripId}`, {
                headers: {
                    Authorization: `Bearer ${jwtToken}`,
                },
            });

            if (!response.ok) {
                throw new Error("Nie udało się pobrać danych wydatków.");
            }

            return await response.json();
        } catch (error) {
            console.error("Błąd podczas pobierania wydatków:", error);
            alert("Nie udało się załadować wydatków.");
            return [];
        }
    }

    // Oblicz podsumowanie
    function calculateSummary(expenses) {
        const totalAmounts = { PLN: 0, EUR: 0, USD: 0 };
        const categoryTotals = {
            transport: 0,
            jedzenie: 0,
            zakwaterowanie: 0,
            rozrywka: 0,
            inne: 0,
        };

        expenses.forEach((expense) => {
            if (totalAmounts[expense.currency] !== undefined) {
                totalAmounts[expense.currency] += expense.cost;
            }

            // Przydziel kwotę do odpowiedniej kategorii
            switch (expense.category?.toLowerCase()) {
                case "transport":
                    categoryTotals.transport += expense.currency === currencyFilter.value ? expense.cost : 0;
                    break;
                case "jedzenie":
                    categoryTotals.jedzenie += expense.currency === currencyFilter.value ? expense.cost : 0;
                    break;
                case "zakwaterowanie":
                    categoryTotals.zakwaterowanie += expense.currency === currencyFilter.value ? expense.cost : 0;
                    break;
                case "rozrywka":
                    categoryTotals.rozrywka += expense.currency === currencyFilter.value ? expense.cost : 0;
                    break;
                default:
                    categoryTotals.inne += expense.currency === currencyFilter.value ? expense.cost : 0;
                    break;
            }
        });

        // Aktualizacja sekcji "Łącznie"
        const totalExpensesElement = document.getElementById("total-expenses");
        totalExpensesElement.innerHTML = `
            ${Object.entries(totalAmounts)
                .filter(([_, amount]) => amount > 0)
                .map(([currency, amount]) => `${amount.toFixed(2)} ${currency}`)
                .join(" / ")}
        `;

        // Aktualizacja tabeli podsumowania według wybranej waluty
        document.getElementById("category-transport").textContent = `${categoryTotals.transport.toFixed(2)} ${currencyFilter.value}`;
        document.getElementById("category-jedzenie").textContent = `${categoryTotals.jedzenie.toFixed(2)} ${currencyFilter.value}`;
        document.getElementById("category-zakwaterowanie").textContent = `${categoryTotals.zakwaterowanie.toFixed(2)} ${currencyFilter.value}`;
        document.getElementById("category-rozrywka").textContent = `${categoryTotals.rozrywka.toFixed(2)} ${currencyFilter.value}`;
        document.getElementById("category-inne").textContent = `${categoryTotals.inne.toFixed(2)} ${currencyFilter.value}`;
    }

    expenses = await fetchExpenses();
    calculateSummary(expenses);

    currencyFilter.addEventListener("change", () => {
        calculateSummary(expenses);
    });
});
