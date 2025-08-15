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
