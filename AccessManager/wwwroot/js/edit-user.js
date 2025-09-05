function debounce(func, delay = 300) {
    let timer;
    return function (...args) {
        clearTimeout(timer);
        timer = setTimeout(() => func.apply(this, args), delay);
    };
}

document.addEventListener('DOMContentLoaded', function () {

    const departmentInput = document.getElementById('departmentInput');
    const departmentHidden = document.getElementById('SelectedDepartmentId');
    const departmentResults = document.getElementById('departmentResults');

    const positionInput = document.getElementById('positionInput');
    const positionHidden = document.getElementById('SelectedPositionId');
    const positionResults = document.getElementById('positionResults');

    const unitInput = document.getElementById('unitInput');
    const unitHidden = document.getElementById('SelectedUnitId');
    const unitResults = document.getElementById('unitResults');

    function updateUnitInputState() {
        if (departmentInput.disabled === true) {
            unitInput.disabled = true;
        }
        else if (!departmentInput.value.trim()) {
            unitInput.value = '';
            unitHidden.value = '';
            unitInput.disabled = true;
        }
        else {
            unitInput.disabled = false;
        }
    }

    updateUnitInputState();

    function setupAutocomplete(input, hidden, resultsDiv, url, extraParamsFn) {

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
                            hidden.text = item.text;
                            resultsDiv.innerHTML = '';

                            if (input === departmentInput) {
                                unitInput.disabled = false;
                            }
                        });
                        resultsDiv.appendChild(div);
                    });
                });
        };

        input.addEventListener('input', debounce(handleInput, 300));

        document.addEventListener('click', e => {
            if (!resultsDiv.contains(e.target) && e.target !== input) {
                resultsDiv.innerHTML = "";
            }
        });
    }

    setupAutocomplete(departmentInput, departmentHidden, departmentResults, '/Department/SearchDepartments', () => ({}));

    setupAutocomplete(positionInput, positionHidden, positionResults, '/Position/SearchPositions', () => ({}));

    setupAutocomplete(unitInput, unitHidden, unitResults, '/Unit/SearchDepartmentUnits', () => ({
        departmentId: departmentHidden.value
    }));

    departmentInput.addEventListener('input', function () {
        unitResults.innerHTML = '';
        updateUnitInputState();
    });
});
