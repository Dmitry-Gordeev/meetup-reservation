-- WP-0.3.1: users, user_roles
-- Phase C Data Model

CREATE SCHEMA IF NOT EXISTS meetup;

CREATE TABLE IF NOT EXISTS meetup.users (
    id              BIGSERIAL PRIMARY KEY,
    email           VARCHAR(255) NOT NULL UNIQUE,
    password_hash   VARCHAR(255) NOT NULL,
    is_blocked     BOOLEAN NOT NULL DEFAULT FALSE,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT (NOW() AT TIME ZONE 'UTC')
);

CREATE TABLE IF NOT EXISTS meetup.user_roles (
    user_id         BIGINT NOT NULL REFERENCES meetup.users(id) ON DELETE CASCADE,
    role            VARCHAR(50) NOT NULL,
    PRIMARY KEY (user_id, role),
    CONSTRAINT chk_role CHECK (role IN ('organizer', 'participant', 'admin'))
);

CREATE INDEX IF NOT EXISTS ix_user_roles_user_id ON meetup.user_roles(user_id);
