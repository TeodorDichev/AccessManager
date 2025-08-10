document.addEventListener("DOMContentLoaded", function () {
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

    document.querySelectorAll('form').forEach(form => {
        form.addEventListener('submit', function () {
            const accessibleChecked = Array.from(form.querySelectorAll('input[name="selectedAccessibleUnitIds"]:checked'));
            const accessibleConcatenated = accessibleChecked.map(cb => cb.value).join(',');
            const hiddenAccessible = form.querySelector('#allSelectedAccessible');
            if (hiddenAccessible) hiddenAccessible.value = accessibleConcatenated;

            const inaccessibleChecked = Array.from(form.querySelectorAll('input[name="selectedInaccessibleUnitIds"]:checked'));
            const inaccessibleConcatenated = inaccessibleChecked.map(cb => cb.value).join(',');
            const hiddenInaccessible = form.querySelector('#allSelectedInaccessible');
            if (hiddenInaccessible) hiddenInaccessible.value = inaccessibleConcatenated;

            form.querySelectorAll('input[name="selectedAccessibleUnitIds"]').forEach(cb => cb.remove());
            form.querySelectorAll('input[name="selectedInaccessibleUnitIds"]').forEach(cb => cb.remove());
        });
    });
});
