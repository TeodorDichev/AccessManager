document.addEventListener("DOMContentLoaded", function () {
    const selectAllAccessibleBtn = document.getElementById('selectAllAccessibleBtn');
    if (selectAllAccessibleBtn) {
        selectAllAccessibleBtn.addEventListener('click', () => {
            document.querySelectorAll('input[name="selectedAccessibleSystemIds"]').forEach(cb => cb.checked = true);
        });
    }

    const selectAllAccessible = document.getElementById('selectAllAccessible');
    if (selectAllAccessible) {
        selectAllAccessible.addEventListener('change', function () {
            document.querySelectorAll('input[name="selectedAccessibleSystemIds"]').forEach(cb => cb.checked = this.checked);
        });
    }

    const selectAllInaccessibleBtn = document.getElementById('selectAllInaccessibleBtn');
    if (selectAllInaccessibleBtn) {
        selectAllInaccessibleBtn.addEventListener('click', () => {
            document.querySelectorAll('input[name="selectedInaccessibleSystemIds"]').forEach(cb => cb.checked = true);
        });
    }

    const selectAllInaccessible = document.getElementById('selectAllInaccessible');
    if (selectAllInaccessible) {
        selectAllInaccessible.addEventListener('change', function () {
            document.querySelectorAll('input[name="selectedInaccessibleSystemIds"]').forEach(cb => cb.checked = this.checked);
        });
    }

    document.querySelectorAll('form').forEach(form => {
        form.addEventListener('submit', function () {
            const accessibleChecked = Array.from(form.querySelectorAll('input[name="selectedAccessibleSystemIds"]:checked'));
            const accessibleConcatenated = accessibleChecked.map(cb => cb.value).join(',');
            const hiddenAccessible = form.querySelector('#allSelectedAccessible');
            if (hiddenAccessible) hiddenAccessible.value = accessibleConcatenated;

            const inaccessibleChecked = Array.from(form.querySelectorAll('input[name="selectedInaccessibleSystemIds"]:checked'));
            const inaccessibleConcatenated = inaccessibleChecked.map(cb => cb.value).join(',');
            const hiddenInaccessible = form.querySelector('#allSelectedInaccessible');
            if (hiddenInaccessible) hiddenInaccessible.value = inaccessibleConcatenated;

            form.querySelectorAll('input[name="selectedAccessibleSystemIds"]').forEach(cb => cb.remove());
            form.querySelectorAll('input[name="selectedInaccessibleSystemIds"]').forEach(cb => cb.remove());
        });
    });
});

document.addEventListener("DOMContentLoaded", function () {
    document.querySelectorAll("select[name='directiveToGrantAccessInner']").forEach(select => {
        select.addEventListener("change", function () {
            let directiveValue = this.value;
            let accessId = this.dataset.accessid;
            let username = this.dataset.username;

            fetch("/Access/UpdateDirective", {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                },
                body: JSON.stringify({
                    username: username,
                    accessId: accessId,
                    directive: directiveValue
                })
            })
                .then(response => response.json())
                .then(data => {
                    let msgDiv = document.getElementById("directiveUpdateMessage");
                    if (!msgDiv) {
                        msgDiv = document.createElement("div");
                        msgDiv.id = "directiveUpdateMessage";
                        this.closest("table").before(msgDiv);
                    }
                    msgDiv.innerHTML = data.success
                        ? `<div class="alert alert-success">${data.message}</div>`
                        : `<div class="alert alert-danger">${data.message}</div>`;
                })
                .catch(err => {
                    console.error("Error updating directive:", err);
                });
        });
    });
});

document.addEventListener("DOMContentLoaded", function () {
    document.querySelectorAll("select[name='directiveToRevokeAccessInner']").forEach(select => {
        select.addEventListener("change", function () {
            let directiveValue = this.value;
            let accessId = this.dataset.accessid;
            let username = this.dataset.username;

            fetch("/Access/UpdateDirective", {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                },
                body: JSON.stringify({
                    username: username,
                    accessId: accessId,
                    directive: directiveValue
                })
            })
                .then(response => response.json())
                .then(data => {
                    let msgDiv = document.getElementById("directiveUpdateMessage");
                    if (!msgDiv) {
                        msgDiv = document.createElement("div");
                        msgDiv.id = "directiveUpdateMessage";
                        this.closest("table").before(msgDiv);
                    }
                    msgDiv.innerHTML = data.success
                        ? `<div class="alert alert-success">${data.message}</div>`
                        : `<div class="alert alert-danger">${data.message}</div>`;
                })
                .catch(err => {
                    console.error("Error updating directive:", err);
                });
        });
    });
});


