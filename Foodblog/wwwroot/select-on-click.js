// Selects the whole value of any field carrying `data-select-on-click` when it is clicked, so a
// snippet (e.g. the image library's [img] embed code) can be copied in one go. Lives in its own
// file because the Content-Security-Policy forbids inline event handlers.
(function () {
    "use strict";

    document.addEventListener("click", function (e) {
        var field = e.target.closest && e.target.closest("[data-select-on-click]");
        if (field && typeof field.select === "function") {
            field.select();
        }
    });
}());
