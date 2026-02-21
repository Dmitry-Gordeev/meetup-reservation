-- Meetup Reservation: полная инициализация схемы БД
-- Объединяет скрипты 001–007
-- Схема: meetup
-- PostgreSQL

-- =============================================================================
-- WP-0.3.1: users, user_roles
-- =============================================================================

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

-- =============================================================================
-- WP-0.3.2: organizer_profiles, participant_profiles
-- =============================================================================

CREATE TABLE IF NOT EXISTS meetup.organizer_profiles (
    user_id             BIGINT NOT NULL PRIMARY KEY REFERENCES meetup.users(id) ON DELETE CASCADE,
    name                VARCHAR(255) NOT NULL,
    description         TEXT,
    avatar_content      BYTEA,
    avatar_content_type VARCHAR(100),
    avatar_file_name    VARCHAR(255)
);

CREATE TABLE IF NOT EXISTS meetup.participant_profiles (
    user_id     BIGINT NOT NULL PRIMARY KEY REFERENCES meetup.users(id) ON DELETE CASCADE,
    first_name  VARCHAR(100) NOT NULL,
    last_name   VARCHAR(100) NOT NULL,
    middle_name VARCHAR(100),
    email       VARCHAR(255) NOT NULL,
    phone       VARCHAR(50)
);

-- =============================================================================
-- WP-0.3.3: categories
-- =============================================================================

CREATE TABLE IF NOT EXISTS meetup.categories (
    id          BIGSERIAL PRIMARY KEY,
    name        VARCHAR(100) NOT NULL UNIQUE,
    is_archived BOOLEAN NOT NULL DEFAULT FALSE,
    sort_order  INTEGER NOT NULL DEFAULT 0
);

-- =============================================================================
-- WP-0.3.4: events, event_categories
-- =============================================================================

CREATE TABLE IF NOT EXISTS meetup.events (
    id          BIGSERIAL PRIMARY KEY,
    organizer_id BIGINT NOT NULL REFERENCES meetup.users(id) ON DELETE CASCADE,
    title       VARCHAR(255) NOT NULL,
    description TEXT,
    start_at    TIMESTAMPTZ NOT NULL,
    end_at      TIMESTAMPTZ NOT NULL,
    location    VARCHAR(500),
    is_online   BOOLEAN NOT NULL DEFAULT FALSE,
    is_public   BOOLEAN NOT NULL DEFAULT TRUE,
    status      VARCHAR(50) NOT NULL DEFAULT 'active',
    created_at  TIMESTAMPTZ NOT NULL DEFAULT (NOW() AT TIME ZONE 'UTC'),
    CONSTRAINT chk_event_status CHECK (status IN ('active', 'cancelled', 'blocked'))
);

CREATE TABLE IF NOT EXISTS meetup.event_categories (
    event_id    BIGINT NOT NULL REFERENCES meetup.events(id) ON DELETE CASCADE,
    category_id BIGINT NOT NULL REFERENCES meetup.categories(id) ON DELETE CASCADE,
    PRIMARY KEY (event_id, category_id)
);

-- =============================================================================
-- WP-0.3.5: ticket_types, registrations (UNIQUE(event_id, email))
-- =============================================================================

CREATE TABLE IF NOT EXISTS meetup.ticket_types (
    id       BIGSERIAL PRIMARY KEY,
    event_id BIGINT NOT NULL REFERENCES meetup.events(id) ON DELETE CASCADE,
    name     VARCHAR(100) NOT NULL,
    price    NUMERIC(10, 2) NOT NULL DEFAULT 0,
    capacity INTEGER NOT NULL
);

CREATE TABLE IF NOT EXISTS meetup.registrations (
    id             BIGSERIAL PRIMARY KEY,
    event_id       BIGINT NOT NULL REFERENCES meetup.events(id) ON DELETE CASCADE,
    ticket_type_id BIGINT NOT NULL REFERENCES meetup.ticket_types(id) ON DELETE CASCADE,
    user_id        BIGINT REFERENCES meetup.users(id) ON DELETE SET NULL,
    email          VARCHAR(255) NOT NULL,
    first_name     VARCHAR(100) NOT NULL,
    last_name      VARCHAR(100) NOT NULL,
    middle_name    VARCHAR(100),
    phone          VARCHAR(50),
    status         VARCHAR(50) NOT NULL DEFAULT 'registered',
    checked_in_at  TIMESTAMPTZ,
    created_at     TIMESTAMPTZ NOT NULL DEFAULT (NOW() AT TIME ZONE 'UTC'),
    CONSTRAINT uq_registrations_event_email UNIQUE (event_id, email),
    CONSTRAINT chk_registration_status CHECK (status IN ('registered', 'checked_in', 'cancelled'))
);

-- =============================================================================
-- WP-0.3.6: event_images, event_attachments (BLOB bytea)
-- =============================================================================

CREATE TABLE IF NOT EXISTS meetup.event_images (
    id           BIGSERIAL PRIMARY KEY,
    event_id     BIGINT NOT NULL REFERENCES meetup.events(id) ON DELETE CASCADE,
    content      BYTEA NOT NULL,
    content_type VARCHAR(100) NOT NULL,
    file_name    VARCHAR(255) NOT NULL,
    sort_order   INTEGER NOT NULL DEFAULT 0
);

CREATE TABLE IF NOT EXISTS meetup.event_attachments (
    id           BIGSERIAL PRIMARY KEY,
    event_id     BIGINT NOT NULL REFERENCES meetup.events(id) ON DELETE CASCADE,
    content      BYTEA NOT NULL,
    content_type VARCHAR(100) NOT NULL,
    file_name    VARCHAR(255) NOT NULL,
    type         VARCHAR(50)
);

-- =============================================================================
-- WP-0.3.7: Индексы для каталога
-- =============================================================================

CREATE INDEX IF NOT EXISTS ix_events_organizer_id ON meetup.events(organizer_id);
CREATE INDEX IF NOT EXISTS ix_events_status_start_at ON meetup.events(status, start_at);
CREATE INDEX IF NOT EXISTS ix_events_created_at ON meetup.events(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_event_categories_category_id ON meetup.event_categories(category_id);
CREATE INDEX IF NOT EXISTS ix_event_categories_event_id ON meetup.event_categories(event_id);
CREATE INDEX IF NOT EXISTS ix_registrations_event_id ON meetup.registrations(event_id);
CREATE INDEX IF NOT EXISTS ix_registrations_user_id ON meetup.registrations(user_id);
