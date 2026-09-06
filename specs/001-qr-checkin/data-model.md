# Data Model: Чек-ин по QR-коду

## Изменения схемы

### `meetup.registrations` — новая колонка

| Колонка | Тип | Ограничения | Описание |
|---|---|---|---|
| `qr_token` | `VARCHAR(32)` | `NOT NULL`, `UNIQUE` | 16 случайных байт в base64url; неизменяем после создания |

Индекс: `uq_registrations_qr_token` (UNIQUE) — поиск при сканировании.

### Миграция `src/backend/db/011_registrations_qr_token.sql`

1. `ALTER TABLE ... ADD COLUMN IF NOT EXISTS qr_token VARCHAR(32)`.
2. Backfill существующих строк: `encode(gen_random_bytes(16), 'base64')` с заменой `+/` на `-_` и обрезкой `=` (расширение `pgcrypto`, `CREATE EXTENSION IF NOT EXISTS pgcrypto`).
3. `ALTER COLUMN qr_token SET NOT NULL`, `ADD CONSTRAINT uq_registrations_qr_token UNIQUE (qr_token)`.

Скрипт идемпотентен: повторный запуск не меняет существующие токены.

## Состояния регистрации (без изменений)

`registered → checked_in`, `registered → cancelled`, `checked_in → cancelled`.

Чек-ин по QR допустим только из `registered`. Из `checked_in` — отказ «уже отмечен», из `cancelled` — отказ «отменена».

## Что не меняется

- `ticket_types`, `events`, `users` — без изменений.
- Экспорт участников (`ExportService`) не включает `qr_token`.
- Существующие DTO (`EventRegistrationDto`, `MyRegistrationDto`) получают только флаг наличия QR через новый endpoint; токен в списки не попадает.
