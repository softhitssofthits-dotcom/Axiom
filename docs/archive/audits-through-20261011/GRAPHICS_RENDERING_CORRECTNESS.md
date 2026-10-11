# AXIOM — ГЛУБОКИЙ FORENSIC-АУДИТ
## GRAPHICS RENDERING CORRECTNESS

---

## 1. Резюме

Проведён полный forensic-аудит подсистемы графического рендеринга Axiom. Подсистема охватывает два режима рисования: исторический `SCREEN` (палитровая модель на 4 страницах) и современный `CANVAS` (полноцветный кадровый цикл). Оба режима работают через единый интерфейс `IGraphicsHost`, реализованный в IDE как `GraphicsHostService` с Direct3D11-рендерингом.

**Главные выводы:**
- Подсистема архитектурно зрелая: чёткое разделение Parser → Binder → Lowerer → BytecodeCompiler → VM/Interpreter → Host.
- Обнаружена 1 критическая ошибка корректности: `GetPixel` в режиме `CANVAS` не применяет смещение камеры, в отличие от `SetPixel`.
- Обнаружена 1 существенная ошибка: `LINE`-валидатор в bytecode всегда снимает 4 координаты со стека, даже когда startPoint не задан (ожидается 2).
- Обнаружено 4 независимых реализации нормализации цвета с разным поведением на граничных значениях.
- STEP-координаты для `PSET`, `PAINT` и canvas-примитивов не защищены от переполнения `long`, в отличие от `LINE`/`CIRCLE`/`GET`/`PUT`.
- Interpreter ↔ BytecodeVM паритет на уровне семантики подтверждён, но отсутствуют попиксельные тесты паритета.
- Производительность VM: все 6 графических benchmark-сценариев показывают ≤1.000x (нет регрессий).

---

## 2. Что представляет собой эта подсистема

Графическая подсистема Axiom отвечает за весь визуальный вывод программ пользователя. Она включает:

- **Два режима рисования:** исторический `SCREEN` (режимы 0–10, палитра 256 цветов, 4 страницы) и современный `CANVAS` (произвольный размер, полноцветный BGRA, кадровый цикл `BEGINFRAME`/`PRESENT`).
- **Текстовый слой:** `PRINT`, `CLS`, `COLOR`, `LOCATE`, `VIEW PRINT` с растеризацией bitmap-шрифта 8×12.
- **Пиксельные примитивы:** `PSET`, `PRESET`, `LINE`, `CIRCLE`, `PAINT`, `DRAW` (макроязык).
- **Блочные операции:** `GET`, `PUT`, `PCOPY`.
- **Современные примитивы:** `DRAWRECT`, `FILLRECT`, `DRAWLINE`, `DRAWCIRCLE`, `FILLCIRCLE`, `DRAWTEXT`.
- **Работа с ресурсами:** `IMAGE`, `SPRITE`, `TILESET`, `TILEMAP` с загрузкой и отрисовкой.
- **Координатная система:** `VIEW`, `WINDOW`, `PMAP`, `VIEWPORT`, `CAMERA`, `RESETCAMERA`.
- **Ввод:** мышь (`MOUSEX`, `MOUSEY`, `MOUSEBUTTON`, `MOUSEPRESS`, `ON MOUSE`) и клавиатура (`INKEY`, `KEYDOWN`, `KEYPRESS`, `ON KEY`).
- **Палитра:** `PALETTE`, `PALETTE USING`, `RGB`.

Подсистема проходит через весь конвейер языка: лексер → парсер → семантика → lowering → байткод → VM/Interpreter → IDE host.

---

## 3. Полный список возможностей

### SCREEN-режим (исторический BASIC-слой)

| ID | Сущность | Тип | Статус |
|---|---|---|---|
| SYS-G01 | `SCREEN` (режимы 0–10) | STATEMENT | IMPLEMENTED |
| SYS-G02 | `SCREEN 0` (текстовый режим) | STATEMENT | IMPLEMENTED |
| SYS-G03 | `CLS` / `CLS color` | STATEMENT | IMPLEMENTED |
| SYS-G04 | `COLOR foreground[, background]` | STATEMENT | IMPLEMENTED |
| SYS-G05 | `LOCATE row, column` | STATEMENT | IMPLEMENTED |
| SYS-G06 | `PSET [STEP](x, y)[, color]` | STATEMENT | IMPLEMENTED |
| SYS-G07 | `PRESET [STEP](x, y)[, color]` | STATEMENT | IMPLEMENTED |
| SYS-G08 | `LINE [[STEP](x1,y1)]-[STEP](x2,y2)[, color][, B|BF]` | STATEMENT | IMPLEMENTED |
| SYS-G09 | `CIRCLE [STEP](x,y), radius[, color[, start[, end[, aspect]]]]` | STATEMENT | IMPLEMENTED |
| SYS-G10 | `PAINT [STEP](x,y)[, fill[, border[, background]]]` | STATEMENT | IMPLEMENTED |
| SYS-G11 | `DRAW string` | STATEMENT | IMPLEMENTED |
| SYS-G12 | `GET [STEP](x1,y1)-[STEP](x2,y2), buffer` | STATEMENT | IMPLEMENTED |
| SYS-G13 | `PUT [STEP](x,y), buffer[, action]` | STATEMENT | IMPLEMENTED |
| SYS-G14 | `VIEW [SCREEN][(x1,y1)-(x2,y2)][, fill[, border]]` | STATEMENT | IMPLEMENTED |
| SYS-G15 | `VIEW PRINT top TO bottom` | STATEMENT | IMPLEMENTED |
| SYS-G16 | `WINDOW [[SCREEN] (x1,y1)-(x2,y2)]` | STATEMENT | IMPLEMENTED |
| SYS-G17 | `PALETTE [USING]` | STATEMENT | IMPLEMENTED |
| SYS-G18 | `PCOPY sourcePage, destPage` | STATEMENT | IMPLEMENTED |
| SYS-G19 | `POINT(x, y)` / `POINT(selector)` | FUNCTION | IMPLEMENTED |
| SYS-G20 | `PMAP(value, selector)` | FUNCTION | IMPLEMENTED |
| SYS-G21 | `RGB(r, g, b)` | FUNCTION | IMPLEMENTED |
| SYS-G22 | `SCREENISACTIVE()` | FUNCTION | IMPLEMENTED |
| SYS-G23 | `SCREENMODE()` | FUNCTION | IMPLEMENTED |
| SYS-G24 | `SCREENWIDTH()` / `SCREENHEIGHT()` | FUNCTION | IMPLEMENTED |

### CANVAS-режим (современный слой)

| ID | Сущность | Тип | Статус |
|---|---|---|---|
| SYS-G30 | `CANVAS width, height` | STATEMENT | IMPLEMENTED |
| SYS-G31 | `BEGINFRAME` | STATEMENT | IMPLEMENTED |
| SYS-G32 | `PRESENT` | STATEMENT | IMPLEMENTED |
| SYS-G33 | `CAMERA x, y` | STATEMENT | IMPLEMENTED |
| SYS-G34 | `RESETCAMERA` | STATEMENT | IMPLEMENTED |
| SYS-G35 | `VIEWPORT [(x1,y1)-(x2,y2)]` | STATEMENT | IMPLEMENTED |
| SYS-G36 | `DRAWRECT x, y, w, h, color` | STATEMENT | IMPLEMENTED |
| SYS-G37 | `FILLRECT x, y, w, h, color` | STATEMENT | IMPLEMENTED |
| SYS-G38 | `DRAWLINE x1, y1, x2, y2, color` | STATEMENT | IMPLEMENTED |
| SYS-G39 | `DRAWCIRCLE x, y, r, color` | STATEMENT | IMPLEMENTED |
| SYS-G40 | `FILLCIRCLE x, y, r, color` | STATEMENT | IMPLEMENTED |
| SYS-G41 | `DRAWTEXT text, x, y, color` | STATEMENT | IMPLEMENTED |
| SYS-G42 | `TEXTWIDTH(text)` / `TEXTHEIGHT(text)` | FUNCTION | IMPLEMENTED |

### Ресурсы (IMAGE, SPRITE, TILESET, TILEMAP)

