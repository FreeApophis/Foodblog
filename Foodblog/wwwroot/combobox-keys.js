// Keyboard navigation for htmx-driven comboboxes: an input plus a dynamically swapped
// list of focusable choices. Used by both the nav quicksearch dropdown and the recipe
// ingredient picker.
//
//   - ArrowDown in the input focuses the first choice.
//   - ArrowDown / ArrowUp move between choices, wrapping back to the input at either edge.
//   - Escape returns focus to the input.
//
// Listening on document (event delegation) means freshly htmx-swapped choices work without
// re-binding. Each shape names its container, input, and choice selectors; the choices are
// already natively focusable (anchors for quicksearch, buttons for ingredients).
(function () {
    "use strict";

    var SHAPES = [
        { box: ".search-box", input: 'input[name="q"]', item: "a.quicksearch-item" },
        { box: ".ingredient-combobox", input: "input.ingredient-name-input", item: "button.ingredient-match" }
    ];

    function shapeFor(target) {
        if (!target.closest) {
            return null;
        }
        for (var i = 0; i < SHAPES.length; i++) {
            var box = target.closest(SHAPES[i].box);
            if (box) {
                return { box: box, shape: SHAPES[i] };
            }
        }
        return null;
    }

    document.addEventListener("keydown", function (e) {
        if (e.key !== "ArrowDown" && e.key !== "ArrowUp" && e.key !== "Escape") {
            return;
        }

        var match = shapeFor(e.target);
        if (!match) {
            return;
        }

        var box = match.box;
        var field = box.querySelector(match.shape.input);
        var list = Array.prototype.slice.call(box.querySelectorAll(match.shape.item));

        if (e.target === field) {
            if (e.key === "ArrowDown" && list.length > 0) {
                e.preventDefault();
                list[0].focus();
            } else if (e.key === "Escape") {
                field.blur();
            }
            return;
        }

        var idx = list.indexOf(e.target);
        if (idx === -1) {
            return;
        }

        if (e.key === "Escape") {
            field.focus();
            return;
        }

        e.preventDefault();
        var next = e.key === "ArrowDown" ? idx + 1 : idx - 1;
        if (next < 0 || next >= list.length) {
            field.focus();
        } else {
            list[next].focus();
        }
    });
}());
