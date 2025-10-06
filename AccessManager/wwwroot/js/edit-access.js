document.addEventListener('DOMContentLoaded', function () {

    // ============================
    // Initialize selected IDs from server model
    // ============================
    const selectedAccessibleIds = new Set(window.SelectedUsersWithAccessIds || []);
    const selectedInaccessibleIds = new Set(window.SelectedUsersWithoutAccessIds || []);

    // Track checkboxes changes
    const trackCheckboxes = (selector, selectedSet) => {
        document.querySelectorAll(selector).forEach(cb => {
            cb.addEventListener('change', () => {
                cb.checked ? selectedSet.add(cb.value) : selectedSet.delete(cb.value);
            });
        });
    };

    trackCheckboxes('input[name="SelectedUsersWithAccessIds"]', selectedAccessibleIds);
    trackCheckboxes('input[name="SelectedUsersWithoutAccessIds"]', selectedInaccessibleIds);

    // ============================
    // Select All buttons
    // ============================
    const toggleCheckboxes = (selector, selectedSet) => {
        const checkboxes = document.querySelectorAll(selector);
        const allChecked = Array.from(checkboxes).every(cb => cb.checked);
        checkboxes.forEach(cb => {
            cb.checked = !allChecked;
            !allChecked ? selectedSet.add(cb.value) : selectedSet.delete(cb.value);
        });
    };

    const selectAllAccessibleBtn = document.getElementById('selectAllAccessibleBtn');
    if (selectAllAccessibleBtn) {
        selectAllAccessibleBtn.addEventListener('click', () =>
            toggleCheckboxes('input[name="SelectedUsersWithAccessIds"]', selectedAccessibleIds)
        );
    }

    const selectAllInaccessibleBtn = document.getElementById('selectAllInaccessibleBtn');
    if (selectAllInaccessibleBtn) {
        selectAllInaccessibleBtn.addEventListener('click', () =>
            toggleCheckboxes('input[name="SelectedUsersWithoutAccessIds"]', selectedInaccessibleIds)
        );
    }

    // ============================
    // Sync hidden inputs before submit
    // ============================
    const syncHiddenInputs = form => {
        ['SelectedUsersWithAccessIds', 'SelectedUsersWithoutAccessIds'].forEach(name => {
            form.querySelectorAll(`input[name="${name}"]`).forEach(i => i.remove());
        });

        selectedAccessibleIds.forEach(id => {
            const input = document.createElement('input');
            input.type = 'hidden';
            input.name = 'SelectedUsersWithAccessIds';
            input.value = id;
            form.appendChild(input);
        });

        selectedInaccessibleIds.forEach(id => {
            const input = document.createElement('input');
            input.type = 'hidden';
            input.name = 'SelectedUsersWithoutAccessIds';
            input.value = id;
            form.appendChild(input);
        });

        // Sync directive hidden inputs
        document.querySelectorAll('.directive-hidden').forEach(hidden => {
            if (!form.contains(hidden)) {
                form.appendChild(hidden.cloneNode(true));
            }
        });
    };

    document.querySelectorAll('form').forEach(form => {
        form.addEventListener('submit', () => syncHiddenInputs(form));
    });

    // ============================
    // Directive search autocomplete
    // ============================
    function debounce(func, delay = 300) {
        let timer;
        return function (...args) {
            clearTimeout(timer);
            timer = setTimeout(() => func.apply(this, args), delay);
        };
    }

    document.querySelectorAll('.directive-search').forEach(input => {
        const container = input.closest('.position-relative');
        const hidden = container.querySelector('.directive-hidden');
        const resultsDiv = container.querySelector('.directive-results');

        const handleInput = debounce(async () => {
            const term = input.value.trim();
            hidden.value = '';
            resultsDiv.innerHTML = '';
            if (!term) return;

            try {
                const url = `/Directive/SearchDirectives?term=${encodeURIComponent(term)}`;
                const response = await fetch(url);
                if (!response.ok) return;
                const items = await response.json();

                resultsDiv.innerHTML = items.map(item =>
                    `<button type="button" class="list-group-item list-group-item-action" 
                             data-id="${item.id}" data-name="${item.text}">
                        ${item.text}
                    </button>`).join('');

                resultsDiv.querySelectorAll('button').forEach(btn => {
                    btn.addEventListener('click', async () => {
                        input.value = btn.dataset.name;
                        hidden.value = btn.dataset.id;
                        resultsDiv.innerHTML = '';

                        const userId = input.dataset.userid;
                        const accessId = input.dataset.accessid;

                        await fetch(`/Access/UpdateUserDirective`, {
                            method: "POST",
                            headers: {
                                "Content-Type": "application/json",
                                "RequestVerificationToken": document.querySelector("input[name='__RequestVerificationToken']").value
                            },
                            body: JSON.stringify({ userId, accessId, directiveId: btn.dataset.id, redirectTo: "MapUserAccess" })
                        });
                    });
                });

            } catch (err) {
                console.error(err);
            }
        }, 300);

        input.addEventListener('input', handleInput);

        document.addEventListener('click', e => {
            if (!container.contains(e.target)) resultsDiv.innerHTML = '';
        });
    });

});
