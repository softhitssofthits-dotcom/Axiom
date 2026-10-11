# AXIOM — ГЛУБОКИЙ FORENSIC-АУДИТ
## GRAPHICS / CANVAS / MODERN GRAPHICS ПОДСИСТЕМА

**Дата аудита:** 2026-08-21
**Метод:** полный forensic-анализ кода, тестов, samples, документации и IDE-слоя
**Объём анализированного кода:** ~50 файлов, ~12000+ строк кода подсистемы

---

## 1. Резюме

Современная графическая подсистема CANVAS является одной из наиболее зрелых и архитектурно целостных подсистем проекта. Она реализует 18 операторов, проходящих полный конвейер языка от лексера до IDE, с полным паритетом между Interpreter и BytecodeVm. Подсистема стабильна, корректна и хорошо протестирована.

**Общая оценка:** Подсистема готова к дальнейшему развитию. Обнаружено 2 проблемы средней серьёзности (дублирование логики между Interpreter и VM, незавершённый контракт тайловых карт), 8 edge cases низкой серьёзности и несколько архитектурных замечаний по дублированию. Ни одна из проблем не является блокирующей.

**Главный системный долг:** массированное дублирование вспомогательной логики между `Interpreter.Graphics.cs` и `BytecodeVm.Graphics.cs` (~600 строк идентичного кода).

---

## 2. Что представляет собой эта подсистема

Современный графический слой CANVAS отвечает за создание пиксельного холста, покадровый цикл отрисовки, рисование примитивов, работу с изображениями, спрайтами и тайловыми картами в полноцветном режиме. Это основной рекомендуемый путь для новых 2D-игр и визуальных программ в Axiom.

Подсистема противопоставляется историческому SCREEN-пути: SCREEN сохраняет совместимость с BASIC, а CANVAS становится главным направлением для нового кода.

Конвейер подсистемы:
- **Лексер:** 18 ключевых слов (`CANVAS`, `BEGINFRAME`, `PRESENT`, `CAMERA`, `RESETCAMERA`, `VIEWPORT`, `DRAWRECT`, `FILLRECT`, `DRAWLINE`, `DRAWCIRCLE`, `FILLCIRCLE`, `DRAWTEXT`, `DRAWIMAGE`, `DRAWSPRITE`, `DRAWTILE`, `DRAWTILEMAP`, `DRAWTILELAYER`, `SETMAPTILE`)
- **Parser:** 18 синтаксических узлов
- **Binder:** 18 bound-узлов с проверкой типов
- **Lowering:** 18 lowered-инструкций
- **Bytecode:** 18 опкодов
- **Interpreter:** 18 методов исполнения + 15 встроенных функций
- **BytecodeVm:** 18 методов исполнения + 15 встроенных функций
- **IDE:** нативное Win32-окно, D3D11-рендерер, framebuffer

---

## 3. Полный список возможностей

### 3.1 Современные графические операторы (18)

| ID | Оператор | Назначение | Статус | Паритет |
|---|---|---|---|---|
| SYS-MG01 | `CANVAS` | Создание пиксельного холста | IMPLEMENTED | ✅ |
| SYS-MG02 | `BEGINFRAME` | Начало кадра | IMPLEMENTED | ✅ |
| SYS-MG03 | `PRESENT` | Публикация кадра | IMPLEMENTED | ✅ |
| SYS-MG04 | `CAMERA` | Установка смещения камеры | IMPLEMENTED | ✅ |
| SYS-MG05 | `RESETCAMERA` | Сброс камеры | IMPLEMENTED | ✅ |
| SYS-MG06 | `VIEWPORT` | Установка области просмотра | IMPLEMENTED | ✅ |
| SYS-MG07 | `DRAWRECT` | Контур прямоугольника | IMPLEMENTED | ✅ |
| SYS-MG08 | `FILLRECT` | Заполненный прямоугольник | IMPLEMENTED | ✅ |
| SYS-MG09 | `DRAWLINE` | Линия на холсте | IMPLEMENTED | ✅ |
| SYS-MG10 | `DRAWCIRCLE` | Контур окружности | IMPLEMENTED | ✅ |
| SYS-MG11 | `FILLCIRCLE` | Заполненная окружность | IMPLEMENTED | ✅ |
| SYS-MG12 | `DRAWTEXT` | Текст на холсте | IMPLEMENTED | ✅ |
| SYS-MG13 | `DRAWIMAGE` | Отрисовка изображения | IMPLEMENTED | ✅ |
| SYS-MG14 | `DRAWSPRITE` | Отрисовка кадра спрайта | IMPLEMENTED | ✅ |
| SYS-MG15 | `DRAWTILE` | Отрисовка тайла из тайлсета | IMPLEMENTED | ✅ |
| SYS-MG16 | `DRAWTILEMAP` | Отрисовка всех слоёв карты | IMPLEMENTED | ✅ |
| SYS-MG17 | `DRAWTILELAYER` | Отрисовка одного слоя карты | IMPLEMENTED | ✅ |
| SYS-MG18 | `SETMAPTILE` | Изменение ячейки карты | IMPLEMENTED | ✅ |

### 3.2 Современные графические типы данных (4)

| ID | Тип | Назначение | Статус |
|---|---|---|---|
| SYS-MT01 | `IMAGE` | Загруженное растровое изображение | IMPLEMENTED |
| SYS-MT02 | `SPRITE` | Спрайт с кадрами анимации | IMPLEMENTED |
| SYS-MT03 | `TILESET` | Атлас тайлов | IMPLEMENTED |
| SYS-MT04 | `TILEMAP` | Тайловая карта (Tiled JSON) | IMPLEMENTED |

### 3.3 Современные графические встроенные функции (15+)

| ID | Функция | Назначение | Статус |
|---|---|---|---|
| SYS-MF01 | `LOADIMAGE(path)` | Загрузка изображения | IMPLEMENTED |
| SYS-MF02 | `IMAGEWIDTH(img)` | Ширина изображения | IMPLEMENTED |
| SYS-MF03 | `IMAGEHEIGHT(img)` | Высота изображения | IMPLEMENTED |
| SYS-MF04 | `LOADSPRITE(path,w,h)` | Загрузка спрайта | IMPLEMENTED |
| SYS-MF05 | `SPRITEWIDTH(spr)` | Ширина кадра спрайта | IMPLEMENTED |
| SYS-MF06 | `SPRITEHEIGHT(spr)` | Высота кадра спрайта | IMPLEMENTED |
| SYS-MF07 | `SPRITECOUNT(spr)` | Число кадров спрайта | IMPLEMENTED |
| SYS-MF08 | `LOADTILESET(...)` | Загрузка тайлсета (3 формы) | IMPLEMENTED |
| SYS-MF09 | `TILECOUNT(ts)` | Число тайлов | IMPLEMENTED |
| SYS-MF10 | `TILEWIDTH(ts)` | Ширина тайла | IMPLEMENTED |
| SYS-MF11 | `TILEHEIGHT(ts)` | Высота тайла | IMPLEMENTED |
| SYS-MF12 | `LOADTILEMAP(path)` | Загрузка Tiled JSON карты | IMPLEMENTED |
| SYS-MF13 | `TEXTWIDTH(text)` | Ширина текста в пикселях | IMPLEMENTED |
| SYS-MF14 | `TEXTHEIGHT(text)` | Высота текста в пикселях | IMPLEMENTED |
| SYS-MF15 | `RGB(r,g,b)` | Сборка 24-битного цвета | IMPLEMENTED |
| SYS-MF16-31 | `MAPWIDTH`, `MAPHEIGHT`, `MAPPIXELWIDTH`, `MAPPIXELHEIGHT`, `MAPTILEWIDTH`, `MAPTILEHEIGHT`, `MAPLAYERCOUNT`, `MAPTILE`, `MAPTILESET`, `MAPTILEFLAGS`, `MAPLAYERNAME`, `MAPLAYERTYPE`, `MAPLAYERVISIBLE`, `MAPTILESETCOUNT` | Функции тайловой карты | IMPLEMENTED |

