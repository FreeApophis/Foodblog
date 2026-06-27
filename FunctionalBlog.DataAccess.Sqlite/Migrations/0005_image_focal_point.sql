-- Per-image crop focal point (fractions of width/height, top-left origin). Defaults to the centre
-- so every existing image keeps its current centre-crop until an editor retunes it in the library.

ALTER TABLE images ADD COLUMN focal_x REAL NOT NULL DEFAULT 0.5;
ALTER TABLE images ADD COLUMN focal_y REAL NOT NULL DEFAULT 0.5;
