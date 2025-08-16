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

document.querySelectorAll("select[name='directiveToRevokeAccessInner'], select[name='directiveToGrantAccessInner']")
    .forEach(select => {
        select.addEventListener("change", function () {
            const username = this.dataset.username;
            const accessId = this.dataset.accessid;
            const directiveId = this.value;

            fetch(`/Access/UpdateUserDirective`, {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                    "RequestVerificationToken": document.querySelector("input[name='__RequestVerificationToken']").value
                },
                body: JSON.stringify({ username, accessId, directiveId })
            });
        });
    });
