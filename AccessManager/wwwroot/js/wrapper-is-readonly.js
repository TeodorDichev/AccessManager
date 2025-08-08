document.addEventListener("DOMContentLoaded", function () {
    document.querySelectorAll(".wrapper").forEach(wrapper => {
        const readonlyContainer = wrapper.closest("[data-readonly]");
        const isReadOnly = readonlyContainer && readonlyContainer.dataset.readonly === "true";
        console.log(isReadOnly);
        if (isReadOnly) {
            wrapper.querySelectorAll("input:not([type=hidden]), select, textarea, button").forEach(el => {
                el.disabled = true;
            });
        }
    });
});
