# Axiom — главный навигационный индекс документации

Единая входная точка: отсюда за несколько шагов можно дойти до любого ответа о проекте — что реализовано, что запланировано, где хвосты, где история. Для человека и для агента.

## 1. Быстрые ответы

| Вопрос | Куда идти |
| --- | --- |
| Что это за проект? | [`PROJECT.md`](../../../../PROJECT.md) |
| Как собрать, запустить, открыть IDE? | [`README.md`](../../../../README.md) |
| Как агенту правильно работать с репозиторием? | [`AGENTS.md`](../../../../AGENTS.md) |
| Что сейчас реализовано (точный договор языка)? | [`docs/SPECIFICATION3.md`](../../../SPECIFICATION3.md) |
| Как устроена архитектура? | [`docs/ARCHITECTURE.md`](../../../ARCHITECTURE.md) |
| Что делать дальше? | [`docs/PLANS4.md`](../../../PLANS4.md) |
| Что делалось в последних сессиях? | [`docs/WORKLOG5.md`](../../../WORKLOG5.md) |
| Что запланировано, но ещё не сделано? | [`docs/archive/root-documents-20261011/FUTURE.md`](../../root-documents-20261011/FUTURE.md) + [`docs/archive/meta-20260826/FEATURE_LEDGER.md`](../../meta-20260826/FEATURE_LEDGER.md) |
| Где незакрытые хвосты? | [`docs/archive/meta-20260826/FEATURE_LEDGER.md`](../../meta-20260826/FEATURE_LEDGER.md) §«Открытые хвосты» |
| Где исторические документы? | [`docs/archive/meta-20260826/DOC_REGISTRY.md`](../../meta-20260826/DOC_REGISTRY.md) зона 5 |
| Как пользоваться языком (справочник)? | [`docs/manual/index.html`](../../../manual/index.html) |

## 2. Карта документации по зонам

```
docs/
├── INDEX.md                ← вы здесь
├── SPECIFICATION3.md       активная спецификация (договор языка)
├── ARCHITECTURE.md         техническая структура и решения
├── PLANS4.md               текущий план
├── WORKLOG5.md             активный журнал сессий
├── BYTECODE_COVERAGE.md    что покрыто BytecodeVm
├── BYTECODE_PERF_MATRIX.md какие сценарии производительности есть
├── LANGUAGE_INVENTORY.md   каноническая инвентаризация языка (356 фич)
├── DOCUMENTATION-STYLE.md  стандарт HTML-справочника
├── FUTURE.md               будущие предложения
├── meta/                   мета-слой (реестр, карта функций, план миграции, гид)
├── agent/                  быстрый вход для агента
├── audit/                  глубокие аудиты подсистем
├── manual/                 HTML-справочник для человека
└── archive/                исторический архив (перенесено 2026-08-26)
```

## 3. Маршруты для агента

**Начало работы:** `AGENTS.md` → `docs/agent/README.md` → `docs/agent/CURRENT_STATE.md` → `docs/PLANS4.md`.

**Задача про конкретную подсистему:** `docs/audit/<ПОДСИСТЕМА>.md` → сверка с `docs/SPECIFICATION3.md` и кодом.

**Задача про байткод/паритет:** `docs/BYTECODE_COVERAGE.md` + `docs/BYTECODE_PERF_MATRIX.md` + `docs/archive/audits-through-20261011/BYTECODE_VM_PERFORMANCE.md`.

**Проверка статуса документа:** `docs/archive/meta-20260826/DOC_REGISTRY.md`.

**Проверка состояния функции/хвоста:** `docs/archive/meta-20260826/FEATURE_LEDGER.md`.

## 4. Маршруты для человека

**Понять продукт:** `PROJECT.md` → `README.md`.

**Научиться языку:** `docs/manual/index.html` → `getting-started.html`.

**Узнать, что сделано и что дальше:** `docs/INDEX.md` (эта страница) → `docs/archive/meta-20260826/FEATURE_LEDGER.md`.

## 5. Правило единого источника правды

При расхождении документов побеждают (в порядке убывания):
1. исходный код, тесты, запускаемые примеры `samples/`;
2. `docs/SPECIFICATION3.md`, `docs/ARCHITECTURE.md`, `docs/PLANS4.md`, `docs/WORKLOG5.md`;
3. реестры `docs/archive/root-documents-20261011/LANGUAGE_INVENTORY.md`, `docs/BYTECODE_COVERAGE.md`;
4. остальные документы.

Если документ не найден или его статус неясен — смотреть `docs/archive/meta-20260826/DOC_REGISTRY.md`.
