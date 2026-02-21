# Phase C: Information Systems Architectures

**Проект:** Meetup Reservation (монорепозиторий)  
**Методология:** TOGAF ADM  
**Версия:** 1.0  
**Зависимости:** [Phase A: Architecture Vision](./phase-a-architecture-vision.md), [Phase B: Business Architecture](./phase-b-business-architecture.md)

---

## 1. Цели и область Phase C

### 1.1 Цели

- Разработать **Data Architecture** (архитектуру данных) — логическую модель данных, сущности, связи.
- Разработать **Application Architecture** (архитектуру приложений) — компоненты, интерфейсы, потоки данных.
- Обеспечить соответствие Information Systems Architectures бизнес-архитектуре (Phase B) и видению (Phase A).
- Выполнить gap-анализ и уточнить технические требования.

### 1.2 Область (из Phase A/B)

- **Backend:** ASP.NET Core (.NET 10), PostgreSQL, SQL-скрипты (без Entity Framework).
- **Frontend:** React + TypeScript + Vite.
- **Монорепозиторий:** backend и frontend в одном репозитории.

---

## 2. Data Architecture

### 2.1 Baseline Data Architecture

Проект **greenfield**: до внедрения Meetup Reservation специализированной системы данных нет. Данные хранятся в разрозненных инструментах (таблицы, email, сторонние формы).

| Аспект | Baseline |
|--------|----------|
| Хранилище | Нет единой БД |
| Структура | Нет формальной модели |
| Целостность | Нет проверки дублей, ссылочной целостности |
| Доступ | Ручной, без API |

### 2.2 Target Data Architecture

**СУБД:** PostgreSQL  
**Управление схемой:** SQL-скрипты, миграции вручную или через скрипты  
**Подход:** без ORM (Entity Framework не используется)

### 2.3 Logical Data Model

#### 2.3.1 Диаграмма сущностей (текстовое описание)

```
┌─────────────────┐     ┌──────────────────────┐     ┌─────────────────┐
│     users       │     │  organizer_profiles   │     │    categories   │
├─────────────────┤     ├──────────────────────┤     ├─────────────────┤
│ id (PK)         │────<│ user_id (PK, FK)     │     │ id (PK)         │
│ email           │     │ name                 │     │ name            │
│ password_hash   │     │ description          │     │ is_archived     │
│ role            │     │ avatar_url           │     │ sort_order      │
│ is_blocked      │     └──────────────────────┘     └────────┬────────┘
│ created_at      │                                           │
└────────┬────────┘                                           │
         │                                                    │
         │     ┌──────────────────────┐                       │
         └────<│ participant_profiles │                       │
               ├──────────────────────┤                       │
               │ user_id (PK, FK)      │                       │
               │ first_name           │                       │
               │ last_name            │                       │
               │ middle_name          │                       │
               │ email                │                       │
               │ phone                │                       │
               └──────────────────────┘                       │
                                                               │
┌─────────────────┐     ┌──────────────────────┐               │
│     events      │     │  event_categories    │               │
├─────────────────┤     ├──────────────────────┤               │
│ id (PK)         │────<│ event_id (PK, FK)    │               │
│ organizer_id(FK)│     │ category_id (PK, FK) │>──────────────┘
│ title           │     └──────────────────────┘
│ description     │
│ start_at        │     ┌──────────────────────┐
│ end_at          │     │   ticket_types       │
│ location        │     ├──────────────────────┤
│ is_online       │────<│ id (PK)              │
│ is_public       │     │ event_id (FK)        │
│ status          │     │ name                 │
│ created_at      │     │ price                │
└────────┬────────┘     │ capacity             │
         │             └──────────┬─────────────┘
         │                        │
         │             ┌──────────────────────┐
         │             │   registrations      │
         └────────────<├──────────────────────┤
                      │ id (PK)              │
                      │ event_id (FK)        │
                      │ ticket_type_id (FK)  │
                      │ user_id (FK, null)   │  -- null для гостей
                      │ email                │
                      │ first_name           │
                      │ last_name            │
                      │ middle_name          │
                      │ phone                │
                      │ status               │
                      │ checked_in_at        │
                      │ created_at           │
                      └──────────────────────┘

┌─────────────────┐     ┌──────────────────────┐
│  event_images   │     │  event_attachments   │
├─────────────────┤     ├──────────────────────┤
│ id (PK)         │     │ id (PK)              │
│ event_id (FK)   │     │ event_id (FK)        │
│ url             │     │ url                  │
│ sort_order      │     │ type (program, etc)  │
└─────────────────┘     └──────────────────────┘
```

