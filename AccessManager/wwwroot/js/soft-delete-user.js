function softDeleteUser(button) {
    const username = button.getAttribute("data-username");

    if (!confirm(`Сигурни ли сте, че искате да деактивирате ${username}?`)) return;

    fetch(`/User/SoftDeleteUser?username=${encodeURIComponent(username)}`, {
        method: "POST",
        headers: {
            "RequestVerificationToken": document.querySelector('input[name="__RequestVerificationToken"]')?.value || ""
        }
    })
        .then(response => {
            if (response.ok) {
                alert("Потребителят е деактивиран успешно.");
                location.reload();
            } else {
                alert("Възникна грешка при деактивиране.");
            }
        })
        .catch(error => {
            console.error("Error:", error);
            alert("Възникна грешка.");
        });
}
