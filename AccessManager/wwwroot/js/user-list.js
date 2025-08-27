document.addEventListener('DOMContentLoaded', function () {
    const clearDepartmentBtn = document.getElementById('clearDepartmentBtn');
    const clearUnitBtn = document.getElementById('clearUnitBtn');

    if (clearDepartmentBtn) {
        clearDepartmentBtn.addEventListener('click', function () {
            const input = document.getElementById('filterDepartmentInput');
            const hidden = document.getElementById('filterDepartment');
            const resultsDiv = document.getElementById('filterDepartmentResults');
            const unitInput = document.getElementById('filterUnitInput');
            const unitHidden = document.getElementById('filterUnit');
            const unitResults = document.getElementById('filterUnitResults');

            input.value = '';
            hidden.value = '';
            resultsDiv.innerHTML = '';

            unitInput.value = '';
            unitHidden.value = '';
            unitResults.innerHTML = '';
            unitInput.disabled = true;

            filterForm.submit();
        });
    }

    if (clearUnitBtn) {
        clearUnitBtn.addEventListener('click', function () {
            const input = document.getElementById('filterUnitInput');
            const hidden = document.getElementById('filterUnit');
            const resultsDiv = document.getElementById('filterUnitResults');

            input.value = '';
            hidden.value = '';
            resultsDiv.innerHTML = '';

            filterForm.submit();
        });
    }
    const filterForm = document.getElementById('usersFilterForm');

    const sortSelect = document.getElementById('selectedSortOption');
    sortSelect.addEventListener('change', () => filterForm.submit());

    const departmentInput = document.getElementById('filterDepartmentInput');
    const departmentHidden = document.getElementById('filterDepartment');
    const departmentResults = document.getElementById('filterDepartmentResults');

    const unitInput = document.getElementById('filterUnitInput');
    const unitHidden = document.getElementById('filterUnit');
    const unitResults = document.getElementById('filterUnitResults');
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
            unitInput.disabled = false;
            unitInput.value = '';
            unitHidden.value = '';
            filterForm.submit();
        }
    );

    setupAutocomplete(unitInput, unitHidden, unitResults, '/Unit/SearchUnits',
        () => ({ departmentId: departmentHidden.value }),
        (id, text) => {
            filterForm.submit();
        }
    );

    departmentInput.addEventListener('input', function () {
        unitInput.value = '';
        unitHidden.value = '';
        unitInput.disabled = !departmentHidden.value;
        unitResults.innerHTML = '';
    });

    unitInput.disabled = !departmentHidden.value;
});
