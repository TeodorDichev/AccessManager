document.addEventListener('DOMContentLoaded', function () {
    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

    document.querySelectorAll('.directive-input').forEach(input => {
        input.addEventListener('change', function () {
            const accessId = input.dataset.accessId;
            const username = input.dataset.username;
            const directive = input.value;

            fetch('/User/UpdateUserAccessDirective', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': token
                },
                body: JSON.stringify({ username, accessId, directive })
            }).then(response => {
                if (!response.ok) alert('Грешка при запис на директива.');
            });
        });
    });

    document.querySelectorAll('.remove-unit-btn').forEach(btn => {
        btn.addEventListener('click', function () {
            const unitId = btn.dataset.unitId;
            const username = btn.dataset.username;

            if (!confirm('Сигурни ли сте, че искате да премахнете този отдел?')) return;

            const formData = new FormData();
            formData.append('username', username);
            formData.append('unitId', unitId);
            formData.append('__RequestVerificationToken', token);

            fetch('/Unit/RemoveUnitAccess', {
                method: 'POST',
                body: formData
            }).then(response => {
                if (response.redirected) {
                    window.location.href = response.url;
                } else {
                    alert('Грешка при премахване на отдела.');
                }
            });
        });
    });

    document.querySelectorAll('.remove-access-btn').forEach(btn => {
        btn.addEventListener('click', function () {
            const accessId = btn.dataset.accessId;
            const username = btn.dataset.username;

            if (!confirm('Сигурни ли сте, че искате да премахнете този достъп?')) return;

            const formData = new FormData();
            formData.append('username', username);
            formData.append('accessId', accessId);
            formData.append('__RequestVerificationToken', token);

            fetch('/User/RemoveUserAccess', {
                method: 'POST',
                body: formData
            }).then(response => {
                if (response.redirected) {
                    window.location.href = response.url;
                } else {
                    alert('Грешка при премахване на достъпа.');
                }
            });
        });
    });
});

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

    const unitInput = document.getElementById('unitInput');
    const unitHidden = document.getElementById('SelectedUnitId');
    const unitResults = document.getElementById('unitResults');

    function updateUnitInputState() {
        if (!departmentInput.value.trim()) {
            unitInput.value = '';
            unitHidden.value = '';
            unitInput.disabled = true;
        } else {
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

    setupAutocomplete(unitInput, unitHidden, unitResults, '/Unit/SearchDepartmentUnits', () => ({
        departmentId: departmentHidden.value
    }));

    departmentInput.addEventListener('input', function () {
        unitResults.innerHTML = '';
        updateUnitInputState();
    });
});
