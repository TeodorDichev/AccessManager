function softDeleteAccess(button) {
    const id = button.getAttribute("data-id");

    if (!confirm(`Сигурни ли сте, че искате да деактивирате достъпа?`)) return;

    fetch(`/Access/SoftDeleteAccess?id=${encodeURIComponent(id)}`, {
        method: "POST",
        headers: {
            "RequestVerificationToken": document.querySelector('input[name="__RequestVerificationToken"]')?.value || ""
        }
    })
        .then(response => {
            if (response.ok) {
                alert("Достъпът е деактивиран успешно.");
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
