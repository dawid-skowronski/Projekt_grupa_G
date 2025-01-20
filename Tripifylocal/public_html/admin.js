// Funkcja do dekodowania tokenu JWT
function parseJwt(token) {
    const base64Url = token.split('.')[1];
    const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
    const jsonPayload = decodeURIComponent(
        atob(base64)
            .split('')
            .map(c => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2))
            .join('')
    );
    return JSON.parse(jsonPayload);
}

// Funkcja do sprawdzania roli użytkownika
function checkUserRole() {
    const token = localStorage.getItem("jwt_token") || sessionStorage.getItem("jwt_token");

    if (!token) {
        alert("Brak tokenu. Zaloguj się ponownie.");
        window.location.href = "login";
        return;
    }

    const userData = parseJwt(token);

    if (userData["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] === "Admin") {
        // Jeśli użytkownik jest adminem, pokaż panel administratora
        document.querySelector(".admin-main").style.display = "block";
        document.querySelector("header").style.display = "none";
        document.querySelector("footer").style.display = "none"; 
    } else {
        // Jeśli nie jest adminem, przekieruj na stronę główną lub profil
        alert("Nie masz uprawnień do tej sekcji.");
        window.location.href = "index";
    }
}

// Funkcja do pokazywania/ukrywania sekcji
function toggleSection(sectionId) {
    const section = document.getElementById(sectionId);
    section.classList.toggle('hidden');
}

// Funkcja do ładowania listy użytkowników
async function loadUsers() {
    try {
        const response = await fetch('http://localhost:7235/api/Admin/users', {
            method: 'GET',
            headers: {
                Authorization: `Bearer ${localStorage.getItem('jwt_token') || sessionStorage.getItem('jwt_token')}`,
                'Content-Type': 'application/json',
            },
        });

        if (!response.ok) {
            throw new Error('Nie udało się załadować użytkowników.');
        }

        const users = await response.json();
        const usersList = document.getElementById('users-list');
        usersList.innerHTML = users.map(user => `
            <div class="user">
                <p><strong>ID:</strong> ${user.id}</p>
                <p><strong>Nazwa:</strong> ${user.username}</p>
                <p><strong>Email:</strong> ${user.email}</p>
                <p><strong>Rola:</strong> ${user.role}</p>
            </div>
        `).join('');
    } catch (error) {
        console.error(error);
        alert('Błąd: Nie udało się załadować użytkowników.');
    }
}

// Funkcja do zmiany roli użytkownika
async function updateUserRole(event) {
    event.preventDefault();
    const userId = document.getElementById('user-id').value;
    const newRole = document.getElementById('new-role').value;

    try {
        const response = await fetch(`http://localhost:7235/api/Admin/user/${userId}`, {
            method: 'PUT',
            headers: {
                Authorization: `Bearer ${localStorage.getItem('jwt_token') || sessionStorage.getItem('jwt_token')}`,
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(newRole),
        });

        if (!response.ok) {
            throw new Error('Nie udało się zmienić roli użytkownika.');
        }

        alert('Rola użytkownika została zmieniona.');
    } catch (error) {
        console.error(error);
        alert('Błąd: Nie udało się zmienić roli użytkownika.');
    }
}

// Funkcja do usuwania użytkownika
async function deleteUser(event) {
    event.preventDefault();
    const userId = document.getElementById('delete-user-id').value;

    try {
        const response = await fetch(`http://localhost:7235/api/Admin/user/${userId}`, {
            method: 'DELETE',
            headers: {
                Authorization: `Bearer ${localStorage.getItem('jwt_token') || sessionStorage.getItem('jwt_token')}`,
            },
        });

        if (!response.ok) {
            throw new Error('Nie udało się usunąć użytkownika.');
        }

        alert('Użytkownik został usunięty.');
    } catch (error) {
        console.error(error);
        alert('Błąd: Nie udało się usunąć użytkownika.');
    }
}

// Funkcja do edytowania wyjazdu
async function editTrip(event) {
    event.preventDefault();
    const tripId = document.getElementById('trip-id').value.trim();

    if (!tripId) {
        alert("Podaj ID wyjazdu.");
        return;
    }

    try {
        console.log(`🔍 Pobieranie danych wyjazdu o ID: ${tripId}...`);

        // Pobieramy dane wyjazdu
        const getResponse = await fetch(`http://localhost:7235/api/Admin/trip/${tripId}`, {
            method: 'GET',
            headers: {
                Authorization: `Bearer ${localStorage.getItem('jwt_token') || sessionStorage.getItem('jwt_token')}`,
                'Content-Type': 'application/json',
            },
        });

        console.log(`🔍 Status odpowiedzi: ${getResponse.status}`);

        if (!getResponse.ok) {
            throw new Error(`Błąd: API zwróciło status ${getResponse.status}.`);
        }

        const currentTrip = await getResponse.json();
        console.log("✅ Otrzymane dane wyjazdu:", currentTrip);

        // Pobieramy nowe wartości lub zostawiamy stare
        const tripName = document.getElementById('trip-name').value.trim() || currentTrip.name;
        const tripDescription = document.getElementById('trip-description').value.trim() || currentTrip.description;
        const startDate = document.getElementById('trip-start-date').value || currentTrip.startDate;
        const endDate = document.getElementById('trip-end-date').value || currentTrip.endDate;

        const updatedTrip = {
            tripId: parseInt(tripId),
            name: tripName,
            description: tripDescription,
            startDate: startDate,
            endDate: endDate
        };

        console.log("📤 Wysyłane dane do API:", JSON.stringify(updatedTrip));

        // Wysyłamy PUT request
        const updateResponse = await fetch(`http://localhost:7235/api/Admin/trip/${tripId}`, {
            method: 'PUT',
            headers: {
                Authorization: `Bearer ${localStorage.getItem('jwt_token') || sessionStorage.getItem('jwt_token')}`,
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(updatedTrip),
        });

        console.log(`🔍 Status odpowiedzi z PUT: ${updateResponse.status}`);

        if (!updateResponse.ok) {
            const errorText = await updateResponse.text();
            throw new Error(`Błąd API: ${errorText}`);
        }

        alert(`✅ Wyjazd został zaktualizowany.`);
    } catch (error) {
        console.error(error);
        alert(`❌ Błąd: ${error.message}`);
    }
}


// Funkcja do usuwania wyjazdu
async function deleteTrip(event) {
    event.preventDefault();
    const tripId = document.getElementById('delete-trip-id').value;

    try {
        const response = await fetch(`http://localhost:7235/api/Admin/trip/${tripId}`, {
            method: 'DELETE',
            headers: {
                Authorization: `Bearer ${localStorage.getItem('jwt_token') || sessionStorage.getItem('jwt_token')}`,
            },
        });

        if (!response.ok) {
            throw new Error('Nie udało się usunąć wyjazdu.');
        }

        alert('Wyjazd został usunięty.');
    } catch (error) {
        console.error(error);
        alert('Błąd: Nie udało się usunąć wyjazdu.');
    }
}

// Funkcja do ładowania wyjazdów użytkownika
async function loadUserTrips(event) {
    event.preventDefault();
    const userId = document.getElementById('user-trips-id').value;

    try {
        const response = await fetch(`http://localhost:7235/api/Admin/user/${userId}/trips`, {
            method: 'GET',
            headers: {
                Authorization: `Bearer ${localStorage.getItem('jwt_token') || sessionStorage.getItem('jwt_token')}`,
            },
        });

        if (!response.ok) {
            throw new Error('Nie udało się załadować wyjazdów użytkownika.');
        }

        const trips = await response.json();
        const tripsContainer = document.getElementById('user-trips');
        tripsContainer.innerHTML = trips.map(trip => `
            <div class="trip">
                <p><strong>ID Wyjazdu:</strong> ${trip.tripId}</p>
                <p><strong>Nazwa:</strong> ${trip.name}</p>
                <p><strong>Opis:</strong> ${trip.description}</p>
                <p><strong>Data rozpoczęcia:</strong> ${trip.startDate}</p>
                <p><strong>Data zakończenia:</strong> ${trip.endDate}</p>
            </div>
        `).join('');
    } catch (error) {
        console.error(error);
        alert('Błąd: Nie udało się załadować wyjazdów użytkownika.');
    }
}

// Sprawdź rolę użytkownika po załadowaniu strony
document.addEventListener("DOMContentLoaded", function () {
    checkUserRole();
});
