-- 0003_configuration.sql
-- A simple key/value configuration store for site-wide settings that must be editable at
-- runtime (the site name and the SMTP credentials used to send registration and
-- password-reset mail). Default rows are written in application code by ConfigurationSeeder,
-- so the keys always exist (with blank SMTP values) on a freshly-migrated database.

CREATE TABLE configuration (
    key   TEXT NOT NULL PRIMARY KEY,
    value TEXT NOT NULL
);
