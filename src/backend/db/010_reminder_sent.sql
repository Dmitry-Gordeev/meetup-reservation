-- WP-2.3: таблица для учёта отправленных напоминаний
-- Позволяет не дублировать напоминания при периодическом запуске фоновой задачи

CREATE TABLE IF NOT EXISTS meetup.reminder_sent (
    event_id     BIGINT NOT NULL REFERENCES meetup.events(id) ON DELETE CASCADE,
    reminder_type VARCHAR(10) NOT NULL,
    sent_at      TIMESTAMPTZ NOT NULL DEFAULT (NOW() AT TIME ZONE 'UTC'),
    PRIMARY KEY (event_id, reminder_type),
    CONSTRAINT chk_reminder_type CHECK (reminder_type IN ('24h', '1h'))
);

CREATE INDEX IF NOT EXISTS ix_reminder_sent_event_id ON meetup.reminder_sent(event_id);
