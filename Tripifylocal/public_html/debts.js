// Funkcja do odczytania ID użytkownika z tokena JWT
function getUserIdFromToken(token) {
    try {
        const payload = JSON.parse(atob(token.split('.')[1]));
        return parseInt(payload["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"]);
    } catch (e) {
        console.error("Nie udało się odczytać ID użytkownika z tokena JWT.", e);
        return null;
    }
}

// Funkcja do sprawdzania, czy istnieje oczekujące zgłoszenie płatności dla danego długu
async function checkPendingPaymentRequest(debtId, token) {
    try {
        const response = await fetch(`http://localhost:7235/api/Expenses/check-payment-request/${debtId}`, {
            headers: {
                "Authorization": `Bearer ${token}`,
                "Content-Type": "application/json",
            },
        });

        if (!response.ok) {
            throw new Error("Nie udało się sprawdzić oczekującego zgłoszenia płatności.");
        }

        const result = await response.json();
        return result.exists; // Zwraca true, jeśli istnieje oczekujące zgłoszenie
    } catch (error) {
        console.error(`Błąd podczas sprawdzania zgłoszenia płatności dla długu ${debtId}:`, error);
        return false;
    }
}

// Funkcja do zmiany statusu długu na "paid"
async function updateDebtStatus(debtId, token) {
    try {
        const response = await fetch(`http://localhost:7235/api/Expenses/updateDebtStatus/${debtId}`, {
            method: "PUT",
            headers: {
                "Authorization": `Bearer ${token}`,
                "Content-Type": "application/json",
            },
            body: JSON.stringify("paid"), // Wysyłamy status "paid"
        });

        if (!response.ok) {
            const errorData = await response.json().catch(() => null);
            throw new Error(errorData?.message || "Nie udało się zaktualizować statusu długu.");
        }

        return true;
    } catch (error) {
        console.error(`Błąd podczas zmiany statusu długu ${debtId}:`, error);
        return false;
    }
}

