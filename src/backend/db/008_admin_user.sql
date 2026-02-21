-- WP-0.5.1: Скрипт первого администратора
-- Создаёт пользователя с ролью admin
-- Пароль по умолчанию: admin123 (обязательно сменить после первого входа)

CREATE EXTENSION IF NOT EXISTS pgcrypto;

INSERT INTO meetup.users (email, password_hash, is_blocked)
VALUES ('admin@example.com', crypt('admin123', gen_salt('bf')), false)
ON CONFLICT (email) DO NOTHING;

INSERT INTO meetup.user_roles (user_id, role)
SELECT id, 'admin' FROM meetup.users WHERE email = 'admin@example.com'
ON CONFLICT (user_id, role) DO NOTHING;