| ID | Сущность | Тип | Статус |
|---|---|---|---|
| SYS-G50 | `IMAGE` (тип) | TYPE | IMPLEMENTED |
| SYS-G51 | `LOADIMAGE(path)` | FUNCTION | IMPLEMENTED |
| SYS-G52 | `IMAGEWIDTH(image)` / `IMAGEHEIGHT(image)` | FUNCTION | IMPLEMENTED |
| SYS-G53 | `DRAWIMAGE image, x, y[, w, h]` | STATEMENT | IMPLEMENTED |
| SYS-G54 | `SPRITE` (тип) | TYPE | IMPLEMENTED |
| SYS-G55 | `LOADSPRITE(path, frameW, frameH)` | FUNCTION | IMPLEMENTED |
| SYS-G56 | `SPRITEWIDTH/HEIGHT/COUNT` | FUNCTION | IMPLEMENTED |
| SYS-G57 | `DRAWSPRITE sprite, frame, x, y[, w, h]` | STATEMENT | IMPLEMENTED |
| SYS-G58 | `TILESET` (тип) | TYPE | IMPLEMENTED |
| SYS-G59 | `LOADTILESET(path, tileW, tileH[, margin, spacing])` | FUNCTION | IMPLEMENTED |
| SYS-G60 | `TILECOUNT/WIDTH/HEIGHT` | FUNCTION | IMPLEMENTED |
| SYS-G61 | `DRAWTILE tileset, index, x, y` | STATEMENT | IMPLEMENTED |
| SYS-G62 | `TILEMAP` (тип) | TYPE | IMPLEMENTED |
| SYS-G63 | `LOADTILEMAP(path)` | FUNCTION | IMPLEMENTED |
| SYS-G64 | `MAPWIDTH/MAPHEIGHT/MAPPIXELWIDTH/MAPPIXELHEIGHT` | FUNCTION | IMPLEMENTED |
| SYS-G65 | `DRAWTILEMAP tilemap, x, y` | STATEMENT | IMPLEMENTED |
| SYS-G66 | `DRAWTILELAYER tilemap, layer, x, y` | STATEMENT | IMPLEMENTED |
| SYS-G67 | `SETMAPTILE tilemap, layer, col, row, ts, ti[, flags]` | STATEMENT | IMPLEMENTED |
| SYS-G68 | `MAPTILE/MAPTILESET/MAPTILEFLAGS` | FUNCTION | IMPLEMENTED |

### Ввод

| ID | Сущность | Тип | Статус |
|---|---|---|---|
| SYS-G70 | `MOUSEX()` / `MOUSEY()` | FUNCTION | IMPLEMENTED |
| SYS-G71 | `MOUSEBUTTON()` / `MOUSEPRESS()` | FUNCTION | IMPLEMENTED |
| SYS-G72 | `ON MOUSE` | EVENT | IMPLEMENTED |
| SYS-G73 | `INKEY()` / `KEYDOWN()` / `KEYPRESS()` | FUNCTION | IMPLEMENTED |
| SYS-G74 | `ON KEY` | EVENT | IMPLEMENTED |

---

## 4. Фактическая архитектура

### Уровень 1: Лексер (`Axiom.Lexing`)
- **Файл:** `Lexer.cs`, `TokenKind.cs`
- **Ответственность:** распознавание всех графических ключевых слов (39 токенов: `ScreenKeyword`, `CanvasKeyword`, `LineKeyword`, `CircleKeyword`, `PaintKeyword`, `DrawRectKeyword`, `DrawImageKeyword`, `DrawSpriteKeyword` и т.д.)
- **Зависимости:** нет
- **Состояние:** стабильно, без проблем

### Уровень 2: Синтаксис (`Axiom.Syntax`)
- **Файл:** `GraphicsSyntax.cs` (~1460 строк)
- **Ответственность:** 34 узла AST для всех графических statements
- **Зависимости:** `Axiom.Core`, `Axiom.Lexing`
- **Состояние:** стабильно, монолитный файл но допустимого размера

### Уровень 3: Парсер (`Axiom.Parsing`)
- **Файлы:** `Parser.Statements.cs`, `Parser.Expressions.cs`, `Parser.Helpers.cs`
- **Ответственность:** разбор всех графических statements (33 метода парсинга)
- **Зависимости:** `Axiom.Lexing`, `Axiom.Syntax`
- **Состояние:** стабильно, все statements покрыты парсером

### Уровень 4: Семантика / Binder (`Axiom.Semantics`)
- **Файлы:** `Binder.IoStatements.cs` (36 методов привязки), `BoundStatements.cs`, `BoundExpressions.cs`
- **Ответственность:** проверка типов, разрешение имён, построение bound-дерева
- **Зависимости:** `Axiom.Core`, `Axiom.Syntax`
- **Состояние:** стабильно, все графические statements привязаны

### Уровень 5: Lowering (`Axiom.Bytecode`)
- **Файлы:** `BoundProgramLowerer.cs`, `LoweredInstruction.cs`, `LoweredInstructionKind.cs`, `Opcode.cs`
- **Ответственность:** понижение bound-дерева в линейное lowered-представление
- **Опкоды:** 34 графических опкода (`Screen`, `Canvas`, `BeginFrame`, `Present`, `Camera`, `Viewport`, `ResetCamera`, `View`, `ViewPrint`, `Window`, `Cls`, `Color`, `Locate`, `Palette`, `PCopy`, `PSet`, `Get`, `Put`, `Line`, `Circle`, `Paint`, `Draw`, `DrawRect`, `FillRect`, `DrawLine`, `DrawCircle`, `FillCircle`, `DrawText`, `DrawImage`, `DrawSprite`, `DrawTile`, `DrawTilemap`, `DrawTileLayer`, `SetMapTile`)
- **Зависимости:** `Axiom.Semantics`
- **Состояние:** работает, но 14 опкодов отсутствуют в `TryGetStackEffect` валидатора

### Уровень 6: Валидатор байткода (`Axiom.Bytecode`)
- **Файл:** `BytecodeModuleValidator.cs`
- **Ответственность:** проверка целостности байткодного модуля до исполнения
- **Проблема:** `LINE`-валидатор всегда снимает 4 координаты, даже без explicit startPoint

### Уровень 7: BytecodeCompiler (`Axiom.Bytecode`)
- **Файл:** `BytecodeCompiler.cs`
- **Ответственность:** эмиссия lowered-представления в финальный байткод

### Уровень 8: Interpreter (`Axiom.Runtime`)
- **Файлы:** `Interpreter.Graphics.cs` (1451 строка), `Interpreter.Statements.cs`, `Interpreter.Expressions.cs`
- **Ответственность:** tree-walking исполнение графических statements
- **Методы:** 28 statement-методов + 13 built-in функций
- **Вспомогательные файлы:** `GraphicsDrawParser.cs`, `GraphicsDrawExecutor.cs`, `GraphicsEllipseRasterizer.cs`, `GraphicsFloodFill.cs`, `GraphicsLineMath.cs`, `GraphicsBufferTransfer.cs`, `GraphicsCoordinateValidation.cs`, `GraphicsCircleValidation.cs`, `GraphicsInstructionArguments.cs`, `GraphicsPaintRequest.cs`
- **Состояние:** работает, несколько edge cases

### Уровень 9: BytecodeVm (`Axiom.Runtime`)
- **Файлы:** `BytecodeVm.Graphics.cs` (1692 строки), `BytecodeVm.cs`, `BytecodeVm.Builtins.cs`
- **Ответственность:** исполнение байткода, диспетчеризация опкодов
- **Методы:** 34 опкод-метода + 27 built-in методов
- **Состояние:** работает, паритет с интерпретатором подтверждён

### Уровень 10: Host Interface (`Axiom.Runtime`)
- **Файл:** `IGraphicsHost.cs` (279 строк)
- **Ответственность:** абстракция графического хоста между runtime и IDE
- **Вспомогательные:** `GraphicsHostDrawing.cs`, `GraphicsCoordinateSpace.cs`, `GraphicsViewport.cs`, `CanvasTextMetrics.cs`
- **Состояние:** стабильно

### Уровень 11: IDE Host (`Axiom.Ide`)
- **Файлы:** `GraphicsHostService.cs` (~2400 строк), `NativeGraphicsScreenWindow.cs`, `Direct3D11SwapChainRenderer.cs`, `ScreenFramebuffer.cs`, `ScreenTextBuffer.cs`, `ScreenTextRasterizer.cs`
- **Ответственность:** реальный рендеринг через Win32 + Direct3D11
- **Состояние:** работает, несколько архитектурных проблем

---

## 5. Полный путь выполнения

```
Исходный код .axm
      ↓
  Lexer (TokenKind: ScreenKeyword, LineKeyword, ...)
      ↓
  Parser (ParseScreenStatement, ParseLineStatement, ...)
      ↓
  Syntax (ScreenStatementSyntax, LineStatementSyntax, ...)
      ↓
  Binder (BindScreenStatement, BindLineStatement, ...)
      ↓
  Bound Model (BoundScreenStatement, BoundLineStatement, ...)
      ↓
  BoundProgramLowerer → LoweredInstruction
      ↓
  BytecodeCompiler → BytecodeModule (Opcode.Screen, Opcode.Line, ...)
      ↓
  ┌─────────────────────────────────┐
  │  Interpreter      │  BytecodeVm  │
  │  (tree-walking)   │  (dispatch)  │
  └────────┬──────────┴──────┬───────┘
           ↓                 ↓
      IGraphicsHost (единый интерфейс)
           ↓
  GraphicsHostService (IDE)
           ↓
  ScreenFramebuffer (indexed + true-color)
           ↓
  NativeGraphicsScreenWindow (Win32 HWND)
           ↓
  Direct3D11SwapChainRenderer (GPU)
           ↓
  Экран пользователя
```

---

