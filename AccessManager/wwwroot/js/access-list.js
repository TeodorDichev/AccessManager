document.addEventListener("DOMContentLoaded", function () {
    const levelSelect = document.getElementById("LevelSelect");
    const parentSearch = document.getElementById("FilterSearch");
    const parentIdInput = document.getElementById("FilterAccessId");
    const candidatesBox = document.getElementById("filterCandidates");
    const form = levelSelect.closest("form");

    let debounceTimer = null;
    const debounceMs = 300;

    function setParentEnabled(enabled) {
        parentSearch.disabled = !enabled;
        if (!enabled) {
            parentSearch.value = "";
            parentIdInput.value = "";
            candidatesBox.innerHTML = "";
        }
    }

    function fetchCandidates() {
        const level = parseInt(levelSelect.value || "0", 10) - 1;
        if (isNaN(level) || level <= 0) {
            candidatesBox.innerHTML = "";
            return;
        }
        const q = parentSearch.value || "";
        fetch(`/Access/GetParentCandidates?level=${encodeURIComponent(level)}&q=${encodeURIComponent(q)}`)
            .then(r => r.json())
            .then(list => {
                candidatesBox.innerHTML = "";
                if (!Array.isArray(list) || list.length === 0) return;
                list.forEach(item => {
                    const el = document.createElement("button");
                    el.type = "button";
                    el.className = "list-group-item list-group-item-action";
                    el.textContent = item.text;
                    el.dataset.id = item.id;
                    el.addEventListener("click", () => {
                        parentSearch.value = item.text;
                        parentIdInput.value = item.id;
                        candidatesBox.innerHTML = "";
                        form.submit(); // auto-submit on selection
                    });
                    candidatesBox.appendChild(el);
                });
            })
            .catch(err => {
                console.error("Нямаше резултати:", err);
            });
    }

    // level changes
    levelSelect.addEventListener("change", function () {
        const level = parseInt(this.value || "0", 10) - 1;
        if (level <= 0) {
            setParentEnabled(false);
            parentIdInput.required = false;
        } else {
            setParentEnabled(true);
            parentIdInput.required = true;
            if (debounceTimer) clearTimeout(debounceTimer);
            debounceTimer = setTimeout(fetchCandidates, debounceMs);
        }
    });

    // typing in search
    parentSearch.addEventListener("input", function () {
        parentIdInput.value = "";
        const level = parseInt(levelSelect.value || "0", 10) - 1;
        if (level <= 0) return;
        if (debounceTimer) clearTimeout(debounceTimer);
        debounceTimer = setTimeout(fetchCandidates, debounceMs);
    });

    // hide suggestions on outside click
    document.addEventListener("click", function (e) {
        if (!candidatesBox.contains(e.target) && e.target !== parentSearch) {
            candidatesBox.innerHTML = "";
        }
    });

    // init
    (function init() {
        const level = parseInt(levelSelect.value || "0", 10) - 1;
        setParentEnabled(level > 0);
    })();
});
