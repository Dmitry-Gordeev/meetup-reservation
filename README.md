# meetup-reservation

Веб-приложение для организации, регистрации и управления событиями, таким как конференции, вебинары, мастер-классы или концерты.

## Запуск

```bash
cd src/backend
dotnet run
```

Frontend собирается автоматически при `dotnet build` и копируется в `wwwroot`. API: `http://localhost:5000` (или порт из конфигурации).

## Развёртывание

Варианты раздачи статики и reverse proxy (Nginx, Caddy) описаны в [docs/deployment.md](docs/deployment.md).

## Тестирование

Чек-лист ручного тестирования по FR-01–FR-09: [docs/testing-checklist.md](docs/testing-checklist.md).

## Спецификации

Проект — демонстрационный: одна и та же система описана тремя способами разной строгости.

| Подход | Где лежит | Что описывает |
|---|---|---|
| TOGAF ADM | [spec/](spec/) | Полный цикл: видение (A), бизнес-возможности (B), данные и приложения (C), технологии (D), план миграции и work packages (E–F) |
| Spec Kit | [.specify/memory/constitution.md](.specify/memory/constitution.md), [specs/001-qr-checkin/](specs/001-qr-checkin/) | Конституция проекта и одна новая функция: чек-ин по QR-коду (`spec.md → plan.md → tasks.md`) |
| OpenSpec | [openspec/specs/](openspec/specs/), [openspec/changes/waitlist/](openspec/changes/waitlist/) | Текущее состояние по десяти бизнес-возможностям из Phase B и изменение «лист ожидания» с дельтами к спецификациям |

Файлы Spec Kit и OpenSpec созданы вручную по формату инструментов. Чтобы подключить команды агента (`/speckit.*`, `/opsx:*`), выполните `specify init --here --integration <agent>` и `openspec init` — существующие спецификации они не перезаписывают.