## 6. Фактический семантический контракт

### SCREEN mode
- **Вход:** `SCREEN mode[, colorswitch, activePage, visualPage]`
- **Режимы 1–10:** активируют графическую поверхность с фиксированным разрешением
- **Режим 0:** текстовый режим 80×25 с прокруткой
- **Выход:** графическое окно или текстовое окно
- **Побочные эффекты:** сброс всех графических состояний (viewport, window, palette, cursor)

### CANVAS mode
- **Вход:** `CANVAS width, height`
- **Обработка:** создаёт полноцветный framebuffer произвольного размера
- **Кадровый цикл:** `BEGINFRAME` → рисование → `PRESENT`
- **Камера:** `CAMERA x, y` сдвигает мировые координаты
- **Выход:** Direct3D11 swap chain

### Цвета
- **SCREEN:** индексы 0–255 через палитру (CGA/EGA + 6×6×6 RGB cube + grayscale ramp)
- **CANVAS:** полноцветные BGRA 32-bit значения через `RGB(r,g,b)`
- **Нормализация:** `ABS(color) MOD 256` для индексных цветов

### Текущая графическая позиция
- **Общая** для `PSET`, `PRESET`, `LINE`, `CIRCLE`, `PAINT`, `DRAW`
- Обновляется после каждого рисования в конечную точку/центр

---

## 7. Анализ Parser

**Статус:** Стабильно.

**Что работает:**
- Все 34 графических statement корректно разобраны
- Выражения-аргументы (координаты, цвета, размеры) корректно разбираются
- STEP-модификаторы корректно распознаются
- DRAW-строки корректно передаются как выражения

**Проблемы:**
- Нет тестов восстановления после ошибок для графических statements
- Нет тестов для выражений-аргументов в SCREEN, CANVAS, VIEWPORT (только литералы)

---

## 8. Анализ Binder

**Статус:** Стабильно.

**Что работает:**
- Все 36 методов привязки графических statements работают корректно
- Типы аргументов проверяются (LONG для координат, INTEGER для цветов, DOUBLE для радиусов)
- STEP-флаги корректно привязываются
- Режимные проверки (SCREEN-only vs CANVAS-only vs shared) выполняются на этапе привязки

**Проблемы:**
- `ExecuteColor` в interpreter использует `BackgroundExpression!` с null-forgiving оператором; если Binder ошибочно допустит `BackgroundMode=Color` с null-выражением, будет NRE в runtime

---

## 9. Анализ Interpreter

### Корректность

**P0 — `GetPixel` не применяет камеру в CANVAS-режиме:**
- `SetLogicalPixelCore` для CANVAS применяет `TranslateCanvasX/Y` (смещение камеры)
- `GetLogicalPixelCore` для CANVAS вызывает `GetPixelCore(x, y)` напрямую без камеры
- **Результат:** чтение пикселя после записи при ненулевой камере возвращает 0 (или неверный пиксель)
- **Место:** `GraphicsHostService.cs`, `GetLogicalPixelCore`

**P1 — STEP без защиты от переполнения:**
- `PSET`, `PAINT` и canvas-примитивы используют простое `x += _host.CurrentGraphicsX`
- `LINE`, `CIRCLE`, `GET`, `PUT` используют `GraphicsLineMath.SaturatingAdd` через `TryResolvePoint`
- **Результат:** при координатах near `long.MaxValue` возможен silent overflow
- **Место:** `Interpreter.Graphics.cs`, ExecutePSet (строка 301–305), ExecutePaint (строка 434–438)

**P1 — `CIRCLE` цвет по умолчанию = 15 вместо `CurrentForegroundColor`:**
- `LINE` и `PSet` по умолчанию берут `_host.CurrentForegroundColor`
- `CIRCLE` хардкодит `15` (белый в стандартной палитре)
- **Результат:** после `COLOR 4` (красный) `CIRCLE (100,100), 50` рисует белый, а не красный
- **Место:** `Interpreter.Graphics.cs`, строка 392
- **Примечание:** может быть осознанным решением совместимости с QuickBASIC, но не документировано

**P2 — `ApplyPaletteUsing` не прерывает выполнение при ошибке:**
- При невыделенном массиве или неверном ранге вызывается `ReportInvalidRuntimeState`, но **не** выбрасывает `RuntimeFailureException`
- Выполнение продолжается с потенциально повреждённой палитрой
- **Место:** `Interpreter.Graphics.cs`, строки 258, 264

**P2 — `DRAWTILEMAP` и `DRAWTILELAYER` не обновляют графическую позицию:**
- `DRAWTILE` обновляет через `SetGraphicsCurrentPosition` (строка 599)
- `DRAWTILEMAP` и `DRAWTILELAYER` — нет
- **Результат:** после `DRAWTILEMAP` текущая позиция "заморожена"

**P3 — Несогласованность в обновлении позиции:**
- Canvas-примитивы обновляют позицию явно в interpreter
- SCREEN-операторы делегируют обновление в host (`DrawLine`, `DrawCircle`)
- Смешанный подход усложняет сопровождение

### Дублирование логики

1. **Нормализация цвета** — 4 независимые реализации:
   - `GraphicsDrawExecutor.NormalizeColor(long, int)` — **не обрабатывает `long.MinValue`** (выбрасывает)
   - `GraphicsFloodFill.NormalizeColor(int)` — безопасна для `int`
   - `GraphicsBufferTransfer.NormalizeIntegralColor(long)` — **самая корректная** (через `ulong`)
   - `GraphicsBufferTransfer.TryNormalizePutSourceColor` — для Double/Decimal

2. **Размер валидации:** `ValidateDrawImageSize`, `ValidateSpriteFrameSize`, `ValidateDrawSpriteSize` — идентичны, различаются только строкой операции

3. **Разрешение STEP:** один и тот же паттерн `x += _host.CurrentGraphicsX; y += _host.CurrentGraphicsY` скопирован 5 раз

4. **Свойства ресурсов:** `GetImageWidth`, `GetImageHeight`, `GetSpriteWidth` и 12 других static-методов полностью дублируются между `Interpreter.Graphics.cs` и `BytecodeVm.Graphics.cs`

---

## 10. Анализ Bytecode / VM

### Lowering

**Статус:** Работает для всех 34 опкодов.

**Проблемы валидатора:**
- **14 опкодов отсутствуют в `TryGetStackEffect`**: `Screen`, `PCopy`, `View`, `Window`, `Color`, `Locate`, `Palette`, `PSet`, `Get`, `Put`, `Line`, `Circle`, `Paint`, `Draw`. Stack height walker останавливается на этих инструкциях, downstream-проверки не выполняются.
- **Баг валидатора `LINE`**: когда `hasExplicitStartPoint = false`, lowerer кладёт 2 координаты + optional color (2–3 значения), но валидатор всегда снимает 4 координаты.
- **`Palette` не проверяет типы стека**: только метаданные операндов валидируются.
- **`Draw` не проверяет тип командной строки**.

**Проблемы дизайна:**
- Поле `mouseEventKind` в `LoweredInstruction` используется для хранения графических флагов (relative, hasColor, boxKind). Имя не соответствует содержанию.
- `ValueCount` для `DrawImage`/`DrawSprite` хранит 0/1 (boolean hasScale) вместо реального числа значений на стеке.

### BytecodeVm

**Статус:** Работает, паритет с interpreter подтверждён.

**Проблемы:**
- **PAINT STEP**: тот же `long` overflow, что и в interpreter (строки 497–501)
- **Canvas-опкоды** используют `PopUnchecked()` без проверки границ стека, в отличие от SCREEN-опкодов которые используют `PopValue()` с проверкой
- **Dead code**: статические `Get*` методы (строки 1053–1096) в `BytecodeVm.Graphics.cs` не вызываются — реальные fast-path в `BytecodeVm.cs`

---

## 11. Анализ Runtime

### Runtime Value типы

| Тип | Файл | Назначение |
|---|---|---|
| `RuntimeImageValue` | `RuntimeImageValue.cs` | Хранит `uint[]` BGRA пиксели + размеры |
| `RuntimeSpriteValue` | `RuntimeSpriteValue.cs` | Хранит image + размеры кадра + количество кадров |
| `RuntimeTilesetValue` | `RuntimeTilesetValue.cs` | Хранит image + tileSize + tileCount + margin/spacing |
| `RuntimeTilemapValue` | `RuntimeTilemapValue.cs` | Хранит Tiled JSON данные: слои, тайлсеты, объекты |

### IGraphicsHost — ключевой контракт

Интерфейс (~279 строк) определяет 60+ методов с default-реализациями (no-op). Host переопределяет только нужные. Критичные default-поведения:
- `DrawLine`, `DrawCircle`, `Paint` делегируют в `GraphicsHostDrawing` (software rasterization)
- `SetAbsolutePixel`/`GetAbsolutePixel` — no-op (могут быть потеряны пиксели на default host)

---

## 12. Взаимодействие с типами

