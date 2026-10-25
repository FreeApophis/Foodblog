-- 0004_email_verification.sql
-- New registrations must confirm their email address before they can log in. Existing accounts
-- (the seeded content authors and the admin) are treated as already verified.

ALTER TABLE users ADD COLUMN email_verified INTEGER NOT NULL DEFAULT 0;
UPDATE users SET email_verified = 1;

CREATE TABLE email_verification_tokens (
    token      TEXT    PRIMARY KEY,
    user_id    INTEGER NOT NULL,
    expires_at TEXT    NOT NULL,
    consumed   INTEGER NOT NULL DEFAULT 0
);