#### 2.3.2 Описание сущностей

| Сущность | Описание | Ключевые атрибуты |
|----------|----------|-------------------|
| **users** | Пользователи платформы (организаторы, участники, администраторы) | id, email, password_hash, role (organizer/participant/admin), is_blocked, created_at |
| **organizer_profiles** | Публичный профиль организатора | user_id, name, description, avatar_url |
| **participant_profiles** | Профиль участника для автозаполнения | user_id, first_name, last_name, middle_name, email, phone |
| **categories** | Категории событий (фиксированный список) | id, name, is_archived, sort_order |
| **events** | События | id, organizer_id, title, description, start_at, end_at, location, is_online, is_public, status (active/cancelled/blocked), created_at |
| **event_categories** | Связь событие–категория (M:N) | event_id, category_id |
| **ticket_types** | Типы билетов события | id, event_id, name, price, capacity |
| **registrations** | Регистрации на мероприятия | id, event_id, ticket_type_id, user_id (null для гостей), email, first_name, last_name, middle_name, phone, status (registered/checked_in/cancelled), checked_in_at, created_at |
| **event_images** | Фото события | id, event_id, url, sort_order |
| **event_attachments** | Вложения (программа и т.п.) | id, event_id, url, type |

#### 2.3.3 Бизнес-правила в модели данных

| Правило | Реализация |
|---------|------------|
| Один email — один билет на событие | UNIQUE(event_id, email) в registrations |
| Минимум один тип билета на событие | Проверка на уровне приложения |
| Изоляция по организаторам | Все запросы событий/участников фильтруются по organizer_id |
| Архивные категории | is_archived в categories; при создании события доступны только неархивные |
| Статусы события | status: active, cancelled, blocked |

### 2.4 Data Entity / Business Function Matrix

| Сущность | BC-01 Учётные записи | BC-02 Категории | BC-03 События | BC-04 Каталог | BC-05 Регистрация | BC-06 Участники | BC-07 Уведомления | BC-08 Экспорт | BC-09 Модерация | BC-10 ЛК участника |
|----------|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|
| users | ● | | ● | | ● | | | | ● | ● |
| organizer_profiles | ● | | ● | ● | | | | | | |
| participant_profiles | ● | | | | ● | | | | | ● |
| categories | | ● | ● | ● | | | | | ● | |
| events | | | ● | ● | ● | ● | ● | ● | ● | |
| event_categories | | | ● | ● | | | | | | |
| ticket_types | | | ● | ● | ● | ● | | ● | | |
| registrations | | | | | ● | ● | ● | ● | | ● |
| event_images | | | ● | ● | | | | | | |
| event_attachments | | | ● | | | | | | | |

● — сущность используется в capability

---

## 3. Application Architecture

### 3.1 Baseline Application Architecture

Проект **greenfield**: специализированных приложений нет. Используются разрозненные инструменты (таблицы, формы, email).

### 3.2 Target Application Architecture

#### 3.2.1 Компонентная диаграмма

```
                    ┌─────────────────────────────────────────────────────────┐
                    │                    КЛИЕНТ (браузер)                      │
                    │  ┌───────────────────────────────────────────────────┐  │
                    │  │         Frontend SPA (React + TypeScript + Vite)   │  │
                    │  │  - Каталог событий                                 │  │
                    │  │  - Страницы организатора/события                   │  │
                    │  │  - Регистрация на мероприятие                       │  │
                    │  │  - Личный кабинет участника                         │  │
                    │  │  - Кабинет организатора                             │  │
                    │  │  - Админ-панель                                     │  │
                    │  └───────────────────────────┬─────────────────────────┘  │
                    └────────────────────────────┼─────────────────────────────┘
                                                 │ HTTPS / REST API
                                                 ▼
┌─────────────────────────────────────────────────────────────────────────────────┐
│                         Backend (ASP.NET Core .NET 10)                           │
│  ┌─────────────┐ ┌─────────────┐ ┌─────────────┐ ┌─────────────┐ ┌─────────────┐ │
│  │   Auth      │ │   Events    │ │ Registrations│ │  Notifications│ │   Admin    │ │
│  │   Module    │ │   Module    │ │   Module    │ │   Module     │ │   Module   │ │
│  └──────┬──────┘ └──────┬──────┘ └──────┬──────┘ └──────┬──────┘ └──────┬──────┘ │
│         │               │               │               │               │        │
│         └───────────────┴───────────────┴───────────────┴───────────────┘        │
│                                    │                                              │
│                         ┌──────────┴──────────┐                                   │
│                         │   Data Access       │                                   │
│                         │   (SQL, Dapper/ADO) │                                   │
│                         └──────────┬──────────┘                                   │
└────────────────────────────────────┼──────────────────────────────────────────────┘
                                     │
                                     ▼
                    ┌────────────────────────────────┐
                    │     PostgreSQL                  │
                    │     - Схема через SQL-скрипты   │
                    └────────────────────────────────┘
                                     │
                    ┌────────────────┴────────────────┐
                    │  Email Service (SMTP)            │
                    │  - Подтверждения, напоминания   │
                    └────────────────────────────────┘
```

