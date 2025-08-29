document.addEventListener("DOMContentLoaded", function () {
    const filters = document.querySelectorAll(".filter-wrapper");

    filters.forEach(wrapper => {
        const input = document.getElementById(wrapper.dataset.inputId);
        const hiddenInput = document.getElementById(wrapper.dataset.hiddenId);
        const resultsContainer = document.getElementById(wrapper.dataset.resultsId);
        const clearBtn = wrapper.querySelector(".btn-outline-danger");

        const submitOnSelect = wrapper.dataset.submitOnSelect === "true";

        if (!input || !hiddenInput || !resultsContainer) return;

        input.addEventListener("input", async () => {
            const query = input.value.trim();
            if (!query) {
                resultsContainer.innerHTML = "";
                resultsContainer.style.display = "none";
                return;
            }

            try {
                const url = wrapper.dataset.url + "?term=" + encodeURIComponent(query);
                const response = await fetch(url);
                if (!response.ok) return;
                const items = await response.json();

                resultsContainer.innerHTML = items.map(d =>
                    `<button type="button" class="list-group-item list-group-item-action" 
                             data-id="${d.id}" data-name="${d.text}">
                        ${d.text}
                    </button>`
                ).join("");

                resultsContainer.querySelectorAll("button").forEach(btn => {
                    btn.addEventListener("click", () => {
                        hiddenInput.value = btn.dataset.id;
                        input.value = btn.dataset.name;
                        resultsContainer.innerHTML = "";
                        resultsContainer.style.display = "none";

                        if (submitOnSelect) {
                            const form = input.closest("form");
                            if (form) form.submit();
                        }
                    });
                });

                resultsContainer.style.display = items.length > 0 ? "block" : "none";
            } catch (err) {
                console.error(err);
            }
        });

        if (clearBtn) {
            clearBtn.addEventListener("click", () => {
                input.value = "";
                hiddenInput.value = "";
                resultsContainer.innerHTML = "";
                resultsContainer.style.display = "none";

                if (submitOnSelect) {
                    const form = input.closest("form");
                    if (form) form.submit();
                }
            });
        }

        document.addEventListener("click", e => {
            if (!wrapper.contains(e.target)) {
                resultsContainer.innerHTML = "";
                resultsContainer.style.display = "none";
            }
        });
    });
});