| Тип | Использование в графике |
|---|---|
| `INTEGER` | Цвета, индексы палитры, координаты (приводятся к LONG), номера страниц, флаги |
| `LONG` | Координаты x/y, размеры canvas, координаты камеры |
| `DOUBLE` | Радиус CIRCLE, углы, aspect ratio, WINDOW-координаты |
| `STRING` | DRAW-команды, текст для DRAWTEXT, паттерны PAINT |
| `BOOLEAN` | SCREENISACTIVE, флаги видимости слоёв |
| `IMAGE` | RuntimeImageValue — загрузка, отрисовка, получение размеров |
| `SPRITE` | RuntimeSpriteValue — спрайтовый лист, кадры |
| `TILESET` | RuntimeTilesetValue — атлас тайлов |
| `TILEMAP` | RuntimeTilemapValue — карта из Tiled JSON |
| `ARRAY` | Буфер GET/PUT (2D массив), палитра USING |

---

## 13. Взаимодействие со String

- DRAW-строки разбираются через `GraphicsDrawParser` (рекурсивный descent-парсер)
- DRAWTEXT принимает строку для отрисовки
- PAINT паттерны кодируются как строки (8-битные, 1–64 байта, IBM CP866)
- Пустая DRAW-строка = пустой список команд = ничего не рисует (корректно)
- Пустая строка DRAWTEXT = ничего не рисует (корректно)

---

## 14. Взаимодействие с Array

- GET захватывает прямоугольную область в 2D массив
- PUT выводит 2D массив на поверхность с операциями PSET/PRESET/AND/OR/XOR
- PALETTE USING принимает массив из 256 цветов
- RuntimeArrayValue используется для всех массивных данных
- Проверка границ массива выполняется в `GraphicsBufferTransfer.TryGetVisibleLength`

---

## 15. Взаимодействие с TYPE

- IMAGE, SPRITE, TILESET, TILEMAP — все являются пользовательскими типами
- Копирование записей при присваивании (value semantics)
- Поля читаются через точку: `IMAGEWIDTH(img)`, `SPRITEWIDTH(spr)`
- Нет пользовательских конструкторов для графических типов

---

## 16. Взаимодействие с Functions / SUB

- Графические операторы работают как statements (не возвращают значений)
- Графические функции работают как expressions (возвращают INTEGER, LONG или значения ресурсов)
- Нет closures или callback-ов в графическом слое
- MOUSE/KEY обработчики (`ON MOUSE`, `ON KEY`) работают через event-систему runtime

---

## 17. Взаимодействие с Control Flow

- Графические statements работают внутри любых control flow конструкций (IF, FOR, WHILE, DO, SELECT CASE)
- `EXIT FOR`/`EXIT DO` корректно прерывают циклы с графическими вызовами
- `TRY/CATCH` перехватывает runtime-ошибки графики (MB3010, MB3007, MB3005)
- Нет специфических проблем взаимодействия с control flow

---

## 18. Взаимодействие с Error / Exception System

### Диагностики

| Код | Значение | Пример |
|---|---|---|
| MB3003 | Неверный ввод | INPUT в SCREEN |
| MB3004 | STEP 0 в FOR | `FOR i = 1 TO 10 STEP 0` |
| MB3005 | Ресурс не найден/невалиден | Пустой IMAGE, невалидный TILEMAP |
| MB3007 | Неверный аргумент | Невалидные координаты, цвет, индекс |
| MB3010 | Отсутствует графический режим | LINE без SCREEN/CANVAS |
| BC5004 | Backend diagnostic | Захват внешней переменной в bytecode |
| MB4002 | Coverage diagnostic | Локальная функция с захватом |

### Восстановление после ошибок
- Runtime-ошибки бросают `RuntimeFailureException`
- Interpreter ловит в главном цикле, сообщает diagnostic
- BytecodeVm ловит аналогично
- После ошибки состояние графического хоста может быть partially dirty (палитра, позиция)

---

## 19. State / Lifetime / Ownership

### Графическое состояние

| Состояние | Владелец | Сброс |
|---|---|---|
| Screen mode | GraphicsHostService | DeactivateScreen |
| Canvas mode | GraphicsHostService | DeactivateCanvas / ActivateScreen |
| Screen pages (4) | GraphicsHostService | ActivateScreen |
| Active/Visual page | GraphicsHostService | ActivateScreen |
| Framebuffer | ScreenFramebuffer | ActivateScreen/Canvas |
| Text buffer | ScreenTextBuffer | ClearGraphics |
| Current position (X, Y) | IGraphicsHost | ActivateScreen/Canvas |
| Current colors (FG, BG) | IGraphicsHost | ActivateScreen/Canvas |
| Viewport | IGraphicsHost | ResetViewport / ActivateScreen |
| Window | IGraphicsHost | ResetWindow / ActivateScreen |
| Camera | IGraphicsHost | ResetCamera / ActivateCanvas |
| Palette | ScreenFramebuffer | ResetPalette / ActivateScreen |

### GCHandle management
- `NativeGraphicsScreenWindow` хранит `GCHandle` для Win32 `GWLP_USERDATA`
- Освобождается в `HandleDestroyed` или финализаторе
- Проверка `IsAllocated` в финализаторе предотвращает double-free

---

## 20. Edge Cases

### Проверенные
- Координаты на границах `[-1e9, 1e9]` — проверены в `GraphicsCoordinateValidation`
- Переполнение `long` при STEP — обработано для LINE/CIRCLE/GET/PUT через `SaturatingAdd`
- Нулевой радиус CIRCLE — рисует один пиксель (корректно)
- Пустая DRAW-строка — пустой список команд (корректно)
- Невалидный индекс POINT(selector) — MB3007 (корректно)

### Непроверенные / рискованные
- **STEP overflow для PSET, PAINT, canvas-примитивов** — нет защиты от `long` overflow
- **Очень длинная линия** (Bresenham без лимита итераций) — возможен DoS
- **PAINT на очень большой области** — стек `Stack<(long,long)>` может исчерпать память
- **Пустой паттерн PAINT** (length 0) — `DivideByZeroException` в `PositiveModulo`
- **DRAW цвет = `long.MinValue`** — `Math.Abs(long.MinValue)` выбрасывает `OverflowException`

---

## 21. Interpreter ↔ Bytecode VM

| Возможность | Interpreter | BytecodeVm | Одинаковый результат | Одинаковая ошибка | Одинаковое состояние |
|---|---|---|---|---|---|
| SCREEN mode activation | ✅ | ✅ | ✅ | ✅ | ✅ |
| CLS | ✅ | ✅ | ✅ | ✅ | ✅ |
| COLOR | ✅ | ✅ | ✅ | ✅ | ✅ |
| LOCATE | ✅ | ✅ | ✅ | ✅ | ✅ |
| PSET/PRESET | ✅ | ✅ | ✅ | ✅ | ✅ |
| LINE (all forms) | ✅ | ✅ | ✅ | ✅ | ✅ |
| CIRCLE (full form) | ✅ | ✅ | ✅ | ✅ | ✅ |
| PAINT (patterns) | ✅ | ✅ | ✅ | ✅ | ✅ |
| DRAW (full string) | ✅ | ✅ | ✅ | ✅ | ✅ |
| GET/PUT | ✅ | ✅ | ✅ | ✅ | ✅ |
| VIEW/WINDOW | ✅ | ✅ | ✅ | ✅ | ✅ |
| VIEW PRINT | ✅ | ✅ | ✅ | ✅ | ✅ |
| PALETTE/PALETTE USING | ✅ | ✅ | ✅ | ✅ | ✅ |
| PCOPY | ✅ | ✅ | ✅ | ✅ | ✅ |
| POINT (both forms) | ✅ | ✅ | ✅ | ✅ | ✅ |
| PMAP | ✅ | ✅ | ✅ | ✅ | ✅ |
| CANVAS | ✅ | ✅ | ✅ | ✅ | ✅ |
| BEGINFRAME/PRESENT | ✅ | ✅ | ✅ | ✅ | ✅ |
| CAMERA/RESETCAMERA | ✅ | ✅ | ✅ | ✅ | ✅ |
| VIEWPORT | ✅ | ✅ | ✅ | ✅ | ✅ |
| DRAWRECT/FILLRECT | ✅ | ✅ | ✅ | ✅ | ✅ |
| DRAWLINE/DRAWCIRCLE/FILLCIRCLE | ✅ | ✅ | ✅ | ✅ | ✅ |
| DRAWTEXT | ✅ | ✅ | ✅ | ✅ | ✅ |
| DRAWIMAGE/DRAWSPRITE | ✅ | ✅ | ✅ | ✅ | ✅ |
| DRAWTILE/DRAWTILEMAP/DRAWTILELAYER | ✅ | ✅ | ✅ | ✅ | ✅ |
| SETMAPTILE | ✅ | ✅ | ✅ | ✅ | ✅ |
| IMAGE/SPRITE/TILESET/TILEMAP builtins | ✅ | ✅ | ✅ | ✅ | ✅ |
| RGB() | ✅ | ✅ | ✅ | ✅ | ✅ |
| TEXTWIDTH/TEXTHEIGHT | ✅ | ✅ | ✅ | ✅ | ✅ |
| MOUSE builtins | ✅ | ✅ | ✅ | ✅ | ✅ |
| ON MOUSE/ON KEY | ✅ | ✅ | ✅ | ✅ | ✅ |

