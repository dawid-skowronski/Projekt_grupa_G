document.addEventListener("DOMContentLoaded", function () {
    const form = document.getElementById("error-form");

    if (!form) {
        console.error("❌ Formularz nie został znaleziony.");
        return;
    }

    form.addEventListener("submit", async function (event) {
        event.preventDefault();

        const email = document.getElementById("user-email").value.trim();
        const subject = document.getElementById("error-subject").value.trim();
        const description = document.getElementById("error-description").value.trim();

        if (!email || !subject || !description) {
            alert("Wypełnij wszystkie pola!");
            return;
        }

        try {
            const response = await fetch("http://localhost:7235/api/ErrorReport/error-report", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ email, subject, description })
            });

            if (!response.ok) {
                throw new Error("Błąd podczas wysyłania zgłoszenia.");
            }

            const data = await response.json();
            alert(data.message);
        } catch (error) {
            console.error("❌ Błąd:", error);
            alert("Wystąpił błąd. Spróbuj ponownie.");
        }
    });
});
