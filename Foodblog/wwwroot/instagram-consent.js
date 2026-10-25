// Two-click Instagram embed. The server renders only a consent placeholder inside
// `data-instagram-embed`; Instagram's iframe is created here once the visitor clicks
// `data-instagram-load` (or earlier agreed and ticked `data-instagram-remember`, kept in
// localStorage — no cookie). "Ausblenden" withdraws that choice again. Lives in its own file
// because the Content-Security-Policy forbids inline scripts and event handlers.
(function () {
    "use strict";

    var storageKey = "instagram-consent";
    var allowedPrefix = "https://www.instagram.com/";

    function remembered() {
        try { return window.localStorage.getItem(storageKey) === "1"; } catch (e) { return false; }
    }

    function remember(value) {
        try {
            if (value) { window.localStorage.setItem(storageKey, "1"); }
            else { window.localStorage.removeItem(storageKey); }
        } catch (e) { /* storage blocked: consent simply is not remembered */ }
    }

    function show(container) {
        var src = container.getAttribute("data-instagram-embed") || "";
        if (src.indexOf(allowedPrefix) !== 0) { return; }

        var placeholder = container.querySelector(".instagram-consent");
        if (placeholder) { placeholder.hidden = true; }

        var frame = document.createElement("iframe");
        frame.src = src;
        frame.title = container.getAttribute("data-instagram-title") || "Instagram";
        frame.className = "instagram-frame";
        frame.loading = "lazy";
        frame.referrerPolicy = "strict-origin-when-cross-origin";
        frame.setAttribute("allowtransparency", "true");

        var hide = document.createElement("button");
        hide.type = "button";
        hide.className = "instagram-hide btn-secondary";
        hide.textContent = container.getAttribute("data-instagram-hide-label") || "Ausblenden";
        hide.addEventListener("click", function () {
            remember(false);
            frame.remove();
            hide.remove();
            if (placeholder) { placeholder.hidden = false; }
        });

        container.appendChild(frame);
        container.appendChild(hide);
    }

    document.addEventListener("click", function (e) {
        var button = e.target.closest && e.target.closest("[data-instagram-load]");
        if (!button) { return; }

        var container = button.closest("[data-instagram-embed]");
        if (!container) { return; }

        var checkbox = container.querySelector("[data-instagram-remember]");
        if (checkbox && checkbox.checked) { remember(true); }
        show(container);
    });

    document.addEventListener("DOMContentLoaded", function () {
        if (!remembered()) { return; }
        document.querySelectorAll("[data-instagram-embed]").forEach(show);
    });
}());
