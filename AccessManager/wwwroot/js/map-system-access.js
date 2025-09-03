document.addEventListener('DOMContentLoaded', function () {

    const selectAllAccessibleBtn = document.getElementById('selectAllWithAccessBtn');
    if (selectAllAccessibleBtn) {
        selectAllAccessibleBtn.addEventListener('change', function () {
            document.querySelectorAll('input[name="SelectedUsersWithAccessIds"]').forEach(cb => cb.checked = this.checked);
        });
    }

    const selectAllInaccessibleBtn = document.getElementById('selectAllWithoutAccessBtn');
    if (selectAllInaccessibleBtn) {
        selectAllInaccessibleBtn.addEventListener('change', function () {
            document.querySelectorAll('input[name="SelectedUsersWithoutAccessIds"]').forEach(cb => cb.checked = this.checked);
        });
    }

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
                const query = new URLSearchParams({ term }).toString();
                const response = await fetch(`/Directive/SearchDirectives?${query}`);
                const items = await response.json();

                resultsDiv.innerHTML = '';
                items.forEach(item => {
                    const div = document.createElement('div');
                    div.textContent = item.text;
                    div.classList.add('list-group-item', 'list-group-item-action');

                    div.addEventListener('click', async () => {
                        input.value = item.text;
                        hidden.value = item.id;
                        resultsDiv.innerHTML = '';

                        const userId = input.dataset.userid;
                        const accessId = input.dataset.accessid;
                        const redirectTo = 'MapUserAccess';

                        await fetch(`/Access/UpdateUserDirective`, {
                            method: "POST",
                            headers: {
                                "Content-Type": "application/json",
                                "RequestVerificationToken": document.querySelector("input[name='__RequestVerificationToken']").value
                            },
                            body: JSON.stringify({ userId, accessId, directiveId: item.id, redirectTo })
                        });
                    });

                    resultsDiv.appendChild(div);
                });

            } catch (err) {
                console.error(err);
            }
        }, 300);

        input.addEventListener('input', handleInput);

        document.addEventListener('click', e => {
            if (!container.contains(e.target)) {
                resultsDiv.innerHTML = '';
            }
        });
    });
});
