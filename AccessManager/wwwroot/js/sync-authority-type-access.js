function syncReadingWithWriting() {
    const readSelect = document.getElementById("SelectedReadingAccess");
    const writeSelect = document.getElementById("SelectedWritingAccess");

    function enforceRule() {
        const writeVal = parseInt(writeSelect.value);
        const readVal = parseInt(readSelect.value);

        if (readVal < writeVal) {
            readSelect.value = writeVal;
            if (typeof updateVisibility === 'function') updateVisibility();
        }
    }

    writeSelect.addEventListener("change", enforceRule);
    readSelect.addEventListener("change", enforceRule);
}

syncReadingWithWriting();
