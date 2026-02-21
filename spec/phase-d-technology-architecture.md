# Phase D: Technology Architecture

**Проект:** Meetup Reservation (монорепозиторий)  
**Методология:** TOGAF ADM  
**Версия:** 1.0  
**Зависимости:** [Phase A: Architecture Vision](./phase-a-architecture-vision.md), [Phase B: Business Architecture](./phase-b-business-architecture.md), [Phase C: Information Systems Architectures](./phase-c-information-systems-architecture.md)

---

## 1. Цели и область Phase D

### 1.1 Цели

- Разработать **Target Technology Architecture** (целевую технологическую архитектуру), обеспечивающую поддержку Data и Application архитектур (Phase C).
- Описать **Baseline Technology Architecture** — текущее технологическое состояние.
- Определить технологический стек, стандарты и инфраструктурные компоненты.
- Выполнить gap-анализ и определить компоненты Technology Architecture для Architecture Roadmap.
- Уточнить технологические требования (Architecture Requirements Specification).

### 1.2 Область (из Phase A/B/C)

- **Backend:** ASP.NET Core (.NET 10), PostgreSQL, SQL-скрипты (без Entity Framework).
- **Frontend:** React + TypeScript + Vite.
- **Монорепозиторий:** backend и frontend в одном репозитории.
- **Развёртывание и хостинг:** пока не определено (self-hosted / cloud / SaaS — решение в последующих фазах).

---

## 2. Baseline Technology Architecture

### 2.1 Описание

Проект **greenfield**: до внедрения Meetup Reservation специализированной технологической платформы нет. Организация мероприятий использует разрозненные инструменты (таблицы, формы, email) без единой инфраструктуры.

| Аспект | Baseline (текущее состояние) |
|--------|-----------------------------|
| **Серверы и runtime** | Нет выделенных серверов для платформы |
| **СУБД** | Нет централизованной БД |
| **Middleware** | Нет |
| **Сеть** | Общая корпоративная/домашняя сеть |
| **Развёртывание** | Нет CI/CD, нет контейнеризации |
| **Мониторинг** | Нет |
| **Резервное копирование** | Нет |

### 2.2 Ключевые ограничения Baseline

- Отсутствие единой технологической платформы.
- Нет автоматизации развёртывания.
- Нет формализованных технологических стандартов.

---

## 3. Target Technology Architecture

### 3.1 Обзор

Целевая технологическая архитектура обеспечивает выполнение требований Phase A/B/C и поддержку компонентов Application Architecture (ABB-01–ABB-11).

### 3.2 Platform Decomposition Diagram

```
┌─────────────────────────────────────────────────────────────────────────────────────┐
│                         КЛИЕНТСКИЙ УРОВЕНЬ (Client Tier)                             │
│  ┌─────────────────────────────────────────────────────────────────────────────┐   │
│  │  Браузер (Chrome, Firefox, Edge, Safari)                                     │   │
│  │  - React SPA (Vite build)                                                     │   │
│  │  - HTTPS, REST API calls                                                      │   │
│  └─────────────────────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────────────────────┘
                                          │ HTTPS
                                          ▼
┌─────────────────────────────────────────────────────────────────────────────────────┐
│                         УРОВЕНЬ ПРИЛОЖЕНИЙ (Application Tier)                        │
│  ┌─────────────────────────────────────────────────────────────────────────────┐   │
│  │  Web Server / Reverse Proxy                                                   │   │
│  │  - Статика (SPA), проксирование API                                          │   │
│  │  - Nginx / Caddy / IIS / встроенный Kestrel (зависит от варианта развёртывания)│   │
│  └─────────────────────────────────────────────────────────────────────────────┘   │
│                                          │                                           │
│  ┌─────────────────────────────────────────────────────────────────────────────┐   │
│  │  Backend Runtime                                                              │   │
│  │  - ASP.NET Core (.NET 10)                                                    │   │
│  │  - Kestrel HTTP Server                                                       │   │
│  │  - Модули: Auth, Events, Registrations, Notifications, Admin, Export          │   │
│  └─────────────────────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────────────────────┘
                                          │
                    ┌─────────────────────┼─────────────────────┐
                    ▼                     ▼                     ▼
┌──────────────────────────┐  ┌──────────────────────────┐  ┌──────────────────────────┐
│  УРОВЕНЬ ДАННЫХ          │  │  ВНЕШНИЕ СЕРВИСЫ          │  │  ФАЙЛОВОЕ ХРАНИЛИЩЕ      │
│  (Data Tier)             │  │  (External Services)      │  │  (в первой версии — БД)   │
│  ┌────────────────────┐   │  │  ┌────────────────────┐   │  │  BLOB в PostgreSQL       │
│  │  PostgreSQL        │   │  │  │  SMTP Server        │   │  │  (фото, вложения, аватар)│
│  │  - Схема SQL       │   │  │  │  (Email)           │   │  └──────────────────────────┘
│  │  - Транзакции      │   │  │  └────────────────────┘   │
│  │  - Индексы         │   │  └──────────────────────────┘
│  └────────────────────┘   │
└──────────────────────────┘
```

