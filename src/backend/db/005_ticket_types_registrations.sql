-- WP-0.3.5: ticket_types, registrations
-- UNIQUE(event_id, email) в registrations

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
