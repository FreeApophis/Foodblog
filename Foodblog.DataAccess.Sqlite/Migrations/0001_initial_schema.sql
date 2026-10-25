-- 0001_initial_schema.sql
-- Complete database schema for the foodblog. Squashed from the original 0001 plus every
-- later schema-changing migration (images, pages, units, recipe times, normalized tags,
-- user avatars, the central slug registry) before release. Pure DDL — content lives in
-- 0002_seed.sql, and translations/roles/slugs are seeded in application code at startup.

-- Identity & authorization ---------------------------------------------------------------

CREATE TABLE roles (
    id   INTEGER PRIMARY KEY,
    name TEXT    NOT NULL UNIQUE
);

CREATE TABLE permission_rules (
    role_id      INTEGER NOT NULL REFERENCES roles(id) ON DELETE CASCADE,
    action_name  TEXT    NOT NULL,
    resource_key TEXT    NOT NULL,
    PRIMARY KEY (role_id, action_name, resource_key)
);

CREATE TABLE images (
    id           INTEGER PRIMARY KEY,
    file_name    TEXT    NOT NULL,
    content_type TEXT    NOT NULL,
    data         BLOB    NOT NULL,
    byte_size    INTEGER NOT NULL,
    uploaded_by  INTEGER NOT NULL,
    created_at   TEXT    NOT NULL
);

CREATE TABLE users (
    id              INTEGER PRIMARY KEY,
    email           TEXT    NOT NULL UNIQUE,
    display_name    TEXT    NOT NULL,
    password_hash   TEXT    NOT NULL,
    created_at      TEXT    NOT NULL,
    avatar_image_id INTEGER NULL REFERENCES images(id)
);

CREATE TABLE user_roles (
    user_id   INTEGER NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    role_name TEXT    NOT NULL,
    PRIMARY KEY (user_id, role_name)
);

CREATE TABLE sessions (
    token      TEXT    PRIMARY KEY,
    user_id    INTEGER NOT NULL,
    expires_at TEXT    NOT NULL
);

CREATE TABLE password_reset_tokens (
    token      TEXT    PRIMARY KEY,
    user_id    INTEGER NOT NULL,
    expires_at TEXT    NOT NULL,
    consumed   INTEGER NOT NULL DEFAULT 0
);

-- Tags (polymorphic, integer-keyed) ------------------------------------------------------

CREATE TABLE taggables (
    id INTEGER PRIMARY KEY
);

CREATE TABLE tags (
    id   INTEGER PRIMARY KEY,
    name TEXT    NOT NULL
);

CREATE INDEX ix_tags_name ON tags (name);

CREATE TABLE taggings (
    taggable_id INTEGER NOT NULL REFERENCES taggables(id) ON DELETE CASCADE,
    tag_id      INTEGER NOT NULL REFERENCES tags(id)      ON DELETE CASCADE,
    PRIMARY KEY (taggable_id, tag_id)
);

-- Blog articles --------------------------------------------------------------------------

CREATE TABLE articles (
    id             INTEGER PRIMARY KEY,
    title          TEXT    NOT NULL,
    teaser         TEXT    NOT NULL,
    text           TEXT    NOT NULL,
    author_id      INTEGER NOT NULL,
    published_at   TEXT    NOT NULL,
    cover_image_id INTEGER NULL,
    taggable_id    INTEGER NOT NULL UNIQUE REFERENCES taggables(id)
);

-- Recipes & ingredients ------------------------------------------------------------------

CREATE TABLE ingredients (
    id              INTEGER PRIMARY KEY,
    name            TEXT NOT NULL,
    image           TEXT NOT NULL,
    description     TEXT NOT NULL,
    density         REAL NOT NULL,
    piece_count     REAL NOT NULL,
    calorific_value REAL NOT NULL,
    protein         REAL NOT NULL,
    fat             REAL NOT NULL,
    carbohydrates   REAL NOT NULL,
    sugar           REAL NOT NULL,
    fiber           REAL NOT NULL
);

-- Units are a database-driven, editable catalog.
-- category: 0 = Weight, 1 = Volume, 2 = Piece. unit_factor is relative to the category base
-- (kg = 1, l = 1, piece = 1). name_key / abbreviation_key reference the translations table
-- (convention unit.<id>.name / .abbr), seeded by TranslationSeeder at startup.
CREATE TABLE units (
    id               INTEGER PRIMARY KEY,
    category         INTEGER NOT NULL,
    unit_factor      REAL    NOT NULL,
    name_key         TEXT    NOT NULL,
    abbreviation_key TEXT    NOT NULL
);

CREATE TABLE recipes (
    id               INTEGER PRIMARY KEY,
    name             TEXT    NOT NULL,
    description      TEXT    NOT NULL,
    author_id        INTEGER NOT NULL,
    difficulty       INTEGER NOT NULL,
    portions         INTEGER NOT NULL,
    preparation_time INTEGER NOT NULL DEFAULT 0,
    cooking_time     INTEGER NOT NULL DEFAULT 0,
    calorific_value  INTEGER NOT NULL DEFAULT 0,
    taggable_id      INTEGER NOT NULL UNIQUE REFERENCES taggables(id)
);

CREATE TABLE recipe_steps (
    recipe_id  INTEGER NOT NULL REFERENCES recipes(id) ON DELETE CASCADE,
    sort_order INTEGER NOT NULL,
    text       TEXT    NOT NULL,
    PRIMARY KEY (recipe_id, sort_order)
);

CREATE TABLE recipe_ingredients (
    recipe_id     INTEGER NOT NULL REFERENCES recipes(id) ON DELETE CASCADE,
    sort_order    INTEGER NOT NULL,
    ingredient_id INTEGER NOT NULL,
    amount        REAL    NOT NULL,
    unit_id       INTEGER NOT NULL REFERENCES units(id),
    PRIMARY KEY (recipe_id, sort_order)
);

CREATE TABLE recipe_images (
    recipe_id  INTEGER NOT NULL REFERENCES recipes(id) ON DELETE CASCADE,
    sort_order INTEGER NOT NULL,
    url        TEXT    NOT NULL,
    PRIMARY KEY (recipe_id, sort_order)
);

CREATE TABLE recipe_hints (
    recipe_id  INTEGER NOT NULL REFERENCES recipes(id) ON DELETE CASCADE,
    sort_order INTEGER NOT NULL,
    text       TEXT    NOT NULL,
    PRIMARY KEY (recipe_id, sort_order)
);

-- Static pages ---------------------------------------------------------------------------

CREATE TABLE pages (
    id      INTEGER PRIMARY KEY,
    title   TEXT    NOT NULL,
    content TEXT    NOT NULL
);

-- Translations ---------------------------------------------------------------------------

CREATE TABLE translations (
    key      TEXT NOT NULL,
    language TEXT NOT NULL,
    variant  TEXT NOT NULL DEFAULT '',
    text     TEXT NOT NULL,
    PRIMARY KEY (key, language, variant)
);

-- Central slug registry ------------------------------------------------------------------
-- Maps a globally-unique URL slug to the entity that owns it. Slugs are generated in
-- application code (Foodblog.Domain.Tags.Slug) and backfilled at startup.

CREATE TABLE slugs (
    slug        TEXT    NOT NULL PRIMARY KEY,
    entity_type TEXT    NOT NULL,
    entity_id   INTEGER NOT NULL
);

CREATE UNIQUE INDEX ux_slugs_entity ON slugs (entity_type, entity_id);
