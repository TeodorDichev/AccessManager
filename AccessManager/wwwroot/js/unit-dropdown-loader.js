document.getElementById("SelectedDepartmentId").addEventListener("change", function () {
    const departmentId = this.value;
    const unitSelect = document.getElementById("SelectedUnitId");

    if (!departmentId) {
        unitSelect.innerHTML = "<option value=''>-- Изберете Дирекция --</option>";
        return;
    }

    fetch(`/UnitDepartment/GetUnitsForDepartment?departmentId=${departmentId}`)
        .then(response => response.json())
        .then(data => {
            unitSelect.innerHTML = "";
            const defaultOption = document.createElement("option");
            defaultOption.value = "";
            defaultOption.text = "-- Изберете --";
            unitSelect.appendChild(defaultOption);

            data.forEach(unit => {
                const option = document.createElement("option");
                option.value = unit.id;
                option.text = unit.description;
                unitSelect.appendChild(option);
            });
        });
});
