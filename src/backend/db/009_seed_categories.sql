-- WP-0.6.1: Seed-категории
-- Конференции, Вебинары, Концерты и т.п.

INSERT INTO meetup.categories (name, is_archived, sort_order)
VALUES
    ('Конференции', false, 1),
    ('Вебинары', false, 2),
    ('Концерты', false, 3),
    ('Мастер-классы', false, 4),
    ('Встречи', false, 5),
    ('Выставки', false, 6),
    ('Фестивали', false, 7),
    ('Спорт', false, 8)
ON CONFLICT (name) DO NOTHING;