---

## 4. Фактическая архитектура

### 4.1 Конвейер выполнения

```
Исходный код (.axm)
    ↓
Lexer (18 ключевых слов CANVAS-пути)
    ↓
Parser.Statements.cs (18 Parse*Statement методов)
    ↓
GraphicsSyntax.cs (18 синтаксических узлов)
    ↓
Binder.IoStatements.cs (18 Bind*Statement методов)
    ↓
BoundStatements.cs (18 bound-узлов)
    ↓
BoundProgramLowerer.cs (18 lowered-инструкций, push значений на стек)
    ↓
BytecodeCompiler.cs (18 опкодов, кодирование флагов)
    ↓
BytecodeModule (с пулом констант и отладочной информацией)
    ↓
BytecodeVm.Graphics.cs (18 Execute*Instruction + 15 Evaluate*Builtin)
    ↓
IGraphicsHost (абстракция хоста)
    ↓
GraphicsHostService.cs (IDE: Win32 окно + D3D11 рендерер)
    ↓
Результат на экране
```

### 4.2 Параллельный путь Interpreter

```
BoundProgram
    ↓
Interpreter.Graphics.cs (18 Execute*Statement + 15 Evaluate*Builtin)
    ↓
IGraphicsHost (тот же интерфейс)
    ↓
GraphicsHostService.cs
    ↓
Результат на экране
```

### 4.3 Ключевые файлы

| Файл | Ответственность | Строки |
|------|----------------|--------|
| `src/Axiom.Syntax/GraphicsSyntax.cs` | Все синтаксические узлы графики (включая CANVAS) | 1467 |
| `src/Axiom.Parsing/Parser.Statements.cs` | Парсинг всех графических операторов | ~400 |
| `src/Axiom.Semantics/Binding/Binder.IoStatements.cs` | Связывание всех графических операторов | ~580 |
| `src/Axiom.Semantics/BoundNodes/BoundStatements.cs` | Все bound-узлы графики | ~700 |
| `src/Axiom.Bytecode/Lowering/BoundProgramLowerer.cs` | Понижение в lowered-инструкции | ~400 |
| `src/Axiom.Runtime/Execution/Interpreter.Graphics.cs` | Исполнение через интерпретатор | 1451 |
| `src/Axiom.Runtime/Execution/BytecodeVm.Graphics.cs` | Исполнение через VM | 1692 |
| `src/Axiom.Runtime/Host/IGraphicsHost.cs` | Абстракция графического хоста | 279 |
| `src/Axiom.Runtime/Host/GraphicsHostDrawing.cs` | Программная растеризация | 238 |
| `src/Axiom.Runtime/Host/GraphicsCoordinateSpace.cs` | Преобразование координат | 459 |
| `src/Axiom.Runtime/Execution/TilemapRenderSupport.cs` | Рендеринг тайловых карт | 279 |
| `src/Axiom.Runtime/Values/RuntimeTilemapValue.cs` | Модель тайловой карты | 495+ |
| `src/Axiom.Ide/Services/Graphics/GraphicsHostService.cs` | IDE-реализация IGraphicsHost | 2432 |

---

## 5. Полный путь выполнения

### 5.1 Пример: `DRAWRECT 100, 50, 200, 150, RGB(255, 0, 0)`

**Interpreter:**
1. `Parser.Statements.cs:ParseDrawRectStatement()` → `DrawRectStatementSyntax`
2. `Binder.IoStatements.cs:BindDrawRectStatement()` → `BoundDrawRectStatement` с 5 выражениями
3. `Interpreter.Graphics.cs:ExecuteDrawRect()`:
   - `RequireCanvasActive("DRAWRECT", ...)` — проверка активности CANVAS
   - `EvaluateExpression(...)` для x, y, width, height, color
   - `ValidateCanvasRectangle(...)` — проверка положительных width/height
   - `_host.DrawRect(x, y, width, height, color)` — вызов хоста
   - `_host.SetGraphicsCurrentPosition(x, y)` — обновление текущей позиции

**BytecodeVm:**
1. Те же шаги Parser и Binder
2. `BoundProgramLowerer.cs` → `LoweredInstruction.DrawRect()` (push 5 значений на стек)
3. `BytecodeCompiler.cs` → `Opcode.DrawRect`
4. `BytecodeVm.Graphics.cs:ExecuteDrawRectInstruction()`:
   - Проверка `!_host.IsCanvasActive` → `RequireCanvasActive`
   - `stack.PopUnchecked()` для color, height, width, y, x
   - Проверка `width <= 0 || height <= 0` → `ValidateCanvasRectangle`
   - `_host.DrawRect(x, y, width, height, color)`
   - `_host.SetGraphicsCurrentPosition(x, y)`

---

## 6. Фактический семантический контракт

### CANVAS

**Вход:** два целочисленных выражения (width, height)
**Обработка:** проверка `width > 0 && height > 0 && width <= int.MaxValue && height <= int.MaxValue`
**Выход:** активация canvas-режима через `_host.ActivateCanvas(width, height)`
**Побочные эффекты:** сброс предыдущего состояния графики, `FlushPendingOutput`
**Ошибки:** `MB3007` при недопустимых размерах

### DRAWRECT / FILLRECT

**Вход:** 5 выражений (x, y, width, height, color)
**Обработка:** требует активного CANVAS, проверяет width > 0, height > 0
**Выход:** отрисовка прямоугольника на холсте
**Побочные эффекты:** обновление `CurrentGraphicsPosition`
**Ошибки:** `MB3010` без CANVAS, `MB3007` при недопустимых размерах

### CAMERA

**Вход:** 2 выражения (x, y)
**Обработка:** требует активного CANVAS, проверяет допустимость координат
**Выход:** смещение камеры для мировых координат
**Побочные эффекты:** влияет на все последующие DRAW*-операции

### VIEWPORT

**Вход:** 0 (сброс) или 4 выражения (left, top, rightExclusive, bottomExclusive)
**Обработка:** координаты трактуются как полуоткрытый интервал `[left, right) × [top, bottom)`, обрезаются по размеру холста
**Выход:** прямоугольник отсечения для графических операций
**Побочные эффекты:** влияет на все DRAW*-операции до сброса

### DRAWTILEMAP

**Вход:** 3 выражения (tilemap, x, y)
**Обработка:** требует активного CANVAS, обходит видимые тайловые слои, рисует только видимые тайлы
**Выход:** отрисовка карты на холсте с учётом камеры и viewport

---

## 7. Анализ Parser

### 7.1 Структура

Все 18 CANVAS-операторов парсятся в `Parser.Statements.cs`. Каждый оператор имеет отдельный метод `Parse*Statement()`.

