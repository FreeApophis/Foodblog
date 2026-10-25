// Confirmation guard for destructive actions. Any element carrying a `data-confirm`
// attribute prompts the user before proceeding; cancelling aborts the action. This covers
// both transport styles used in the app:
//
//   - Native form posts (most delete buttons): intercept the form's submit event.
//   - htmx requests (e.g. the units row delete): intercept htmx's confirm event.
//
// One attribute, `data-confirm="<question>"`, drives both paths.
(function () {
    "use strict";

    document.addEventListener("submit", function (e) {
        var form = e.target;
        var question = form.getAttribute && form.getAttribute("data-confirm");
        if (question && !window.confirm(question)) {
            e.preventDefault();
        }
    });

    // htmx fires htmx:confirm before every request it issues. Preventing the default defers
    // the request until we explicitly re-issue it (with skip-confirmation, to avoid a loop).
    document.addEventListener("htmx:confirm", function (e) {
        var question = e.target.getAttribute("data-confirm");
        if (!question) {
            return;
        }
        e.preventDefault();
        if (window.confirm(question)) {
            e.detail.issueRequest(true);
        }
    });
}());
