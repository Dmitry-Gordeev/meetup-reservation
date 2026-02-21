# Развёртывание Meetup Reservation

## Вариант 1: Kestrel (по умолчанию)

Backend отдаёт SPA и API из одного процесса. Сборка копирует `frontend/dist` в `wwwroot`, Kestrel раздаёт статику и проксирует SPA-маршруты на `index.html`.

```bash
cd src/backend
dotnet publish -c Release
# Запуск: dotnet MeetupReservation.Api.dll
```

---

## Вариант 2: Nginx как reverse proxy

Nginx раздаёт статику, API проксируется на Kestrel. Подходит для production с SSL и кэшированием.

1. Соберите frontend и backend:
   ```bash
   cd src/backend && dotnet publish -c Release
   ```

2. Скопируйте `src/backend/bin/Release/net10.0/publish/` на сервер (включая `wwwroot`).

3. Пример конфига Nginx (`/etc/nginx/sites-available/meetup-reservation`):

```nginx
server {
    listen 80;
    server_name example.com;

    # Статика SPA
    root /var/www/meetup-reservation/wwwroot;
    index index.html;

    location / {
        try_files $uri $uri/ /index.html;
    }

    # API
    location /api/ {
        proxy_pass http://127.0.0.1:5000;
        proxy_http_version 1.1;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }

    location /health {
        proxy_pass http://127.0.0.1:5000;
    }
}
```

4. Запустите backend на порту 5000 (без раздачи статики — опционально, можно оставить UseStaticFiles, Nginx будет приоритетнее при обращении снаружи).

---

## Вариант 3: Caddy как reverse proxy

Caddy автоматически получает HTTPS-сертификаты (Let's Encrypt).

1. Соберите и разверните приложение аналогично варианту 2.

2. Пример `Caddyfile`:

```
example.com {
    root * /var/www/meetup-reservation/wwwroot

    # API
    handle /api/* {
        reverse_proxy 127.0.0.1:5000
    }
    handle /health {
        reverse_proxy 127.0.0.1:5000
    }

    # SPA fallback
    handle {
        try_files {path} {path}/ /index.html
        file_server
    }
}
```

3. Запуск: `caddy run` или `caddy start`.

---

## Переменные окружения (production)

Backend читает конфигурацию из переменных окружения (приоритет над appsettings). Синтаксис: `Section__Key` (двойное подчёркивание).

| Переменная | Описание |
|------------|----------|
| `ConnectionStrings__DefaultConnection` | Строка подключения к PostgreSQL |
| `Jwt__SecretKey` | Секретный ключ для JWT (обязательно в production) |
| `Jwt__Issuer`, `Jwt__Audience` | Опционально, по умолчанию MeetupReservation |
| `Smtp__Host` | Хост SMTP-сервера |
| `Smtp__Port` | Порт (25, 587 и т.д.) |
| `Smtp__UseSsl` | true/false |
| `Smtp__UserName`, `Smtp__Password` | Учётные данные SMTP |
| `Smtp__From` | Адрес отправителя (noreply@...) |

Пример (Linux/macOS):
```bash
export ConnectionStrings__DefaultConnection="Host=db;Database=meetup;Username=app;Password=secret"
export Jwt__SecretKey="your-secret-key-min-32-characters-long"
export Smtp__Host="smtp.example.com"
export Smtp__Port="587"
export Smtp__UseSsl="true"
export Smtp__UserName="noreply"
export Smtp__Password="secret"
export Smtp__From="noreply@example.com"
```

---

## Frontend: API URL (config.json)

Файл `src/frontend/src/config.json` задаёт базовый URL API. Импортируется при сборке.

| apiBaseUrl | Сценарий |
|------------|----------|
| `""` (пусто) | Production (SPA и API на одном домене) или dev с Vite proxy |
| `"https://api.example.com"` | API на отдельном домене — указать перед `npm run build` |

Пример для cross-origin:
```json
{"apiBaseUrl": "https://api.example.com"}
```

Сборка с правильным URL:
```bash
# Перед сборкой отредактировать config.json при необходимости
cd src/frontend
npm run build
```
