document.getElementById('forgot-password-form').addEventListener('submit', async (e) => {
    e.preventDefault();

    const email = document.getElementById('email').value;

    const response = await fetch('http://localhost:7235/api/Account/forgot-password', {
        method: 'POST',
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        },
        body: JSON.stringify({ email })
    });

    const messageElement = document.querySelector('.message');

    if (response.ok) {
        messageElement.textContent = "Wysłano link resetujący na podany adres e-mail.";
        messageElement.classList.remove('btn-danger');
        messageElement.classList.add('btn-success');
    } else {
        const error = await response.json();
        messageElement.textContent = error.message || "Wystąpił błąd.";
        messageElement.classList.remove('btn-success');
        messageElement.classList.add('btn-danger');
    }
});
