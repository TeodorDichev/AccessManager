document.addEventListener("DOMContentLoaded", function () {
    document.querySelectorAll(".directive-name").forEach(input => {
        input.addEventListener("change", function () {
            const row = this.closest("tr");
            const directiveId = row.dataset.id;
            const newName = this.value;

            fetch(`/Directive/UpdateDirectiveName`, {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                    "RequestVerificationToken": document.querySelector('input[name="__RequestVerificationToken"]').value
                },
                body: JSON.stringify({ id: directiveId, name: newName })
            })
                .then(response => response.json())
                .then(data => {
                    if (!data.success) {
                        alert("Грешка при запазването: " + data.message);
                    }
                })
                .catch(() => {
                    alert("Възникна грешка при свързването със сървъра.");
                });
        });
    });
});