### 3.3 Technology Components Catalog

| ID | Компонент | Технология | Версия (целевая) | Описание |
|----|-----------|------------|------------------|----------|
| TC-01 | **Backend Runtime** | .NET / ASP.NET Core | 10 | HTTP API, Kestrel, middleware |
| TC-02 | **Backend Language** | C# | 14 | Язык разработки backend |
| TC-03 | **СУБД** | PostgreSQL | 16+ | Хранение данных, транзакции |
| TC-04 | **Data Access** | ADO.NET / Dapper | — | Доступ к БД без ORM |
| TC-05 | **Frontend Framework** | React | 18+ | SPA, компонентный UI |
| TC-06 | **Frontend Language** | TypeScript | 5+ | Типизация |
| TC-07 | **Build Tool** | Vite | 5+ | Сборка frontend |
| TC-08 | **Auth** | JWT (System.IdentityModel.Tokens.Jwt) | — | Токены, Bearer |
| TC-09 | **Email** | SMTP | — | Отправка писем (MailKit / SmtpClient) |
| TC-10 | **Export** | Библиотека Excel/PDF (ClosedXML, QuestPDF и т.п.) | — | Генерация отчётов |
| TC-11 | **Reverse Proxy / Web Server** | Nginx / Caddy / IIS | — | Статика, проксирование (зависит от развёртывания) |
| TC-12 | **Version Control** | Git | — | Монорепозиторий |

### 3.4 Technology Standards Catalog

| ID | Стандарт | Описание | Источник |
|----|----------|----------|----------|
| TS-01 | **API версионирование** | Префикс `/api/v1/` для всех endpoints | Phase C |
| TS-02 | **Пагинация** | Cursor-based (cursor, limit, nextCursor) | Phase C |
| TS-03 | **Аутентификация** | JWT в заголовке `Authorization: Bearer <token>` | Phase C |
| TS-04 | **Формат данных** | JSON для запросов и ответов | Phase C |
| TS-05 | **Дата/время** | UTC в БД и API; ISO 8601 в JSON | Phase C |
| TS-06 | **Конфигурация** | Переменные окружения (connection strings, SMTP) | Phase C |
| TS-07 | **Схема БД** | SQL-скрипты, без ORM | Phase A, C |
| TS-08 | **Хранение файлов** | BLOB (bytea) в PostgreSQL в первой версии | Phase C |
| TS-09 | **HTTPS** | Обязательно в production | NFR |
| TS-10 | **CORS** | Настройка для SPA | Phase C |

### 3.5 Technology / Application Matrix

| Application Building Block | TC-01 .NET | TC-03 PostgreSQL | TC-04 Dapper | TC-05 React | TC-07 Vite | TC-08 JWT | TC-09 SMTP | TC-10 Export |
|---------------------------|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|
| ABB-01 Frontend SPA | | | | ● | ● | ● | | |
| ABB-02 Backend API | ● | | | | | | | |
| ABB-03 Auth Module | ● | ● | ● | | | ● | | |
| ABB-04 Events Module | ● | ● | ● | | | ● | | |
| ABB-05 Registrations Module | ● | ● | ● | | | ● | | |
| ABB-06 Notifications Module | ● | | | | | | ● | |
| ABB-07 Admin Module | ● | ● | ● | | | ● | | |
| ABB-08 Export Module | ● | ● | ● | | | | | ● |
| ABB-09 Data Access | ● | ● | ● | | | | | |
| ABB-10 Database | | ● | | | | | | |
| ABB-11 Email Service | ● | | | | | | ● | |