**Нет обнаруженных расхождений** в семантике между Interpreter и BytecodeVm.

---

## 22. Анализ производительности

| Сценарий | Interpreter | VM | Ускорение | Классификация |
|---|---:|---:|---:|---|
| modern_canvas_primitive_burst | 1.000x | 0.823x | 1.21x | CLEAR WIN |
| modern_canvas_sprite_burst | 1.000x | 0.932x | 1.07x | MODERATE WIN |
| modern_graphics_metadata_sweep | 1.000x | 0.447x | 2.24x | CLEAR WIN |
| maze_chase_grid_logic | 1.000x | 0.683x | 1.46x | CLEAR WIN |
| maze_chase_input_polling | 1.000x | 0.739x | 1.35x | CLEAR WIN |
| maze_chase_mixed_frame | 1.000x | 0.732x | 1.37x | CLEAR WIN |

**Все 6 сценариев показывают выигрыш VM.** Нет регрессий.

---

## 23. Качество Benchmark

- Используется `BenchmarkDotNet` с warmup и итерациями
- Короткие прогоны (3 итерации) для `maze_chase` — менее стабильны
- Стандартные прогоны (20 итераций, 3 warmup) для остальных
- Нет dedicated benchmark для classic SCREEN операторов
- Нет benchmark для tileset/tilemap операций

---

## 24. Hot Paths и Bottlenecks

1. **`FindNearestDefaultPaletteIndex`** — brute-force поиск по 256 палитровым записям при каждом `GetPixel` для true-color пикселей. O(256) на пиксель.
2. **`DrawAbsoluteLine`** — Bresenham без лимита итераций. Для линий spanning весь экран это нормально, но для координат near ±1e9 будет катастрофой.
3. **`GraphicsFloodFill.Flood`** — стек без ограничения размера. Для screen-sized fills ~8M entries = ~128MB.
4. **Per-frame snapshot allocation** — каждый кадр создаёт новый `uint[]` массив (256KB–1.2MB). Нет повторного использования буферов.
5. **Per-frame scaling** — при несовпадении размеров swap chain и framebuffer каждый кадр выделяет `uint[]` для масштабирования.

---

## 25. Архитектурные проблемы

### 25.1. Несогласованность обновления позиции (P2)
Canvas-примитивы обновляют позицию в interpreter, SCREEN-операторы делегируют в host. Смешанный подход.

### 25.2. Дублирование кода между Interpreter и BytecodeVm (P2)
~600 строк идентичного кода: свойства ресурсов, tilemap cell access, palette helpers, resource guards, size validation.

### 25.3. `mouseEventKind` field misuse (P3)
Поле `mouseEventKind` в `LoweredInstruction` используется для хранения графических флагов. Имя не соответствует назначению.

### 25.4. 4 независимые реализации нормализации цвета (P1)
`GraphicsDrawExecutor`, `GraphicsFloodFill`, `GraphicsBufferTransfer` (2 штуки). Только `GraphicsBufferTransfer.NormalizeIntegralColor` корректно обрабатывает все edge cases.

### 25.5. Dispatcher bypass в `SetAbsolutePixel`/`GetAbsolutePixel` (P1)
Эти методы в `GraphicsHostService` не используют `InvokeOnDispatcher`, что создаёт потенциальный data race при вызове из background thread.

### 25.6. Нет double-buffering для snapshot (P3)
Каждый кадр выделяет новый массив. Для 60fps это значительное давление на GC.

---

## 26. Дублирование логики

| Логика | Места | Рекомендация |
|---|---|---|
| Нормализация цвета | 4 файла | Вынести в `GraphicsColorNormalization.Normalize(int/long)` |
| Размер валидация | 3 метода в Interpreter | Один `ValidatePositiveSize(op, w, h, loc)` |
| STEP разрешение | 5 копий в Interpreter | `ResolveStep(x, y, isRelative)` helper |
| Свойства ресурсов | Interpreter + BytecodeVm + fast-path | Общий static utility class |
| Tilemap cell access | Interpreter + BytecodeVm | Общий static helper |
| Resource guards | Interpreter + BytecodeVm (разные имена) | `RequireResource<T>()` generic |

---

## 27. Single Source of Truth

| Сущность | Источник истины | Проблема |
|---|---|---|
| Синтаксис | `GraphicsSyntax.cs` | Нет |
| Семантика | `Binder.IoStatements.cs` + `BoundStatements.cs` | Нет |
| Lowering | `BoundProgramLowerer.cs` + `LoweredInstruction.cs` | Нет |
| Runtime behavior | `IGraphicsHost.cs` | Нет |
| Рендеринг | `GraphicsHostService.cs` + `ScreenFramebuffer.cs` | Нет |
| Цветовая нормализация | **4 файла** | **ДА — нет единого источника** |
| Рasterization | `GraphicsHostDrawing.cs` + `GraphicsEllipseRasterizer.cs` + `GraphicsFloodFill.cs` | Частично — базовые алгоритмы централизованы |

---

## 28. Legacy / Dead Code

1. **`LoweredInstruction.Screen(location)` (без valueCount)** — не используется, только вариант с `valueCount`
2. **`LoweredInstruction.Cls(location)` (без hasColor)** — не используется
3. **`GraphicsEllipseRasterizer.EstimateEllipsePerimeter`** — определён, но нигде не вызывается
4. **`SoftwareFramebuffer.CompareBmp`** — определён в тестах, но нигде не используется
5. **Static `Get*` методы в `BytecodeVm.Graphics.cs`** (строки 1053–1096) — дублируют fast-path в `BytecodeVm.cs` и не вызываются

---

## 29. Надёжность и безопасность

- **Path handling:** `LoadImage`/`LoadSprite`/`LoadTileset` обрабатывают `IOException`, `UnauthorizedAccessException`, `NotSupportedException` и возвращают `null`
- **Resource exhaustion:** Bresenham без лимита итераций, flood fill без лимита стека, per-frame allocation без reuse
- **Memory abuse:** `ScreenFramebuffer` хранит 3 массива на каждый пиксель (index + trueColor + hasTrueColor). Для 1920×1080 это ~22MB
- **Thread safety:** `SetAbsolutePixel`/`GetAbsolutePixel` обходят dispatcher — потенциальный data race
- **Win32 safety:** GCHandle lifecycle корректно защищён через `IsAllocated` проверку

---

## 30. Анализ тестов

### Тестовое покрытие

| Тип тестов | Количество | Что покрывают |
|---|---|---|
| Interpreter unit tests | ~60 | Все statements + built-ins + edge cases |
| BytecodeVm unit tests | ~55 | Зеркало interpreter + opcode emission |
| GraphicsBaseline tests | 9 | Попиксельная проверка базовых примитивов |
| Parser tests | ~25 | Парсинг всех statements |
| ExecutionPipeline tests | ~12 | Сквозной путь через bytecode mode |
| IDE tests | ~50+ | Рендеринг, ввод, окно, палитра |
| Acceptance tests | ~30 | Запуск .axm samples через bytecode |
| Smoke harness | 5 сценариев | SCREEN 10, GET/PUT, SPRITE, INKEY, event log |

### Критические пробелы

1. **Нет попиксельного паритета Interpreter ↔ BytecodeVm.** `SoftwareFramebuffer.ComparePixels()` существует, но не используется.
2. **Нет baseline тестов для:** DRAWCIRCLE arcs, aspect ratio, PAINT fill shape, DRAW string output, GET/PUT round-trip, DRAWTILE/MAP, DRAWIMAGE/SPRITE, CAMERA transform, WINDOW mapping, PALETTE remapping, PCOPY.
3. **Нет pipeline тестов для:** CANVAS primitives, VIEWPORT, CAMERA, DRAWIMAGE/SPRITE/DRAWTILE, tilemap operations, palette, PCOPY, resource builtins, TEXTWIDTH/HEIGHT, RGB() через bytecode pipeline.
4. **Нет парсерных тестов восстановления после ошибок** для графических statements.
5. **Нет тестов валидации SCREEN mode** (невалидные номера режимов).
6. **Нет тестов изоляции состояния** между запусками программ.

---

## 31. Недостающие тесты

### Обычные сценарии
- DRAWCIRCLE с дугами и aspect ratio
- PAINT с различными паттернами и границами
- DRAW string с полным набором команд
- GET/PUT round-trip (захват → восстановление)
- DRAWTILE/DRAWTILEMAP/DRAWTILELAYER попиксельно
- CAMERA + VIEWPORT + DRAWIMAGE совместно
- PALETTE USING с полным массивом 256 цветов
- PCOPY между страницами с разным содержимым

