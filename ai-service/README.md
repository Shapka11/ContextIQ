# ContextIQ — Python AI service

Рабочий микросервис FastAPI для университетского проекта: независимый анализ акустических сигналов, определение настроения по русскому тексту/эмодзи и воспроизводимое ранжирование каталога. C# backend, React frontend и БД в этой задаче не создаются. Docker не требуется. Без API-ключа сервис автоматически использует правила Demo Fallback.

## Быстрый запуск в текущем проекте (Windows PowerShell)

PyCharm уже создал и настроил виртуальное окружение `ContextIQ/.venv` с Python **3.14.7**. Зависимости установлены. Из корня репозитория:

```powershell
Set-Location ai-service
$servicePython = (Resolve-Path ..\.venv\Scripts\python.exe).Path
& $servicePython -m uvicorn app.main:app --host 127.0.0.1 --port 8000
```

Откройте `http://127.0.0.1:8000/docs` (Swagger) или `http://127.0.0.1:8000/health`. Остановка — Ctrl+C. Для разработки можно добавить `--reload`.

На другой машине с Python 3.12+ создайте окружение внутри `ai-service/`:

```powershell
Set-Location ai-service
py -3.12 -m venv .venv
$servicePython = (Resolve-Path .\.venv\Scripts\python.exe).Path
& $servicePython -m pip install -r requirements.txt
& $servicePython -m uvicorn app.main:app --host 127.0.0.1 --port 8000
```

Активация необязательна. Если она разрешена настройками PowerShell, выполните `..\.venv\Scripts\Activate.ps1` для текущего окружения или `.\.venv\Scripts\Activate.ps1` для нового. После активации все команды доступны через `python`.

## PyCharm

1. Откройте существующий проект ContextIQ. Текущий интерпретатор — `ContextIQ\.venv\Scripts\python.exe`.
2. Для запуска API создайте Python Run Configuration: **Module name** `uvicorn`, **Parameters** `app.main:app --host 127.0.0.1 --port 8000`, **Working directory** `ContextIQ\ai-service`.
3. Для гарантированной демонстрации без внешних вызовов добавьте Environment variable `EMOTION_MODE=demo`. В обычном режиме `auto` отсутствие ключа также включает demo.
4. Эксперименты запускайте как обычные Python-скрипты `scripts/run_experiment.py` и `scripts/evaluate_ai.py`; пути вычисляются относительно файлов.
5. Для pytest используйте каталог `ai-service/tests`, рабочий каталог `ai-service`, тот же интерпретатор. Пакеты `app` доступны через `pytest.ini`; пометка `ai-service` как Sources Root удобна для подсказок IDE.

## Проверки и эксперименты

Из `ai-service/`, используя `$servicePython` из предыдущих команд:

```powershell
& $servicePython -m pytest -v
& $servicePython scripts/run_experiment.py
& $servicePython scripts/evaluate_ai.py --mode demo
& $servicePython scripts/smoke_api.py
```

После активации достаточно одной команды для всех тестов:

```powershell
python -m pytest -v
```

Фактически выполнено: **69 тестов пройдены**, физический эксперимент на 10 PCM16 WAV, оценка 25 фраз и HTTP-проверка всех endpoint'ов через отдельный процесс Uvicorn. Детали: [технический отчёт](reports/technical_report.md), [тесты](reports/test_results.json), [HTTP](reports/api_smoke.json). При прогоне имеется одно предупреждение DeprecationWarning внутри Starlette/AnyIO; тесты завершаются с кодом 0. Starlette ограничена версией `<1` для совместимости TestClient с требуемым httpx.

Эксперимент записывает синтетические WAV в `reports/synthetic_audio/`, графики в `reports/graphs/`, измерения в `reports/physics_metrics.json`. Повторный запуск обновляет эти результаты. Реальные записи поместите в `data/audio/*.wav`: скрипт проанализирует их отдельно в `realRecordings`. Каталог исходных записей не изменяется. Сейчас реальных записей нет.

