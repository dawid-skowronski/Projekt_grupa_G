const params = new URLSearchParams(window.location.search);
const token = params.get('token');

// Funkcja do pokazywania/ukrywania hasła
document.querySelectorAll('.toggle-password').forEach(button => {
    button.addEventListener('click', () => {
        const input = button.previousElementSibling;
        if (input.type === 'password') {
            input.type = 'text';
            button.textContent = '🙈';
        } else {
            input.type = 'password';
            button.textContent = '👁';
        }
    });
});

document.getElementById('reset-password-form').addEventListener('submit', async (e) => {
    e.preventDefault();

    const newPassword = document.getElementById('new-password').value;
    const confirmPassword = document.getElementById('confirm-password').value;

    if (newPassword !== confirmPassword) {
        document.querySelector('.message').textContent = "Hasła muszą być takie same.";
        document.querySelector('.message').classList.add('highlight');
        return;
    }

    const response = await fetch('http://localhost:7235/api/Account/reset-password', {
        method: 'POST',
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        },
        body: JSON.stringify({ token, newPassword, confirmPassword })
    });

    if (response.ok) {
        document.querySelector('.message').textContent = "Hasło zostało zresetowane pomyślnie!";
        document.querySelector('.message').classList.add('btn-success');
    } else {
        const error = await response.json();
        document.querySelector('.message').textContent = error.message || "Wystąpił błąd.";
        document.querySelector('.message').classList.add('btn-danger');
    }
});
