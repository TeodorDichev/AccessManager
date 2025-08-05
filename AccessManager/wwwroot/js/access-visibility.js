const readSelect = document.getElementById("SelectedReadingAccess");
const writeSelect = document.getElementById("SelectedWritingAccess");
const passwordContainer = document.getElementById("password-container");
const unitAccessContainer = document.getElementById("unit-access-container");

function updateVisibility() {
    const readVal = parseInt(readSelect.value);
    const writeVal = parseInt(writeSelect.value);

    const showPassword = readVal === 0 && writeVal === 0;
    const showUnitAccess = readVal === 1 || writeVal === 1;

    if (passwordContainer !== null) {
        passwordContainer.style.display = showPassword ? "none" : "block";
    }
    if (unitAccessContainer !== null) { 
        unitAccessContainer.style.display = showUnitAccess ? "block" : "none";
    }
}

readSelect.addEventListener("change", updateVisibility);
writeSelect.addEventListener("change", updateVisibility);
updateVisibility();
