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

document.addEventListener('DOMContentLoaded', function () {
    const filterForm1 = document.getElementById('FilterForm1');
    const filterForm2 = document.getElementById('FilterForm2');

    const clearDepartmentBtn1 = document.getElementById('clearDepartmentBtn1');
    const clearDepartmentBtn2 = document.getElementById('clearDepartmentBtn2');

    if (clearDepartmentBtn1) {
        clearDepartmentBtn1.addEventListener('click', function () {
            const input = document.getElementById('filterDepartmentInput1');
            const hidden = document.getElementById('filterDepartment1');
            const resultsDiv = document.getElementById('filterDepartmentResults1');

            input.value = '';
            hidden.value = '';
            resultsDiv.innerHTML = '';

            filterForm1.submit();
        });
    }

    if (clearDepartmentBtn2) {
        clearDepartmentBtn2.addEventListener('click', function () {
            const input = document.getElementById('filterDepartmentInput2');
            const hidden = document.getElementById('filterDepartment2');
            const resultsDiv = document.getElementById('filterDepartmentResults2');

            input.value = '';
            hidden.value = '';
            resultsDiv.innerHTML = '';

            filterForm2.submit();
        });
    }

    const departmentInput1 = document.getElementById('filterDepartmentInput1');
    const departmentHidden1 = document.getElementById('filterDepartment1');
    const departmentResults1 = document.getElementById('filterDepartmentResults1');

    const departmentInput2 = document.getElementById('filterDepartmentInput2');
    const departmentHidden2 = document.getElementById('filterDepartment2');
    const departmentResults2 = document.getElementById('filterDepartmentResults2');

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

    setupAutocomplete(departmentInput1, departmentHidden1, departmentResults1, '/Department/SearchDepartments',
        () => ({}),
        (id, text) => {
            filterForm1.submit();
        }
    );

    setupAutocomplete(departmentInput2, departmentHidden2, departmentResults2, '/Department/SearchDepartments',
        () => ({}),
        (id, text) => {
            filterForm2.submit();
        }
    );
});
