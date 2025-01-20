document.addEventListener("DOMContentLoaded", async () => {
    const expensesContainer = document.getElementById("expenses-container");
    const tripId = new URLSearchParams(window.location.search).get("tripId");
    const jwtToken = localStorage.getItem("jwt_token") || sessionStorage.getItem("jwt_token");

    if (!jwtToken || !tripId) {
        alert("Brak tokenu JWT lub ID wyjazdu.");
        return;
    }

    let expenses = []; 

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

    // Wyślij zmienione długi do API
    async function updateDebts(expenseId, debts) {
        try {
            const response = await fetch(`http://localhost:7235/api/Expenses/updateDebts/${expenseId}`, {
                method: "PUT",
                headers: {
                    "Authorization": `Bearer ${jwtToken}`,
                    "Content-Type": "application/json",
                },
                body: JSON.stringify({ debts }),
            });
    
            if (!response.ok) {
                const errorData = await response.json();
                if (response.status === 400 || response.status === 403) {
                    alert(errorData.message || "Nie masz uprawnień do zmiany podziału długu.");
                    return false;
                }
                throw new Error("Nie udało się zaktualizować długów.");
            }
    
            return true; 
        } catch (error) {
            console.error("Błąd podczas aktualizacji długów:", error);
            alert(error.message || "Nie udało się zaktualizować długów.");
            return false;
        }
    }
    

// Wyświetlanie wydatków
async function displayExpenses() {
    expenses = await fetchExpenses();
    if (!expenses || expenses.length === 0) {
        alert("Brak wydatków do wyświetlenia.");
        return;
    }

    expensesContainer.innerHTML = ""; 

    expenses.forEach((expense) => {
        const expenseDiv = document.createElement("div");
        expenseDiv.classList.add("expense");
    
        expenseDiv.innerHTML = `
            <h3>${expense.description}</h3>
            <p>Koszt: ${expense.cost} ${expense.currency}</p>
            <p>Data: ${new Date(expense.date).toLocaleDateString("pl-PL")}</p>
        `;
    
        // Tabela dłużników
        const debtTable = document.createElement("table");
        debtTable.classList.add("table", "table-bordered", "table-striped");
    
        const tableHeader = `
            <thead>
                <tr>
                    <th>Dłużnik</th>
                    <th>Kwota</th>
                </tr>
            </thead>
        `;
        const tableBody = document.createElement("tbody");
    
        expense.debts.forEach((debt) => {
            const debtRow = document.createElement("tr");
    
            debtRow.innerHTML = `
                <td>${debt.user.username}</td>
                <td>
                    <input type="number" class="form-control debt-amount" value="${debt.amount}" data-debt-id="${debt.debtId}" />
                </td>
            `;
    
            tableBody.appendChild(debtRow);
        });
    
        debtTable.innerHTML = tableHeader;
        debtTable.appendChild(tableBody);
        expenseDiv.appendChild(debtTable);
    
        // Przycisk "Zaktualizuj"
        const updateAllButton = document.createElement("button");
        updateAllButton.classList.add("btn", "btn-primary");
        updateAllButton.textContent = "Zaktualizuj";
    
        updateAllButton.addEventListener("click", async () => {
            // Obsługa aktualizacji
            const updates = [];
            let totalAssignedAmount = 0;
            const totalExpenseAmount = parseFloat(expense.cost);
    
            tableBody.querySelectorAll("input.debt-amount").forEach((input) => {
                const debtId = input.dataset.debtId;
                const newAmount = parseFloat(input.value);
    
                if (!isNaN(newAmount) && newAmount >= 0) {
                    updates.push({ debtId, newAmount });
                    totalAssignedAmount += newAmount;
                }
            });
    
            if (totalAssignedAmount !== totalExpenseAmount) {
                alert("Suma nie zgadza się z kwotą wydatku.");
                return;
            }
    
            const success = await updateDebts(expense.expenseId, updates);
            if (success) {
                alert("Zaktualizowano długi.");
                displayExpenses();
            }
        });
    
        expenseDiv.appendChild(updateAllButton);
    
        // Przycisk "Usuń"
        const deleteButton = document.createElement("button");
        deleteButton.classList.add("btn", "btn-danger");
        deleteButton.textContent = "Usuń";
    
        deleteButton.addEventListener("click", async () => {
            const confirmDelete = confirm(`Czy na pewno chcesz usunąć wydatek "${expense.description}"?`);
            if (confirmDelete) {
                try {
                    const response = await fetch(`http://localhost:7235/api/Expenses/${expense.expenseId}`, {
                        method: "DELETE",
                        headers: {
                            Authorization: `Bearer ${jwtToken}`,
                        },
                    });
    
                    if (!response.ok) {
                        const errorData = await response.json();
                        throw new Error(errorData.message || "Nie udało się usunąć wydatku.");
                    }
    
                    alert("Wydatek został usunięty.");
                    displayExpenses();
                } catch (error) {
                    console.error("Błąd podczas usuwania wydatku:", error);
                    alert(error.message || "Wystąpił błąd podczas usuwania wydatku.");
                }
            }
        });
    
        expenseDiv.appendChild(deleteButton);
    
        expensesContainer.appendChild(expenseDiv);
    });
    
}

displayExpenses();

});
