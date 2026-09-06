# Tasks: Чек-ин по QR-коду

**Input**: spec.md, plan.md, data-model.md, contracts/api.md
**Правило**: одна задача — один проверяемый результат; `[P]` — можно выполнять параллельно; `[USn]` — история из spec.md. После каждой задачи: `dotnet build`, `npm run build`.

## Phase 1: Setup

- [ ] T001 Добавить пакет `QRCoder` в `src/backend/MeetupReservation.Api.csproj`
- [ ] T002 [P] Добавить пакет `html5-qrcode` в `src/frontend/package.json`
- [ ] T003 [P] Обновить `spec/phase-a-architecture-vision.md` (раздел 3.2, FR «Чек-ин») и `spec/phase-b-business-architecture.md` (FR-05.5 → Must): чек-ин по QR входит в текущую версию

## Phase 2: Foundational (блокирует истории)

- [ ] T004 Создать `src/backend/db/011_registrations_qr_token.sql` по data-model.md: колонка, backfill через pgcrypto, NOT NULL, UNIQUE
- [ ] T005 Применить скрипт на dev-БД и проверить: все строки `registrations` имеют уникальный `qr_token`
- [ ] T006 Добавить генерацию токена в `RegistrationsService.CreateRegistrationAsync` (16 байт → base64url) и сохранение в INSERT
- [ ] T007 [P] Создать `src/backend/Registrations/QrCodeService.cs`: `byte[] RenderPng(string token, int size = 256)` через QRCoder
- [ ] T008 Обновить `src/backend/db/README.md`: шаг применения `011_registrations_qr_token.sql`

## Phase 3: User Story 1 — участник получает QR-код

- [ ] T009 [US1] В `EmailTemplates.RegistrationConfirmation` добавить блок с `<img src="cid:qr">` и текстовым кодом
- [ ] T010 [US1] В `EmailService.SendAsync` добавить перегрузку с inline-вложением (`LinkedResources`, Content-Id `qr`); использовать в `CreateRegistrationAsync`
- [ ] T011 [US1] Endpoint `GET /api/v1/registrations/{id}/qr` в `RegistrationsController`: проверка владельца или организатора, ответ `image/png`
- [ ] T012 [US1] В `src/frontend/src/pages/MyRegistrationsPage.tsx` показать QR-изображение регистрации (`GET /registrations/{id}/qr`)
- [ ] T013 [US1] Проверить quickstart сценарии 1 и 5

## Phase 4: User Story 2 — организатор сканирует QR

- [ ] T014 [US2] `RegistrationsService.CheckInByQrAsync(eventId, token, organizerUserId)`: поиск по (event_id, qr_token), переходы статусов по data-model.md, результат с причиной отказа
- [ ] T015 [US2] Endpoint `POST /api/v1/events/{id}/check-in/qr` в `RegistrationsController` по contracts/api.md (200 / 404 / 409 / 403 / 400)
- [ ] T016 [US2] Создать `src/frontend/src/pages/OrganizerScanPage.tsx`: сканер `html5-qrcode`, поле ручного ввода, экран результата (успех / уже отмечен / отменена / не найден)
- [ ] T017 [US2] Добавить маршрут `/organizer/events/:id/scan` в `App.tsx` и кнопку «Сканировать QR» на странице события в кабинете организатора
- [ ] T018 [US2] Добавить вызовы новых endpoints в `src/frontend/src/api/client.ts`
- [ ] T019 [US2] Проверить quickstart сценарии 2 и 3

## Phase 5: User Story 3 — ручная отметка остаётся

- [ ] T020 [US3] Убедиться, что `PATCH /registrations/{id}/check-in` и кнопка в списке не изменились; проверить quickstart сценарий 4

## Phase 6: Polish

- [ ] T021 [P] Дополнить `docs/testing-checklist.md` разделом FR-05.5 (пять сценариев из quickstart.md)
- [ ] T022 [P] Обновить `spec/phase-c-information-systems-architecture.md`: два новых endpoint в таблице API, колонка `qr_token` в модели
- [ ] T023 Обновить `README.md` проекта: упоминание сканера и требования HTTPS для камеры
- [ ] T024 `/speckit.converge`: сверить код с spec.md, plan.md, tasks.md; дописать оставшееся

## Dependencies

- T004 → T005 → T006; T006 и T007 → T010; T014 → T015 → T016.
- Истории независимы после Phase 2: US1 (T009–T013) и US2 (T014–T019) можно вести параллельно.
- Контрольная точка: перед T009 — `/speckit.analyze`, spec ↔ plan ↔ tasks без противоречий.
