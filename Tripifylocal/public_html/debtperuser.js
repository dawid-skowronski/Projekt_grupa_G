document.addEventListener("DOMContentLoaded", async () => {
    const debtsSummaryTableBody = document.querySelector("#debts-summary tbody");
    const jwtToken = localStorage.getItem("jwt_token") || sessionStorage.getItem("jwt_token");

    if (!jwtToken) {
        alert("Brak tokenu JWT.");
        return;
    }
    
    // Funkcja do pobierania danych z API
    async function fetchDebtsSummary() {
        try {
            const response = await fetch("http://localhost:7235/api/Expenses/debts-summary", {
                method: "GET",
                headers: {
                    Authorization: `Bearer ${jwtToken}`,
                    "Content-Type": "application/json"
                }
            });

            if (!response.ok) {
                throw new Error("Nie udało się pobrać danych. Upewnij się, że jesteś zalogowany.");
            }

            return await response.json();
        } catch (error) {
            console.error("Błąd podczas pobierania danych:", error);
            return [];
        }
    }

    // Funkcja do wyświetlania danych w tabeli
    function populateDebtsSummaryTable(debts) {

        debts = debts.filter(debt => debt.totalAmount > 0.00);

        if (debts.length === 0) {
            const noDataRow = document.createElement("tr");
            const noDataCell = document.createElement("td");
            noDataCell.colSpan = 3; 
            noDataCell.textContent = "Brak aktywnych długów.";
            noDataCell.classList.add("text-center");
            noDataRow.appendChild(noDataCell);
            debtsSummaryTableBody.appendChild(noDataRow);
            return;
        }

        debts.forEach(debt => {
            const row = document.createElement("tr");

            const userCell = document.createElement("td");
            userCell.textContent = debt.creatorName;

            const amountCell = document.createElement("td");
            amountCell.textContent = debt.totalAmount;

            const currencyCell = document.createElement("td");
            currencyCell.textContent = debt.currency;

            row.appendChild(userCell);
            row.appendChild(amountCell);
            row.appendChild(currencyCell);

            debtsSummaryTableBody.appendChild(row);
        });
    }

    async function main() {
        const debtsSummary = await fetchDebtsSummary();
        populateDebtsSummaryTable(debtsSummary);
    }

    main();
});
