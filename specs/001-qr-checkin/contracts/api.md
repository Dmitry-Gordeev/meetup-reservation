# API Contract: Чек-ин по QR-коду

Префикс `/api/v1`. Существующие endpoints (`PATCH /registrations/{id}/check-in`, `POST /events/{id}/registrations`, `GET /me/registrations`) не меняются.

## POST /events/{eventId}/check-in/qr

Чек-ин по токену. Только организатор события (`Authorize`, роль `organizer`).

**Request**

```json
{ "token": "k2Jx9Q3mP8vN1sL0aZ7cYw" }
```

**Responses**

| Код | Тело | Когда |
|---|---|---|
| 200 | `{ "status": "checked_in", "registrationId": 42, "participant": { "firstName": "...", "lastName": "...", "ticketType": "Стандарт" }, "checkedInAt": "2026-10-01T09:05:11Z" }` | регистрация была `registered` |
| 404 | `{ "error": "Registration not found for this event" }` | токен не найден среди регистраций этого события |
| 409 | `{ "error": "Already checked in", "reason": "already_checked_in", "checkedInAt": "..." }` | повторное сканирование |
| 409 | `{ "error": "Registration is cancelled", "reason": "cancelled" }` | регистрация отменена |
| 403 | — | пользователь не организатор события |
| 400 | `{ "error": "Token is required" }` | пустой токен |

Данные участника возвращаются только при 200.

## GET /registrations/{id}/qr

PNG-изображение QR-кода регистрации. Доступ: владелец регистрации (по `user_id` или совпадению email учётной записи) либо организатор события.

**Response**: `200 image/png`, размер 256×256; `403` для остальных; `404` если регистрация не найдена.

## Письмо-подтверждение регистрации

Без изменения существующих полей. Добавляется inline-изображение `cid:qr` и текстовая строка с токеном под ним для клиентов без картинок.

## Frontend

- `POST /events/{eventId}/check-in/qr` вызывается со страницы `/organizer/events/{id}/scan`.
- `GET /registrations/{id}/qr` используется как `src` изображения в «Мои регистрации» и в кабинете организатора.
