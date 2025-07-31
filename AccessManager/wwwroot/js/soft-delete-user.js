document.addEventListener("DOMContentLoaded", function () {
    document.querySelectorAll(".soft-delete-btn").forEach(button => {
        button.addEventListener("click", function () {
            if (!confirm("Сигурни ли сте?")) {
                return;
            }

            const username = this.getAttribute("data-username");
            fetch(`/User/SoftDeleteUser?username=${encodeURIComponent(username)}`, {
                method: "POST",
                headers: {
                    "RequestVerificationToken": getAntiForgeryToken()
                }
            })
                .then(response => {
                    if (response.ok) {
                        // Optionally reload the page or remove the user row from UI
                        window.location.reload();
                    } else {
                        alert("Грешка при деактивиране.");
                    }
                })
                .catch(() => alert("Грешка при деактивиране."));
        });
    });
});

function getAntiForgeryToken() {
    const tokenElement = document.querySelector('input[name="__RequestVerificationToken"]');
    return tokenElement ? tokenElement.value : "";
}
