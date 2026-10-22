# ContextIQ AI service

FastAPI-сервис для анализа настроения, ранжирования рекомендаций и физического анализа PCM WAV. Каталог: `data/catalog.json`.

## Запуск

Из папки `ai-service`, с существующим окружением репозитория:

```powershell
$env:EMOTION_MODE = 'demo'
& ..\.venv\Scripts\python.exe -m uvicorn app.main:app --host 127.0.0.1 --port 8000
```

Если окружения нет:

```powershell
py -m venv .venv
& .\.venv\Scripts\python.exe -m pip install -r requirements.txt
& .\.venv\Scripts\python.exe -m uvicorn app.main:app --host 127.0.0.1 --port 8000
```

Swagger: http://127.0.0.1:8000/docs. Схема API: http://127.0.0.1:8000/openapi.json.

## Настройки

Настройки загружаются из окружения и локального `.env`. Пример — `.env.example`.

| Переменная | По умолчанию | Назначение |
|---|---|---|
| `EMOTION_MODE` | `auto` | `demo`: правила; `openai`: LLM; `auto`: OpenAI при наличии ключа, иначе demo |
| `OPENAI_API_KEY` | Пусто | Ключ OpenAI |
| `OPENAI_MODEL` | `gpt-4o-mini` | Модель анализа |
| `OPENAI_TIMEOUT_SECONDS` | `20` | Таймаут провайдера |
| `MAX_AUDIO_BYTES` | `20971520` | Максимум 20 MiB |
| `MAX_AUDIO_SECONDS` | `120` | Максимальная длительность |

Demo — детерминированные правила, а не обученная модель. При сбое настроенного OpenAI сервис возвращает ошибку, без переключения на demo. Режим виден в `/health` и заголовке `X-Analysis-Mode`.

## API

JSON использует camelCase; неизвестные поля отклоняются.

| Метод | Путь | Вход | Ответ |
|---|---|---|---|
| GET | `/health` | — | status, textAnalysisMode, version |
| POST | `/internal/analyze` | text, emoji | mood, energy, valence, themes, confidence |
| POST | `/internal/rank` | analysis, preferences, candidates | До 5 результатов: id, score, components, explanation |
| POST | `/internal/audio-features` | Multipart поле file | durationSeconds, sampleRate, rms, peakAmplitude, dominantFrequencyHz, spectralCentroidHz |

Для анализа нужно хотя бы одно непустое поле: text до 8000 символов или emoji до 100. Настроения: happy, sad, stressed, excited, neutral. Категории: movie, music, book, activity.

Ranking принимает `preferences: {categories, tags, seenIds}` и кандидатов с полями `id`, `title`, `category`, `tags`, `moods`, `energy`.

WAV: little-endian RIFF, integer PCM 8/16/24/32 bit, mono/stereo, до 120 секунд и 20 MiB. Акустические характеристики не определяют эмоции по голосу.

Ошибки: 422 — неверные данные или WAV, 413 — превышение размера, 502 — ошибка текстового провайдера.

## Тесты

Из папки `ai-service`:

```powershell
& ..\.venv\Scripts\python.exe -m pytest
```

Для локального окружения замените путь на `.\.venv\Scripts\python.exe`. Тесты не обращаются к реальному LLM API.
