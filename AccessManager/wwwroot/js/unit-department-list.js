document.addEventListener('DOMContentLoaded', function () {
    const clearDepartmentBtn = document.getElementById('clearDepartmentBtn');

    if (clearDepartmentBtn) {
        clearDepartmentBtn.addEventListener('click', function () {
            const input = document.getElementById('filterDepartmentInput');
            const hidden = document.getElementById('filterDepartment');
            const resultsDiv = document.getElementById('filterDepartmentResults');

            input.value = '';
            hidden.value = '';
            resultsDiv.innerHTML = '';

            filterForm.submit();
        });
    }
    const filterForm = document.getElementById('FilterForm');

    const departmentInput = document.getElementById('filterDepartmentInput');
    const departmentHidden = document.getElementById('filterDepartment');
    const departmentResults = document.getElementById('filterDepartmentResults');
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
                            onSelected(item.id, item.text);
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

    setupAutocomplete(departmentInput, departmentHidden, departmentResults, '/Department/SearchDepartments',
        () => ({}),
        (id, text) => {
            filterForm.submit();
        }
    );
});
