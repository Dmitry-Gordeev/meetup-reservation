# Implementation Plan: Чек-ин по QR-коду

**Branch**: `001-qr-checkin` | **Date**: 2026-09-06 | **Spec**: [spec.md](./spec.md)
**Input**: spec.md, `.specify/memory/constitution.md`, `spec/phase-c-information-systems-architecture.md`

## Summary

Каждая регистрация получает уникальный токен. Токен кодируется в QR-изображение, которое вкладывается в письмо-подтверждение и показывается в «Мои регистрации». Организатор сканирует код на странице события; backend по паре (событие, токен) переводит регистрацию в `checked_in`. Ручной чек-ин не меняется.

## Technical Context

**Language/Version**: C# 14 / .NET 10; TypeScript 5 / React 18
**Primary Dependencies**: Dapper, MailKit, ASP.NET Core; новые — `QRCoder` (генерация PNG на backend), `html5-qrcode` (сканирование в браузере)
**Storage**: PostgreSQL, схема `meetup`; новая колонка `registrations.qr_token`
**Testing**: ручное по `quickstart.md` и `docs/testing-checklist.md`
**Target Platform**: web; камера в браузере требует HTTPS в production (TS-09)
**Project Type**: монорепозиторий, backend + frontend
**Performance Goals**: ответ endpoint чек-ина < 300 мс; генерация QR < 50 мс
**Constraints**: без ORM; SQL-скрипт миграции; ПДн не покидают систему; контракт существующих endpoints не меняется
**Scale/Scope**: события до ~1000 участников; один сканер на событие

## Constitution Check

| Принцип | Соответствие |
|---|---|
| I. Стек | ✓ .NET 10, React; новые библиотеки — только .NET и npm пакеты |
| II. Схема БД скриптами | ✓ `src/backend/db/011_registrations_qr_token.sql`, идемпотентный, с backfill |
| III. Контракт API явно | ✓ новый endpoint `POST /api/v1/events/{id}/check-in/qr` и `GET /api/v1/registrations/{id}/qr`; существующие не меняются; контракт в `contracts/api.md` |
| IV. ПДн внутри системы | ✓ QR генерируется QRCoder на backend; изображение в письме — inline; внешние сервисы не используются |
| V. Границы v1 | ✓ FR-05.5 переводится из «след. фаза» в первую версию решением владельца продукта; Phase A/B обновляются в T003 |

**Отступления**: две новые библиотеки — см. Complexity Tracking.

## Project Structure

### Documentation

```text
specs/001-qr-checkin/
├── spec.md
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── api.md
└── tasks.md
```

### Source Code

```text
src/backend/
├── db/011_registrations_qr_token.sql
├── Registrations/
│   ├── RegistrationsService.cs        # генерация токена при создании, CheckInByQrAsync
│   └── QrCodeService.cs               # PNG из токена (QRCoder)
├── Controllers/RegistrationsController.cs   # новые endpoints
└── Notifications/EmailTemplates.cs    # QR в письме-подтверждении

src/frontend/src/
├── pages/OrganizerScanPage.tsx        # камера + ручной ввод
├── pages/MyRegistrationsPage.tsx      # показ QR
└── api/client.ts                      # вызовы новых endpoints
```

## Design Decisions

1. **Токен, а не id регистрации.** Id последовательный и угадываемый; токен — 16 случайных байт в base64url (22 символа). Хранится в `registrations.qr_token` с UNIQUE.
2. **Чек-ин привязан к событию.** Endpoint принимает `eventId` из маршрута и токен из тела; регистрация ищется по паре, организатор проверяется через `IsOrganizerOfEventAsync`. Код чужого события не раскрывает данных.
3. **Изображение генерируется по запросу**, не хранится: `GET /registrations/{id}/qr` отдаёт PNG для владельца регистрации или организатора; в письмо PNG вкладывается inline при отправке.
4. **Повторный чек-ин — 409** с `checkedInAt` в теле; отменённая регистрация — 409 с `reason: cancelled`; неизвестный токен — 404.
5. **Сканер на клиенте** — `html5-qrcode`, работает без сервера; при недоступной камере то же поле принимает ручной ввод.

## Complexity Tracking

| Отступление | Почему нужно | Отвергнутая альтернатива |
|---|---|---|
| Библиотека `QRCoder` | Генерация PNG без внешних сервисов (принцип IV) | Внешний API генерации QR — передаёт токен наружу; своя реализация QR — избыточна |
| Библиотека `html5-qrcode` | Доступ к камере и декодирование в браузере | Только ручной ввод — не убирает очередь; нативное приложение — вне scope |

## Phase Gates

- Перед `/speckit.tasks`: `spec ↔ plan ↔ constitution` согласованы (`/speckit.analyze`).
- Перед реализацией T010+: миграция применена на dev-БД, backfill проверен.
- Перед закрытием: `quickstart.md` пройден, `docs/testing-checklist.md` дополнен.
