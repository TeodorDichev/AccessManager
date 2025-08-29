    const selectAllAccessibleBtn = document.getElementById('selectAllAccessibleBtn');
    if (selectAllAccessibleBtn) {
        selectAllAccessibleBtn.addEventListener('click', () => {
            document.querySelectorAll('input[name="selectedAccessibleUnitIds"]').forEach(cb => cb.checked = true);
        });
    }

    const selectAllAccessible = document.getElementById('selectAllAccessible');
    if (selectAllAccessible) {
        selectAllAccessible.addEventListener('change', function () {
            document.querySelectorAll('input[name="selectedAccessibleUnitIds"]').forEach(cb => cb.checked = this.checked);
        });
    }

    const selectAllInaccessibleBtn = document.getElementById('selectAllInaccessibleBtn');
    if (selectAllInaccessibleBtn) {
        selectAllInaccessibleBtn.addEventListener('click', () => {
            document.querySelectorAll('input[name="selectedInaccessibleUnitIds"]').forEach(cb => cb.checked = true);
        });
    }

    const selectAllInaccessible = document.getElementById('selectAllInaccessible');
if (selectAllInaccessible) {
    selectAllInaccessible.addEventListener('change', function () {
        document.querySelectorAll('input[name="selectedInaccessibleUnitIds"]').forEach(cb => cb.checked = this.checked);
    });
}