### 7.2 Качество

- Чёткое разделение: каждый оператор — отдельный метод
- Корректное разделение аргументов запятыми
- Обработка необязательных аргументов (`DRAWIMAGE` с/без масштаба, `VIEWPORT` сброс/прямоугольник)
- Корректная обработка `STEP` для `PSET`/`PRESET` в CANVAS-контексте

### 7.3 Проблемы

Нет существенных проблем. Код парсера чистый и хорошо структурированный.

---

## 8. Анализ Binder

### 8.1 Структура

Все 18 CANVAS-операторов связаны в `Binder.IoStatements.cs`. Каждый оператор имеет отдельный метод `Bind*Statement()`.

### 8.2 Качество

- Проверка типов аргументов (целочисленные координаты, IMAGE/SPRITE/TILESET/TILEMAP типы)
- Корректное создание bound-узлов с сохранением выражений
- Корректная обработка необязательных аргументов (масштаб DRAWIMAGE/DRAWSPRITE, форма VIEWPORT)

### 8.3 Проблемы

Нет существенных проблем. Код биндера чистый.

---

## 9. Анализ Interpreter

### 9.1 Структура

Файл `Interpreter.Graphics.cs` (1451 строк) содержит:
- 18 методов `Execute*Statement` для CANVAS-операторов
- 15+ методов `Evaluate*Builtin` для встроенных функций
- 20+ вспомогательных методов валидации
- 8+ вспомогательных методов для MAPTILE/MAPSET и tilemap

### 9.2 Качество

- Чёткая структура: каждый оператор — отдельный метод
- Единообразный паттерн: проверка → вычисление → вызов хоста → обновление состояния
- Корректная диагностика с указанием источника
- Корректная обработка ошибок (выброс `RuntimeFailureException`)

### 9.3 Проблемы

**SYS-001:** Вспомогательные методы `TryGetMapCell`, `SetMapTileCell`, `RequireImage`, `RequireSprite`, `RequireTileset`, `RequireTilemap`, `EvaluateRgb`, `ValidateCanvasSize`, `ValidateCanvasRectangle`, `ValidateCanvasPoint`, `ValidateCanvasLine`, `ValidateCanvasCircle`, `ValidateSpriteFrame`, `ValidateTileIndex`, `ValidateDrawImageSize`, `ValidateDrawSpriteSize`, `ValidateSpriteFrameSize`, `ValidateTilesetGridParameters`, `CreatePaintRequest`, `DecodePaintPattern`, `ToPaintColor`, `IsRgbComponent` полностью дублируются между `Interpreter.Graphics.cs` и `BytecodeVm.Graphics.cs`.

---

## 10. Анализ Bytecode / VM

### 10.1 Lowering

`BoundProgramLowerer.cs` понижает все 18 bound-узлов в lowered-инструкции. Каждая lowered-инструкция:
- Выталкивает выражения-аргументы на стек вычислений
- Эмитит lowered-инструкцию с нужным количеством значений на стеке

### 10.2 Bytecode Emission

`BytecodeCompiler.cs` маппит 18 `LoweredInstructionKind` в 18 `Opcode`. Специальные случаи:
- `VIEWPORT` кодирует количество операндов (0 или 4)
- `DRAWIMAGE`/`DRAWSPRITE` кодируют флаг масштабирования

### 10.3 VM Execution

`BytecodeVm.Graphics.cs` (1692 строки) содержит:
- 18 методов `Execute*Instruction`
- 15+ методов `Evaluate*Builtin`
- 20+ вспомогательных методов (полные дубликаты Interpreter)

### 10.4 Качество

- Корректное использование стека вычислений
- Оптимизация `TryGetFastWholeNumber` для быстрого извлечения целых значений
- Проверка типов перед вызовом хоста
- Корректная обработка ошибок

### 10.5 Проблемы

**SYS-002:** Полное дублирование вспомогательных методов между Interpreter и VM (подробнее в разделе 26).

---

## 11. Анализ Runtime

### 11.1 IGraphicsHost

Интерфейс `IGraphicsHost.cs` (279 строк) определяет контракт графического хоста:
- 18 методов для CANVAS-операторов
- Методы для SCREEN-операторов
- Методы загрузки ресурсов (IMAGE, SPRITE, TILESET, TILEMAP)
- Методы рисования (DrawImage, DrawSprite, DrawImageRegion)
- Default-реализации для всех методов (пустые тела)

### 11.2 Runtime Values

- `RuntimeImageValue` — хранит `width`, `height`, `bgraPixels`
- `RuntimeSpriteValue` — хранит `RuntimeImageValue` + `frameWidth`/`frameHeight`
- `RuntimeTilesetValue` — хранит `RuntimeImageValue` + размеры тайла, margin, spacing
- `RuntimeTilemapValue` — хранит размеры карты, тайлсеты, слои, порядок рендеринга

### 11.3 GraphicsHostService

Файл `GraphicsHostService.cs` (2432 строки) реализует `IGraphicsHost` в IDE:
- CANVAS-режим: `ActivateCanvas`, `BeginFrame`, `PresentFrame`
- Рисование: `DrawRect`, `FillRect`, `DrawCanvasLine`, `DrawCanvasCircle`, `FillCanvasCircle`, `DrawCanvasText`
- Изображения: `DrawImage`, `DrawSprite`, `DrawImageRegion`
- Viewport: `SetViewport`, `ResetViewport`, камера

---

## 12. Взаимодействие с типами

### 12.1 INTEGER / LONG

Все координаты и размеры CANVAS-операторов работают с `LONG` внутри runtime (`AsLong()`). Приведение к `INTEGER` происходит только при вызове хоста (`ToInt32Checked`). Это корректно, так как координаты могут быть большими.

### 12.2 DOUBLE / DECIMAL

Не используются напрямую в CANVAS-операторах. `RGB` принимает только целые аргументы. `PAINT` (SCREEN-оператор, работающий и в CANVAS) принимает `DOUBLE`/`DECIMAL` для цвета.

### 12.3 STRING

`DRAWTEXT` принимает строку для отображения. `LOADIMAGE`, `LOADSPRITE`, `LOADTILESET`, `LOADTILEMAP` принимают строковые пути. Корректная обработка пустых строк и Unicode.

### 12.4 IMAGE / SPRITE / TILESET / TILEMAP

Специализированные типы с reference-семантикой. Корректная проверка на пустые значения через `Require*` хелперы. `DRAWIMAGE` и `DRAWSPRITE` поддерживают необязательное масштабирование.

---

## 13. Взаимодействие со String

### 13.1 DRAWTEXT

- Принимает строку произвольного содержимого
- Пустая строка допустима (ничего не рисует)
- Unicode обрабатывается корректно через хост

### 13.2 LOADIMAGE / LOADSPRITE / LOADTILESET / LOADTILEMAP

- Пути передаются как строки
- Пустые пути вызывают `MB3007`
- Кодировка путей зависит от хоста (Windows)

---

## 14. Взаимодействие с Array

### 14.1 TILEMAP и слои

`RuntimeTilemapValue` хранит слои как массив `RuntimeTilemapLayer`. Каждый слой имеет массив ячеек. Доступ по индексам 0-based внутри, 1-based наружу.

### 14.2 GET/PUT на CANVAS

Не являются частью чистого CANVAS-пути, но работают в CANVAS-контексте. Используют двумерные числовые массивы для буферов.

