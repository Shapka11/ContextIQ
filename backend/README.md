# ContextIQ Backend

ASP.NET Core Minimal API на .NET 10: гостевые сессии, профили, история, рекомендации и интеграция с Python.

## Запуск

Откройте `ContextIQ.sln` в Rider и запустите `ContextIQ.Api` с профилем `http`.

Или из папки `backend`:

```powershell
dotnet restore
dotnet build
dotnet run --project .\ContextIQ.Api --launch-profile http
```

Swagger: http://localhost:5000/swagger. В Development используется SQLite, файл `ContextIQ.Api/contextiq.db`. Миграции и импорт `ai-service/data/catalog.json` выполняются при запуске. Python запускается отдельно по инструкции в `ai-service/README.md`.

## Настройки

Переменные окружения переопределяют appsettings. `.env.example` — пример значений; .NET не загружает этот файл автоматически.

| Переменная | Значение |
|---|---|
| `PythonAi__BaseUrl` | `http://localhost:8000` |
| `PythonAi__TimeoutSeconds` | `30` |
| `Database__Provider` | `Sqlite` или `PostgreSQL` |
| `ConnectionStrings__Database` | Строка подключения выбранного провайдера |
| `Database__AutoMigrate` | `true` |
| `Sessions__LifetimeDays` | `30` |

Для PostgreSQL в Development задайте оба параметра:

```powershell
$env:Database__Provider = 'PostgreSQL'
$env:ConnectionStrings__Database = 'Host=localhost;Port=5432;Database=contextiq;Username=contextiq;Password=your-password'
dotnet run --project .\ContextIQ.Api --launch-profile http
```

PostgreSQL можно запустить через `compose.yaml`, предварительно задав `CONTEXTIQ_DB_PASSWORD`. Для изменения миграций выполните `dotnet tool restore`; у `SqliteAppDbContext` и `PostgresAppDbContext` отдельные миграции.

## API

Создайте сессию через `POST /api/sessions/guest`. Передавайте возвращённый `token` в заголовке `Authorization: Bearer <token>`. В Swagger вставьте токен в Authorize.

| Метод | Путь | Назначение |
|---|---|---|
| POST | `/api/sessions/guest` | Создать гостя |
| GET / PATCH / DELETE | `/api/profile` | Профиль и удаление данных |
| POST | `/api/checkins` | Анализ настроения и рекомендации |
| GET | `/api/history?page=1&pageSize=20` | История настроений |
| POST | `/api/feedback` | Оценка рекомендации: 1 или -1 |
| POST | `/api/audio/analyze` | Multipart поле `file`, PCM WAV до 20 MiB и 120 секунд |
| GET | `/api/catalog` | Общий каталог |
| GET | `/api/health` | Состояние БД и Python |

Пример check-in:

```json
{"text":"Я устал, хочу отдохнуть","emoji":"😴"}
```

Ошибки возвращаются в формате ProblemDetails. При недоступности Python анализ возвращает 503. `analysisMode` показывает режим Python: `demo` или `openai`.

Примеры запросов для Rider: `ContextIQ.Api/ContextIQ.Api.http`. WAV можно загрузить через Swagger. CORS разрешён для React на `http://localhost:5173`.

## Тесты

```powershell
dotnet test
```

Используются отдельная SQLite и подставной Python API, без LLM-вызовов.

## Docker

Общий стек (React + backend + Python + PostgreSQL) из корня: `docker compose up --build`.
Backend доступен на порту 5000, внутри Docker — 8080. Смотрите [корневой README](../README.md).
Локальный `backend/compose.yaml` оставлен для отдельной разработки backend с PostgreSQL 18; общий проверенный стек использует PostgreSQL 17 и отдельный volume.

Из корня репозитория:

```powershell
docker build -f backend/Dockerfile -t contextiq-backend .
```

Контейнер слушает порт 8080. Подключение PostgreSQL и адрес Python задаются переменными окружения. Docker необязателен для разработки.