Оценка текста сохраняется в `reports/ai_metrics.json`, матрица ошибок — `reports/graphs/confusion_matrix.png`. Показатели вычисляются по фактическим ответам; при ошибке провайдера скрипт завершается с ошибкой и не заменяет её выдуманным ответом. Время — локальный вызов анализатора, без HTTP. Небольшая демонстрационная выборка размечена предварительно и требует ручной проверки; составление вместе с правилами ограничивает независимость оценки.

`smoke_api.py` запускает Uvicorn на свободном локальном порту, вызывает API через TCP, проверяет ответы, сохраняет OpenAPI в `reports/openapi.json` и останавливает свой сервер. Перед ним выполните физический эксперимент: используется созданный WAV.

## API для C#

JSON имеет поля camelCase, UTF-8, конечные числовые значения и явные enum. Спецификация доступна по `/openapi.json`. Swagger — `/docs`. Неизвестные поля JSON отклоняются.

| Метод и путь | Вход | Ответ |
|---|---|---|
| GET `/health` | — | `status`, `textAnalysisMode`, `version` |
| POST `/internal/analyze` | `text`, `emoji` (хотя бы одно непустое) | `mood`, `energy`, `valence`, `themes`, `confidence` |
| POST `/internal/audio-features` | multipart, поле `file`, PCM WAV | `durationSeconds`, `sampleRate`, `rms`, `peakAmplitude`, `dominantFrequencyHz`, `spectralCentroidHz` |
| POST `/internal/rank` | `analysis`, `preferences`, `candidates` | массив до 5 объектов: `id`, `score`, `components`, `explanation` |

Настроения: `happy`, `sad`, `stressed`, `excited`, `neutral`. Категории: `movie`, `music`, `book`, `activity`. Режим текста указан в `/health` и заголовке `X-Analysis-Mode`. Ошибки: 422 — невалидный JSON/WAV, 413 — слишком большой файл, 502 — ошибка OpenAI/отказ/невалидный структурированный результат. Меньше пяти кандидатов — возвращаются все, пустой каталог — `[]`, повторяющиеся ID — 422.

Реальный запрос PowerShell (в отдельном терминале при запущенном сервисе):

```powershell
$base = 'http://127.0.0.1:8000'
Invoke-RestMethod "$base/health"
$body = @{ text = 'Я устал после университета, хочу отдохнуть под Человека-паука'; emoji = '😴' } | ConvertTo-Json
$analysis = Invoke-RestMethod "$base/internal/analyze" -Method Post `
    -ContentType 'application/json; charset=utf-8' -Body ([Text.Encoding]::UTF8.GetBytes($body))
$analysis | ConvertTo-Json
```

Фактический ответ при проверке в demo:

```json
{"mood":"stressed","energy":0.2,"valence":-0.4,"themes":["spider-man","relax"],"confidence":0.825}
```

Загрузка WAV из `ai-service/`:

```powershell
curl.exe -X POST 'http://127.0.0.1:8000/internal/audio-features' `
    -F 'file=@reports/synthetic_audio/sine_440Hz_A0.5.wav;type=audio/wav'
```

Ранжирование предоставленного каталога:

```powershell
$candidates = Get-Content data/catalog.json -Raw -Encoding UTF8 | ConvertFrom-Json
$rankBody = @{
    analysis = $analysis
    preferences = @{ categories = @('music'); tags = @('relax'); seenIds = @() }
    candidates = @($candidates)
} | ConvertTo-Json -Depth 10
Invoke-RestMethod "$base/internal/rank" -Method Post `
    -ContentType 'application/json; charset=utf-8' -Body ([Text.Encoding]::UTF8.GetBytes($rankBody))
