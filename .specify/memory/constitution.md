# Meetup Reservation — Constitution

Принципы и ограничения проекта. Читаются агентом перед `/speckit.specify`, `/speckit.plan` и `/speckit.tasks`; каждый план проходит проверку соответствия (Constitution Check).

Источник: `spec/phase-a-architecture-vision.md` (принципы AP-1–AP-5), `spec/phase-d-technology-architecture.md` (стандарты TS-01–TS-18).

## Core Principles

### I. Фиксированный технологический стек (NON-NEGOTIABLE)

- Backend: ASP.NET Core (.NET 10), C#. Доступ к данным через Dapper / ADO.NET.
- СУБД: PostgreSQL 16+, схема `meetup`.
- Frontend: React + TypeScript + Vite; пакетный менеджер npm, Node.js 22 LTS.
- Entity Framework и любые ORM запрещены. Другие стеки (Node.js backend, Vue/Angular) запрещены.

### II. Схема БД только ручными SQL-скриптами

- Все изменения схемы — нумерованные SQL-скрипты в `src/backend/db/` (`NNN_name.sql`), идемпотентные (`IF NOT EXISTS`).
- Инструменты миграций (DbUp, Flyway, EF Migrations) не используются.
- Новая таблица или колонка без скрипта в `src/backend/db/` не считается реализованной.
- Даты и время хранятся в UTC (`TIMESTAMPTZ`), конвертация — на клиенте.

### III. Контракт API меняется только явно

- Все endpoints под префиксом `/api/v1/`, формат JSON, ошибки — `{ "error": "..." }` с корректным HTTP-кодом.
- Существующие endpoints не меняют сигнатуру и семантику молча: изменение контракта фиксируется в `contracts/` соответствующей функции и в `spec/phase-c-information-systems-architecture.md`.
- Аутентификация: JWT в `Authorization: Bearer <token>`, срок жизни 1 час, без refresh token. Роли: `organizer`, `participant`, `admin`.
- Пагинация списков — cursor-based (`cursor`, `limit`, `nextCursor`).

### IV. Персональные данные не покидают систему

- Email отправляется только через локальный SMTP (MailKit). Внешние email/SMS-провайдеры не используются.
- Файлы (фото, вложения, аватары, QR-изображения) хранятся в PostgreSQL как `bytea`, лимит 50 MB; внешние хранилища не используются.
- Данные участников не передаются сторонним сервисам; генерация QR-кодов, экспорт и любая обработка ПДн выполняются внутри backend.
- Пароли — Argon2id.

### V. Мультитенантность и границы первой версии

- Все запросы событий, участников и экспорта фильтруются по `organizer_id`; организатор не видит чужие данные.
- Вне первой версии (требуют отдельного решения, не добавляются «по логике»): реальные платежи и возвраты, SMS, интеграции с видеоплатформами, публичный API, полнотекстовый поиск, мультиязычность, восстановление пароля, подтверждение email, клонирование событий, промокоды, несколько билетов на участника.
- Функция, которая выходит за эти границы, начинается с изменения `spec/phase-a-architecture-vision.md`, а не с кода.

## Additional Constraints

- Логирование — `ILogger`, без Serilog/NLog.
- Экспорт: ClosedXML (Excel), PdfSharp (PDF).
- CORS в первой версии — `AllowAnyOrigin`.
- Тестирование первой версии — ручное по `docs/testing-checklist.md`; автотесты допускаются, но не обязательны.
- Новая внешняя библиотека допускается только с записью причины в `plan.md` функции (раздел Complexity Tracking).

## Development Workflow

- Порядок: `constitution → specify → clarify → plan → tasks → analyze → implement`.
- Каждая функция — папка `specs/NNN-feature/` с `spec.md`, `plan.md`, `tasks.md`; артефакты меняются в том же изменении, что и код.
- `/speckit.clarify` задаёт вопросы по одному; ответы записываются в `spec.md`, а не остаются в чате.
- Перед `/speckit.tasks` проверяется согласованность `spec ↔ plan ↔ constitution`; противоречие возвращает к артефакту, а не к коду.
- Задачи выполняются по одной; после каждой — `dotnet build`, `npm run build`, ручная проверка по сценариям из `quickstart.md`.

## Governance

- Конституция имеет приоритет над остальными артефактами Spec Kit. Отступление требует явной записи в `plan.md` (Complexity Tracking) с обоснованием и более простой отвергнутой альтернативой.
- Изменение конституции — отдельный коммит с обновлением версии и даты.
- Границы продукта (раздел V) меняет владелец продукта через Phase A, агент их не расширяет.

**Version**: 1.0.0 | **Ratified**: 2026-09-06 | **Last Amended**: 2026-09-06