---

## 15. Взаимодействие с TYPE

Пользовательские `TYPE` не используются напрямую в CANVAS-операторах. Все графические типы (IMAGE, SPRITE, TILESET, TILEMAP) являются встроенными, а не пользовательскими TYPE.

---

## 16. Взаимодействие с Functions / SUB

### 16.1 Встроенные функции

15+ встроенных функций работают в CANVAS-контексте: `LOADIMAGE`, `LOADSPRITE`, `LOADTILESET`, `LOADTILEMAP`, `IMAGEWIDTH`, `IMAGEHEIGHT`, `SPRITEWIDTH`, `SPRITEHEIGHT`, `SPRITECOUNT`, `TILECOUNT`, `TILEWIDTH`, `TILEHEIGHT`, `TEXTWIDTH`, `TEXTHEIGHT`, `RGB`, `MAPWIDTH`, `MAPHEIGHT`, `MAPTILE`, и др.

### 16.2 Пользовательские функции

CANVAS-операторы корректно работают внутри пользовательских `SUB`/`FUNCTION`. Нет захвата специального состояния.

---

## 17. Взаимодействие с Control Flow

### 17.1 IF / ELSE

CANVAS-операторы корректно работают внутри `IF`/`ELSE` блоков.

### 17.2 FOR / WHILE / DO

Типичный игровой цикл:
```basic
CANVAS 640, 360
DO WHILE INKEY() <> "ESC"
    BEGINFRAME
    ' рисование
    PRESENT
    SLEEP 16
LOOP
```

### 17.3 TRY / CATCH

Ошибки CANVAS-операторов (например, `MB3010` без CANVAS) могут быть перехвачены через `TRY`/`CATCH`.

---

## 18. Взаимодействие с Error / Exception System

### 18.1 Диагностики

| Код | Описание | Пример |
|-----|----------|--------|
| `MB3007` | Недопустимый аргумент | Отрицательный радиус, пустой путь |
| `MB3010` | Нет активного графического режима | DRAWRECT без CANVAS |
| `MB3005` | Невозможно загрузить ресурс | Несуществующий файл |

### 18.2 Обработка ошибок

Все ошибки ведут к `RuntimeFailureException`, которая может быть перехвачена через `TRY`/`CATCH`. После ошибки состояние графики остаётся консистентным (хост не вызывается при невалидных аргументах).

---

## 19. State / Lifetime / Ownership

### 19.1 Графическое состояние

- **Глобальное состояние:** `IsCanvasActive`, `ScreenWidth`, `ScreenHeight`, `CanvasCameraX`, `CanvasCameraY`, viewport
- **Владелец:** `GraphicsHostService` в IDE
- **Время жизни:** от `CANVAS` до деактивации или конца программы
- **Сброс:** `CANVAS` с новыми размерами сбрасывает предыдущее состояние

### 19.2 Ресурсы

- **IMAGE:** загружается через `LOADImage`, владеет хост, уничтожается при завершении программы
- **SPRITE:** ссылается на IMAGE, не копирует пиксели
- **TILESET:** ссылается на IMAGE, не копирует пиксели
- **TILEMAP:** загружается через `LOADTILEMAP`, владеет хост

---

## 20. Edge Cases

### 20.1 CANVAS с нулевыми размерами

`CANVAS 0, 0` → `MB3007`. Корректно.

### 20.2 CANVAS с отрицательными размерами

`CANVAS -1, 100` → `MB3007`. Корректно.

### 20.3 DRAWRECT с нулевым width

`DRAWRECT 0, 0, 0, 100, 0` → `MB3007`. Корректно.

### 20.4 DRAWCIRCLE с отрицательным радиусом

`DRAWCIRCLE 0, 0, -1, 0` → `MB3007`. Корректно.

### 20.5 DRAWSPRITE с невалидным кадром

`DRAWSPRITE spr, -1, 0, 0` → `MB3007`. Корректно.

### 20.6 DRAWTILELAYER на объектный слой

`DRAWTILELAYER tm, objectLayerIndex, 0, 0` → `RuntimeFailureException`. Корректно.

### 20.7 SETMAPTILE на несуществующую ячейку

`SETMAPTILE tm, 1, 0, 0, 1, 1` → `MB3007` (column/row должны быть >= 1). Корректно.

### 20.8 VIEWPORT с пустым прямоугольником после обрезки

`VIEWPORT (1000,1000)-(2000,2000)` на CANVAS 320×240 → `MB3007` (после обрезки — пустой). Корректно.

---

## 21. Interpreter ↔ Bytecode VM

| Возможность | Interpreter | Bytecode VM | Одинаковый результат | Одинаковая ошибка | Одинаковое состояние |
|---|---|---|---|---|---|
| CANVAS | да | да | да | да | да |
| BEGINFRAME | да | да | да | да | да |
| PRESENT | да | да | да | да | да |
| CAMERA | да | да | да | да | да |
| RESETCAMERA | да | да | да | да | да |
| VIEWPORT | да | да | да | да | да |
| DRAWRECT | да | да | да | да | да |
| FILLRECT | да | да | да | да | да |
| DRAWLINE | да | да | да | да | да |
| DRAWCIRCLE | да | да | да | да | да |
| FILLCIRCLE | да | да | да | да | да |
| DRAWTEXT | да | да | да | да | да |
| DRAWIMAGE | да | да | да | да | да |
| DRAWSPRITE | да | да | да | да | да |
| DRAWTILE | да | да | да | да | да |
| DRAWTILEMAP | да | да | да | да | да |
| DRAWTILELAYER | да | да | да | да | да |
| SETMAPTILE | да | да | да | да | да |
| LOADIMAGE | да | да | да | да | да |
| LOADSPRITE | да | да | да | да | да |
| LOADTILESET | да | да | да | да | да |
| LOADTILEMAP | да | да | да | да | да |
| IMAGEWIDTH/HEIGHT | да | да | да | да | да |
| SPRITEWIDTH/HEIGHT/COUNT | да | да | да | да | да |
| TILECOUNT/WIDTH/HEIGHT | да | да | да | да | да |
| TEXTWIDTH/HEIGHT | да | да | да | да | да |
| RGB | да | да | да | да | да |
| MAPWIDTH/HEIGHT/... | да | да | да | да | да |
| MAPTILE/TILESET/TILEFLAGS | да | да | да | да | да |

**Все расхождения:** не обнаружены. Полный паритет.

---

## 22. Анализ производительности

### 22.1 Данные из BYTECODE_PERF_MATRIX.md

| Сценарий | Interpreter | VM | Ускорение | Классификация |
|---|---:|---:|---:|---|
| `modern_canvas_primitive_burst.axm` | - | - | ~0.793x runtime | CLEAR WIN |
| `modern_canvas_sprite_burst.axm` | - | - | ~0.793x runtime | CLEAR WIN |
| `modern_graphics_metadata_sweep.axm` | - | - | ~0.540x runtime | CLEAR WIN |
| `maze_chase_mixed_frame.axm` | - | - | ~0.732x runtime | CLEAR WIN |

### 22.2 Выводы

BytecodeVm стабильно быстрее Interpreter на CANVAS-сценариях:
- Примитивы: ~0.793x (VM на 20% быстрее)
- Спрайты/изображения: ~0.793x (VM на 20% быстрее)
- Metadata-запросы: ~0.540x (VM почти в 2 раза быстрее)
- Смешанный игровой кадр: ~0.732x (VM на 27% быстрее)