```

## Режимы анализа текста

`.env` необязателен. При необходимости скопируйте `.env.example` в `.env`; существующий файл не перезаписывайте. Переменные окружения приоритетнее `.env`.

| Переменная | По умолчанию | Смысл |
|---|---|---|
| `EMOTION_MODE` | `auto` | `auto`: OpenAI при наличии ключа, иначе demo; `demo`: всегда правила; `openai`: обязателен ключ |
| `OPENAI_API_KEY` | пусто | Секрет только в окружении/локальном `.env` |
| `OPENAI_MODEL` | `gpt-4o-mini` | Модель с поддержкой Structured Outputs, доступная аккаунту |
| `OPENAI_TIMEOUT_SECONDS` | `20` | Таймаут SDK, повторы отключены |
| `MAX_AUDIO_BYTES` | `20971520` | До 20 MiB на файл |
| `MAX_AUDIO_SECONDS` | `120` | Длительность до двух минут |

Demo — детерминированные правила по словам и эмодзи, локальное подавление отрицаний и словарь тем. Это **не обученная нейросеть**. `confidence` — эвристическая сила свидетельств (0.35 без найденных правил, до 0.85), не статистически калиброванная вероятность. При равных оценках настроений порядок разрешения: happy, sad, stressed, excited. Ирония, сложные отрицания и неизвестные культурные отсылки могут распознаваться неверно.

OpenAI использует `AsyncOpenAI.responses.parse` и схему Pydantic с повторной локальной валидацией; `store=False`. Подход соответствует [official OpenAI documentation — Structured Outputs](https://developers.openai.com/api/docs/guides/structured-outputs). При настроенном ключе ошибка API возвращается как 502, чтобы режим не менялся незаметно. Сетевые вызовы OpenAI в этой проверке не выполнялись; ветвь проверена заглушками успешного ответа, отказа и неправильной структуры. Для отдельного исследования с настроенным ключом: `& $servicePython scripts/evaluate_ai.py --mode openai`.

## Физика и формулы

Обозначения: `x[n]` — нормированный цифровой отсчёт, `N` — число кадров mono, `fs` — частота дискретизации (Гц), `t[n]=n/fs` (с). Длительность `T=N/fs`; последний отсчёт имеет время `(N-1)/fs`.

- Signed PCM: `x=q/2^(b-1)`. Unsigned PCM8: `x=(q-128)/128`. PCM24 SciPy хранит со сдвигом в int32, поэтому используется деление на `2^31`. Stereo: `(L+R)/2` после перехода к float64. Сигналы в противофазе могут подавиться.
- `RMS=sqrt(sum(x[n]^2)/N)`, `Peak=max(abs(x[n]))`. Для целого числа периодов `x=A*sin(2*pi*f*t)` теоретически `RMS=A/sqrt(2)`.
- Перед FFT вычитается среднее: `y=x-mean(x)`. Периодическое окно Hann `w[n]=0.5-0.5*cos(2*pi*n/N)` при `N>=3`, иначе прямоугольное. `X[k]=sum(y[n]*w[n]*exp(-2*pi*i*k*n/N))`.
- Односторонняя амплитуда: `a[k]=2*abs(X[k])/sum(w)`, без удвоения DC и частоты Найквиста. DC исключён из тональных показателей. Частоты `f[k]=k*fs/N`. Доминирующая частота — частота максимума `a[k]`.
- Амплитудный спектральный центроид `C=sum(f[k]*a[k])/sum(a[k])`. Это характеристика распределения спектра, не оценка эмоций и не всегда основная частота голоса.
- STFT вычисляет оконную FFT в последовательных кадрах: `Z[m,k]=sum(x[n+mH]*w[n]*exp(-2*pi*i*k*n/L))`. Здесь `L=min(1024,N)`, минимум 2, `noverlap=L//2`, `H=L-noverlap`. SciPy `scaling='spectrum'` нормирует на сумму окна. Односторонняя амплитуда переводится в `20*log10(max(a,1e-6))`, нижняя граница −120 dBFS. Дополнение нулями обеспечивает обработку краёв и коротких сигналов.

