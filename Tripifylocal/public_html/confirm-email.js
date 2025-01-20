const params = new URLSearchParams(window.location.search);
const token = params.get('token');

const messageElement = document.querySelector('.message');

if (token) {
    fetch(`http://localhost:7235/api/Account/confirm-email?token=${token}`, {
        method: 'GET',
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        }
    })
    .then(response => {
        if (response.ok) {
            messageElement.textContent = "Twoje konto zostało pomyślnie potwierdzone! Przekierowanie na stronę logowania...";
            messageElement.classList.add('btn-success');
            setTimeout(() => {
                window.location.href = 'login.html';
            }, 3000);
        } else {
            messageElement.textContent = "Nie udało się potwierdzić konta. Token może być nieprawidłowy lub wygasły.";
            messageElement.classList.add('btn-danger');
        }
    })
    .catch(error => {
        console.error("Wystąpił błąd podczas przetwarzania żądania:", error.message || error);
        messageElement.textContent = "Wystąpił błąd podczas przetwarzania żądania. Szczegóły błędu: " + error.message;
        messageElement.classList.add('btn-danger');
    });
} else {
    messageElement.textContent = "Brak tokenu w URL. Nie można potwierdzić konta.";
    messageElement.classList.add('btn-danger');
}