---

## 4. Deployment Architecture

### 4.1 Варианты развёртывания (решение в последующих фазах)

Согласно Phase A, хостинг и способ развёртывания пока не определены. Ниже — типовые варианты для целевой архитектуры.

| Вариант | Описание | Компоненты |
|---------|----------|-------------|
| **Self-hosted (VM)** | Развёртывание на собственных или арендованных виртуальных машинах | Linux/Windows VM, Nginx/Caddy, .NET runtime, PostgreSQL, SMTP (внешний или локальный) |
| **Cloud (IaaS/PaaS)** | Azure, AWS, GCP, Yandex Cloud и т.п. | App Service / EC2 / Compute Engine, Managed PostgreSQL, SMTP-сервис |
| **Container (Docker)** | Контейнеризация для гибкого развёртывания | Docker, docker-compose; образы backend, frontend (статический хостинг), PostgreSQL |
| **SaaS** | Готовые платформы (Heroku, Railway, Render и т.п.) | Managed runtime, managed DB, add-on SMTP |

### 4.2 Минимальные требования к окружению

| Ресурс | Минимум (оценка) | Рекомендация |
|--------|------------------|--------------|
| **Backend (CPU/RAM)** | 1 vCPU, 512 MB RAM | 2 vCPU, 2 GB RAM |
| **PostgreSQL** | 1 vCPU, 1 GB RAM, 10 GB disk | 2 vCPU, 2 GB RAM, 20 GB disk |
| **Frontend** | Статический хостинг (CDN/общий web server) | — |
| **Сеть** | HTTPS, исходящий SMTP (порт 25/587) | — |

### 4.3 Environments and Locations (концептуально)

```
┌─────────────────────────────────────────────────────────────────┐
│  Development                                                    │
│  - Локальная разработка: dotnet run, npm run dev                │
│  - Локальный PostgreSQL или Docker                              │
│  - SMTP: MailHog / Ethereal / заглушка                          │
└─────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────┐
│  Staging (опционально)                                           │
│  - Тестовое окружение, приближённое к production                 │
│  - Отдельная БД, тестовый SMTP                                   │
└─────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────┐
│  Production                                                      │
│  - Целевое окружение (вариант развёртывания TBD)                 │
│  - HTTPS, резервное копирование БД, мониторинг                   │
└─────────────────────────────────────────────────────────────────┘
```

---

## 5. Gap Analysis

### 5.1 Technology Architecture Gaps

| # | Baseline | Target | Gap |
|---|----------|--------|-----|
| T1 | Нет runtime | ASP.NET Core (.NET 10) | Установка .NET SDK/runtime |
| T2 | Нет СУБД | PostgreSQL | Установка и настройка PostgreSQL |
| T3 | Нет frontend-сборки | React + Vite | Установка Node.js, npm/pnpm |
| T4 | Нет инфраструктуры | Web server, reverse proxy | Выбор и настройка (Nginx/Caddy/IIS) |
| T5 | Нет SMTP | Email-сервис | Настройка SMTP (внешний провайдер или локальный) |
| T6 | Нет развёртывания | CI/CD, контейнеры (опционально) | Решение в последующих фазах |
| T7 | Нет мониторинга | Логирование, метрики | Решение в последующих фазах |

### 5.2 Выводы

- Проект greenfield: все технологические компоненты создаются с нуля.
- Критичные зависимости: .NET 10, PostgreSQL, Node.js (для сборки frontend), SMTP.
- Развёртывание, мониторинг и резервное копирование — в scope последующих фаз.

---

## 6. Architecture Requirements Specification (Phase D)

### 6.1 Технологические требования

