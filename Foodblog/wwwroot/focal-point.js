// Focal-point picker for the image library. Clicking anywhere on a [data-focal] image records the
// click position as fractions of the image's width/height into the form's hidden focal_x/focal_y
// fields, moves the marker dot, and submits — persisting the new crop centre for that image.
(function () {
    function clamp(value) {
        return value < 0 ? 0 : value > 1 ? 1 : value;
    }

    document.addEventListener('click', function (event) {
        var pick = event.target.closest('[data-focal]');
        if (!pick) {
            return;
        }

        var form = pick.closest('form');
        if (!form) {
            return;
        }

        var rect = pick.getBoundingClientRect();
        var x = clamp((event.clientX - rect.left) / rect.width);
        var y = clamp((event.clientY - rect.top) / rect.height);

        form.querySelector('input[name="focal_x"]').value = x.toFixed(4);
        form.querySelector('input[name="focal_y"]').value = y.toFixed(4);

        var dot = pick.querySelector('.focal-dot');
        if (dot) {
            dot.style.left = (x * 100).toFixed(2) + '%';
            dot.style.top = (y * 100).toFixed(2) + '%';
        }

        if (form.requestSubmit) {
            form.requestSubmit();
        } else {
            form.submit();
        }
    });
})();
