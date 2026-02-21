# Database Schema

SQL-скрипты для создания схемы БД Meetup Reservation (PostgreSQL).

**Схема:** `meetup` — все таблицы создаются в схеме `meetup`.

## Варианты применения

### Вариант 1: Один скрипт (рекомендуется)

```bash
psql -d meetup_reservation -f 000_init_schema.sql
```

Скрипт `000_init_schema.sql` объединяет все миграции 001–007.

### Вариант 2: Отдельные скрипты

Выполнять в порядке номеров:

1. `001_users_user_roles.sql` — users, user_roles
2. `002_organizer_participant_profiles.sql` — organizer_profiles, participant_profiles
3. `003_categories.sql` — categories
4. `004_events_event_categories.sql` — events, event_categories
5. `005_ticket_types_registrations.sql` — ticket_types, registrations
6. `006_event_images_event_attachments.sql` — event_images, event_attachments
7. `007_indexes.sql` — индексы для каталога

## Создание БД и применение схемы

```bash
# Создать БД (если ещё не создана)
createdb meetup_reservation

# Применить все скрипты
psql -d meetup_reservation -f 001_users_user_roles.sql
psql -d meetup_reservation -f 002_organizer_participant_profiles.sql
psql -d meetup_reservation -f 003_categories.sql
psql -d meetup_reservation -f 004_events_event_categories.sql
psql -d meetup_reservation -f 005_ticket_types_registrations.sql
psql -d meetup_reservation -f 006_event_images_event_attachments.sql
psql -d meetup_reservation -f 007_indexes.sql
```

Или одной командой (Windows PowerShell):

```powershell
Get-ChildItem 00[1-7]*.sql | Sort-Object Name | ForEach-Object { psql -d meetup_reservation -f $_.FullName }
```

## Первый администратор (WP-0.5)

После применения схемы выполните скрипт создания администратора:

```bash
psql -d meetup_reservation -f 008_admin_user.sql
```

Создаётся пользователь `admin@example.com` с паролем `admin123` и ролью `admin`. **Обязательно смените пароль после первого входа.**

## Seed-категории (WP-0.6)

```bash
psql -d meetup_reservation -f 009_seed_categories.sql
```

Добавляет категории: Конференции, Вебинары, Концерты, Мастер-классы, Встречи, Выставки, Фестивали, Спорт.
