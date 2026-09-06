# Accounts Specification

## Purpose

Учётные записи платформы (BC-01): регистрация организаторов и участников, вход по email и паролю, роли, публичный профиль организатора. Источник: FR-01, `AuthController`, `AuthService`, таблицы `users`, `user_roles`, `organizer_profiles`, `participant_profiles`.

## Requirements

### Requirement: Регистрация на платформе

Система SHALL регистрировать пользователя по email и паролю через `POST /api/v1/auth/register` с параметром `role` (`organizer` или `participant`), создавать соответствующий профиль и записывать роль в `user_roles`.

#### Scenario: Регистрация организатора

- **WHEN** запрос содержит уникальный email, пароль, `role = organizer` и название
- **THEN** создаются `users`, `organizer_profiles` и роль `organizer`, ответ 201

#### Scenario: Регистрация участника

- **WHEN** запрос содержит уникальный email, пароль, `role = participant`, имя и фамилию
- **THEN** создаются `users`, `participant_profiles` и роль `participant`

#### Scenario: Email уже занят

- **WHEN** email уже есть в `users`
- **THEN** система отвечает ошибкой и не создаёт пользователя

### Requirement: Хранение паролей

Система SHALL хранить только хеш пароля, вычисленный Argon2id.

#### Scenario: Пароль не хранится в открытом виде

- **WHEN** пользователь зарегистрирован
- **THEN** в `users.password_hash` находится хеш Argon2id, исходный пароль нигде не сохраняется

### Requirement: Вход

Система SHALL выдавать JWT со сроком жизни 1 час через `POST /api/v1/auth/login` по email и паролю; refresh token не используется.

#### Scenario: Успешный вход

- **WHEN** email и пароль верны и пользователь не заблокирован
- **THEN** ответ содержит JWT с идентификатором пользователя и ролями

#### Scenario: Заблокированный пользователь

- **WHEN** у пользователя `is_blocked = true`
- **THEN** вход отклоняется

### Requirement: Авторизация запросов

Система SHALL принимать JWT в заголовке `Authorization: Bearer <token>` и проверять роль для защищённых endpoints; администраторские endpoints требуют роль `admin`.

#### Scenario: Запрос без токена к защищённому endpoint

- **WHEN** запрос к endpoint с `[Authorize]` не содержит валидного токена
- **THEN** ответ 401

#### Scenario: Роль не подходит

- **WHEN** пользователь без роли `admin` обращается к `/api/v1/admin/*`
- **THEN** ответ 403

### Requirement: Публичный профиль организатора

Система SHALL отдавать публичный профиль организатора (название, описание, наличие аватара) через `GET /api/v1/organizers/{id}` и аватар через `GET /api/v1/organizers/{id}/avatar` без авторизации.

#### Scenario: Просмотр профиля гостем

- **WHEN** неавторизованный посетитель запрашивает профиль существующего организатора
- **THEN** ответ содержит название, описание и признак наличия аватара

### Requirement: Профиль текущего пользователя

Система SHALL отдавать данные текущего пользователя через `GET /api/v1/me` и `GET /api/v1/me/profile` для авторизованного запроса.

#### Scenario: Данные участника для автозаполнения

- **WHEN** авторизованный участник запрашивает `GET /api/v1/me/profile`
- **THEN** ответ содержит фамилию, имя, отчество, email и телефон из `participant_profiles`

### Requirement: Первый администратор

Система SHALL создавать первого администратора SQL-скриптом `src/backend/db/008_admin_user.sql`; регистрация с ролью `admin` через API не допускается.

#### Scenario: Попытка зарегистрировать администратора через API

- **WHEN** запрос регистрации содержит `role = admin`
- **THEN** запрос отклоняется
