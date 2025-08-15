const selectAllAccessibleBtn = document.getElementById('selectAllWithAccessBtn');
    if (selectAllAccessibleBtn) {
        selectAllAccessibleBtn.addEventListener('click', () => {
            document.querySelectorAll('input[name="SelectedUsersWithAccessIds"]').forEach(cb => cb.checked = true);
        });
    }

const selectAllAccessible = document.getElementById('selectAllWithAccessBtn');
    if (selectAllAccessible) {
        selectAllAccessible.addEventListener('change', function () {
            document.querySelectorAll('input[name="SelectedUsersWithAccessIds"]').forEach(cb => cb.checked = this.checked);
        });
    }

const selectAllInaccessibleBtn = document.getElementById('selectAllWithoutAccessBtn');
    if (selectAllInaccessibleBtn) {
        selectAllInaccessibleBtn.addEventListener('click', () => {
            document.querySelectorAll('input[name="SelectedUsersWithoutAccessIds"]').forEach(cb => cb.checked = true);
        });
    }

const selectAllInaccessible = document.getElementById('selectAllWithoutAccessBtn');
    if (selectAllInaccessible) {
        selectAllInaccessible.addEventListener('change', function () {
            document.querySelectorAll('input[name="SelectedUsersWithoutAccessIds"]').forEach(cb => cb.checked = this.checked);
        });
    }

document.querySelectorAll("select[name='directiveToRevokeAccessInner'], select[name='directiveToGrantAccessInner']")
    .forEach(select => {
        select.addEventListener("change", function () {
            const userId = this.dataset.userid;
            const accessId = this.dataset.accessid;
            const directiveId = this.value;

            fetch(`/Access/UpdateUserDirective`, {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                    "RequestVerificationToken": document.querySelector("input[name='__RequestVerificationToken']").value
                },
                body: JSON.stringify({ userId, accessId, directiveId })
            });
        });
    });