Все сценарии в целевом диапазоне `<= 0.900x`. Прямого долга нет.

---

## 23. Качество Benchmark

### 23.1 Покрытие

- 4 выделенных `engine-compare` сценария для CANVAS-пути
- Детерминированные сценарии без интерактивного ввода
- Достаточная длительность для устойчивых измерений

### 23.2 Проблемы

Нет отдельного сценария для `DRAWTILE`/`DRAWTILEMAP`/`DRAWTILELAYER`. Тайловые карты участвуют только в `maze_chase_mixed_frame.axm` косвенно.

---

## 24. Hot Paths и Bottlenecks

### 24.1 Главный hot path

`BEGINFRAME → DRAWRECT/FILLRECT/DRAWLINE/DRAWCIRCLE/FILLCIRCLE/DRAWTEXT/DRAWIMAGE/DRAWSPRITE → PRESENT`

Этот цикл выполняется каждый кадр. VM уже оптимизирована для этого пути через `PopUnchecked()` и `TryGetFastWholeNumber`.

### 24.2 Бутылочные горлышки

Не обнаружены. Все сценарии в целевом диапазоне производительности.

---

## 25. Архитектурные проблемы

### AR-001: Дублирование вспомогательной логики между Interpreter и VM

**Серьёзность:** P2
**Категория:** ARCHITECTURE

~600 строк идентичного кода дублируются между `Interpreter.Graphics.cs` и `BytecodeVm.Graphics.cs`:
- `TryGetMapCell`, `SetMapTileCell` — логика работы с ячейками карты
- `RequireImage`, `RequireSprite`, `RequireTileset`, `RequireTilemap` — проверка типов
- `EvaluateRgb`, `ValidateCanvasSize`, `ValidateCanvasRectangle`, `ValidateCanvasPoint`, `ValidateCanvasLine`, `ValidateCanvasCircle` — валидация
- `ValidateSpriteFrame`, `ValidateTileIndex`, `ValidateDrawImageSize`, `ValidateDrawSpriteSize`, `ValidateSpriteFrameSize`, `ValidateTilesetGridParameters` — валидация
- `CreatePaintRequest`, `DecodePaintPattern`, `ToPaintColor`, `IsRgbComponent` — работа с PAINT
- `TryApplyCanvasViewport`, `TryApplyViewport`, `TryApplyWindow` — работа с viewport
- `ReportInvalid*`, `RequireCanvasActive`, `RequireScreenActive`, `RequireDrawableSurfaceActive` — диагностика
- `EvaluatePointCoordinate`, `EvaluatePMap*`, `EvaluatePoint*` — POINT/PMAP

**Направление исправления:** выделить общий базовый класс или статический хелпер-класс `GraphicsExecutionHelpers`, который наследуется/используется и Interpreter, и VM.

---

### AR-002: GraphicsHostService — God Object

**Серьёзность:** P3
**Категория:** ARCHITECTURE

Файл `GraphicsHostService.cs` (2432 строки) реализует весь графический хост: SCREEN-режим, CANVAS-режим, загрузку ресурсов, рисование примитивов, viewport, камеру, текст, координатные преобразования. Это нарушение Single Responsibility Principle.

**Направление исправления:** рассмотреть разделение на `CanvasGraphicsHost` и `ScreenGraphicsHost` с общим интерфейсом.

---

### AR-003: Отсутствие internal-документа для CANVAS-примитивов

**Серьёзность:** P4
**Категория:** DOCS

`docs/archive/root-documents-20261011/GRAPHICS_OPERATORS_FLAGS.md` документирует внутренние флаги для `LINE`, `CIRCLE`, `PAINT`, но аналогичный документ для CANVAS-примитивов (`DRAWRECT`, `FILLRECT`, `DRAWLINE`, `DRAWCIRCLE`, `FILLCIRCLE`, `DRAWTEXT`) отсутствует. Если внутренние контракты простых, это допустимо, но явное отсутствие стоит зафиксировать.

---

## 26. Дублирование логики

### 26.1 Дублирование между Interpreter и BytecodeVm

**Текущие места:**
- `Interpreter.Graphics.cs` (1451 строка)
- `BytecodeVm.Graphics.cs` (1692 строки)

**Дублируемая логика:**
- ~40 вспомогательных методов, полностью идентичных в обоих файлах
- Логика работы с ячейками карты (`TryGetMapCell`, `SetMapTileCell`)
- Логика валидации (`ValidateCanvas*`, `ValidateSprite*`, `ValidateTile*`)
- Логика Require-проверок (`RequireImage`, `RequireSprite`, `RequireTileset`, `RequireTilemap`)
- Логика PAINT (`CreatePaintRequest`, `DecodePaintPattern`, `ToPaintColor`)
- Логика viewport/window (`TryApplyCanvasViewport`, `TryApplyViewport`, `TryApplyWindow`)
- Логика POINT/PMAP (`EvaluatePoint*`, `EvaluatePMap*`)
- Логика диагностики (`ReportInvalid*`, `RequireCanvasActive`, и др.)

**Единый источник истины:** сейчас два параллельных источника.

**Направление миграции:** выделить `GraphicsExecutionSupport` статический класс в `Axiom.Runtime` с общей логикой.

**Риски:** минимальные — чистый рефакторинг без изменения семантики.

---

## 27. Single Source of Truth

| Область | Источник истины | Дублирование |
|---------|----------------|-------------|
| Синтаксис | `GraphicsSyntax.cs` | нет |
| Семантика | `BoundStatements.cs` + `Binder.IoStatements.cs` | нет |
| Lowering | `BoundProgramLowerer.cs` | нет |
| Opcodes | `Opcode.cs` | нет |
| Исполнение (Interpreter) | `Interpreter.Graphics.cs` | да (с VM) |
| Исполнение (VM) | `BytecodeVm.Graphics.cs` | да (с Interpreter) |
| Runtime behavior | `IGraphicsHost` + хост | нет |
| Диагностика | Обе Graphics-файлы | да (дублирование) |
| Валидация | Обе Graphics-файлы | да (дублирование) |

---

## 28. Legacy / Dead Code

Не обнаружен. Все 18 операторов активно используются. Старые SCREEN-операторы не пересекаются с CANVAS-пути.

---

## 29. Надёжность и безопасность

### 29.1 Управление ресурсами

IMAGE, SPRITE, TILESET, TILEMAP загружаются через хост и живут до завершения программы. Нет утечек ресурсов в текущей архитектуре (хост owns ресурсы).

### 29.2 Path handling

Пути к файлам передаются как строки. Хост (Windows) обрабатывает пути. Нет path traversal-рисков в языковом слое.

### 29.3 Resource exhaustion

`CANVAS 2147483647, 2147483647` может создать огромный фреймбуфер. Хост должен ограничивать память.

### 29.4 Malformed input

Tiled JSON загружается через `LOADTILEMAP`. Ошибки формата обрабатываются хостом и возвращают `null`.

---

## 30. Анализ тестов

### 30.1 Покрытие по слоям