| ID | Требование | Приоритет | Источник |
|----|------------|-----------|----------|
| TR-01 | Backend на ASP.NET Core (.NET 10) | Must | AP-1, AR-01 |
| TR-02 | PostgreSQL 16+ как СУБД | Must | AP-2, DR-01 |
| TR-03 | Доступ к БД через ADO.NET/Dapper, без ORM | Must | AP-3, DR-02 |
| TR-04 | Frontend на React + TypeScript + Vite | Must | AP-4, AR-02 |
| TR-05 | Node.js для сборки frontend (build-time) | Must | — |
| TR-06 | JWT для аутентификации | Must | AR-04 |
| TR-07 | SMTP для отправки email | Must | IR-03 |
| TR-08 | Конфигурация через переменные окружения | Must | IR-04 |
| TR-09 | HTTPS в production | Must | NFR-02 |
| TR-10 | Поддержка основных браузеров (Chrome, Firefox, Edge, Safari) | Must | NFR-03 |
| TR-11 | Монорепозиторий (Git) | Must | AP-5 |
| TR-12 | Резервное копирование БД | Should | NFR-02 |
| TR-13 | Логирование ошибок и запросов | Should | Поддерживаемость |

### 6.2 Инфраструктурные требования

| ID | Требование | Приоритет |
|----|------------|-----------|
| IR-T01 | Доступность PostgreSQL для backend | Must |
| IR-T02 | Исходящий доступ к SMTP (порт 25/587) | Must |
| IR-T03 | Возможность обслуживания HTTPS | Must |
| IR-T04 | Размещение статики frontend (SPA) | Must |

### 6.3 Сводка требований из Phase B и Phase C

Технологическая архитектура должна удовлетворять:

- **Phase B:** FR-01–FR-09, NFR-01–NFR-05.
- **Phase C:** DR-01–DR-08, AR-01–AR-10, IR-01–IR-04.

---

## 7. Technology Architecture Components of Architecture Roadmap

Компоненты для включения в Architecture Roadmap (Phase E):

| # | Компонент | Описание | Зависимости |
|---|------------|----------|-------------|
| TRC-1 | **Technology Stack Setup** | Установка .NET 10, PostgreSQL, Node.js; создание монорепозитория | — |
| TRC-2 | **Database Infrastructure** | PostgreSQL: установка, создание схемы (SQL-скрипты), индексы | TRC-1 |
| TRC-3 | **Backend Runtime** | ASP.NET Core приложение, Kestrel, модули | TRC-1 |
| TRC-4 | **Frontend Build** | React + Vite, сборка статики | TRC-1 |
| TRC-5 | **Email Infrastructure** | Настройка SMTP (провайдер или локальный сервис) | TRC-1 |
| TRC-6 | **Web Server / Reverse Proxy** | Раздача статики, проксирование API | TRC-3, TRC-4 |
| TRC-7 | **Deployment Pipeline** | CI/CD, выбор варианта развёртывания (TBD) | TRC-1–TRC-6 |

---

## 8. Связь с Phase A, B и C

| Phase | Артефакт | Связь с Phase D |
|-------|----------|-----------------|
| **Phase A** | Принципы AP-1–AP-5, Scope, технологические ограничения | Technology Standards (TS-07), Technology Components (TC-01–TC-12) |
| **Phase B** | Business Capabilities, FR/NFR, Candidate Roadmap | TR-01–TR-13, IR-T01–IR-T04 |
| **Phase C** | Data Architecture (PostgreSQL, сущности), Application Architecture (ABB-01–ABB-11), DR/AR/IR | Platform Decomposition, Technology/Application Matrix, TR, IR-T |

---

## 9. Утверждение

| Роль | Статус | Дата |
|------|--------|------|
| Архитектор | Ожидает | — |
| Технический лидер | Ожидает | — |
| DevOps / Инфраструктура | Ожидает | — |

---

*Документ подготовлен в формате TOGAF Phase D (Technology Architecture) на основе [Phase A](./phase-a-architecture-vision.md), [Phase B](./phase-b-business-architecture.md) и [Phase C](./phase-c-information-systems-architecture.md). Следующий этап: Phase E (Opportunities and Solutions) — формирование Architecture Roadmap и планов миграции.*
