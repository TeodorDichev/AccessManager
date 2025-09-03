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

    function setupAutocomplete(input, hidden, resultsDiv, url, extraParamsFn, onSelected) {
        const handleInput = () => {
            const term = input.value.trim();
            hidden.value = '';
            resultsDiv.innerHTML = '';

            if (!term) return;

            const params = { term, ...extraParamsFn() };
            const query = new URLSearchParams(params).toString();

            fetch(`${url}?${query}`)
                .then(res => res.json())
                .then(data => {
                    resultsDiv.innerHTML = '';
                    data.forEach(item => {
                        const div = document.createElement('div');
                        div.textContent = item.text;
                        div.classList.add('list-group-item', 'list-group-item-action');
                        div.addEventListener('click', function () {
                            input.value = item.text;
                            hidden.value = item.id;
                            resultsDiv.innerHTML = '';
                            if (onSelected) onSelected(item.id, item.text);
                        });
                        resultsDiv.appendChild(div);
                    });
                });
        };

        input.addEventListener('input', debounce(handleInput, 300));

        document.addEventListener('click', e => {
            if (!resultsDiv.contains(e.target) && e.target !== input) {
                resultsDiv.innerHTML = '';
            }
        });
    }

    document.querySelectorAll(".directive-search").forEach(input => {
        const hidden = input.parentElement.querySelector(".directive-hidden");
        const resultsDiv = input.parentElement.querySelector(".directive-results");

        setupAutocomplete(
            input,
            hidden,
            resultsDiv,
            '/Directive/SearchDirectives',
            () => ({}),
            (id) => {
                const userId = input.dataset.userid;
                const accessId = input.dataset.accessid;
                const redirectTo = 'EditAccess';
                fetch(`/Access/UpdateUserDirective`, {
                    method: "POST",
                    headers: {
                        "Content-Type": "application/json",
                        "RequestVerificationToken": document.querySelector("input[name='__RequestVerificationToken']").value
                    },
                    body: JSON.stringify({ userId, accessId, directiveId: id, redirectTo })
                });
            }
        );
    });
});