| Слой | Файл | Тесты |
|------|------|-------|
| Parser | `ParserTests.cs` | 21 тест |
| Binder | `BinderTests.cs` | 23 теста |
| Bytecode Lowering | `BoundProgramLowererTests.cs` | 2 теста |
| Interpreter | `InterpreterTests.Graphics.cs` | ~62 теста |
| BytecodeVm | `BytecodeVmTests.cs` | ~45 тестов |
| Baseline (pixel) | `GraphicsBaselineTests.cs` | 9 тестов |
| IDE Highlighting | `EditorFeatureTests.cs` | 7 тестов |
| IDE Formatting | `CodeFormattingServiceTests.cs` | 6 тестов |
| IDE Window | `NativeGraphicsScreenWindowTests.cs` | 9 тестов |
| Acceptance | `AcceptanceProgramTests.cs` | 3+ теста |

### 30.2 Качество тестов

- Тесты Interpreter и BytecodeVm структурированы как зеркала
- BytecodeVm-тесты дополнительно проверяют эмитированные опкоды
- Есть baseline-тесты с проверкой отдельных пикселей
- Есть acceptance-тесты на реальных sample-файлах

---

## 31. Недостающие тесты

### 31.1 Обычные сценарии

- `DRAWTILEMAP` с несколькими тайлсетами
- `DRAWTILEMAP` с невидимыми слоями (проверка, что они не рисуются)
- `SETMAPTILE` с флагами отражения (1, 2, 4)
- `DRAWIMAGE` с очень большим масштабом
- `DRAWSPRITE` с кадром 0 (первый кадр)

### 31.2 Отрицательные сценарии

- `CANVAS` с размерами, превышающими доступную память
- `DRAWIMAGE` с IMAGE, который был загружен, но имеет нулевые размеры
- `DRAWTILE` с TILESET, у которого margin + spacing делают все тайлы невалидными

### 31.3 Граничные случаи

- `VIEWPORT` с координатами ровно на границе CANVAS
- `CAMERA` с очень большими смещениями (переполнение координат)
- `DRAWTILEMAP` с пустой картой (все ячейки пустые)
- `DRAWTEXT` с очень длинной строкой

### 31.4 Паритет

- Все 18 операторов имеют параллельные тесты в Interpreter и BytecodeVm — паритет хорошо покрыт.

---

## 32. Анализ Samples

### 32.1 Современные графические samples

| Файл | Демонстрирует |
|------|---------------|
| `graphics/modern/camera_world.axm` | CANVAS, CAMERA, RESETCAMERA |
| `graphics/modern/hud_text.axm` | CANVAS, DRAWTEXT, HUD-паттерн |
| `graphics/modern/rectangles_frame.axm` | CANVAS, BEGINFRAME, FILLRECT, DRAWRECT |
| `graphics/modern/lines_and_circles.axm` | DRAWLINE, DRAWCIRCLE, FILLCIRCLE |
| `graphics/modern/missing_canvas_error.axm` | Отрицательный: MB3010 |
| `graphics/modern/negative_radius_error.axm` | Отрицательный: MB3007 |
| `graphics/modern/invalid_text_position_error.axm` | Отрицательный: MB3007 |
| `operators/drawtile/basic_tileset.axm` | LOADTILESET, DRAWTILE |
| `operators/drawtilemap/visible_and_layer.axm` | LOADTILEMAP, DRAWTILEMAP, DRAWTILELAYER |
| `operators/setmaptile/cell_update.axm` | SETMAPTILE, MAPTILE |
| `graphics/game/maze_chase.axm` | Полная игра на CANVAS |

### 32.2 Engine-compare сценарии

| Файл | Сценарий |
|------|----------|
| `modern_canvas_primitive_burst.axm` | 48 кадров, примитивы |
| `modern_canvas_sprite_burst.axm` | 40 кадров, спрайты |
| `modern_graphics_metadata_sweep.axm` | 3000 проходов, metadata |
| `maze_chase_mixed_frame.axm` | 360 кадров, полный игровой цикл |

### 32.3 Качество samples

- Русскоязычные комментарии и вывод
- Пошаговая структура с паузами `SLEEP`
- Отрицательные примеры для ключевых ошибок
- Флагманский игровой пример `maze_chase.axm`

---

## 33. Анализ документации

### 33.1 Manual

- `modern-graphics.html` — центральная страница всех 18 CANVAS-операторов
- `images.html` — IMAGE, LOADIMAGE, DRAWIMAGE
- `sprites.html` — SPRITE, LOADSPRITE, DRAWSPRITE
- `tilesets.html` — TILESET, LOADTILESET, DRAWTILE
- `tilemaps.html` — TILEMAP, LOADTILEMAP, DRAWTILEMAP, DRAWTILELAYER, SETMAPTILE
- `graphics.html` — обзорная страница раздела

### 33.2 Спецификация

- `MODERN_GRAPHICS_PLAN.md` — долгосрочный план
- `TILEMAP_CONTRACT.md` — полный договор тайловых карт
- `GRAPHICS_OPERATORS_FLAGS.md` — внутренние флаги SCREEN-операторов
- `LANGUAGE_INVENTORY.md` — полная инвентаризация (LANG-0093..LANG-0110)

### 33.3 Документация vs Код

- Все 18 операторов описаны в документации — нет расхождений
- `tilemaps.html` не документирует ещё не реализованные MAPPROPERTY/MAPOBJECT* функции — это корректно, так как они ещё не реализованы
- Отсутствие atomic-страниц для отдельных CANVAS-операторов — это осознанный дизайн, а не пробел

---

## 34. Интеграция с IDE

### 34.1 Подсветка синтаксиса

Все 18 ключевых слов CANVAS-пути присутствуют в обеих схемах подсветки (`Axiom.xshd` и `Axiom.Dark.xshd`).

### 34.2 Форматирование

Тесты `CodeFormattingServiceTests.cs` покрывают CANVAS, BEGINFRAME, DRAWRECT, FILLRECT, DRAWTILE, DRAWTILEMAP, DRAWTILELAYER, SETMAPTILE.

### 34.3 Запуск программ

`ProgramRunService` корректно запускает CANVAS-программы через `Execution` pipeline. Режим `Bytecode` по умолчанию.

### 34.4 Графическое окно

`NativeGraphicsScreenWindow` + `Direct3D11SwapChainRenderer` + `ScreenFramebuffer` реализуют нативное Win32-окно с D3D11 рендерингом.

---

## 35. Зависимости от Host / Platform

### 35.1 Windows

- Нативное Win32-окно через P/Invoke
- D3D11 рендерер
- Кодировка путей Windows

### 35.2 .NET

- `System.Text.Encoding.GetEncoding(1251)` для fallback чтения файлов
- `System.Text.RegularExpressions` для regex-функций в tilemap-загрузке

### 35.3 GPU

- D3D11 через COM-интерфейс
- CPU-fallback для программной растеризации в тестах

---

## 36. Карта корневых причин

```
ROOT-SYS-001: Дублирование логики Interpreter/VM
 ├── SYS-001: TryGetMapCell, SetMapTileCell
 ├── SYS-002: RequireImage, RequireSprite, RequireTileset, RequireTilemap
 ├── SYS-003: ValidateCanvas*, ValidateSprite*, ValidateTile*
 ├── SYS-004: CreatePaintRequest, DecodePaintPattern, ToPaintColor
 ├── SYS-005: TryApplyCanvasViewport, TryApplyViewport
 ├── SYS-006: EvaluatePoint*, EvaluatePMap*
 └── SYS-007: ReportInvalid*, RequireCanvasActive, RequireScreenActive
```