// Ładowanie danych o długach po załadowaniu DOM
document.addEventListener("DOMContentLoaded", async () => {
    const debtsYouOweContainer = document.getElementById("debts-you-owe");
    const debtsOwedToYouContainer = document.getElementById("debts-owed-to-you");

    try {
        // Pobierz token JWT z localStorage lub sessionStorage
        const token = localStorage.getItem("jwt_token") || sessionStorage.getItem("jwt_token");
        const userId = getUserIdFromToken(token);

        if (!token || !userId) {
            throw new Error("Brak tokenu uwierzytelniającego. Zaloguj się ponownie.");
        }

        // Wyślij żądanie do API, aby pobrać podsumowanie długów
        const response = await fetch("http://localhost:7235/api/Expenses/summary", {
            method: "GET",
            headers: {
                "Authorization": `Bearer ${token}`,
                "Content-Type": "application/json",
            },
        });

        if (!response.ok) {
            const errorData = await response.json().catch(() => null);
            throw new Error(errorData?.message || "Nie udało się załadować danych o długach.");
        }

        const data = await response.json();

        // Funkcja do tworzenia tabeli z długami
        const createTable = async (debts, isOwedByYou, container) => {
            debts = debts.filter(debt => debt.amount > 0.00);
            debts.sort((a, b) => {
                // Najpierw sortuj po statusie (opłacone na dole)
                if (a.status === "paid" && b.status !== "paid") return 1;
                if (a.status !== "paid" && b.status === "paid") return -1;
            
                // Następnie sortuj po dacie wydatku (rosnąco)
                return new Date(a.expenseDate) - new Date(b.expenseDate);
            });
            
            const tableContainer = document.createElement("div");
        
            const toggleButton = document.createElement("button");
            toggleButton.textContent = "Pokaż opłacone";
            toggleButton.classList.add("btn", "btn-primary", "mb-3");
            toggleButton.addEventListener("click", () => {
                const paidRows = table.querySelectorAll("tr.paid");
                paidRows.forEach(row => row.style.display = row.style.display === "none" ? "" : "none");
                toggleButton.textContent = toggleButton.textContent === "Pokaż opłacone" ? "Ukryj opłacone" : "Pokaż opłacone";
            });
        
            tableContainer.appendChild(toggleButton);
        
            const table = document.createElement("table");
            table.classList.add("table", "table-striped", "table-bordered");
        
            // Nagłówki tabeli
            const thead = document.createElement("thead");
            const headerRow = document.createElement("tr");
            headerRow.innerHTML = `
                <th>${isOwedByYou ? "Dłużnik" : "Wierzyciel"}</th>
                <th>Kwota</th>
                <th>Waluta</th>
                <th>Opis</th>
                <th>Wyjazd</th>
                <th>Data Wydatku</th> <!-- Dodaj nową kolumnę -->
                ${isOwedByYou ? "" : "<th>Metoda Płatności</th>"}
                <th>Akcje</th>
            `;
            thead.appendChild(headerRow);
            table.appendChild(thead);
        
            // Dane tabeli
            const tbody = document.createElement("tbody");
        
            for (const debt of debts) {
                const row = document.createElement("tr");
                if (debt.status === "paid") {
                    row.classList.add("paid");
                    row.style.display = "none"; 
                }
        
                row.innerHTML = `
                    <td>${debt.username || "Nieznany użytkownik"}</td>
                    <td>${debt.amount.toFixed(2)}</td>
                    <td>${debt.currency}</td>
                    <td>${debt.description}</td>
                    <td>${debt.tripName || "Brak wyjazdu"}</td>
                     <td>${new Date(debt.expenseDate).toLocaleDateString("pl-PL")}</td> <!-- Wyświetl datę wydatku -->
                    ${isOwedByYou ? "" : `<td><select class="payment-method">
                                              <option value="Cash">Gotówka</option>
                                              <option value="Blik">Blik</option>
                                              <option value="Revolut">Revolut</option>
                                          </select></td>`}
                `;
        
                const actionCell = document.createElement("td");
                // Obsługa akcji (zgłaszanie płatności, opłacone, itp.)
                const pendingRequest = await checkPendingPaymentRequest(debt.debtId, token);
        
                if (debt.status === "paid") {
                    const paidLabel = document.createElement("button");
                    paidLabel.textContent = "Opłacone";
                    paidLabel.classList.add("btn", "btn-secondary");
                    paidLabel.disabled = true; 
                    actionCell.appendChild(paidLabel);
                } else if (isOwedByYou) {
                    const forgiveButton = document.createElement("button");
                    forgiveButton.textContent = "Odpuść";
                    forgiveButton.classList.add("btn", "btn-danger");
                    forgiveButton.addEventListener("click", async () => {
                        const confirmed = confirm("Czy na pewno chcesz odpuścić ten dług?");
                        if (confirmed) {
                            const success = await updateDebtStatus(debt.debtId, token);
                            if (success) {
                                alert("Dług został odpuszczony.");
                                
                                row.classList.add("paid"); 
                    
                                forgiveButton.disabled = true;
                                forgiveButton.textContent = "Opłacone";
                                forgiveButton.classList.replace("btn-danger", "btn-secondary");
                            } else {
                                alert("Nie udało się odpuścić długu.");
                            }
                        }
                    });
                    actionCell.appendChild(forgiveButton);
                } else if (pendingRequest) {
                    const pendingLabel = document.createElement("button");
                    pendingLabel.textContent = "Oczekujące";
                    pendingLabel.classList.add("btn", "btn-warning");
                    pendingLabel.disabled = true; // Wyłączony przycisk
                    actionCell.appendChild(pendingLabel);
                } else {
                    const payButton = document.createElement("button");
                    payButton.textContent = "Zgłoś płatność";
                    payButton.classList.add("btn", "btn-success");
                    payButton.addEventListener("click", async () => {
                        try {
                            const paymentMethod = isOwedByYou ? "" : row.querySelector(".payment-method").value;
                            const payload = {
                                debtId: debt.debtId,
                                requestedById: userId,
                                requestedAt: new Date().toISOString(),
                                status: "Pending",
                                paymentMethod: paymentMethod,
                            };
        
                            const paymentResponse = await fetch("http://localhost:7235/api/Expenses/request-payment", {
                                method: "POST",
                                headers: {
                                    "Authorization": `Bearer ${token}`,
                                    "Content-Type": "application/json",
                                },
                                body: JSON.stringify(payload),
                            });
        
                            if (!paymentResponse.ok) {
                                throw new Error("Nie udało się zarejestrować płatności.");
                            }
        
                            alert("Płatność została zgłoszona pomyślnie.");
                            payButton.disabled = true;
                            payButton.textContent = "Oczekujące";
                            payButton.classList.replace("btn-success", "btn-warning");
                        } catch (error) {
                            console.error("Błąd podczas zgłaszania płatności:", error);
                            alert("Wystąpił błąd podczas zgłaszania płatności.");
                        }
                    });
        
                    actionCell.appendChild(payButton);
                }
        
                row.appendChild(actionCell);
                tbody.appendChild(row);
            }
        
            table.appendChild(tbody);
            tableContainer.appendChild(table);
            container.appendChild(tableContainer);
        };
        

        // Wyświetlanie "Twoje długi"
        if (data.debtsYouOwe && data.debtsYouOwe.length > 0) {
            debtsYouOweContainer.innerHTML = "";
            await createTable(data.debtsYouOwe, false, debtsYouOweContainer); // isOwedByYou = false
        }

        // Wyświetlanie "Twoi wierzyciele"
        if (data.debtsOwedToYou && data.debtsOwedToYou.length > 0) {
            debtsOwedToYouContainer.innerHTML = "";
            await createTable(data.debtsOwedToYou, true, debtsOwedToYouContainer); // isOwedByYou = true
        }
    } catch (error) {
        console.error("Błąd:", error);
    }
});