Шкала FS безразмерная: амплитуды относительно цифрового полного масштаба. Без калибровки микрофона их нельзя называть давлением в Па или уровнем звукового давления в dB SPL. Нормализация не усиливает тихие записи. RMS/Peak сохраняют DC; FFT его убирает. Если переменная составляющая не превосходит `1e-12 FS`, частота и центроид возвращаются как 0 — договорённость для отсутствующего тона. Пустой WAV отклоняется; один отсчёт обрабатывается, но частотная интерпретация неинформативна.

Разрешение FFT `deltaF=fs/N=1/T`. Для стационарной изолированной синусоиды вдали от DC/Найквиста выбор ближайшего бина обычно даёт погрешность до `deltaF/2`; это не универсальная гарантия для шумов, голоса или коротких записей. В эксперименте `deltaF=0.5 Гц`. Нулевые ошибки частоты у 440/880 Гц объясняются точным попаданием в сетку FFT.

WAV: little-endian RIFF/WAVE, integer PCM 8/16/24/32, mono/stereo, частота 1..192000 Гц. MP3, big-endian RIFX, IEEE float WAV и сжатые кодеки не поддерживаются. Для числовых массивов поддержаны float в диапазоне [-1,1]. Метаданные, длина RIFF, число прочитанных кадров и конечность отсчётов проверяются.

## Рекомендации

`Score = 0.4*moodMatch + 0.3*preferenceMatch + 0.2*contextMatch + 0.1*novelty`.

Все компоненты в [0,1]:

- `moodMatch`: 1, если настроение пользователя входит в `item.moods`, иначе 0. Разметка каталога — предварительные ручные характеристики.
- `preferenceMatch`: среднее доступных оценок категории (0/1) и доли пользовательских тегов, найденных у кандидата. Если предпочтений нет — нейтральные 0.5.
- `contextMatch`: среднее `1-abs(userEnergy-itemEnergy)` и доли тем пользователя в тегах кандидата. При отсутствии тем эта часть равна 0.5.
- `novelty`: 0 для ID из `seenIds`, иначе 1; без истории все считаются новыми. Это новизна относительно переданной истории, не абсолютная новизна контента.

Теги приводятся к casefold, пробелы по краям убираются, дубликаты считаются один раз. Итог округляется до 8 знаков; сортировка по убыванию score, затем по ID лексикографически. Случайность отсутствует. `valence` и `confidence` сохраняются в контракте анализа, но не входят в заданную формулу.

Каталог содержит реальные произведения и общие идеи домашних/повседневных занятий. URL, расписания и объявления о реальных мероприятиях не создаются. Наличие контента на конкретной платформе не проверяется.

## Структура и дальнейшая интеграция

`app/main.py` — HTTP; `schemas.py` — контракты; `config.py` — окружение; `physics/` — DSP и графики; `services/emotion.py` — правила/OpenAI; `services/recommender.py` — ранжирование; `scripts/` — измерения и HTTP-проверка; `tests/` — pytest; `data/` — входные данные; `reports/` — фактические результаты.

ASP.NET Core сможет использовать `HttpClient`: JSON UTF-8 для analyze/rank, `MultipartFormDataContent` с полем `file` для WAV, `System.Text.Json` с camelCase. Сервис рассчитан на вызовы внутренним backend'ом; сейчас слушает loopback. Дальнейшие задачи: C# DTO/клиент и авторизация внутренних вызовов, PostgreSQL-каталог и история просмотров, frontend, независимая ручная разметка текста, реальные голосовые записи с согласием участников и проверка OpenAI с доступным ключом. Акустические признаки сами по себе не доказывают эмоции.

## Источники

- [SciPy: чтение WAV и типы PCM](https://docs.scipy.org/doc/scipy/reference/generated/scipy.io.wavfile.read.html).
- [NumPy: определение DFT и частотная сетка](https://numpy.org/doc/stable/reference/routines.fft.html).
- [SciPy: STFT, окна и нормировка](https://docs.scipy.org/doc/scipy/reference/generated/scipy.signal.stft.html). Используется доступная legacy-функция; миграция на ShortTimeFFT возможна без изменения внешнего контракта.