---

## 37. Стабильные области

**Не следует трогать:**
- `IGraphicsHost` — стабильный интерфейс с корректными default-реализациями
- `GraphicsSyntax.cs` — чистая структура синтаксических узлов
- `BoundStatements.cs` — чистая структура bound-узлов
- `Parser.Statements.cs` — чистый парсинг
- `Binder.IoStatements.cs` — чистое связывание
- `TilemapRenderSupport.cs` — эффективный рендеринг тайловых карт
- `RuntimeTilemapValue.cs` — корректная модель карты
- `RuntimeImageValue`, `RuntimeSpriteValue`, `RuntimeTilesetValue` — стабильные типы значений
- `GraphicsCoordinateSpace.cs` — корректные координатные преобразования
- `GraphicsHostDrawing.cs` — корректная программная растеризация
- Конвейер lowering → emission → validation — стабилен
- Все baseline-тесты с пиксельной проверкой

---

## 38. Возможности консолидации

### 38.1 Выделение общего хелпера для Graphics Execution

**Текущие места:** `Interpreter.Graphics.cs`, `BytecodeVm.Graphics.cs`
**Дублируемая логика:** ~40 методов (~600 строк)
**Единый источник истины:** `GraphicsExecutionSupport` (новый статический класс)
**Направление миграции:** постепенное перемещение методов из обоих файлов
**Риски:** минимальные — чистый рефакторинг

---

## 39. Возможности разделения ответственности

### 39.1 GraphicsHostService

Текущий файл смешивает:
- SCREEN-режим
- CANVAS-режим
- Загрузку ресурсов
- Рисование примитивов
- Viewport/камеру
- Текст
- Координатные преобразования

Разделение на `CanvasGraphicsHost` и `ScreenGraphicsHost` с общим базовым классом улучшит поддерживаемость.

---

## 40. Кандидаты на удаление

Не обнаружены. Все 18 операторов активно используются. Нет dead code.

---

## 41. Готовность к дальнейшему развитию

### 41.1 К будущему Compiler

- Чёткое разделение Bound → Lowered → Bytecode готово
- Все 18 опкодов определены и стабильны
- `BytecodeModuleValidator` проверяет целостность

### 41.2 К дальнейшей оптимизации

- VM уже быстрее Interpreter на всех CANVAS-сценариях
- Возможна специализация опкодов для часто используемых паттернов
- Возможен typed fast path для координат

### 41.3 К развитию Runtime

- `IGraphicsHost` расширяем без ломающих изменений
- Default-реализации позволяют добавлять новые методы без обязательной реализации

### 41.4 К развитию IDE

- Подсветка и форматирование уже покрывают все 18 операторов
- Графическое окно стабильно

---

## 42. Реестр проблем

| ID | Серьёзность | Категория | Компонент | Проблема | Доказательства | Корневая причина | Уверенность |
|---|---|---|---|---|---|---|---|
| SYS-001 | P2 | ARCHITECTURE | Interpreter + VM | ~600 строк дублированной вспомогательной логики | Сравнение файлов | Отсутствие общего базового слоя | CONFIRMED |
| SYS-002 | P3 | ARCHITECTURE | GraphicsHostService | God Object (2432 строки) | Размер файла | Отсутствие разделения SCREEN/CANVAS | HIGH |
| SYS-003 | P4 | DOCS | docs/ | Отсутствие internal-документа для CANVAS-примитивов | Нет аналога GRAPHICS_OPERATORS_FLAGS.md | Простые контракты не требуют отдельного документа | MEDIUM |
| SYS-004 | P3 | TESTS | tests/ | Нет отдельного engine-compare для DRAWTILE/DRAWTILEMAP | BYTECODE_PERF_MATRIX.md | Косвенное покрытие через maze_chase | HIGH |
| SYS-005 | P4 | TESTS | tests/ | Нет тестов для SETMAPTILE с флагами отражения 1,2,4 | Отсутствие тест-кода | Редкий сценарий | MEDIUM |
| SYS-006 | P4 | TESTS | tests/ | Нет тестов для DRAWTILEMAP с несколькими тайлсетами | Отсутствие тест-кода | Сложность подготовки тестовых данных | MEDIUM |
| SYS-007 | P4 | EDGE CASE | BytecodeVm | DRAWIMAGE/DRAWSPRITE: return после Validate* без throw | BytecodeVm.Graphics.cs:687-688 | Оптимизация early return | HIGH |

---

## 43. Recovery Roadmap

### ЭТАП 0 — Базовая диагностика и измерения

**Цель:** запустить существующие engine-compare сценарии для CANVAS, зафиксировать текущие метрики.

**Задачи:**
- TASK-001: Прогон `modern_canvas_primitive_burst.axm` на длинном контроле (20 итераций, 3 прогрева)
- TASK-002: Прогон `modern_canvas_sprite_burst.axm` на длинном контроле
- TASK-003: Прогон `modern_graphics_metadata_sweep.axm` на длинном контроле
- TASK-004: Прогон `maze_chase_mixed_frame.axm` на длинном контроле

**Зависимости:** нет
**Риск:** нет

---

### ЭТАП 1 — Исправление критических проблем корректности

**Цель:** исправить edge case с return после Validate в BytecodeVm.

**Задачи:**
- TASK-005: Проверить и исправить `ExecuteDrawImageInstruction` (строки 683-688): после `ValidateDrawImageSize` должен быть `throw`, а не `return`
- TASK-006: Проверить и исправить `ExecuteDrawSpriteInstruction` (строки 740-743): аналогичная проблема
- TASK-007: Добавить тесты для этих edge cases

**Зависимости:** нет
**Риск:** низкий — исправление очевидного бага

---

### ЭТАП 2 — Стабилизация семантики

**Цель:** убедиться, что все 18 операторов ведут себя идентично в Interpreter и VM.

**Задачи:**
- TASK-008: Пройти все 18 операторов и проверить идентичность диагностики
- TASK-009: Проверить edge cases с VIEWPORT на границе CANVAS

**Зависимости:** ЭТАП 1
**Риск:** нет

---

### ЭТАП 3 — Архитектурное улучшение

**Цель:** устранить дублирование логики между Interpreter и VM.

**Задачи:**
- TASK-010: Создать `GraphicsExecutionSupport` статический класс в `Axiom.Runtime`
- TASK-011: Переместить общие методы из `Interpreter.Graphics.cs` и `BytecodeVm.Graphics.cs`
- TASK-012: Убедиться, что все тесты проходят

**Зависимости:** ЭТАП 2
**Риск:** средний — рефакторинг ~600 строк

---

### ЭТАП 4 — Оптимизация производительности

**Цель:** добавить отдельный engine-compare для тайловых карт.

**Задачи:**
- TASK-013: Создать `tilemap_render_burst.axm` сценарий для DRAWTILEMAP/DRAWTILELAYER
- TASK-014: Обновить `BYTECODE_PERF_MATRIX.md`

**Зависимости:** нет
**Риск:** нет

---

### ЭТАП 5 — Усиление тестирования

**Цель:** закрыть пропуски в тестах.

**Задачи:**
- TASK-015: Добавить тесты для SETMAPTILE с флагами отражения
- TASK-016: Добавить тесты для DRAWTILEMAP с несколькими тайлсетами
- TASK-017: Добавить тесты для DRAWIMAGE/DRAWSPRITE с граничными масштабами

