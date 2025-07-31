function syncReadingWithWriting() {
    const readSelect = document.getElementById("SelectedReadingAccess");
    const writeSelect = document.getElementById("SelectedWritingAccess");

    writeSelect.addEventListener("change", () => {
        const writeVal = parseInt(writeSelect.value);
        const readVal = parseInt(readSelect.value);

        if (writeVal > readVal) {
            readSelect.value = writeVal;
            if (typeof updateVisibility === 'function') updateVisibility();
        }
    });
}

syncReadingWithWriting();
