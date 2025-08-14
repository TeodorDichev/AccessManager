document.addEventListener("DOMContentLoaded", function () {
    document.querySelectorAll(".remove-access-btn").forEach(btn => {
        btn.addEventListener("click", function () {
            let username = this.dataset.username;
            let unitId = this.dataset.unitid;

            fetch(`/Unit/RemoveUnitAccess?username=${encodeURIComponent(username)}&unitId=${encodeURIComponent(unitId)}`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
                },
                body: JSON.stringify({ username: username, unitId: unitId })
            })
                .then(response => response.json())
                .then(data => {
                    let messageDiv = document.getElementById("removeAccessMessage");
                    if (data.success) {
                        messageDiv.innerHTML = `<div class="alert alert-success">${data.message}</div>`;
                        this.closest("tr").remove();
                    } else {
                        messageDiv.innerHTML = `<div class="alert alert-danger">${data.message}</div>`;
                    }
                });
        });
    });
});
