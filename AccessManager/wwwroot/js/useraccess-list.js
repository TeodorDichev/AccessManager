document.addEventListener("DOMContentLoaded", function () {

    const accessSearch = document.getElementById("FilterAccessSearch");
    const userSearch = document.getElementById("FilterUserSearch");
    const directiveSearch = document.getElementById("FilterDirectiveSearch");
    const accessIdInput = document.getElementById("FilterAccessId");
    const directiveIdInput = document.getElementById("FilterDirectiveId");
    const userIdInput = document.getElementById("FilterUserId");
    const candidatesAccessBox = document.getElementById("filterAccessCandidates");
    const candidatesUserBox = document.getElementById("filterUserCandidates");
    const candidatesDirectiveBox = document.getElementById("filterDirectiveCandidates");
    const form = accessSearch.closest("form");
    let debounceTimer = null;
    const debounceMs = 300;

    const clearBtn = document.getElementById("clearFiltersBtn");
    if (clearBtn) {
        clearBtn.addEventListener("click", () => {
            accessSearch.value = "";
            userSearch.value = "";
            accessIdInput.value = "";
            userIdInput.value = "";
            directiveIdInput.value = "";
            directiveSearch.value = "";
            form.submit();
        });
    }
    function fetchUsers() {
        const q = userSearch.value || "";
        fetch(`/User/GetAccessibleUsers?q=${encodeURIComponent(q)}`)
            .then(r => r.json())
            .then(list => {
                candidatesUserBox.innerHTML = "";
                if (!Array.isArray(list) || list.length === 0) return;
                list.forEach(item => {
                    const el = document.createElement("button");
                    el.type = "button";
                    el.className = "list-group-item list-group-item-action";
                    el.textContent = item.text;
                    el.dataset.id = item.id;
                    el.addEventListener("click", () => {
                        userSearch.value = item.text;
                        userIdInput.value = item.id;
                        candidatesUserBox.innerHTML = "";
                        form.submit();
                    });
                    candidatesUserBox.appendChild(el);
                });
            })
            .catch(err => {
                console.error("Нямаше резултати:", err);
            });
    }

    function fetchDirectives() {
        const q = directiveSearch.value || "";
        fetch(`/Directive/GetDirectives?q=${encodeURIComponent(q)}`)
            .then(r => r.json())
            .then(list => {
                candidatesDirectiveBox.innerHTML = "";
                if (!Array.isArray(list) || list.length === 0) return;
                list.forEach(item => {
                    const el = document.createElement("button");
                    el.type = "button";
                    el.className = "list-group-item list-group-item-action";
                    el.textContent = item.text;
                    el.dataset.id = item.id;
                    el.addEventListener("click", () => {
                        directiveSearch.value = item.text;
                        directiveIdInput.value = item.id;
                        candidatesDirectiveBox.innerHTML = "";
                        form.submit();
                    });
                    candidatesDirectiveBox.appendChild(el);
                });
            })
            .catch(err => {
                console.error("Нямаше резултати:", err);
            });
    }

    function fetchAccesses() {
        const q = accessSearch.value || "";
        fetch(`/Access/GetAccesses?q=${encodeURIComponent(q)}`)
            .then(r => r.json())
            .then(list => {
                candidatesAccessBox.innerHTML = "";
                if (!Array.isArray(list) || list.length === 0) return;
                list.forEach(item => {
                    const el = document.createElement("button");
                    el.type = "button";
                    el.className = "list-group-item list-group-item-action";
                    el.textContent = item.text;
                    el.dataset.id = item.id;
                    el.addEventListener("click", () => {
                        accessSearch.value = item.text;
                        accessIdInput.value = item.id;
                        candidatesAccessBox.innerHTML = "";
                        form.submit(); // auto-submit on selection
                    });
                    candidatesAccessBox.appendChild(el);
                });
            })
            .catch(err => {
                console.error("Нямаше резултати:", err);
            });
    }

    accessSearch.addEventListener("input", function () {
        accessIdInput.value = "";
        if (debounceTimer) clearTimeout(debounceTimer);
        debounceTimer = setTimeout(fetchAccesses, debounceMs);
    });

    userSearch.addEventListener("input", function () {
        userIdInput.value = "";
        if (debounceTimer) clearTimeout(debounceTimer);
        debounceTimer = setTimeout(fetchUsers, debounceMs);
    });

    directiveSearch.addEventListener("input", function () {
        directiveIdInput.value = "";
        if (debounceTimer) clearTimeout(debounceTimer);
        debounceTimer = setTimeout(fetchDirectives, debounceMs);
    });

    document.addEventListener("click", function (e) {
        if (!candidatesAccessBox.contains(e.target) && e.target !== accessSearch) {
            candidatesAccessBox.innerHTML = "";
        }
    });

    document.addEventListener("click", function (e) {
        if (!candidatesUserBox.contains(e.target) && e.target !== userSearch) {
            candidatesUserBox.innerHTML = "";
        }
    });

    document.addEventListener("click", function (e) {
        if (!candidatesDirectiveBox.contains(e.target) && e.target !== directiveSearch) {
            candidatesDirectiveBox.innerHTML = "";
        }
    });
});