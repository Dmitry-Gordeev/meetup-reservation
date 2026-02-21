-- WP-0.3.7: Индексы для каталога
-- Фильтрация, сортировка по событиям

-- Каталог: события по организатору
CREATE INDEX IF NOT EXISTS ix_events_organizer_id ON meetup.events(organizer_id);

-- Каталог: фильтр по статусу, сортировка по дате
CREATE INDEX IF NOT EXISTS ix_events_status_start_at ON meetup.events(status, start_at);

-- Каталог: сортировка по дате создания
CREATE INDEX IF NOT EXISTS ix_events_created_at ON meetup.events(created_at DESC);

-- Каталог: фильтр по категории (через event_categories)
CREATE INDEX IF NOT EXISTS ix_event_categories_category_id ON meetup.event_categories(category_id);
CREATE INDEX IF NOT EXISTS ix_event_categories_event_id ON meetup.event_categories(event_id);

-- Регистрации: поиск по событию
CREATE INDEX IF NOT EXISTS ix_registrations_event_id ON meetup.registrations(event_id);

-- Регистрации: поиск по пользователю (мои регистрации)
CREATE INDEX IF NOT EXISTS ix_registrations_user_id ON meetup.registrations(user_id);
