document.addEventListener('DOMContentLoaded', function () {

    // Select all buttons
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

    // Debounce helper
    function debounce(func, delay = 300) {
        let timer;
        return function (...args) {
            clearTimeout(timer);
            timer = setTimeout(() => func.apply(this, args), delay);
        };
    }

    // ✅ Single unified autocomplete
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

    // --- Filters ---
    const filterForm1 = document.getElementById('FilterForm1');
    const filterForm2 = document.getElementById('FilterForm2');

    const DirectiveInput1 = document.getElementById('filterDirectiveInput1');
    const DirectiveHidden1 = document.getElementById('filterDirective1');
    const DirectiveResults1 = document.getElementById('filterDirectiveResults1');

    const DirectiveInput2 = document.getElementById('filterDirectiveInput2');
    const DirectiveHidden2 = document.getElementById('filterDirective2');
    const DirectiveResults2 = document.getElementById('filterDirectiveResults2');

    setupAutocomplete(DirectiveInput1, DirectiveHidden1, DirectiveResults1, '/Directive/SearchDirectives',
        () => ({}), () => filterForm1.submit());

    setupAutocomplete(DirectiveInput2, DirectiveHidden2, DirectiveResults2, '/Directive/SearchDirectives',
        () => ({}), () => filterForm2.submit());

    // Clear filter buttons
    const clearDirectiveBtn1 = document.getElementById('clearDirectiveBtn1');
    if (clearDirectiveBtn1) {
        clearDirectiveBtn1.addEventListener('click', function () {
            DirectiveInput1.value = '';
            DirectiveHidden1.value = '';
            DirectiveResults1.innerHTML = '';
            filterForm1.submit();
        });
    }

    const clearDirectiveBtn2 = document.getElementById('clearDirectiveBtn2');
    if (clearDirectiveBtn2) {
        clearDirectiveBtn2.addEventListener('click', function () {
            DirectiveInput2.value = '';
            DirectiveHidden2.value = '';
            DirectiveResults2.innerHTML = '';
            filterForm2.submit();
        });
    }

    // --- Grant / Revoke ---
    const directiveToRevokeAccessInput = document.getElementById('directiveToRevokeAccessInput');
    const directiveToRevokeAccessHidden = document.getElementById('directiveToRevokeAccessId');
    const directiveToRevokeAccessResults = document.getElementById('directiveToRevokeAccessResults');

    const directiveToGrantAccessInput = document.getElementById('directiveToGrantAccessInput');
    const directiveToGrantAccessHidden = document.getElementById('directiveToGrantAccessId');
    const directiveToGrantAccessResults = document.getElementById('directiveToGrantAccessResults');

    setupAutocomplete(directiveToGrantAccessInput, directiveToGrantAccessHidden, directiveToGrantAccessResults,
        '/Directive/SearchDirectives', () => ({}));

    setupAutocomplete(directiveToRevokeAccessInput, directiveToRevokeAccessHidden, directiveToRevokeAccessResults,
        '/Directive/SearchDirectives', () => ({}));

    // --- Row-level directive search ---
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

                fetch(`/Access/UpdateUserDirective`, {
                    method: "POST",
                    headers: {
                        "Content-Type": "application/json",
                        "RequestVerificationToken": document.querySelector("input[name='__RequestVerificationToken']").value
                    },
                    body: JSON.stringify({ userId, accessId, directiveId: id })
                });
            }
        );
    });
});
