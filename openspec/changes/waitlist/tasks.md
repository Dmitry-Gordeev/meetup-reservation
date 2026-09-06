# Tasks: Лист ожидания

Порядок: схема → домен → API → уведомления → frontend → развёртывание и откат. После каждого блока — `dotnet build`, `npm run build`, ручная проверка по сценариям из `specs/`.

## 1. Схема

- [ ] 1.1 `src/backend/db/012_waitlist.sql`: колонка `ticket_types.waitlist_enabled BOOLEAN NOT NULL DEFAULT FALSE`; таблица `waitlist_entries` по design.md; частичный UNIQUE `(event_id, email) WHERE status = 'waiting'`; индекс `(ticket_type_id, status, created_at, id)`
- [ ] 1.2 `src/backend/db/012_waitlist_down.sql`: откат
- [ ] 1.3 Применить на dev-БД, обновить `src/backend/db/README.md`

## 2. Домен

- [ ] 2.1 `EventsService.CreateEventAsync`: принять `WaitlistEnabled` у `CreateTicketTypeRequest`, отклонить при `Price > 0`, сохранить в `ticket_types`
- [ ] 2.2 `RegistrationsService.CreateRegistrationAsync`: при исчерпании мест и `waitlist_enabled` создать запись очереди вместо 400; проверки email против регистраций и очереди
- [ ] 2.3 `RegistrationsService.PromoteFromWaitlistAsync(conn, tx, ticketTypeId)`: первая `waiting` запись `FOR UPDATE SKIP LOCKED`, повторная проверка мест, создание регистрации, статус `promoted`
- [ ] 2.4 Вызвать продвижение из `CancelRegistrationAsync` и `AdminService.BlockUserAsync` в той же транзакции
- [ ] 2.5 `EventsService.CancelEventAsync` и `AdminService.BlockOrganizerAsync`: перевести записи `waiting` в `cancelled`
- [ ] 2.6 `RegistrationsService.CancelWaitlistEntryAsync(entryId, userId)` и `GetEventWaitlistAsync(eventId)`

## 3. API

- [ ] 3.1 `POST /api/v1/events/{id}/registrations`: ответ 202 `{ status, waitlistEntryId, position }`
- [ ] 3.2 `DELETE /api/v1/waitlist/{id}` в `RegistrationsController` (участник, по email учётной записи)
- [ ] 3.3 `GET /api/v1/events/{id}/waitlist` (организатор события)
- [ ] 3.4 `GET /api/v1/me/registrations`: массив `waitlist` с позицией
- [ ] 3.5 `GET /api/v1/events/{id}`: признак `waitlistEnabled` у типов билетов

## 4. Уведомления

- [ ] 4.1 `EmailTemplates.WaitlistJoined(participantName, eventTitle, ticketType, position)`
- [ ] 4.2 `EmailTemplates.WaitlistPromoted(participantName, eventTitle, startAt, location, hasAccount)`
- [ ] 4.3 Отправка в 2.2 и 2.3 после коммита транзакции; ожидающие включены в рассылку об отмене события (2.5)

## 5. Frontend

- [ ] 5.1 Форма создания события: переключатель «Лист ожидания» у бесплатного типа билета (`OrganizerPage.tsx`)
- [ ] 5.2 Страница события: при заполненном типе с листом ожидания кнопка «Встать в очередь» вместо «Мест нет» (`EventPage.tsx`, `RegisterPage.tsx`), экран с позицией после 202
- [ ] 5.3 «Мои регистрации»: блок очереди с позицией и кнопкой «Выйти из очереди» (`MyRegistrationsPage.tsx`)
- [ ] 5.4 Кабинет организатора: вкладка «Очередь» на странице события (`OrganizerCabinetPage.tsx`)
- [ ] 5.5 `api/client.ts`: вызовы новых endpoints

## 6. Развёртывание и откат

- [ ] 6.1 `docs/testing-checklist.md`: раздел FR-09.3 со сценариями из дельт (постановка, продвижение, два параллельных освобождения, выход, отмена события, платный тип)
- [ ] 6.2 `spec/phase-b-business-architecture.md` FR-09.3 → Must; `spec/phase-c-information-systems-architecture.md`: таблица `waitlist_entries`, новые endpoints; `spec/phase-a-architecture-vision.md` раздел 3.2
- [ ] 6.3 Проверить откат: `012_waitlist_down.sql` на копии dev-БД, приложение запускается, регистрации целы
- [ ] 6.4 `/opsx:verify`, затем `/opsx:sync` и `/opsx:archive`
