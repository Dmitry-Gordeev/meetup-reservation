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
