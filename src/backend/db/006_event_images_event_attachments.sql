-- WP-0.3.6: event_images, event_attachments
-- BLOB (bytea)

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