### Отрицательные сценарии
- STEP overflow near long.MaxValue для PSET/PAINT
- Пустой паттерн PAINT (length 0)
- CIRCLE с нулевым aspect
- DRAW с командой X (historical, должен отклоняться)
- DRAW с цветом long.MinValue

### Граничные случаи
- Координаты точно на границах ±1e9
- VIEWPORT с вырожденным прямоугольником (0 width или height)
- WINDOW с очень близкими координатами (huge scale factor)
- SCREEN mode 1 (320×200) — минимальное разрешение
- CANVAS 1×1 — минимальный canvas

### Паритетные тесты
- Одна и та же программа через Interpreter и BytecodeVm → сравнение пикселей
- Одна и та же программа → сравнение диагностик

---

## 32. Анализ Samples

### Покрытие samples

| Категория | Количество | Примеры |
|---|---|---|
| graphics/primitives | 5 | pset_line_circle, circle_full_turn, circle_arcs, paint_fill, step_and_point |
| graphics/modern | 8 | lines_and_circles, rectangles_frame, viewport_hud, camera_world, hud_text, errors |
| graphics/text | 2 | print_locate_color, transparent_text |
| graphics/screen | 5 | text_scroll, mode_info, invalid_page, close_window, best_fit |
| graphics/input | 2 | text_screen_input, input_on_screen |
| graphics/images | 2 | load_and_draw_image, missing_image_error |
| graphics/sprites | 3 | walk_cycle, page_flip_walk, invalid_frame_error |
| graphics/mouse | 1 | mouse_events |
| graphics/game | 4+ | snake, paddle_ball, maze_chase (+calibration) |
| operators/draw | 7 | square, rotated_spokes, paint_segments, etc. |
| operators/paint | 6 | canvas_region_fill, fish_fill, pattern_repaint, etc. |
| operators/get,put | 4 | rectangle_capture, canvas_block, default_xor, etc. |
| operators/palette | 4 | using_array_colors, change_and_reset, etc. |
| operators/view | 2 | screen_and_relative, invalid_bounds |
| operators/view-print | 2 | scrolling_region, invalid_rows |
| operators/window | 2 | cartesian_and_screen, zero_size |
| operators/pcopy | 3 | text_screen_error, page_copy, invalid_page |
| operators/setmaptile | 1 | cell_update |
| operators/drawtile | 1 | basic_tileset |
| operators/on-mouse | 4 | wheel, release, left_button, canvas_world |
| functions (graphics) | 12 | point, pmap, mousex/y/button/press, textwidth/height |
| engine-compare | 7 | modern_graphics/Canvas sprite/grid/input/мixed/cflow |

**Всего:** ~80+ graphics-related `.axm` samples.

---

## 33. Анализ документации

### Пользовательский manual (`docs/manual/`)

| Страница | Покрывает |
|---|---|
| `graphics.html` | Обзор графики |
| `graphics-primitives.html` | PSET, PRESET, LINE, CIRCLE, PAINT, DRAW |
| `graphics-text.html` | PRINT, COLOR, LOCATE в SCREEN |
| `modern-graphics.html` | CANVAS, BEGINFRAME, PRESENT, RGB |
| `screen.html` | SCREEN operator |
| `screen-functions.html` | SCREENWIDTH, SCREENHEIGHT |
| `pcopy.html` | PCOPY |
| `palette.html` | PALETTE |
| `window.html` | WINDOW |
| `view.html` | VIEW |
| `pmap.html` | PMAP |
| `images.html` | IMAGE, LOADIMAGE, DRAWIMAGE |
| `sprites.html` | SPRITE, DRAWSPRITE |
| `tilesets.html` | TILESET, DRAWTILE |
| `tilemaps.html` | TILEMAP, DRAWTILEMAP, DRAWTILELAYER |
| `get-put.html` | GET, PUT |
| `on-mouse.html`, `mousex.html`, etc. | Mouse functions |

### Техническая документация

| Документ | Назначение |
|---|---|
| `GRAPHICS_OPERATORS_FLAGS.md` | Внутренний договор флагов LINE, CIRCLE, PAINT |
| `MODERN_GRAPHICS_PLAN.md` | Долгосрочный план развития графики |
| `TILEMAP_CONTRACT.md` | Полный договор тайловых карт |
| `SPECIFICATION3.md` | Активная спецификация (секции 2129–4073) |

### Расхождения документации и кода
- `GRAPHICS_OPERATORS_FLAGS.md` не упоминает, что CIRCLE цвет по умолчанию = 15 (хардкод), а не `CurrentForegroundColor`
- `MODERN_GRAPHICS_PLAN.md` не обновлён после переноса POINT/PSET/PRESET/PAINT/GET/PUT в CANVAS

---

## 34. Интеграция с IDE

### Что работает
- `GraphicsHostService` реализует все 60+ методов `IGraphicsHost`
- Direct3D11 рендеринг через swap chain с vsync
- Win32 native window с обработкой ввода
- Текстовый слой с bitmap-шрифтом 8×12 (ASCII + Cyrillic + box-drawing)
- Dual-storage: indexed palette + true-color BGRA
- Масштабирование с сохранением aspect ratio
- Вертикальная прокрутка для текстового режима
- Smoke harness для SCREEN 10, GET/PUT, SPRITE, INKEY

### Проблемы IDE
- `GetLogicalPixelCore` для CANVAS не применяет камеру (P0)
- `SetAbsolutePixel`/`GetAbsolutePixel` обходят dispatcher (P1)
- `FindNearestDefaultPaletteIndex` — brute-force O(256) на пиксель
- Per-frame snapshot allocation без reuse
- `NativeGraphicsScreenWindow` не вызывает `Dispose()` явно

---

## 35. Зависимости от Host / Platform

| Зависимость | Тип | Примечание |
|---|---|---|
| Windows | HOST LIMITATION | Win32 HWND, Direct3D11 |
| .NET / WPF | HOST LIMITATION | DispatcherTimer, Dispatcher.Invoke |
| Direct3D11 | HOST LIMITATION | SharpDX COM interop |
| Файловая система | HOST LIMITATION | Path resolution для LOADIMAGE и др. |
| Кодировка | HOST LIMITATION | IBM CP866 для DRAW и PAINT паттернов |

---

## 36. Карта корневых причин

### ROOT-SYS-001: Дублирование логики между Interpreter и BytecodeVm
├── SYS-G-R01: 4 реализации нормализации цвета
├── SYS-G-R02: Дублированные static-методы свойств ресурсов
├── SYS-G-R03: Дублированные tilemap cell access
├── SYS-G-R04: Дублированные resource guards
└── SYS-G-R05: Дублированная size validation

### ROOT-SYS-002: Неполная защита от overflow в STEP-координатах
├── SYS-G-R10: PSET STEP — простое long addition
├── SYS-G-R11: PAINT STEP — простое long addition
├── SYS-G-R12: Canvas-примитивы STEP — простое long addition
└── (LINE/CIRCLE/GET/PUT — защищены через SaturatingAdd)

### ROOT-SYS-003: Асимметрия SetPixel/GetPixel в CANVAS
└── SYS-G-R20: GetLogicalPixelCore не применяет камеру

---

## 37. Стабильные области

**Не следует трогать:**
- `IGraphicsHost` интерфейс — стабильный контракт
- `GraphicsCoordinateValidation` — корректная валидация координат
- `GraphicsLineMath` — математически безупречное saturating arithmetic
- `GraphicsCircleValidation` — корректная валидация CIRCLE параметров
- `GraphicsInstructionArguments.TryCreateLine` / `TryCreateCircle` — корректная нормализация
- Lowering для `Canvas`, `BeginFrame`, `Present`, `Camera`, `Viewport`, `DrawRect`, `FillRect`, `DrawLine`, `DrawCircle`, `FillCircle`, `DrawText`, `DrawTile`, `DrawTilemap`, `DrawTileLayer`, `SetMapTile` — чистый lowering без проблем
- `ScreenTextBuffer` и `ScreenTextRasterizer` — стабильная текстовая подсистема
- Bitmap-шрифт с Cyrillic и box-drawing — работает корректно

---

## 38. Возможности консолидации

| Текущие места | Дублируемая логика | Единый источник | Направление миграции |
|---|---|---|---|
| 4 файла | Нормализация цвета | `GraphicsColorNormalization` | Вынести в `Axiom.Runtime` |
| 3 метода | Size validation | `ValidatePositiveSize` | Один метод в helper |
| 5 копий | STEP resolution | `GraphicsStepResolution.Resolve` | Один helper |
| 2 файла | Resource properties | `GraphicsResourceProperties` | Static utility |
| 2 файла | Tilemap cell access | `TilemapCellAccess` | Static utility |
| 2 файла | Resource guards | `GraphicsResourceGuard.Require<T>` | Generic helper |

---

## 39. Возможности разделения ответственности

- **Цветовая нормализация** должна быть единственной функцией в одном месте, а не четырьмя вариациями
- **STEP-разрешение** должно быть единым helper-методом, используемым и interpreter, и VM
- **Валидация координат** уже централизована в `GraphicsCoordinateValidation` — хорошо
- **Rasterization** уже разделена на отдельные классы — хорошо