#### 3.2.2 Application Building Blocks

| ID | Компонент | Технология | Описание |
|----|-----------|------------|----------|
| ABB-01 | **Frontend SPA** | React, TypeScript, Vite | Клиентское приложение; маршрутизация, формы, запросы к API |
| ABB-02 | **Backend API** | ASP.NET Core (.NET 10) | REST API; контроллеры, middleware, валидация |
| ABB-03 | **Auth Module** | ASP.NET Core Identity / JWT | Регистрация, авторизация, роли (organizer, participant, admin) |
| ABB-04 | **Events Module** | ASP.NET Core | CRUD событий, типы билетов, категории, изображения |
| ABB-05 | **Registrations Module** | ASP.NET Core | Регистрация на мероприятия, проверка дублей, оплата-заглушка |
| ABB-06 | **Notifications Module** | ASP.NET Core + SMTP | Отправка email (подтверждение, напоминания, отмена) |
| ABB-07 | **Admin Module** | ASP.NET Core | Модерация, блокировка, управление пользователями и категориями |
| ABB-08 | **Export Module** | ASP.NET Core + библиотека Excel/PDF | Генерация Excel/PDF со списком участников |
| ABB-09 | **Data Access** | ADO.NET / Dapper, SQL | Доступ к PostgreSQL без ORM |
| ABB-10 | **Database** | PostgreSQL | Хранение данных; схема через SQL-скрипты |
| ABB-11 | **Email Service** | SMTP | Отправка писем |

#### 3.2.3 Application / Business Function Matrix

| Business Function | ABB-01 Frontend | ABB-02 API | ABB-03 Auth | ABB-04 Events | ABB-05 Registrations | ABB-06 Notifications | ABB-07 Admin | ABB-08 Export |
|-------------------|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|
| Регистрация организатора | ● | ● | ● | | | | | |
| Регистрация участника | ● | ● | ● | | | | | |
| Авторизация | ● | ● | ● | | | | | |
| Профиль организатора | ● | ● | ● | ● | | | | |
| Управление категориями | ● | ● | | | | | ● | |
| Создание события | ● | ● | ● | ● | | | | |
| Публикация события | ● | ● | ● | ● | | | | |
| Единый каталог | ● | ● | | ● | | | | |
| Страница организатора | ● | ● | | ● | | | | |
| Фильтрация, сортировка | ● | ● | | ● | | | | |
| Регистрация на мероприятие | ● | ● | ● | ● | ● | | | |
| Оплата (заглушка) | ● | ● | | | ● | | | |
| Список участников | ● | ● | ● | ● | ● | | | |
| Чек-ин (ручная отметка) | ● | ● | ● | | ● | | | |
| Отмена регистрации | ● | ● | ● | | ● | ● | | | |
| Экспорт Excel/PDF | ● | ● | ● | | ● | | | ● |
| Подтверждение, напоминания | | ● | | | ● | ● | | | |
| Личный кабинет участника | ● | ● | ● | | ● | | | |
| Модерация, блокировка | ● | ● | ● | ● | ● | ● | ● | |

### 3.3 Основные интерфейсы (API)