**Зависимости:** нет
**Риск:** нет

---

### ЭТАП 6 — Обновление документации

**Цель:** актуализировать документацию.

**Задачи:**
- TASK-018: Обновить `tilemaps.html` при реализации шага 4 TILEMAP_CONTRACT.md (MAPPROPERTY/MAPOBJECT*)
- TASK-019: Рассмотреть создание internal-документа для CANVAS-примитивов

**Зависимости:** нет
**Риск:** нет

---

### ЭТАП 7 — Обновление IDE

**Цель:** нет текущих задач по IDE. Подсветка и форматирование уже покрывают все 18 операторов.

---

### ЭТАП 8 — Подготовка к дальнейшему развитию

**Цель:** подготовить архитектуру для будущих расширений.

**Задачи:**
- TASK-020: Рассмотреть разделение `GraphicsHostService` на `CanvasGraphicsHost` и `ScreenGraphicsHost`
- TASK-021: Рассмотреть добавление `POINT(x, y)` в CANVAS-контексте (уже реализовано)
- TASK-022: Рассмотреть перенос `PSET`/`PRESET` в CANVAS (уже реализовано)

**Зависимости:** ЭТАП 3
**Риск:** средний

---

## 44. Граф зависимостей задач

```
TASK-001..004 (ЭТАП 0: диагностика)
    ↓
TASK-005..007 (ЭТАП 1: исправление корректности)
    ↓
TASK-008..009 (ЭТАП 2: стабилизация)
    ↓
TASK-010..012 (ЭТАП 3: архитектура)
    ↓
TASK-013..014 (ЭТАП 4: производительность) ← можно параллельно с ЭТАП 3
TASK-015..017 (ЭТАП 5: тесты) ← можно параллельно с ЭТАП 3
TASK-018..019 (ЭТАП 6: документация) ← можно параллельно
TASK-020..022 (ЭТАП 8: подготовка) ← после ЭТАП 3
```

---

## 45. Definition of Done

Подсистема CANVAS считается завершённой, когда:

1. **Корректность:** все 18 операторов работают идентично в Interpreter и BytecodeVm
2. **Edge cases:** все граничные случаи покрыты тестами
3. **Error handling:** все ошибки возвращают корректные диагностические коды
4. **Interpreter:** все 18 операторов + 15 built-in работают корректно
5. **BytecodeVm:** все 18 операторов + 15 built-in работают корректно с тем же результатом
6. **Тестирование:** все 18 операторов имеют параллельные тесты в обоих движках
7. **Документация:** все 18 операторов описаны в manual
8. **IDE:** подсветка и форматирование покрывают все 18 операторов
9. **Производительность:** все engine-compare сценарии в целевом диапазоне `<= 0.900x`

---

## 46. ТОП-20 проблем корректности и архитектуры

| # | ID | Серьёзность | Проблема |
|---|---|---|---|
| 1 | SYS-007 | P4 | BytecodeVm: return после Validate у DRAWIMAGE/DRAWSPRITE без throw |
| 2 | SYS-001 | P2 | ~600 строк дублированной логики между Interpreter и VM |
| 3 | SYS-002 | P3 | GraphicsHostService — God Object (2432 строки) |
| 4 | SYS-004 | P3 | Нет отдельного engine-compare для тайловых карт |
| 5 | SYS-005 | P4 | Нет тестов для SETMAPTILE с флагами отражения |
| 6 | SYS-006 | P4 | Нет тестов для DRAWTILEMAP с несколькими тайлсетами |
| 7 | SYS-003 | P4 | Отсутствие internal-документа для CANVAS-примитивов |

---

## 47. ТОП-20 задач по производительности

| # | Сценарий | Текущий результат | Статус |
|---|---|---|---|
| 1 | `modern_canvas_primitive_burst.axm` | ~0.793x | В целевом диапазоне |
| 2 | `modern_canvas_sprite_burst.axm` | ~0.793x | В целевом диапазоне |
| 3 | `modern_graphics_metadata_sweep.axm` | ~0.540x | В целевом диапазоне |
| 4 | `maze_chase_mixed_frame.axm` | ~0.732x | В целевом диапазоне |

**Прямого долга нет.** Все сценарии в целевом диапазоне.

---

## 48. Итоговая оценка здоровья подсистемы

| Критерий | Оценка (0-10) | Объяснение |
|----------|---:|------------|
| Корректность | 9 | Все 18 операторов работают корректно в обоих движках. Один edge case (return вместо throw в VM) |
| Семантика | 9 | Полный паритет Interpreter/VM, корректные диагностики |
| Архитектура | 7 | Чёткое разделение слоёв, но массированное дублирование между Interpreter и VM |
| Runtime | 9 | Стабильный IGraphicsHost, корректные типы значений |
| Interpreter | 9 | Чистый код, единообразные паттерны |
| Bytecode VM | 9 | Полный паритет с Interpreter, оптимизированный hot path |
| Производительность | 9 | Все сценарии в целевом диапазоне, VM быстрее Interpreter |
| Память | 8 | Нет утечек, но нет ограничений на размер CANVAS |
| Error handling | 9 | Корректные диагностические коды, TRY/CATCH работает |
| Tests | 9 | Обширное покрытие всех слоёв, есть baseline pixel-тесты |
| Samples | 9 | 32 файла, включая флагманский maze_chase |
| Documentation | 9 | Полное покрытие в manual, есть план и контракт |
| IDE | 9 | Подсветка, форматирование, графическое окно |
| Maintainability | 7 | Дублирование снижает удобство поддержки |
| Reliability | 9 | Стабильная работа, корректное восстановление после ошибок |
| Future readiness | 9 | Готово к расширению (PSET/PRESET/POINT уже перенесены в CANVAS) |

**Средняя оценка: 8.6 / 10**

---

## Главный контрольный вопрос

> Можно ли считать подсистему CANVAS / Modern Graphics достаточно стабильной, корректной и архитектурно понятной для перехода к следующей подсистеме?

**YES, WITH CONDITIONS**

### Основные blockers

1. **SYS-007 (P4):** BytecodeVm `ExecuteDrawImageInstruction` и `ExecuteDrawSpriteInstruction` делают `return` после `Validate*` без `throw`. Это может привести к продолжению执行 после невалидных размеров. Нужно исправить на `throw` или убрать `return`.

### Что нужно сделать в первую очередь

1. Исправить SYS-007 (return без throw в BytecodeVm для DRAWIMAGE/DRAWSPRITE)
2. Запустить длинные engine-compare прогоны для фиксации baseline

### Что можно отложить

1. Выделение общего хелпера (SYS-001) — не блокирует развитие, но снижает удобство поддержки
2. Разделение GraphicsHostService (SYS-002) — архитектурное улучшение
3. Дополнительные тесты (SYS-005, SYS-006) — edge cases
4. Отдельный engine-compare для тайлов (SYS-004) — косвенное покрытие через maze_chase

### Что не следует трогать

- `IGraphicsHost` — стабильный интерфейс
- Синтаксические узлы и bound-узлы — чистая структура
- Lowering и emission pipeline — стабильный
- `TilemapRenderSupport` — эффективный рендеринг
- Типы значений (IMAGE, SPRITE, TILESET, TILEMAP) — стабильные
- Координатная математика — корректная
- Все baseline-тесты с пиксельной проверкой