---

## 40. Кандидаты на удаление

| Код | Доказательства | Рекомендация |
|---|---|---|
| `LoweredInstruction.Screen(location)` (без valueCount) | Не используется lowerer'ом | Удалить overload |
| `LoweredInstruction.Cls(location)` (без hasColor) | Не используется lowerer'ом | Удалить overload |
| `GraphicsEllipseRasterizer.EstimateEllipsePerimeter` | Нигде не вызывается | Удалить или интегрировать в iteration budget |
| `SoftwareFramebuffer.CompareBmp` | Определён но не используется | Удалить |
| Static `Get*` в `BytecodeVm.Graphics.cs` (1053–1096) | Дублируют fast-path | Удалить |

---

## 41. Готовность к дальнейшему развитию

| Направление | Готовность | Оценка |
|---|---|---|
| Будущий Compiler | Высокая | Чистое lowering, стабильные опкоды |
| Дальнейшая оптимизация VM | Средняя | Hot paths идентифицированы, но нет typed fast paths |
| Специализированные Opcode | Высокая | Архитектура готова к добавлению новых опкодов |
| Typed fast paths | Низкая | RuntimeValue boxing на каждом пикселе |
| Развитие Runtime | Высокая | IGraphicsHost расширяем |
| Развитие IDE | Высокая | GraphicsHostService модульный |
| Расширение языка | Высокая | Полный pipeline от parser до host |
| Тайловые карты | Высокая | Договор зафиксирован, implementation в пути |

---

## 42. Реестр проблем

| ID | Серьёзность | Категория | Компонент | Проблема | Доказательства | Корневая причина | Уверенность |
|---|---|---|---|---|---|---|---|
| SYS-G-001 | P0 | CORRECTNESS | IDE Host | GetPixel в CANVAS не применяет камеру | `GetLogicalPixelCore` vs `SetLogicalPixelCore` | Пропущена трансформация | CONFIRMED |
| SYS-G-002 | P1 | VM | BytecodeValidator | LINE-валидатор всегда снимает 4 координаты | Validator code lines 1624-1641 | Не проверён hasExplicitStartPoint | CONFIRMED |
| SYS-G-003 | P1 | ARCHITECTURE | Runtime | 4 независимые реализации нормализации цвета | GraphicsDrawExecutor, GraphicsFloodFill, GraphicsBufferTransfer | Отсутствие единого helper | CONFIRMED |
| SYS-G-004 | P1 | RUNTIME | Interpreter/VM | STEP overflow для PSET/PAINT/canvas primitives | Простое long addition без SaturatingAdd | Непоследовательная защита | CONFIRMED |
| SYS-G-005 | P1 | THREAD SAFETY | IDE Host | SetAbsolutePixel/GetAbsolutePixel обходят dispatcher | Методы без InvokeOnDispatcher | Обход UI thread | CONFIRMED |
| SYS-G-006 | P2 | CORRECTNESS | Interpreter | CIRCLE цвет по умолчанию = 15 вместо foreground | Строка 392, хардкод | QuickBASIC-совместимость (undocumented) | HIGH |
| SYS-G-007 | P2 | CORRECTNESS | Interpreter | ApplyPaletteUsing не прерывает при ошибке | Строки 258, 264 — report но не throw | Нет RuntimeFailureException | CONFIRMED |
| SYS-G-008 | P2 | CORRECTNESS | Interpreter/VM | DRAWTILEMAP/DRAWTILELAYER не обновляют позицию | Нет SetGraphicsCurrentPosition вызова | Пропущено | CONFIRMED |
| SYS-G-009 | P2 | ARCHITECTURE | Runtime | mouseEventKind field misuse в LoweredInstruction | Поле хранит graphics flags | Неправильное имя поля | CONFIRMED |
| SYS-G-010 | P2 | CORRECTNESS | Runtime | GraphicsDrawExecutor: Math.Abs(long.MinValue) overflow | NormalizeColor(long, int) | Граничный случай | HIGH |
| SYS-G-011 | P2 | CORRECTNESS | Runtime | Пустой паттерн PAINT → DivideByZeroException | GraphicsPaintRequest.CreatePattern не проверяет length > 0 | Отсутствие валидации | CONFIRMED |
| SYS-G-012 | P3 | PERFORMANCE | IDE Host | FindNearestDefaultPaletteIndex — brute force O(256) | Вызывается при каждом GetPixel для true-color | Нет кэша/таблицы | CONFIRMED |
| SYS-G-013 | P3 | PERFORMANCE | Runtime | Bresenham без лимита итераций | DrawAbsoluteLine в GraphicsHostDrawing | Нет iteration guard | CONFIRMED |
| SYS-G-014 | P3 | PERFORMANCE | Runtime | Flood fill без лимита стека | Stack<(long,long)> в GraphicsFloodFill | Нет memory guard | CONFIRMED |
| SYS-G-015 | P3 | PERFORMANCE | IDE Host | Per-frame snapshot allocation | Нет double-buffering | Нет reuse | CONFIRMED |
| SYS-G-016 | P3 | VM | BytecodeValidator | 14 опкодов отсутствуют в TryGetStackEffect | Screen, PCopy, View, Window, Color, Locate, Palette, PSet, Get, Put, Line, Circle, Paint, Draw | Неполный validator | CONFIRMED |
| SYS-G-017 | P3 | VM | BytecodeVm | Canvas-опкоды используют PopUnchecked | DRAWRECT, FILLRECT и др. | Несогласованность | CONFIRMED |
| SYS-G-018 | P3 | ARCHITECTURE | Runtime | ValueCount misuse для DrawImage/DrawSprite | Хранит 0/1 вместо реального count | Semantics mismatch | CONFIRMED |
| SYS-G-019 | P3 | TESTS | Tests | Нет попиксельного паритета Interpreter ↔ VM | ComparePixels() определён но не используется | Пробел в тестах | CONFIRMED |
| SYS-G-020 | P4 | LEGACY | Bytecode | Dead code: LoweredInstruction.Screen/Cls no-arg overloads | Не используются | Не удалены | CONFIRMED |

---

## 43. Recovery Roadmap

### ЭТАП 0 — Базовая диагностика и измерения
- TASK-G-00: Измерить текущий попиксельный паритет Interpreter ↔ VM через `SoftwareFramebuffer.ComparePixels()`

### ЭТАП 1 — Исправление критических проблем корректности
- TASK-G-01: Исправить `GetLogicalPixelCore` — добавить камеру для CANVAS
- TASK-G-02: Исправить LINE-валидатор в bytecode — проверять hasExplicitStartPoint
- TASK-G-03: Вынести единую нормализацию цвета в `GraphicsColorNormalization`
- TASK-G-04: Добавить SaturatingAdd для STEP в PSET/PAINT/canvas primitives
- TASK-G-05: Обернуть SetAbsolutePixel/GetAbsolutePixel в dispatcher

### ЭТАП 2 — Стабилизация семантики
- TASK-G-06: Задокументировать или исправить CIRCLE default color = 15
- TASK-G-07: Добавить RuntimeFailureException в ApplyPaletteUsing
- TASK-G-08: Добавить SetGraphicsCurrentPosition в DRAWTILEMAP/DRAWTILELAYER
- TASK-G-09: Добавить валидацию length > 0 в GraphicsPaintRequest.CreatePattern

> Статус: Этап 1 (TASK-G-01..05), Этап 2 (TASK-G-06..09) и Этап 3 (TASK-G-10, G-11) выполнены 2026-08-26 (Sessions 1095-1097). Исправлены SYS-G-001..009, SYS-G-011, SYS-G-016.

### ЭТАП 3 — Архитектурное улучшение
- TASK-G-10: Добавить TryGetStackEffect для 14 отсутствующих опкодов
- TASK-G-11: Переименовать mouseEventKind → flags в LoweredInstruction
- TASK-G-12: Выделить общий helper для свойств ресурсов (static utility)
- TASK-G-13: Выделить общий helper для STEP resolution
- TASK-G-14: Выделить общий helper для resource guards
- TASK-G-15: Удалить dead code (5 элементов)

### ЭТАП 4 — Оптимизация производительности
- TASK-G-16: Заменить brute-force FindNearestDefaultPaletteIndex на lookup table
- TASK-G-17: Добавить iteration guard в Bresenham (maxIterations по длине линии)
- TASK-G-18: Добавить memory guard в flood fill (max stack size)
- TASK-G-19: Implement double-buffering для snapshot (reuse arrays)

### ЭТАП 5 — Усиление тестирования
- TASK-G-20: Implement попиксельные паритетные тесты Interpreter ↔ VM
- TASK-G-21: Добавить baseline тесты для CIRCLE arcs, PAINT shape, DRAW output
- TASK-G-22: Добавить pipeline тесты для CANVAS primitives через bytecode
- TASK-G-23: Добавить parser error recovery тесты для graphics statements
- TASK-G-24: Добавить STEP overflow negative тесты

