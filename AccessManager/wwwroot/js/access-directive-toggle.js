document.querySelectorAll(".system-checkbox").forEach(sys => {
    sys.addEventListener("change", function () {
        const container = this.closest(".border");
        const inputs = container.querySelectorAll("input[type='checkbox']");
        const directive = container.querySelector(".system-directive");
        inputs.forEach(cb => cb.checked = this.checked);
        directive.style.display = this.checked ? "block" : "none";
    });
});

document.querySelectorAll(".access-checkbox").forEach(acc => {
    acc.addEventListener("change", function () {
        const wrapper = this.closest(".ms-4");
        const directive = wrapper.querySelector(".access-directive");
        const subs = wrapper.querySelectorAll(".subaccess-checkbox");
        const subDirectives = wrapper.querySelectorAll(".subaccess-directive");
        subs.forEach(cb => cb.checked = this.checked);
        subDirectives.forEach(txt => txt.style.display = "none");
        directive.style.display = this.checked ? "block" : "none";
    });
});

document.querySelectorAll(".subaccess-checkbox").forEach(sub => {
    sub.addEventListener("change", function () {
        const directive = this.closest(".ms-5").querySelector(".subaccess-directive");
        directive.style.display = this.checked ? "block" : "none";
    });
});

document.getElementById("selectAllSystemsBtn")?.addEventListener("click", function () {
    document.querySelectorAll(".system-checkbox").forEach(cb => {
        if (!cb.checked) cb.click();
    });
});