| Метод | Endpoint | Описание |
|-------|----------|----------|
| POST | /api/auth/register | Регистрация |
| POST | /api/auth/login | Вход |
| GET | /api/events | Список событий (каталог, фильтры, сортировка) |
| GET | /api/events/{id} | Детали события |
| GET | /api/organizers/{id}/events | События организатора |
| POST | /api/events | Создание события (organizer) |
| POST | /api/events/{id}/cancel | Отмена события |
| POST | /api/events/{id}/registrations | Регистрация на мероприятие |
| GET | /api/events/{id}/registrations | Список участников (organizer) |
| PATCH | /api/registrations/{id}/check-in | Чек-ин |
| DELETE | /api/registrations/{id} | Отмена регистрации |
| GET | /api/me/registrations | Мои регистрации |
| GET | /api/events/{id}/registrations/export | Экспорт Excel/PDF |
| GET/POST/PATCH | /api/admin/categories | Управление категориями |
| GET/PATCH | /api/admin/users | Управление пользователями |
| PATCH | /api/admin/events/{id}/block | Блокировка события |
| PATCH | /api/admin/organizers/{id}/block | Блокировка организатора |

---

## 4. Gap Analysis

### 4.1 Data Architecture Gaps

| # | Baseline | Target | Gap |
|---|----------|--------|-----|
| D1 | Нет БД | PostgreSQL с полной схемой | Создание с нуля |
| D2 | Нет модели | 10 сущностей, связи, индексы | Разработка SQL-скриптов |
| D3 | Нет миграций | SQL-скрипты миграций | Ручные или скриптованные миграции |

### 4.2 Application Architecture Gaps

| # | Baseline | Target | Gap |
|---|----------|--------|-----|
| A1 | Нет backend | ASP.NET Core API | Разработка с нуля |
| A2 | Нет frontend | React SPA | Разработка с нуля |
| A3 | Нет auth | JWT/сессии, роли | Реализация Auth Module |
| A4 | Нет email | SMTP-сервис | Интеграция SMTP |
| A5 | Нет экспорта | Excel/PDF генерация | Библиотека экспорта |

### 4.3 Выводы

- Все компоненты создаются с нуля (greenfield).
- Нет миграции данных.
- Зависимости между компонентами определены в Candidate Roadmap (Phase B).

---

## 5. Architecture Requirements Specification (Phase C)

### 5.1 Требования к данным

| ID | Требование | Приоритет |
|----|------------|-----------|
| DR-01 | PostgreSQL как СУБД | Must |
| DR-02 | Схема через SQL-скрипты, без ORM | Must |
| DR-03 | Поддержка транзакций при регистрации | Must |
| DR-04 | UNIQUE(event_id, email) для запрета дублей | Must |
| DR-05 | Индексы для каталога (фильтрация, сортировка) | Must |
| DR-06 | Изоляция данных по organizer_id | Must |

### 5.2 Требования к приложениям

| ID | Требование | Приоритет |
|----|------------|-----------|
| AR-01 | Backend на ASP.NET Core (.NET 10) | Must |
| AR-02 | Frontend на React + TypeScript + Vite | Must |
| AR-03 | REST API, JSON | Must |
| AR-04 | Аутентификация: JWT или cookie-сессии | Must |
| AR-05 | Роли: organizer, participant, admin | Must |
| AR-06 | CORS для SPA | Must |
| AR-07 | Валидация на backend | Must |
| AR-08 | Обработка ошибок, коды HTTP | Must |

### 5.3 Интеграционные требования

| ID | Требование | Приоритет |
|----|------------|-----------|
| IR-01 | Frontend вызывает Backend API по HTTPS | Must |
| IR-02 | Backend подключается к PostgreSQL | Must |
| IR-03 | Backend отправляет email через SMTP | Must |
| IR-04 | Конфигурация (connection strings, SMTP) через переменные окружения | Must |

---

## 6. Связь с Phase A и Phase B

| Phase A | Phase B | Phase C |
|---------|---------|---------|
| Технологический стек | Business Capabilities | Data Model, Application Components |
| Scope (backend, frontend) | Business Processes | API, модули, сущности |
| Принципы (AP-1–AP-5) | FR/NFR | DR, AR, IR |
| Candidate Roadmap (Phase B) | RC-1–RC-8 | ABB-01–ABB-11, сущности |

---

## 7. Утверждение

| Роль | Статус | Дата |
|------|--------|------|
| Архитектор | Ожидает | — |
| Технический лидер | Ожидает | — |
| Разработчик | Ожидает | — |

---

*Документ подготовлен в формате TOGAF Phase C (Information Systems Architectures) на основе [Phase A](./phase-a-architecture-vision.md) и [Phase B](./phase-b-business-architecture.md).*