### ЭТАП 6 — Обновление документации
- TASK-G-25: Обновить GRAPHICS_OPERATORS_FLAGS.md про CIRCLE default color
- TASK-G-26: Обновить MODERN_GRAPHICS_PLAN.md после переноса POINT/PSET/PRESET/PAINT/GET/PUT в CANVAS

### ЭТАП 7 — Подготовка к дальнейшему развитию
- TASK-G-27: Добавить dedicated engine-compare benchmark для classic SCREEN
- TASK-G-28: Добавить engine-compare benchmark для tileset/tilemap

---

## 44. Граф зависимостей задач

```
TASK-G-01 ──────────────────────────────┐
TASK-G-02 ──────────────────────────────┤
TASK-G-03 ──────────────────────────────┤
TASK-G-04 ──────────────────────────────┤── TASK-G-20 (паритетные тесты)
TASK-G-05 ──────────────────────────────┤
TASK-G-06 ──────────────────────────────┤
TASK-G-07 ──────────────────────────────┘
TASK-G-09 ──────────────────────────────── TASK-G-24 (overflow тесты)
TASK-G-10 ──────────────────────────────── TASK-G-22 (pipeline тесты)
TASK-G-11 ──────────────────────────────── TASK-G-15 (удаление dead code)
TASK-G-03 ──────────────────────────────── TASK-G-12, G-13, G-14 (consolidation)
```

---

## 45. Definition of Done

Подсистема GRAPHICS RENDERING CORRECTNESS считается завершённой, когда:

1. **Корректность:** `GetPixel` в CANVAS применяет ту же камеру, что и `SetPixel`.
2. **Edge cases:** STEP-координаты защищены от overflow во всех операторах.
3. **Consistency:** Единая нормализация цвета во всех 4 местах.
4. **Interpreter/VM:** Попиксельный паритет подтверждён тестами.
5. **Tests:** Baseline тесты покрывают все базовые примитивы (включая arcs, patterns, DRAW).
6. **Documentation:** CIRCLE default color и PAINT edge cases документированы.
7. **Performance:** Нет регрессий по сравнению с текущим состоянием.
8. **Architecture:** Дублированный код консолидирован в общие helpers.

---

## 46. ТОП-20 проблем корректности и архитектуры

| # | Проблема | Серьёзность |
|---|---|---|
| 1 | GetPixel в CANVAS не применяет камеру | P0 |
| 2 | LINE-валидатор: всегда 4 координаты | P1 |
| 3 | 4 реализации нормализации цвета | P1 |
| 4 | STEP overflow для PSET/PAINT/canvas | P1 |
| 5 | SetAbsolutePixel/GetAbsolutePixel обходят dispatcher | P1 |
| 6 | CIRCLE default color = 15 (undocumented) | P2 |
| 7 | ApplyPaletteUsing не прерывает при ошибке | P2 |
| 8 | DRAWTILEMAP/DRAWTILELAYER не обновляют позицию | P2 |
| 9 | mouseEventKind field misuse | P2 |
| 10 | Math.Abs(long.MinValue) в DRAW executor | P2 |
| 11 | Пустой паттерн PAINT → DivideByZero | P2 |
| 12 | FindNearestDefaultPaletteIndex O(256) | P3 |
| 13 | Bresenham без лимита итераций | P3 |
| 14 | Flood fill без лимита стека | P3 |
| 15 | Per-frame snapshot allocation | P3 |
| 16 | 14 опкодов отсутствуют в TryGetStackEffect | P3 |
| 17 | Canvas-опкоды используют PopUnchecked | P3 |
| 18 | ValueCount misuse для DrawImage/DrawSprite | P3 |
| 19 | Нет попиксельного паритета Interpreter ↔ VM | P3 |
| 20 | Dead code в LoweredInstruction | P4 |

---

## 47. ТОП-20 задач по производительности

| # | Задача | Ожидаемый эффект | Сложность |
|---|---|---|---|
| 1 | Lookup table для FindNearestDefaultPaletteIndex | 10–50x ускорение GetPixel для true-color | Низкая |
| 2 | Double-buffering snapshot arrays | Снижение GC pressure на ~50% | Средняя |
| 3 | Reuse scaling arrays в Direct3D11 renderer | Снижение allocation per frame | Низкая |
| 4 | Iteration guard в Bresenham | Предотвращение DoS | Низкая |
| 5 | Memory guard в flood fill | Предотвращение OOM | Низкая |
| 6 | Bulk pixel read/write API для GET/PUT | Уменьшение virtual call overhead | Высокая |
| 7 | Typed fast paths для graphics opcodes | Уменьшение boxing overhead | Высокая |
| 8 | Cached viewport resolution | Уменьшение повторных вычислений | Низкая |
| 9 | Canvas VIEWPORT clip optimization | Избегание redundant clipping | Низкая |
| 10 | DRAW string caching | Избежать re-parsing при повторных вызовах | Средняя |
| 11 | Tilemap visible-region culling | Уменьшение отрисовки невидимых тайлов | Средняя |
| 12 | Sprite sheet atlas на GPU | Уменьшение CPU→GPU копирований | Высокая |
| 13 | Dirty rectangle tracking | Обновление только изменённых областей | Высокая |
| 14 | GPU-based flood fill | Перенос PAINT на GPU | Очень высокая |
| 15 | Batch draw commands | Накопление команд перед present | Высокая |
| 16 | Pre-computed angle table для ellipse | Уменьшение Atan2 calls | Средняя |
| 17 | Viewport-aware Bresenham clipping | Избегание per-pixel viewport check | Средняя |
| 18 | Image/sprite resource cache | Избежать disk re-read | Низкая |
| 19 | Scale map cache eviction policy | Контроль роста кэша | Низкая |
| 20 | DispatcherTimer reuse вместо creation per refresh | Уменьшение allocation | Низкая |

---

## 48. Итоговая оценка здоровья подсистемы

| Критерий | Оценка (0–10) | Объяснение |
|---|---|---|
| Корректность | 7 | 1 критическая (GetPixel camera), 2 существенных (validator, STEP overflow) |
| Семантика | 8 | Полный набор операторов, чёткий контракт, но CIRCLE default undocumentated |
| Архитектура | 7 | Чистое разделение слоёв, но дублирование кода между interpreter и VM |
| Runtime | 8 | Стабильный IGraphicsHost, корректные helpers |
| Interpreter | 8 | Все statements работают, несколько edge cases |
| Bytecode VM | 7 | Паритет подтверждён, но validator неполный (14 опкодов) |
| Производительность | 7 | VM быстрее во всех сценариях, но есть hot path неоптимальности |
| Память | 6 | Per-frame allocation, brute-force palette lookup, no resource caching |
| Error handling | 7 | Большинство ошибок обработано, но ApplyPaletteUsing — gap |
| Тесты | 6 | Хорошее покрытие interpreter и VM, но нет попиксельного паритета и много baseline gaps |
| Samples | 8 | 80+ samples, хорошее покрытие операторов |
| Документация | 8 | Полный manual + technical docs, minor discrepancies |
| IDE | 7 | Работает через D3D11, но threading concerns и allocation pressure |
| Maintainability | 6 | Значительное дублирование между interpreter и VM |
| Reliability | 7 | Стабильная работа, но есть edge cases с overflow и memory |
| Future readiness | 8 | Архитектура готова к расширению |

**Общий Health Score: 7.1 / 10**

---

## Главный контрольный вопрос

> Можно ли считать подсистему GRAPHICS RENDERING CORRECTNESS достаточно стабильной, корректной и архитектурно понятной для перехода к следующей подсистеме?

**YES, WITH CONDITIONS**

### Основные blockers
1. **P0:** `GetPixel` в CANVAS не применяет камеру — критическая ошибка корректности, влияющая на `POINT(x,y)` и debug-сценарии.
2. **P1:** LINE-валидатор в bytecode — может привести к stack corruption при specific lowering path.
3. **P1:** STEP overflow для PSET/PAINT — potential undefined behavior при extreme coordinates.

### Что нужно сделать в первую очередь
1. Исправить `GetLogicalPixelCore` — добавить камеру для CANVAS (TASK-G-01)
2. Исправить LINE-валидатор (TASK-G-02)
3. Вынести единую нормализацию цвета (TASK-G-03)
4. Добавить SaturatingAdd для STEP (TASK-G-04)

### Что можно отложить
- Оптимизация производительности (TASK-G-16–G-19)
- Дополнительные baseline тесты (TASK-G-21)
- Консолидация дублированного кода (TASK-G-12–G-14)
- Engine-compare benchmarks для classic SCREEN (TASK-G-27)

### Что не следует трогать
- `IGraphicsHost` интерфейс
- `GraphicsCoordinateValidation`, `GraphicsLineMath`, `GraphicsCircleValidation`
- Lowering для canvas-примитивов (чистый, без проблем)
- `ScreenTextBuffer` и `ScreenTextRasterizer`
- Текущие passing тесты
