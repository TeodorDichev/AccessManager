document.addEventListener('DOMContentLoaded', function () {
    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

    // ---- Directive Update ----
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

    // ---- Remove Unit ----
    document.querySelectorAll('.remove-unit-btn').forEach(btn => {
        btn.addEventListener('click', function () {
            const unitId = btn.dataset.unitId;
            const username = btn.dataset.username;

            if (!confirm('Сигурни ли сте, че искате да премахнете този отдел?')) return;

            const formData = new FormData();
            formData.append('username', username);
            formData.append('unitId', unitId);
            formData.append('__RequestVerificationToken', token);

            fetch('/UnitDepartment/RemoveUnitAccess', {
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

    // ---- Remove Access ----
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
