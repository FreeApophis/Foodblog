// Honour the `autofocus` attribute on content that htmx swaps in. Browsers only act on
// `autofocus` during the initial page parse, not for nodes inserted via AJAX — so when a
// handler marks a freshly added field (e.g. a new recipe ingredient or step row) with
// `autofocus`, we move the cursor there ourselves once the swap settles.
//
// The attribute is removed after focusing so a later swap of a different section never
// re-targets a stale field.
(function () {
    "use strict";

    document.addEventListener("htmx:afterSwap", function () {
        var el = document.querySelector("[autofocus]");
        if (!el) {
            return;
        }

        el.removeAttribute("autofocus");
        el.focus();

        // Place the caret at the end of any pre-filled text field. Number inputs reject
        // setSelectionRange, so guard against it.
        if (el.value && typeof el.setSelectionRange === "function") {
            try {
                el.setSelectionRange(el.value.length, el.value.length);
            } catch (err) {
                /* unsupported input type — focus alone is enough */
            }
        }
    });
}());
