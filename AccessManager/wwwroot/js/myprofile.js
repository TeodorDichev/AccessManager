function loadUnits(page) {
    $.get("/User/GetAccessibleUnits", { page: page }, function (data) {
        $("#accessibleUnitsContainer").html(data);
    });
}

function loadAccesses(page) {
    $.get("/User/GetUserAccesses", { page: page }, function (data) {
        $("#userAccessesContainer").html(data);
    });
}

$(function () {
    // Initial load
    loadUnits(1);
    loadAccesses(1);

    // Handle pagination clicks (event delegation)
    $(document).on("click", "#accessibleUnitsContainer .pagination a", function (e) {
        e.preventDefault();
        let page = $(this).data("page");
        if (page) loadUnits(page);
    });

    $(document).on("click", "#userAccessesContainer .pagination a", function (e) {
        e.preventDefault();
        let page = $(this).data("page");
        if (page) loadAccesses(page);
    });
});
