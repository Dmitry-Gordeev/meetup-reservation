-- WP-0.3.4: events, event_categories

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
