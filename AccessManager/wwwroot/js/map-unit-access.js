document.addEventListener('DOMContentLoaded', function () {

    // ============================
    // Initialize selected IDs
    // ============================
    const selectedAccessibleIds = new Set(window.SelectedAccessibleUnitIds || []);
    const selectedInaccessibleIds = new Set(window.SelectedInaccessibleUnitIds || []);

    const trackCheckboxes = (selector, set) => {
        document.querySelectorAll(selector).forEach(cb => {
            cb.checked = set.has(cb.value); // restore checked state
            cb.addEventListener('change', () => {
                cb.checked ? set.add(cb.value) : set.delete(cb.value);
            });
        });
    };

    trackCheckboxes('input[name="SelectedAccessibleUnitIds"]', selectedAccessibleIds);
    trackCheckboxes('input[name="SelectedInaccessibleUnitIds"]', selectedInaccessibleIds);

    // ============================
    // Select All Buttons
    // ============================
    const toggleCheckboxes = (selector, set, checkAll) => {
        document.querySelectorAll(selector).forEach(cb => {
            cb.checked = checkAll;
            checkAll ? set.add(cb.value) : set.delete(cb.value);
        });
    };

    const selectAllAccessibleBtn = document.getElementById('selectAllAccessibleBtn');
    const selectAllAccessible = document.getElementById('selectAllAccessible');
    const selectAllInaccessibleBtn = document.getElementById('selectAllInaccessibleBtn');
    const selectAllInaccessible = document.getElementById('selectAllInaccessible');

    if (selectAllAccessibleBtn) {
        selectAllAccessibleBtn.addEventListener('click', () => toggleCheckboxes('input[name="SelectedAccessibleUnitIds"]', selectedAccessibleIds, true));
    }
    if (selectAllAccessible) {
        selectAllAccessible.addEventListener('change', function () {
            toggleCheckboxes('input[name="SelectedAccessibleUnitIds"]', selectedAccessibleIds, this.checked);
        });
    }

    if (selectAllInaccessibleBtn) {
        selectAllInaccessibleBtn.addEventListener('click', () => toggleCheckboxes('input[name="SelectedInaccessibleUnitIds"]', selectedInaccessibleIds, true));
    }
    if (selectAllInaccessible) {
        selectAllInaccessible.addEventListener('change', function () {
            toggleCheckboxes('input[name="SelectedInaccessibleUnitIds"]', selectedInaccessibleIds, this.checked);
        });
    }

    // ============================
    // Sync selected IDs to form before submit
    // ============================
    const form = document.getElementById('MapUserUnitForm');
    if (form) {
        form.addEventListener('submit', () => {

            // remove previous hidden inputs
            form.querySelectorAll('input[type="hidden"][name="SelectedAccessibleUnitIds"], input[type="hidden"][name="SelectedInaccessibleUnitIds"]').forEach(h => h.remove());

            selectedAccessibleIds.forEach(id => {
                const input = document.createElement('input');
                input.type = 'hidden';
                input.name = 'SelectedAccessibleUnitIds';
                input.value = id;
                form.appendChild(input);
            });

            selectedInaccessibleIds.forEach(id => {
                const input = document.createElement('input');
                input.type = 'hidden';
                input.name = 'SelectedInaccessibleUnitIds';
                input.value = id;
                form.appendChild(input);
            });
        });
    }

});
