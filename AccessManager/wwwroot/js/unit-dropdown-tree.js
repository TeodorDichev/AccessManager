const selectedUnitIdsInput = document.getElementById("SelectedAccessibleUnitIds");
let selectedUnitIds = selectedUnitIdsInput.value ? selectedUnitIdsInput.value.split(',') : [];

function updateHiddenInput() {
    selectedUnitIdsInput.value = selectedUnitIds.join(',');
}

function toggleUnit(unitId, checked) {
    if (checked) {
        if (!selectedUnitIds.includes(unitId)) selectedUnitIds.push(unitId);
    } else {
        selectedUnitIds = selectedUnitIds.filter(id => id !== unitId);
    }
    updateHiddenInput();
}

function toggleDepartment(deptCheckbox, unitCheckboxes) {
    unitCheckboxes.forEach(chk => {
        chk.checked = deptCheckbox.checked;
        toggleUnit(chk.value, chk.checked);
    });
}

fetch(`/User/GetAccessibleDepartmentsForUser`)
    .then(res => res.json())
    .then(data => {
        const dropdown = document.getElementById("units-dropdown");

        data.forEach(dept => {
            const deptId = dept.departmentId;
            const deptName = dept.departmentName;
            const units = dept.units;

            const deptItem = document.createElement("li");
            deptItem.innerHTML = `
                <div class="form-check mb-1">
                    <input class="form-check-input dept-checkbox" type="checkbox" id="dept-${deptId}" />
                    <label class="form-check-label fw-bold" for="dept-${deptId}">${deptName}</label>
                </div>
                <ul class="list-unstyled ms-3">
                    ${units.map(u => `
                        <li>
                            <div class="form-check">
                                <input class="form-check-input unit-checkbox" type="checkbox" id="unit-${u.unitId}" value="${u.unitId}" />
                                <label class="form-check-label" for="unit-${u.unitId}">${u.unitName}</label>
                            </div>
                        </li>`).join("")}
                </ul>
            `;
            dropdown.appendChild(deptItem);

            const deptCheckbox = deptItem.querySelector(".dept-checkbox");
            const unitCheckboxes = Array.from(deptItem.querySelectorAll(".unit-checkbox"));

            unitCheckboxes.forEach(chk => {
                if (selectedUnitIds.includes(chk.value)) {
                    chk.checked = true;
                }
            });

            deptCheckbox.checked = unitCheckboxes.length > 0 && unitCheckboxes.every(chk => chk.checked);

            unitCheckboxes.forEach(chk => {
                toggleUnit(chk.value, chk.checked);
            });

            deptCheckbox.addEventListener("change", () => {
                toggleDepartment(deptCheckbox, unitCheckboxes);
            });

            unitCheckboxes.forEach(chk => {
                chk.addEventListener("change", () => {
                    toggleUnit(chk.value, chk.checked);
                    deptCheckbox.checked = unitCheckboxes.every(c => c.checked);
                });
            });
        });

        updateHiddenInput();
    });

