# AXIOM — ГЛУБОКИЙ FORENSIC-АУДИТ
## GRAPHICS / SCREEN ПОДСИСТЕМА

**Дата аудита:** 2026-08-21
**Метод:** полный forensic-анализ кода, тестов, samples, документации и IDE-слоя
**Объём анализированного кода:** ~141 файлов, ~15000+ строк графического runtime

---

## 1. Резюме

Графическая подсистема Axiom является одной из наиболее зрелых и архитектурно продуманных подсистем проекта. Она реализует два параллельных пути: исторический SCREEN-путь (совместимый с BASIC/QuickBASIC) и современный CANVAS-путь (для 2D-игр и произвольной графики). Оба пути полностью поддерживаются в Interpreter и BytecodeVm с полным паритетом.

**Общая оценка:** Подсистема стабильна, корректна и архитектурно понятна. Обнаружено 3 бага средней серьёзности, 16 edge cases низкой серьёзности и несколько архитектурных замечаний. Ни одна из проблем не является блокирующей для дальнейшего развития.

---

## 2. Что представляет собой эта подсистема

Графическая подсистема Axiom отвечает за весь визуальный вывод программ: от текстового режима SCREEN 0 до пиксельной графики CANVAS, включая рисование примитивов, работу с изображениями, спрайтами, тайловыми картами и пользовательским вводом (клавиатура, мышь).

Подсистема проходит через весь конвейер языка:
- **Лексер:** 34 графических ключевых слова
- **Parser:** 30 синтаксических узлов + 1 вспомогательный (`GraphicsPointSyntax`)
- **Binder:** 29 bound-узлов + 6 вспомогательных перечислений
- **Lowering:** 34 lowered-инструкций
- **Bytecode:** 34 опкода
- **BytecodeVm:** 34 метода исполнения + 35 встроенных функций
- **Interpreter:** 34 метода исполнения + 35 встроенных функций
- **IDE:** нативное Win32-окно, D3D11-рендерер, framebuffer, текстовый буфер

---

## 3. Полный список возможностей

### 3.1 Графические операторы (34)

#### SCREEN-путь (исторический BASIC-слой) — 19 операторов:
| ID | Оператор | Назначение | Статус |
|---|---|---|---|
| SYS-G01 | `SCREEN` | Активация экранного режима (0..10) | IMPLEMENTED |
| SYS-G02 | `CLS` | Очистка экрана/холста | IMPLEMENTED |
| SYS-G03 | `COLOR` | Установка цвета текста | IMPLEMENTED |
| SYS-G04 | `LOCATE` | Позиционирование текстового курсора | IMPLEMENTED |
| SYS-G05 | `PSET` | Установка одного пикселя | IMPLEMENTED |
| SYS-G06 | `PRESET` | Установка пикселя фоновым цветом | IMPLEMENTED |
| SYS-G07 | `LINE` | Рисование линии/прямоугольника | IMPLEMENTED |
| SYS-G08 | `CIRCLE` | Рисование окружности/дуги/эллипса | IMPLEMENTED |
| SYS-G09 | `PAINT` | Заливка области | IMPLEMENTED |
| SYS-G10 | `DRAW` | Исполнение строковых команд | IMPLEMENTED |
| SYS-G11 | `GET` | Захват прямоугольника в буфер | IMPLEMENTED |
| SYS-G12 | `PUT` | Восстановление буфера на экран | IMPLEMENTED |
| SYS-G13 | `VIEW` | Установка графического viewport | IMPLEMENTED |
| SYS-G14 | `VIEW PRINT` | Установка текстового viewport | IMPLEMENTED |
| SYS-G15 | `WINDOW` | Установка логических координат | IMPLEMENTED |
| SYS-G16 | `PALETTE` | Управление палитрой | IMPLEMENTED |
| SYS-G17 | `PCOPY` | Копирование страниц экрана | IMPLEMENTED |

#### CANVAS-путь (современный слой) — 18 операторов:
| ID | Оператор | Назначение | Статус |
|---|---|---|---|
| SYS-G18 | `CANVAS` | Создание пиксельного холста | IMPLEMENTED |
| SYS-G19 | `BEGINFRAME` | Начало кадра | IMPLEMENTED |
| SYS-G20 | `PRESENT` | Публикация кадра | IMPLEMENTED |
| SYS-G21 | `CAMERA` | Установка смещения камеры | IMPLEMENTED |
| SYS-G22 | `RESETCAMERA` | Сброс камеры | IMPLEMENTED |
| SYS-G23 | `VIEWPORT` | Установка viewport на холсте | IMPLEMENTED |
| SYS-G24 | `DRAWRECT` | Контур прямоугольника | IMPLEMENTED |
| SYS-G25 | `FILLRECT` | Заполненный прямоугольник | IMPLEMENTED |
| SYS-G26 | `DRAWLINE` | Линия на холсте | IMPLEMENTED |
| SYS-G27 | `DRAWCIRCLE` | Контур окружности | IMPLEMENTED |
| SYS-G28 | `FILLCIRCLE` | Заполненная окружность | IMPLEMENTED |
| SYS-G29 | `DRAWTEXT` | Текст на холсте | IMPLEMENTED |
| SYS-G30 | `DRAWIMAGE` | Отрисовка изображения | IMPLEMENTED |
| SYS-G31 | `DRAWSPRITE` | Отрисовка кадра спрайта | IMPLEMENTED |
| SYS-G32 | `DRAWTILE` | Отрисовка тайла | IMPLEMENTED |
| SYS-G33 | `DRAWTILEMAP` | Отрисовка всех слоёв карты | IMPLEMENTED |
| SYS-G34 | `DRAWTILELAYER` | Отрисовка одного слоя карты | IMPLEMENTED |
| SYS-G35 | `SETMAPTILE` | Изменение ячейки карты | IMPLEMENTED |

### 3.2 Графические встроенные функции (35)

| ID | Функция | Назначение | Статус |
|---|---|---|---|
| SYS-F01 | `SCREENISACTIVE()` | Активен ли экран | IMPLEMENTED |
| SYS-F02 | `SCREENMODE()` | Текущий режим экрана | IMPLEMENTED |
| SYS-F03 | `SCREENWIDTH()` | Ширина экрана/холста | IMPLEMENTED |
| SYS-F04 | `SCREENHEIGHT()` | Высота экрана/холста | IMPLEMENTED |
| SYS-F05 | `TEXTWIDTH(text)` | Ширина текста в пикселях | IMPLEMENTED |
| SYS-F06 | `TEXTHEIGHT(text)` | Высота текста в пикселях | IMPLEMENTED |
| SYS-F07 | `POINT(x, y)` | Цвет пикселя | IMPLEMENTED |
| SYS-F08 | `POINT(selector)` | Позиция графического курсора | IMPLEMENTED |
| SYS-F09 | `PMAP(coord, sel)` | Преобразование координат | IMPLEMENTED |
| SYS-F10 | `LOADIMAGE(path)` | Загрузка изображения | IMPLEMENTED |
| SYS-F11 | `IMAGEWIDTH(img)` | Ширина изображения | IMPLEMENTED |
| SYS-F12 | `IMAGEHEIGHT(img)` | Высота изображения | IMPLEMENTED |
| SYS-F13 | `LOADSPRITE(path,w,h)` | Загрузка спрайта | IMPLEMENTED |
| SYS-F14 | `SPRITEWIDTH(spr)` | Ширина кадра спрайта | IMPLEMENTED |
| SYS-F15 | `SPRITEHEIGHT(spr)` | Высота кадра спрайта | IMPLEMENTED |
| SYS-F16 | `SPRITECOUNT(spr)` | Число кадров спрайта | IMPLEMENTED |
| SYS-F17 | `LOADTILESET(path,w,h)` | Загрузка тайлсета (3 формы) | IMPLEMENTED |
| SYS-F18 | `TILECOUNT(ts)` | Число тайлов | IMPLEMENTED |
| SYS-F19 | `TILEWIDTH(ts)` | Ширина тайла | IMPLEMENTED |
| SYS-F20 | `TILEHEIGHT(ts)` | Высота тайла | IMPLEMENTED |
| SYS-F21 | `LOADTILEMAP(path)` | Загрузка Tiled JSON карты | IMPLEMENTED |
| SYS-F22 | `MAPWIDTH(tm)` | Ширина карты в тайлах | IMPLEMENTED |
| SYS-F23 | `MAPHEIGHT(tm)` | Высота карты в тайлах | IMPLEMENTED |
| SYS-F24 | `MAPPIXELWIDTH(tm)` | Ширина карты в пикселях | IMPLEMENTED |
| SYS-F25 | `MAPPIXELHEIGHT(tm)` | Высота карты в пикселях | IMPLEMENTED |
| SYS-F26 | `MAPTILEWIDTH(tm)` | Ширина тайла в карте | IMPLEMENTED |
| SYS-F27 | `MAPTILEHEIGHT(tm)` | Высота тайла в карте | IMPLEMENTED |
| SYS-F28 | `MAPLAYERCOUNT(tm)` | Число слоёв | IMPLEMENTED |
| SYS-F29 | `MAPTILE(tm,l,c,r)` | Индекс тайла в ячейке | IMPLEMENTED |
| SYS-F30 | `MAPTILESET(tm,l,c,r)` | Индекс тайлсета в ячейке | IMPLEMENTED |
| SYS-F31 | `MAPTILEFLAGS(tm,l,c,r)` | Флаги ячейки | IMPLEMENTED |
| SYS-F32 | `RGB(r, g, b)` | Сборка 24-битного цвета | IMPLEMENTED |

### 3.3 Графические типы данных (4)

| ID | Тип | Назначение | Статус |
|---|---|---|---|
| SYS-T01 | `IMAGE` | Загруженное растровое изображение | IMPLEMENTED |
| SYS-T02 | `SPRITE` | Спрайт с кадрами анимации | IMPLEMENTED |
| SYS-T03 | `TILESET` | Атлас тайлов | IMPLEMENTED |
| SYS-T04 | `TILEMAP` | Тайловая карта (Tiled JSON) | IMPLEMENTED |

---

## 4. Фактическая архитектура

### 4.1 Конвейер выполнения

```
Исходный код (.axm)
    ↓
Lexer (34 ключевых слова)
    ↓
Parser (30 синтаксических узлов, GraphicsPointSyntax)
    ↓
Binder (29 bound-узлов, проверка типов, валидация буферов)
    ↓
BoundProgram
    ↓
BoundProgramLowerer (34 lowered-инструкций, push значений на стек)
    ↓
BytecodeCompiler (34 опкода, кодирование флагов)
    ↓
BytecodeModule (с пулом констант и отладочной информацией)
    ↓
BytecodeVm (34 Execute*Instruction + 35 Evaluate*Builtin)
    ↓
IGraphicsHost (абстракция хоста, 40+ методов)
    ↓
GraphicsHostService (IDE: Win32 окно + D3D11 рендерер)
    ↓
Результат на экране
```

### 4.2 Параллельный путь Interpreter

```
BoundProgram
    ↓
Interpreter.Graphics.cs (34 Execute*Statement + 35 Evaluate*Builtin)
    ↓
IGraphicsHost (тот же интерфейс)
    ↓
GraphicsHostService
    ↓
Результат на экране
```

### 4.3 Ключевые файлы

| Файл | Ответственность | Строки |
|------|----------------|--------|
| `src/Axiom.Syntax/GraphicsSyntax.cs` | Все синтаксические узлы графики | 1467 |
| `src/Axiom.Parsing/Parser.Statements.cs` | Парсинг всех графических операторов | ~400 |
| `src/Axiom.Semantics/Binding/Binder.IoStatements.cs` | Связывание всех графических операторов | ~580 |
| `src/Axiom.Semantics/BoundNodes/BoundStatements.cs` | Все bound-узлы графики | ~700 |
| `src/Axiom.Bytecode/Lowering/BoundProgramLowerer.cs` | Понижение в lowered-инструкции | ~400 |
| `src/Axiom.Runtime/Execution/Interpreter.Graphics.cs` | Исполнение через интерпретатор | 1448 |
| `src/Axiom.Runtime/Execution/BytecodeVm.Graphics.cs` | Исполнение через VM | 1689 |
| `src/Axiom.Runtime/Host/IGraphicsHost.cs` | Абстракция графического хоста | 279 |
| `src/Axiom.Runtime/Host/GraphicsHostDrawing.cs` | Программная растеризация | 238 |
| `src/Axiom.Runtime/Host/GraphicsCoordinateSpace.cs` | Преобразование координат | 459 |
| `src/Axiom.Runtime/Execution/GraphicsBufferTransfer.cs` | GET/PUT передача буферов | 349 |
| `src/Axiom.Runtime/Execution/GraphicsFloodFill.cs` | Алгоритм заливки | 226 |
| `src/Axiom.Runtime/Execution/GraphicsEllipseRasterizer.cs` | Растеризация эллипсов | 239 |
| `src/Axiom.Runtime/Execution/GraphicsDrawExecutor.cs` | Исполнение DRAW команд | 194 |
| `src/Axiom.Runtime/Execution/GraphicsDrawParser.cs` | Парсинг DRAW строк | 389 |
| `src/Axiom.Runtime/Execution/TilemapRenderSupport.cs` | Рендеринг тайловых карт | 279 |
| `src/Axiom.Runtime/Values/RuntimeTilemapValue.cs` | Модель тайловой карты | 495+ |
| `src/Axiom.Ide/Graphics/NativeGraphicsScreenWindow.cs` | Нативное Win32-окно | 787 |
| `src/Axiom.Ide/Graphics/Direct3D11SwapChainRenderer.cs` | D3D11 рендерер | 193 |
| `src/Axiom.Ide/Graphics/ScreenFramebuffer.cs` | Управление фреймбуфером | 734 |
| `src/Axiom.Ide/Services/Graphics/GraphicsHostService.cs` | IDE-реализация IGraphicsHost | 2432 |

---

## 5. Полный путь выполнения

### 5.1 SCREEN-путь (исторический)

```
SCREEN 7
    → Lexer: [SCREEN_KEYWORD] [INTEGER_LITERAL(7)]
    → Parser: ScreenStatementSyntax { ModeExpression = 7 }
    → Binder: BoundScreenStatement { ModeExpression = BoundLiteralExpression(7) }
    → Lowerer: LoweredInstruction.Screen(location, valueCount=4)
        → push 7 (mode), push 0 (activePage), push 0 (visualPage), push 4 (valueCount)
    → BytecodeCompiler: BytecodeInstruction { Opcode.Screen, operands=[4] }
    → BytecodeVm.ExecuteScreenInstruction:
        → pop valueCount=4, visualPage=0, activePage=0, mode=7
        → _host.ActivateScreen(7, 0, 0)
    → GraphicsHostService.ActivateScreen(7):
        → ScreenModeSizeProvider.GetSize(7) → {320, 200}
        → Create NativeGraphicsScreenWindow(320, 200)
        → Create ScreenFramebuffer(320, 200)
        → Create Direct3D11SwapChainRenderer
```

### 5.2 CANVAS-путь (современный)

```
CANVAS 320, 180
    → Binder: BoundCanvasStatement { Width=320, Height=180 }
    → Lowerer: LoweredInstruction.Canvas → push 320, push 180
    → BytecodeVm.ExecuteCanvasInstruction:
        → pop width=320, height=180
        → _host.ActivateCanvas(320, 180)
    → GraphicsHostService.ActivateCanvas(320, 180):
        → Create ScreenFramebuffer(320, 180)
        → Create NativeGraphicsScreenWindow(320, 180)
```

### 5.3 Рисование примитива

```
DRAWRECT 10, 10, 50, 30, RGB(255, 0, 0)
    → Binder: BoundDrawRectStatement { X=10, Y=10, W=50, H=30, Color=0xFF0000 }
    → Lowerer: push 10, 10, 50, 30, 0xFF0000
    → BytecodeVm.ExecuteDrawRectInstruction:
        → pop x=10, y=10, w=50, h=30, color=0xFF0000
        → _host.DrawRect(10, 10, 50, 30, 0xFF0000)
        → _host.SetGraphicsCurrentPosition(10, 10)
    → GraphicsHostService.DrawRect:
        → DrawAbsoluteLineCore (4 линии контура)
```

---

## 6. Фактический семантический контракт

### 6.1 SCREEN

**Вход:** `SCREEN mode [, activePage [, visualPage]]`
- `mode`: INTEGER, диапазон 0..10
- `activePage`: INTEGER, диапазон 0..3 (по умолчанию 0)
- `visualPage`: INTEGER, диапазон 0..3 (по умолчанию 0)
- `mode = 0`: деактивирует экран

**Обработка:** Валидация режима → создание/пересоздание окна и framebuffer → установка состояния

**Выход:** Активный экран с заданным разрешением; `IsScreenActive = true`

**Побочные эффекты:** Деактивирует CANVAS, если был активен; сбрасывает VIEW, WINDOW, PALETTE

### 6.2 CANVAS

**Вход:** `CANVAS width, height`
- `width`: LONG, > 0
- `height`: LONG, > 0

**Обработка:** Валидация размера → создание framebuffer → создание окна

**Выход:** Активный холст; `IsCanvasActive = true`

**Побочные эффекты:** Деактивирует SCREEN, если был активен

### 6.3 GET/PUT

**Вход (GET):** `GET [STEP] (x1,y1) - [STEP] (x2,y2), buffer`
- Координаты: LONG
- buffer: 2D числовой массив, размер точно совпадает с захватываемым прямоугольником

**Вход (PUT):** `PUT [STEP] (x,y), buffer [, action]`
- Координаты: LONG
- buffer: 2D числовой массив
- action: PSET | PRESET | AND | OR | XOR (по умолчанию XOR)

**Валидация в Binder:** массив должен быть 2D, числовой тип элемента
**Валидация в Runtime:** размер буфера точно совпадает с прямоугольником

---

## 7. Анализ Parser

### 7.1 Покрытие

Все 34 графических оператора имеют dedicated-методы парсинга в `Parser.Statements.cs`. Каждый метод:
- Корректно распознаёт ключевое слово
- Корректно разбирает аргументы через запятую
- Поддерживает опциональные аргументы (цвет, флаги)
- Использует `GraphicsPointSyntax` для координат `(x, y)`

### 7.2 Дизамбигуация LINE

Оператор `LINE` дизамбигуируется от присваивания переменной через peek на 1 токен вперёд: если следующий токен — `(`, `STEP` или `-`, парсится как графический LINE. Это корректно работает, но создаёт зависимость от порядка токенов.

### 7.3 Полиморфизм VIEW

`VIEW` dispatch'ится на `ViewStatementSyntax` или `ViewPrintStatementSyntax` в зависимости от наличия `PRINT` после `VIEW`. Это корректная реализация.

### 7.4 PSET/PRESET

Оба оператора используют один `PSetStatementSyntax` с флагом `Reset`. Это элегантное решение без дублирования.

### 7.5 Оценка Parser: 9/10

Парсер графической подсистемы — зрелый, полный и корректный. Нет пропущенных операторов, нет неразобранных форм.

---

## 8. Анализ Binder

### 8.1 Покрытие

Все 29 bound-узлов создаются в `Binder.IoStatements.cs`. Каждый метод:
- Связывает все выражения с ожидаемыми типами
- Применяет неявные преобразования (LONG для координат, INTEGER для цветов, DOUBLE для логических координат WINDOW)
- Валидирует специфические контракты (GET/PUT буфер = 2D числовой массив, PAINT fill = числовой или строковый)

### 8.2 Валидация GET/PUT буферов

`BindGraphicsBufferExpression` проверяет:
1. Выражение должно быть массивом
2. Ранг массива должен быть точно 2
3. Тип элемента должен быть числовым

Это корректная многоуровневая валидация.

### 8.3 Валидация PAINT

`BindPaintStatement` выполняет cross-validation:
- fill: числовой или строковый
- border: числовой
- background: строковый (только если fill — строковый)

Это сложная, но корректная логика.

### 8.4 SCREEN colorSwitch

Параметр `colorSwitch` в SCREEN явно не поддерживается: binder выдаёт `ReportUnsupportedScreenColorSwitch`. Это честная диагностика, а не молчаливое игнорирование.

### 8.5 Оценка Binder: 9/10

Binder графической подсистемы — зрелый и полный. Типовая проверка корректна, cross-validation для PAINT работает правильно.

---

## 9. Анализ Interpreter

### 9.1 Архитектура

Все 34 метода исполнения находятся в `Interpreter.Graphics.cs` (1448 строк). Каждый метод:
1. Вычисляет аргументы
2. Проверяет контекст (RequireScreenActive / RequireCanvasActive / RequireDrawableSurfaceActive)
3. Валидирует параметры
4. Делегирует на `_host`
5. Обновляет `CurrentGraphicsPosition` (для DRAW-операций)

### 9.2 Guard-методы

| Метод | Назначение |
|---|---|
| `RequireScreenActive` | Требует активный SCREEN |
| `RequireCanvasActive` | Требует активный CANVAS |
| `RequireDrawableSurfaceActive` | Требует SCREEN или CANVAS |
| `RequireImage` | Извлекает RuntimeImageValue |
| `RequireSprite` | Извлекает RuntimeSpriteValue |
| `RequireTileset` | Извлекает RuntimeTilesetValue |
| `RequireTilemap` | Извлекает RuntimeTilemapValue |

Все guard-методы выбрасывают `RuntimeFailureException` при нарушении контракта.

### 9.3 Специальные хелперы

- `GraphicsDrawExecutor` — исполнение DRAW команд (мини-язык)
- `GraphicsBufferTransfer` — GET/PUT передача
- `GraphicsFloodFill` — алгоритм заливки (scanline)
- `GraphicsEllipseRasterizer` — растеризация эллипсов с дугами
- `GraphicsCoordinateSpaceMath` — преобразование координат VIEW/WINDOW/PMAP
- `TilemapRenderSupport` — рендеринг тайловых карт с flip-трансформациями

### 9.4 Оценка Interpreter: 8/10

Interpreter-путь графики — зрелый и полный. Единственное замечание — пропуск `SetGraphicsCurrentPosition` в DRAWTILEMAP/DRAWTILELAYER (подробности в реестре проблем).

---

## 10. Анализ Bytecode / VM

### 10.1 Lowering

Все 34 bound-узла корректно понижаются в `LoweredInstruction` в `BoundProgramLowerer.cs`. Каждая lowered-инструкция:
- Push'ит аргументы на стек вычислений в правильном порядке
- Кодирует опциональные параметры как флаги (битовые маски)
- Кодирует `valueCount` для динамического числа аргументов

### 10.2 Bytecode Emission

`BytecodeCompiler` транслирует каждую `LoweredInstruction` в `BytecodeInstruction` с соответствующим `Opcode` и операндами. Процесс тривиален: 1:1 маппинг.

### 10.3 BytecodeModuleValidator

Валидатор проверяет:
- Типы значений на стеке (INTEGER, LONG, DOUBLE для координат/цветов)
- Число значений (valueCount)
- Корректность флагов (hasColor, hasStart, boxKind и т.д.)
- Корректность операндов

Валидация обширна (~1900 строк специфичных для графики проверок).

### 10.4 BytecodeVm

Все 34 метода исполнения в `BytecodeVm.Graphics.cs` (1689 строк) точно повторяют логику Interpreter, но работают со стеком вычислений вместо bound-узлов.

### 10.5 Оценка Bytecode VM: 8/10

Байткодный путь полностью параллелен Interpreter. Обнаружено 2 проблемы специфичных для VM (DRAWIMAGE/DRAWSPRITE validation fallthrough, избыточные double-guard'ы).

---

## 11. Анализ Runtime

### 11.1 RuntimeValue для графики

| Тип | Внутреннее представление | Свойства |
|---|---|---|
| `RuntimeImageValue` | `uint[] BgraPixels` | Width, Height, BgraPixels |
| `RuntimeSpriteValue` | image + frame grid | FrameWidth, FrameHeight, FrameCount, Columns, Rows |
| `RuntimeTilesetValue` | image + tile grid | TileWidth, TileHeight, TileCount, Margin, Spacing |
| `RuntimeTilemapValue` | layers + tilesets + cells | Width, Height, TileWidth, TileHeight, Layers, Tilesets |

### 11.2 IGraphicsHost

Интерфейс `IGraphicsHost` (279 строк) определяет 40+ методов для:
- Управления экраном/холстом (ActivateScreen, ActivateCanvas, BeginFrame, PresentFrame)
- Рисования примитивов (DrawLine, DrawCircle, FillRect, Paint, etc.)
- Работы с изображениями (LoadImage, DrawImage, DrawSprite, DrawTile)
- Управления координатами (SetViewport, SetWindow, SetCanvasCamera)
- Текста (SetTextColor, LocateText, WriteGraphicsText)
- Пикселей (SetPixel, GetPixel, SetAbsolutePixel, GetAbsolutePixel)

Все методы имеют default-реализации (no-op или программная растеризация).

### 11.3 Программная растеризация

`GraphicsHostDrawing`提供:
- `DrawLine` — алгоритм Брезенхема
- `DrawCircle` — растеризация средней точки эллипса с поддержкой дуг
- `Paint` — scanline flood fill с поддержкой паттернов

### 11.4 Оценка Runtime: 8/10

Runtime-слой графики — зрелый и хорошо структурированный. Типы значений immutable (за исключением RuntimeTilemapValue), хелперы корректны.

---

## 12. Взаимодействие с типами

### 12.1 Графические типы как opaque handles

`IMAGE`, `SPRITE`, `TILESET`, `TILEMAP` — это opaque handles без полей и методов. Все операции выполняются через dedicated-операторы и встроенные функции. Это упрощает модель, но ограничивает расширяемость.

### 12.2 Числовые типы

- Координаты: LONG (для SCREEN и CANVAS)
- Цвета: INTEGER (24-бит RGB)
- Логические координаты WINDOW: DOUBLE
- Радиус CIRCLE: DOUBLE (SCREEN), LONG (DRAWCIRCLE/FILLCIRCLE на CANVAS)
- Радиус DRAWCIRCLE/FILLCIRCLE: LONG

### 12.3 Массивы

- GET/PUT буфер: 2D числовой массив (INTEGER, LONG, DOUBLE, DECIMAL)
- PALETTE USING: 1D числовой массив (до 256 элементов)

### 12.4 Строки

- DRAW команды: строка мини-языка
- DRAWTEXT: произвольная строка
- TEXTWIDTH/TEXTHEIGHT: произвольная строка
- LOADIMAGE/LOADSPRITE/LOADTILESET/LOADTILEMAP: путь к файлу (строка)

---

## 13. Взаимодействие со String

- DRAW команды: строковый аргумент, парсится GraphicsDrawParser
- DRAWTEXT: произвольная строка, растеризуется ScreenTextRasterizer (8px/символ)
- TEXTWIDTH/TEXTHEIGHT: измеряют строку (8px * длина, 12px * число строк)
- Пустые строки: корректно обрабатываются (0 ширина, пустой вывод)
- Unicode: ScreenTextRasterizer поддерживает ASCII + кириллицу, остальное — fallback-символ

---

## 14. Взаимодействие с Array

- GET: захватывает прямоугольник в 2D массив; размер должен точно совпадать
- PUT: восстанавливает 2D массив на экран; поддерживает PSET/PRESET/AND/OR/XOR
- PALETTE USING: читает 1D массив до 256 элементов
- Все массивные операции сохраняют shared-reference семантику

---

## 15. Взаимодействие с TYPE

- IMAGE, SPRITE, TILESET, TILEMAP — не пользовательские TYPE, а встроенные opaque типы
- Нет полей, методов, конструкторов у графических типов
- Все операции через dedicated-операторы и функции

---

## 16. Взаимодействие с Functions / SUB

- Графические операторы не могут быть вызваны как функции (все — statements)
- RGB() — встроенная функция, не statement
- LOADIMAGE/LOADSPRITE/LOADTILESET/LOADTILEMAP — встроенные функции, возвращающие handle
- Все графические built-in'ы работают через BuiltinFunctionId, а не через строковые имена

---

## 17. Взаимодействие с Control Flow

- Графические операторы корректно работают внутри IF/FOR/WHILE/DO/SELECT/TRY
- SCREEN/CANVAS активация сохраняется между блоками
- VIEWPORT/WINDOW/CAMERA сохраняют состояние до явного сброса
- Нет специфических проблем с control flow

---

## 18. Взаимодействие с Error / Exception System

### 18.1 Compile-time ошибки

- MB2xxx: неверные типы аргументов (TypeMismatch)
- MB2003: неверный тип элемента массива для PALETTE USING
- MB2019: попытка использовать SUB как значение
- MB2020: вызов не-функционального значения

### 18.2 Runtime ошибки

- MB3005: ресурс не найден (LOADIMAGE, LOADSPRITE, LOADTILESET, LOADTILEMAP)
- MB3007: недопустимые аргументы (отрицательный радиус, неверный индекс, пустой буфер)
- MB3009: вызов пустой функции
- MB3010: отсутствует SCREEN/CANVAS

### 18.3 Backend-диагностики

- BC5004: захват внешних переменных в локальной routine (не поддержан в VM)
- MB4002: захват внешнего локала (не поддержан в VM)

---

## 19. State / Lifetime / Ownership

### 19.1 Графическое состояние

| Состояние | Владелец | Сброс |
|---|---|---|
| `IsScreenActive` | IGraphicsHost | SCREEN 0 |
| `IsCanvasActive` | IGraphicsHost | SCREEN (любой) |
| `ScreenWidth/Height` | IGraphicsHost | SCREEN/CANVAS |
| `CurrentGraphicsX/Y` | IGraphicsHost | Каждая DRAW-операция |
| `CurrentForegroundColor` | IGraphicsHost | COLOR |
| `CurrentBackgroundColor` | IGraphicsHost | COLOR |
| Viewport | IGraphicsHost | VIEW / VIEWPORT (reset) |
| Window | IGraphicsHost | WINDOW (reset) |
| Camera | IGraphicsHost | RESETCAMERA |
| Palette[256] | IGraphicsHost | PALETTE (reset) |
| ScreenPages[4] | GraphicsHostService | SCREEN (пересоздание) |
| Framebuffer | GraphicsHostService | CLS / BEGINFRAME |
| Tilemap data | RuntimeTilemapValue | SETMAPTILE (мутация) |

### 19.2 Ownership ресурсов

| Ресурс | Создаёт | Владеет | Освобождает |
|---|---|---|---|
| NativeGraphicsScreenWindow | GraphicsHostService | GraphicsHostService | Dispose / HandleDestroyed |
| ScreenFramebuffer | GraphicsHostService | GraphicsHostService | GC (нет IDisposable) |
| Direct3D11SwapChainRenderer | NativeGraphicsScreenWindow | NativeGraphicsScreenWindow | Dispose |
| RuntimeImageValue | LoadImage | RuntimeValue | GC |
| RuntimeSpriteValue | LoadSprite | RuntimeValue | GC |
| RuntimeTilesetValue | LoadTileset | RuntimeValue | GC |
| RuntimeTilemapValue | LoadTilemap | RuntimeValue | GC |

---

## 20. Edge Cases

### 20.1 Координаты

- Координаты валидируются в диапазоне [-1_000_000_000, 1_000_000_000]
- Переполнение арифметики координат обрабатывается через saturating arithmetic (`GraphicsLineMath`)
- Экстремальные значения радиуса (1e300) приводятся к long.MaxValue безопасно

### 20.2 Буферы GET/PUT

- Пустые буферы (0x0) возвращают success с length=0 (молчаливый no-op)
- Буферы с нечётными размерами работают корректно
- AND/OR/XOR операции работают на полном int-диапазоне без нормализации

### 20.3 Flood Fill

- Использует heap-allocated Stack (не рекурсия)
- Для очень больших областей может потреблять много памяти
- Корректно обрабатывает границы экрана

### 20.4 DRAW команды

- Пустая строка: no-op
- Неизвестная команда: MB3007
- M-команда строже QuickBASIC (требует знак у обеих координат)

### 20.5 Тайловые карты

- GetCell/SetCell не имеют bounds checking (полагаются на вызывающих)
- LoadFromTiledJson не валидирует tileWidth/tileHeight из JSON
- JsonDocument leak при загрузке внешних TSX

---

## 21. Interpreter ↔ Bytecode VM

### 21.1 Таблица паритета

| Возможность | Interpreter | Bytecode VM | Одинаковый результат | Одинаковая ошибка | Одинаковое состояние |
|---|---|---|---|---|---|
| SCREEN | ✅ | ✅ | ✅ | ✅ | ✅ |
| CANVAS | ✅ | ✅ | ✅ | ✅ | ✅ |
| BEGINFRAME/PRESENT | ✅ | ✅ | ✅ | ✅ | ✅ |
| CAMERA/RESETCAMERA | ✅ | ✅ | ✅ | ✅ | ✅ |
| VIEWPORT | ✅ | ✅ | ✅ | ✅ | ✅ |
| CLS | ✅ | ✅ | ✅ | ✅ | ✅ |
| COLOR | ✅ | ✅ | ✅ | ✅ | ✅ |
| LOCATE | ✅ | ✅ | ✅ | ✅ | ✅ |
| PALETTE | ✅ | ✅ | ✅ | ✅ | ✅ |
| PCOPY | ✅ | ✅ | ✅ | ✅ | ✅ |
| PSET/PRESET | ✅ | ✅ | ✅ | ✅ | ✅ |
| GET/PUT | ✅ | ✅ | ✅ | ✅ | ✅ |
| LINE | ✅ | ✅ | ✅ | ✅ | ✅ |
| CIRCLE | ✅ | ✅ | ✅ | ✅ | ✅ |
| PAINT | ✅ | ✅ | ✅ | ✅ | ✅ |
| DRAW | ✅ | ✅ | ✅ | ✅ | ✅ |
| DRAWRECT/FILLRECT | ✅ | ✅ | ✅ | ✅ | ✅ |
| DRAWLINE | ✅ | ✅ | ✅ | ✅ | ✅ |
| DRAWCIRCLE/FILLCIRCLE | ✅ | ✅ | ✅ | ✅ | ✅ |
| DRAWTEXT | ✅ | ✅ | ✅ | ✅ | ✅ |
| DRAWIMAGE | ✅ | ✅ | ⚠️ | ⚠️ | ✅ |
| DRAWSPRITE | ✅ | ✅ | ⚠️ | ⚠️ | ✅ |
| DRAWTILE | ✅ | ✅ | ✅ | ✅ | ✅ |
| DRAWTILEMAP | ✅ | ✅ | ✅ | ✅ | ✅ |
| DRAWTILELAYER | ✅ | ✅ | ✅ | ✅ | ✅ |
| SETMAPTILE | ✅ | ✅ | ✅ | ✅ | ✅ |
| VIEW/WINDOW | ✅ | ✅ | ✅ | ✅ | ✅ |
| RGB | ✅ | ✅ | ✅ | ✅ | ✅ |
| POINT | ✅ | ✅ | ✅ | ✅ | ✅ |
| PMAP | ✅ | ✅ | ✅ | ✅ | ✅ |
| IMAGE/SPRITE/TILE/TILEMAP builtins | ✅ | ✅ | ✅ | ✅ | ✅ |

### 21.2 Расхождения

| Расхождение | Классификация | Серьёзность |
|---|---|---|
| DRAWIMAGE/DRAWSPRITE: validation fallthrough в VM | Ошибка корректности | P1 (только VM) |
| DRAWTILEMAP/DRAWTILELAYER: пропуск SetGraphicsCurrentPosition | Ошибка корректности | P2 (оба движка) |
| PALETTE USING: non-fatal error handling | Ошибка корректности | P2 (оба движка) |

---

## 22. Анализ производительности

### 22.1 Hot paths

| Путь | Частота | Стоимость |
|---|---|---|
| SetAbsolutePixel | Каждый пиксель | Прямой доступ к массиву |
| GetAbsolutePixel | Каждый пиксель чтения | Прямой доступ к массиву |
| DrawLine (Bresenham) | Линия | O(length) пикселей |
| DrawCircle (midpoint) | Окружность | O(circumference) пикселей |
| Paint (flood fill) | Заливка | O(area) пикселей |
| CreateSnapshot | Каждый кадр | O(width*height) |
| ScaleFramePixels | Каждый кадр (при масштабировании) | O(targetW*targetH) |

### 22.2 Бутылочные горлышки

1. **CreateSnapshot()** — O(width*height) каждый кадр, полное копирование
2. **ScaleFramePixels()** — выделяет новый массив каждый кадр
3. **FindNearestDefaultPaletteIndex()** — O(256) на каждый вызов GetPixel для true-color
4. **No image caching** — каждый LoadImage перечитывает файл
5. **No dirty-region tracking** — весь framebuffer перерисовывается каждый кадр
6. **ScreenTextRasterizer.DrawGlyph()** — 6 проверок на пиксель, нет кэша глифов

### 22.3 Interpreter vs VM

Для графических операций разница Interpreter/VM минимальна, поскольку основная стоимость — в IGraphicsHost-методах (растеризация, D3D11 present). Байткодный путь даёт выигрыш только на подготовке аргументов и dispatch'е, что составляет малую долю от общей стоимости графической операции.

---

## 23. Качество Benchmark

### 23.1 Существующие сценарии

| Сценарий | Путь | Описание |
|---|---|---|
| `modern_canvas_primitive_burst.axm` | CANVAS | Burst примитивов |
| `modern_canvas_sprite_burst.axm` | CANVAS | Burst спрайтов |
| `modern_graphics_metadata_sweep.axm` | CANVAS | Sweep метаданных |

### 23.2 Недостающие сценарии

- SCREEN-путь (исторический) не имеет dedicated benchmark'ов
- GET/PUT round-trip не имеет benchmark'а
- PAINT flood fill на большой области не имеет benchmark'а
- Тайловые карты не имеют benchmark'а рендеринга
- D3D11 present path не имеет benchmark'а

---

## 24. Hot Paths и Bottlenecks

### 24.1 Критические hot paths

1. **SetAbsolutePixel / GetAbsolutePixel** — вызываются из PAINT flood fill, GET/PUT, POINT. Должны быть максимально быстрыми.
2. **CreateSnapshot** — вызывается каждый кадр. Для 1920×1080 = 2M пикселей копирования.
3. **ScaleFramePixels** — выделяет ~8MB каждый кадр при масштабировании 320×200 → 1920×1080.
4. **FindNearestDefaultPaletteIndex** — O(256) на каждый GetPixel для true-color пикселей.

### 24.2 Рекомендации по оптимизации

1. **Кэш LoadImage** — кэшировать загруженные изображения по пути
2. **Persistent staging buffer** для ScaleFramePixels — переиспользовать массив
3. **Dirty-region tracking** — обновлять только изменившиеся области
4. **Glyph atlas** для ScreenTextRasterizer — предварительно растеризовать глифы
5. **Palette lookup table** — заменить O(256) сканирование на O(1) lookup

---

## 25. Архитектурные проблемы

### 25.1 Двойственный пиксельный формат

`ScreenFramebuffer` хранит 3 параллельных массива: `_colorIndices` (палитровый индекс), `_trueColorPixels` (BGRA), `_hasTrueColor` (флаг). Это создаёт сложность и потенциальные рассинхронизации.

### 25.2 Отсутствие IDisposable у ScreenFramebuffer

`ScreenFramebuffer` содержит большие массивы, но не реализует `IDisposable`. При переключении режимов старые фреймбуферы остаются в памяти до GC.

### 25.3 Потокобезопасность

`SetAbsolutePixel` / `GetAbsolutePixel` bypass `InvokeOnDispatcher` — они напрямую обращаются к `_framebuffer` из любого потока. Это создаёт data race с UI-потоком.

### 25.4 Отсутствие DPI awareness

`NativeGraphicsScreenWindow` не учитывает DPI. На high-DPI дисплее координаты мыши и размеры окна будут неверными.

---

## 26. Дублирование логики

### 26.1 Interpreter ↔ BytecodeVm

Основное дублирование — между `Interpreter.Graphics.cs` (1448 строк) и `BytecodeVm.Graphics.cs` (1689 строк). Логика исполнения практически идентична, но:
- Interpreter использует bound-узлы
- BytecodeVm использует стек вычислений
- Guard-методы (RequireScreenActive и т.д.) дублируются
- Валидация параметров дублируется

Это сознательное архитектурное решение: два параллельных движка исполнения.

### 26.2 LoadImage дублирование

`GraphicsHostService.LoadImage` и `GraphicsHostService.LoadImageFromResolvedPath` содержат дублированную логику декодирования изображений.

### 26.3 Validation patterns

Паттерн `if (!condition) { Validate(condition); }` в BytecodeVm создаёт избыточные double-guard'ы для CANVAS-операций.

---

## 27. Single Source of Truth

### 27.1 Где должен быть единственный источник истины

| Аспект | Текущий источник | Комментарий |
|---|---|---|
| Синтаксис | `GraphicsSyntax.cs` + `Parser.Statements.cs` | Корректно |
| Семантика | `Binder.IoStatements.cs` + `BoundStatements.cs` | Корректно |
| Runtime behavior | `IGraphicsHost.cs` (контракт) | Корректно |
| Ошибки | `Binder.Diagnostics.cs` + Runtime guard-методы | Разделено (допустимо) |
| Метаданные built-in | `BuiltinFunctions.Input.cs` + `BuiltinFunctionId.cs` | Корректно |
| Оpcodes | `Opcode.cs` (Bytecode) | Корректно |
| Типы значений | `RuntimeValue.cs` + отдельные файлы Values/ | Корректно |

Архитектура single source of truth соблюдена.

---

## 28. Legacy / Dead Code

### 28.1 Кандидаты на удаление

| Код | Статус | Доказательства |
|---|---|---|
| `SCREEN colorSwitch` параметр | DOCUMENTED_ONLY | Binder выдаёт `ReportUnsupportedScreenColorSwitch` |
| `DRAW X` (VARPTR$) | DOCUMENTED_ONLY | GraphicsDrawParser выдаёт MB3007 "неподдерживаемая историческая команда X" |
| `FillRect` SyntaxKind/BoundNode | IMPLEMENTED | Полностью работает, хотя `DRAWRECT` + `LINE BF` тоже существуют |

### 28.2 POSSIBLY DEAD

Нет кода, который можно было бы однозначно считать мёртвым. Все операторы и функции имеют тесты и samples.

---

## 29. Надёжность и безопасность

### 29.1 Path handling

- `LOADIMAGE/LOADSPRITE/LOADTILESET/LOADTILEMAP` принимают строковые пути
- `ResolveResourcePath` раскрывает переменные окружения и разрешает относительные пути
- Нет валидации на path traversal (../../../etc/passwd)
- Нет ограничения на директории загрузки

### 29.2 Resource exhaustion

- Нет ограничения на размер загружаемых изображений (10000×10000 = 400MB)
- Нет ограничения на размер CANVAS
- Flood fill может потреблять неограниченную память
- `_scaleMapCache` растёт без ограничения

### 29.3 Host exceptions

- Ошибки файловой системы (IOException, UnauthorizedAccessException) перехватываются и возвращают null
- JsonException при парсинге Tiled JSON не перехватывается (может упасть)
- COM-ошибки D3D11 перехватываются в Dispose

---

## 30. Анализ тестов

### 30.1 Существующие тесты

| Файл | Количество | Покрытие |
|---|---|---|
| `InterpreterTests.Graphics.cs` | ~55 тестов | SCREEN, CANVAS, все операторы, edge cases |
| `GraphicsBaselineTests.cs` | 7 тестов | Pixel-level verification |
| `BytecodeVmTests.cs` | ~55 тестов (зеркальные) | Полный паритет с Interpreter |
| `ExecutionPipelineTests.cs` | ~15 тестов | Pipeline-level graphics |
| `BytecodeModuleValidatorTests.cs` | 0 graphics-specific | Нет проверки graphics opcodes |
| `NativeGraphicsScreenWindowTests.cs` | 6 тестов | Input handling |

### 30.2 Качество тестов

- **Отличный паритет Interpreter ↔ VM**: каждый тест имеет `Compiled_` counterpart
- **DRAW команды**: thoroughly протестированы включая edge cases
- **Диагностики**: MB3007, MB3005, MB3010 покрыты
- **Mouse events**: comprehensive coverage

### 30.3 Пропуски в тестах

- PUT с XOR/AND/OR modes не протестированы
- Type mismatch (IMAGE→SPRITE) не протестирован
- SCREEN mode transitions не протестированы
- VIEWPORT partial clip не протестирован
- DRAWIMAGE/DRAWSPRITE с отрицательными размерами не протестированы
- Пустые строки в DRAWTEXT/TEXTWIDTH не протестированы
- DRAWTILEMAP с CAMERA/VIEWPORT не протестирован

---

## 31. Недостающие тесты

### 31.1 Обычные сценарии

1. PUT с XOR, AND, OR modes
2. SCREEN mode transitions (1 → 0 → 1)
3. VIEWPORT partial clip (линия начинается внутри, заканчивается снаружи)
4. DRAWTILEMAP с CAMERA active
5. DRAWTILELAYER с VIEWPORT active
6. DRAWTEXT с пустой строкой
7. TEXTWIDTH/TEXTHEIGHT с пустой строкой
8. CAMERA negative offsets
9. CLS без аргументов на CANVAS

### 31.2 Отрицательные сценарии

1. IMAGE → SPRITE type mismatch
2. SPRITE → IMAGE type mismatch
3. CIRCLE с отрицательным радиусом
4. CIRCLE с нулевым радиусом
5. DRAWIMAGE с отрицательными размерами
6. VIEWPORT с инвертированными координатами
7. WINDOW с нулевой областью

### 31.3 Edge cases

1. GET/PUT с буфером 1×1
2. PAINT где начальный пиксель совпадает с цветом границы
3. DRAW с S0 (нулевой масштаб)
4. DRAW с A4 (невалидный угол)
5. RGB(255,255,255), RGB(0,0,0), RGB(-1,0,0)
6. SETMAPTILE с flags > 7

---

## 32. Анализ Samples

### 32.1 Существующие samples (~45 файлов)

| Категория | Путь | Количество |
|---|---|---|
| DRAW команды | `samples/operators/draw/` | 8 |
| DRAWTILE | `samples/operators/drawtile/` | 1 |
| DRAWTILEMAP | `samples/operators/drawtilemap/` | 1 |
| GET | `samples/operators/get/` | 2 |
| PAINT | `samples/operators/paint/` | 5 |
| PALETTE | `samples/operators/palette/` | 4 |
| PCOPY | `samples/operators/pcopy/` | 3 |
| PRESET | `samples/operators/preset/` | 1 |
| PSET | `samples/operators/pset/` | 1 |
| PUT | `samples/operators/put/` | 2 |
| SETMAPTILE | `samples/operators/setmaptile/` | 1 |
| VIEW | `samples/operators/view/` | 2 |
| VIEW PRINT | `samples/operators/view-print/` | 2 |
| WINDOW | `samples/operators/window/` | 2 |
| PMAP | `samples/functions/pmap/` | 2 |
| POINT | `samples/functions/point/` | 2 |
| TEXTHEIGHT | `samples/functions/textheight/` | 1 |
| TEXTWIDTH | `samples/functions/textwidth/` | 1 |
| Engine-compare | `samples/smoke/engine-compare/` | 3 |

### 32.2 Качество samples

- Samples короткие, фокусированные и запускаемые в IDE
- Русскоязычные комментарии и вывод
- Покрывают основные сценарии
- Некоторые edge cases не покрыты (PUT XOR/AND/OR, DRAWTEXT пустая строка)

---

## 33. Анализ документации

### 33.1 Страницы manual (16+)

| Страница | Статус | Замечания |
|---|---|---|
| `graphics.html` | ✅ Точная | Обзорная страница |
| `screen.html` | ✅ Точная | Полное описание SCREEN |
| `screen-functions.html` | ✅ Точная | Все функции состояния |
| `modern-graphics.html` | ⚠️ 1 замечание | SETMAPTILE группирован с CANVAS, но не требует его |
| `graphics-primitives.html` | ⚠️ 2 замечания | CIRCLE default color не указан; PAINT default fill не указан |
| `graphics-text.html` | ✅ Точная | CLS, COLOR, LOCATE |
| `images.html` | ✅ Точная | IMAGE type |
| `sprites.html` | ✅ Точная | SPRITE type |
| `tilesets.html` | ✅ Точная | TILESET type |
| `tilemaps.html` | ✅ Точная | TILEMAP type |
| `get-put.html` | ✅ Точная | GET/PUT |
| `palette.html` | ✅ Точная | PALETTE |
| `pcopy.html` | ✅ Точная | PCOPY |
| `pmap.html` | ✅ Точная | PMAP |
| `view.html` | ✅ Точная | VIEW |
| `window.html` | ✅ Точная | WINDOW |
| `GRAPHICS_OPERATORS_FLAGS.md` | ✅ Точная | Developer reference |
| `MODERN_GRAPHICS_PLAN.md` | ✅ Точная | Strategic plan |

### 33.2 Расхождения документации и кода

| # | Серьёзность | Страница | Проблема |
|---|---|---|---|
| 1 | P4 | `graphics-primitives.html` | CIRCLE default color 15 не указан на пользовательской странице |
| 2 | P4 | `graphics-primitives.html` | PAINT default fill (CurrentBackgroundColor) не указан явно |
| 3 | P4 | `modern-graphics.html` | SETMAPTILE группирован с CANVAS, но не требует его |

---

## 34. Интеграция с IDE

### 34.1 Подсветка синтаксиса

Все 34 ключевых слова, 4 типа данных и 32+ встроенных функции присутствуют в `Axiom.xshd` и `Axiom.Dark.xshd`. Подсветка корректна.

### 34.2 Форматирование кода

`AxiomCodeFormattingService` корректно форматирует все графические операторы. Тесты подтверждают.

### 34.3 Запуск программ

`ProgramRunService` → `AxiomExecutionPipeline` корректно запускает графические программы. Smoke harness покрывает GET/PUT, SPRITE, SCREEN 10.

### 34.4 Нативное окно

`NativeGraphicsScreenWindow` создаёт Win32-окно с D3D11 рендерером. Поддерживает клавиатуру, мышь, скролл.

---

## 35. Зависимости от Host / Platform

| Зависимость | Тип | Комментарий |
|---|---|---|
| Windows | HOST LIMITATION | Win32 API для нативного окна |
| .NET 10.0 | HOST LIMITATION | Требует .NET 10.0 |
| D3D11 | HOST LIMITATION | Требует DirectX 11 |
| WPF | HOST_LIMITATION | Dispatcher для UI-потока |
| UTF-8 файлы | LANGUAGE DEFECT | Tiled JSON читается как UTF-8 |
| BGRA пиксели | HOST LIMITATION | D3D11 ожидает BGRA формат |

---

## 36. Карта корневых причин

### ROOT-SYS-001: Пропуск обновления графической позиции
- SYS-01 (DRAWTILEMAP без SetGraphicsCurrentPosition)
- SYS-02 (DRAWTILELAYER без SetGraphicsCurrentPosition)

### ROOT-SYS-002: Несогласованность error handling
- SYS-03 (PALETTE USING non-fatal error)
- SYS-04 (различия в guard patterns между Interpreter и VM)

### ROOT-SYS-003: VM-specific validation fallthrough
- SYS-05 (DRAWIMAGE validation fallthrough)
- SYS-06 (DRAWSPRITE validation fallthrough)

### ROOT-SYS-004: Отсутствие кэширования ресурсов
- SYS-07 (LoadImage без кэша)
- SYS-08 (_scaleMapCache unbounded growth)
- SYS-09 (No glyph atlas for text rasterizer)

### ROOT-SYS-005: Потокобезопасность framebuffer
- SYS-10 (SetAbsolutePixel/GetAbsolutePixel bypass dispatcher)

---

## 37. Стабильные области

### Что работает и что не следует трогать

1. **Parser** — полностью корректен, все 34 оператора разобраны
2. **Binder** — полностью корректен, типовая проверка и cross-validation работают
3. **Lowering** — полностью корректен, 1:1 маппинг bound → lowered
4. **Bytecode emission** — полностью корректен, 1:1 маппинг lowered → opcode
5. **BytecodeModuleValidator** — обширная валидация graphics opcodes
6. **RuntimeValue типы** — immutable, корректны
7. **IGraphicsHost контракт** — стабильный, хорошо спроектированный
8. **GraphicsHostDrawing** — программная растеризация корректна
9. **GraphicsCoordinateSpaceMath** — преобразование координат корректно
10. **GraphicsBufferTransfer** — GET/PUT работают корректно
11. **GraphicsDrawParser/Executor** — DRAW мини-язык thoroughly протестирован
12. **Тесты** — excellent parity coverage между Interpreter и VM
13. **Документация** — high quality, minimal discrepancies

---

## 38. Возможности консолидации

### 38.1 LoadImage дублирование

**Текущие места:** `GraphicsHostService.LoadImage`, `GraphicsHostService.LoadImageFromResolvedPath`
**Дублируемая логика:** Декодирование BitmapDecoder → BGRA32 → uint[]
**Единый источник истины:** Один приватный метод `DecodeImageToBgra(string path)`
**Направление миграции:** Extract method
**Риски:** Минимальные

### 38.2 Validation patterns в VM

**Текущие места:** 13+ мест с `if (!condition) { Require(condition); }`
**Дублируемая логика:** Проверка + вызов guard
**Единый источник истины:** Убрать outer `if`, всегда вызывать guard
**Направление миграции:** Simplify
**Риски:** Микро-оптимизация (избежание метод-колла) будет потеряна

---

## 39. Возможности разделения ответственности

### 39.1 GraphicsHostService

Текущий `GraphicsHostService` (2432 строки) отвечает за:
- Управление SCREEN/CANVAS режимами
- Рисование примитивов
- Загрузку ресурсов
- Управление фреймбуфером
- D3D11 рендеринг
- Текстовый буфер

**Рекомендация:** Выделить `GraphicsResourceManager` для кэширования изображений и `GraphicsRenderer` для D3D11.

### 39.2 ScreenFramebuffer

Текущий `ScreenFramebuffer` (734 строки) отвечает за:
- Хранение палитровых индексов
- Хранение true-color пикселей
- Рисование примитивов
- Текстовый рендеринг
- Snapshot для D3D11

**Рекомендация:** Разделить на `PixelBuffer` (хранение) и `SoftwareRenderer` (рисование).

---

## 40. Кандидаты на удаление

| Код | Статус | Обоснование |
|---|---|---|
| `SCREEN colorSwitch` параметр | DOCUMENTED_ONLY | Явно не поддерживается, binder выдаёт диагностику |
| `DRAW X` (VARPTR$) | DOCUMENTED_ONLY | GraphicsDrawParser выдаёт MB3007 |
| Double-guard patterns в VM | POSSIBLY DEAD | Избыточные проверки |

Ничего из перечисленного не требует немедленного удаления.

---

## 41. Готовность к дальнейшему развитию

### 41.1 Готовность к Compiler

- Оpcodes уже определены и валидируются
- Lowering уже работает
- BytecodeModule уже собирается
- **Готовность: ВЫСОКАЯ**

### 41.2 Готовность к оптимизации

- Hot paths идентифицированы
- Кэширование возможно без изменения архитектуры
- Dirty-region tracking возможен
- **Готовность: ВЫСОКАЯ**

### 41.3 Готовность к расширению языка

- Новые операторы легко добавляются (следовать паттерну existing)
- Новые built-in функции легко добавляются через BuiltinFunctionDescriptor
- **Готовность: ВЫСОКАЯ**

### 41.4 Готовность к развитию IDE

- IGraphicsHost абстракция позволяет заменить D3D11 на другой рендерер
- Нативное окно изолировано от логики языка
- **Готовность: ВЫСОКАЯ**

---

## 42. Реестр проблем

| ID | Серьёзность | Категория | Компонент | Проблема | Доказательства | Корневая причина | Уверенность |
|---|---|---|---|---|---|---|---|
| SYS-01 | P2 | CORRECTNESS | Interpreter + VM | DRAWTILEMAP/DRAWTILELAYER не обновляют CurrentGraphicsPosition | Код: отсутствие SetGraphicsCurrentPosition | ROOT-SYS-001 | CONFIRMED |
| SYS-02 | P2 | CORRECTNESS | Interpreter + VM | PALETTE USING: non-fatal error при невалидном массиве | Код: return вместо throw | ROOT-SYS-002 | CONFIRMED |
| SYS-03 | P1 | CORRECTNESS | VM only | DRAWIMAGE/DRAWSPRITE: validation fallthrough — изображение не рисуется после успешной валидации | Код: return после ValidateSpriteFrame | ROOT-SYS-003 | CONFIRMED |
| SYS-04 | P3 | PERFORMANCE | IDE | LoadImage без кэширования | Код: каждый вызов перечитывает файл | ROOT-SYS-004 | CONFIRMED |
| SYS-05 | P3 | MEMORY | IDE | _scaleMapCache unbounded growth | Код: Dictionary без очистки | ROOT-SYS-004 | CONFIRMED |
| SYS-06 | P3 | PERFORMANCE | IDE | ScaleFramePixels выделяет новый массив каждый кадр | Код: new uint[] в PresentFrame | ROOT-SYS-004 | CONFIRMED |
| SYS-07 | P3 | RELIABILITY | IDE | SetAbsolutePixel/GetAbsolutePixel bypass dispatcher | Код: прямой доступ к _framebuffer | ROOT-SYS-005 | CONFIRMED |
| SYS-08 | P3 | RELIABILITY | Runtime | RuntimeTilemapValue.GetCell/SetCell без bounds checking | Код: прямой доступ к массиву | — | CONFIRMED |
| SYS-09 | P3 | RELIABILITY | Runtime | JsonDocument leak в LoadFromTiledJson | Код: отсутствие using | — | CONFIRMED |
| SYS-10 | P4 | DOCS | Manual | CIRCLE default color не указан на пользовательской странице | Сравнение manual и кода | — | CONFIRMED |
| SYS-11 | P4 | DOCS | Manual | PAINT default fill не указан явно | Сравнение manual и кода | — | CONFIRMED |
| SYS-12 | P4 | DOCS | Manual | SETMAPTILE группирован с CANVAS, но не требует его | Сравнение manual и кода | — | CONFIRMED |
| SYS-13 | P3 | PERFORMANCE | IDE | FindNearestDefaultPaletteIndex O(256) на каждый GetPixel | Код: linear scan | — | CONFIRMED |
| SYS-14 | P3 | PERFORMANCE | IDE | ScreenTextRasterizer без кэша глифов | Код: per-pixel rasterization | — | CONFIRMED |
| SYS-15 | P4 | SECURITY | Runtime | LOADIMAGE/LOADSPRITE нет валидации path traversal | Код: ResolveResourcePath без ограничений | — | MEDIUM |
| SYS-16 | P4 | SECURITY | Runtime | Нет ограничения на размер загружаемых изображений | Код: отсутствие проверки | — | CONFIRMED |
| SYS-17 | P4 | ARCHITECTURE | IDE | NativeGraphicsScreenWindow без DPI awareness | Код: отсутствие DPI handling | — | CONFIRMED |
| SYS-18 | P4 | ARCHITECTURE | IDE | ScreenFramebuffer не реализует IDisposable | Код: отсутствие интерфейса | — | CONFIRMED |
| SYS-19 | P3 | RELIABILITY | Runtime | GraphicsEllipseRasterizer: отрицательный sweep даёт пустую дугу (отличие от QB) | Код: delta <= sweep + epsilon | — | CONFIRMED |
| SYS-20 | P3 | RELIABILITY | Runtime | GraphicsDrawParser: M-команда строже QuickBASIC | Код: requires both coordinates signed | — | CONFIRMED |

---

## 43. Recovery Roadmap

### ЭТАП 0 — Базовая диагностика и измерения

**Цель:** Профилировать текущее состояние графической подсистемы

| Задача | Приоритет | Сложность |
|---|---|---|
| Профилировать CreateSnapshot на 1920×1080 | Высокий | Низкая |
| Профилировать ScaleFramePixels allocation | Высокий | Низкая |
| Профилировать FindNearestDefaultPaletteIndex | Средний | Низкая |
| Профилировать ScreenTextRasterizer.DrawGlyph | Средний | Низкая |
| Замерить memory footprint 4 screen pages | Средний | Низкая |

### ЭТАП 1 — Исправление критических проблем корректности

**Цель:** Исправить баги, влияющие на корректность выполнения

| Задача | Приоритет | Сложность |
|---|---|---|
| Исправить DRAWIMAGE/DRAWSPRITE validation fallthrough в VM (SYS-03) | Критический | Низкая |
| Исправить DRAWTILEMAP/DRAWTILELAYER SetGraphicsCurrentPosition (SYS-01) | Высокий | Низкая |
| Исправить PALETTE USING error handling (SYS-02) | Высокий | Низкая |

### ЭТАП 2 — Стабилизация семантики

**Цель:** Привести edge cases к консистентному поведению

| Задача | Приоритет | Сложность |
|---|---|---|
| Добавить bounds checking в RuntimeTilemapValue.GetCell/SetCell (SYS-08) | Средний | Низкая |
| Исправить JsonDocument leak в LoadFromTiledJson (SYS-09) | Средний | Низкая |
| Добавить валидацию tileWidth/tileHeight из JSON | Средний | Низкая |

### ЭТАП 3 — Архитектурное улучшение

**Цель:** Улучшить архитектуру IDE-слоя

| Задача | Приоритет | Сложность |
|---|---|---|
| Добавить IDisposable в ScreenFramebuffer (SYS-18) | Средний | Низкая |
| Исправить потокобезопасность SetAbsolutePixel/GetAbsolutePixel (SYS-07) | Средний | Средняя |
| Выделить GraphicsResourceManager для кэширования | Средний | Средняя |
| Добавить DPI awareness в NativeGraphicsScreenWindow (SYS-17) | Низкий | Средняя |

### ЭТАП 4 — Оптимизация производительности

**Цель:** Устранить бутылочные горлышки

| Задача | Приоритет | Сложность |
|---|---|---|
| Кэшировать LoadImage по пути (SYS-04) | Средний | Низкая |
| Persistent staging buffer для ScaleFramePixels (SYS-06) | Средний | Низкая |
| Palette lookup table для GetPixel (SYS-13) | Средний | Низкая |
| Glyph atlas для ScreenTextRasterizer (SYS-14) | Низкий | Средняя |
| Dirty-region tracking для framebuffer | Низкий | Высокая |

### ЭТАП 5 — Усиление тестирования

**Цель:** Закрыть пробелы в тестах

| Задача | Приоритет | Сложность |
|---|---|---|
| PUT с XOR/AND/OR modes | Высокий | Низкая |
| Type mismatch для IMAGE/SPRITE/TILESET | Высокий | Низкая |
| SCREEN mode transitions | Средний | Низкая |
| VIEWPORT partial clip | Средний | Низкая |
| DRAWIMAGE/DRAWSPRITE с отрицательными размерами | Средний | Низкая |
| DRAWTILEMAP с CAMERA/VIEWPORT | Средний | Низкая |
| BytecodeModuleValidator graphics opcode tests | Средний | Средняя |

### ЭТАП 6 — Обновление документации

**Цель:** Закрыть расхождения документации и кода

| Задача | Приоритет | Сложность |
|---|---|---|
| Добавить CIRCLE default color в manual (SYS-10) | Низкий | Низкая |
| Добавить PAINT default fill в manual (SYS-11) | Низкий | Низкая |
| Уточнить SETMAPTILE independence от CANVAS (SYS-12) | Низкий | Низкая |
| Добавить LOADTILESET grid mismatch в diagnostics | Низкий | Низкая |

### ЭТАП 7 — Обновление IDE

**Цель:** Улучшить IDE-интеграцию

| Задача | Приоритет | Сложность |
|---|---|---|
| Исправить SetFocus на каждый mouse move | Средний | Низкая |
| Добавить WM_ERASEBKGND handler | Низкий | Низкая |
| Добавить fallback WARP device для D3D11 | Низкий | Средняя |

### ЭТАП 8 — Подготовка к дальнейшему развитию

**Цель:** Подготовить базу для будущих расширений

| Задача | Приоритет | Сложность |
|---|---|---|
| SCREEN colorSwitch support (если нужно) | Низкий | Средняя |
| Alpha transparency для SCREEN palette | Низкий | Средняя |
| Hardware-accelerated text (DirectWrite) | Низкий | Высокая |
| Multi-monitor support | Низкий | Высокая |

---

## 44. Граф зависимостей задач

```
ЭТАП 0 (диагностика)
    ↓
ЭТАП 1 (критические fixes) ← не зависит от ЭТАП 0
    ↓
ЭТАП 2 (стабилизация) ← зависит от ЭТАП 1
    ↓
ЭТАП 3 (архитектура) ← не зависит от ЭТАП 2
    ↓
ЭТАП 4 (оптимизация) ← зависит от ЭТАП 3
    ↓
ЭТАП 5 (тесты) ← не зависит от ЭТАП 4
    ↓
ЭТАП 6 (документация) ← зависит от ЭТАП 1
    ↓
ЭТАП 7 (IDE) ← зависит от ЭТАП 3
    ↓
ЭТАП 8 (будущее) ← зависит от всех предыдущих
```

---

## 45. Definition of Done

Подсистема GRAPHICS/SCREEN считается завершённой, когда:

1. **Корректность:** Все 34 оператора и 35 встроенных функций работают корректно в обоих движках
2. **Edge cases:** Все обнаруженные edge cases исправлены или документированы
3. **Error handling:** Все ошибки возвращают человекопонятные диагностики с координатами
4. **Interpreter:** Все операторы работают через интерпретатор
5. **VM:** Все операторы работают через BytecodeVm с паритетом с интерпретатором
6. **Тесты:** Каждый оператор имеет минимум 1 позитивный и 1 негативный тест
7. **Документация:** Каждый оператор имеет страницу в manual с синтаксисом, параметрами и примерами
8. **IDE:** Подсветка синтаксиса, форматирование и запуск работают для всех операторов
9. **Производительность:** Hot paths профилированы и оптимизированы
10. **Samples:** Каждый оператор имеет запускаемый .axm пример

---

## 46. ТОП-20 проблем корректности и архитектуры

| # | ID | Проблема | Серьёзность |
|---|---|---|---|
| 1 | SYS-03 | DRAWIMAGE/DRAWSPRITE validation fallthrough в VM | P1 |
| 2 | SYS-01 | DRAWTILEMAP/DRAWTILELAYER без SetGraphicsCurrentPosition | P2 |
| 3 | SYS-02 | PALETTE USING non-fatal error handling | P2 |
| 4 | SYS-07 | SetAbsolutePixel/GetAbsolutePixel потокобезопасность | P3 |
| 5 | SYS-08 | RuntimeTilemapValue.GetCell/SetCell без bounds checking | P3 |
| 6 | SYS-09 | JsonDocument leak в LoadFromTiledJson | P3 |
| 7 | SYS-19 | Отрицательный sweep в эллипсе (отличие от QB) | P3 |
| 8 | SYS-20 | M-команда строже QuickBASIC | P3 |
| 9 | SYS-04 | LoadImage без кэширования | P3 |
| 10 | SYS-05 | _scaleMapCache unbounded growth | P3 |
| 11 | SYS-06 | ScaleFramePixels allocation каждый кадр | P3 |
| 12 | SYS-13 | FindNearestDefaultPaletteIndex O(256) | P3 |
| 13 | SYS-14 | ScreenTextRasterizer без кэша глифов | P3 |
| 14 | SYS-15 | Path traversal в LOADIMAGE | P4 |
| 15 | SYS-16 | Нет ограничения размера изображений | P4 |
| 16 | SYS-17 | NativeGraphicsScreenWindow без DPI | P4 |
| 17 | SYS-18 | ScreenFramebuffer без IDisposable | P4 |
| 18 | SYS-10 | CIRCLE default color не в manual | P4 |
| 19 | SYS-11 | PAINT default fill не в manual | P4 |
| 20 | SYS-12 | SETMAPTILE группирован с CANVAS в docs | P4 |

---

## 47. ТОП-20 задач по производительности

| # | Задача | Потенциальный выигрыш | Сложность |
|---|---|---|---|
| 1 | Кэш LoadImage по пути | Устраняет повторное декодирование | Низкая |
| 2 | Persistent staging buffer для ScaleFramePixels | Устраняет ~8MB/frame allocation | Низкая |
| 3 | Palette lookup table для GetPixel | O(1) вместо O(256) | Низкая |
| 4 | Glyph atlas для ScreenTextRasterizer | Устраняет per-pixel dictionary lookup | Средняя |
| 5 | Dirty-region tracking для framebuffer | Обновление только изменённых областей | Высокая |
| 6 | Batch rendering для примитивов | Уменьшение draw calls | Высокая |
| 7 | GPU texture для framebuffer (вместо UpdateSubresource) | Ускорение present | Средняя |
| 8 | Pre-rasterized text cache | Устраняет per-frame text rendering | Средняя |
| 9 | Image atlas для tileset tiles | Уменьшение texture switches | Средняя |
| 10 | Frustum culling для тайловых карт | Уменьшение числа отрисовываемых тайлов | Низкая |
| 11 | Spritesheet batching | Уменьшение draw calls для спрайтов | Средняя |
| 12 | Async image loading | Не блокировать UI при загрузке | Средняя |
| 13 | Memory pooling для flood fill stack | Устраняет allocation при заливке | Низкая |
| 14 | Coordinate transform cache | Устраняет повторные вычисления | Низкая |
| 15 | Frame timing measurement | Профилирование D3D11 present | Низкая |
| 16 | VSync toggle option | Возможность uncapped для benchmark | Низкая |
| 17 | Text measurement cache | Устраняет повторные вызовы TEXTWIDTH | Низкая |
| 18 | Tile visibility pre-pass | Пропуск невидимых тайлов | Низкая |
| 19 | Palette change detection | Избежать full palette rebuild | Низкая |
| 20 | Screen page swap optimization | Page flip вместо copy | Средняя |

---

## 48. Итоговая оценка здоровья подсистемы

| Аспект | Оценка (0-10) | Обоснование |
|---|---|---|
| Корректность | 8 | 1 P1-баг (VM DRAWIMAGE/DRAWSPRITE), 2 P2-бага |
| Семантика | 9 | Полная, консистентная, documented |
| Архитектура | 9 | Чёткое разделение слоёв, чистая абстракция IGraphicsHost |
| Runtime | 8 | Зрелый, но есть проблемы потокобезопасности в IDE |
| Interpreter | 9 | Полная реализация, корректные guard'ы |
| Bytecode VM | 8 | Полная реализация, 1 P1-баг |
| Производительность | 7 | Hot paths идентифицированы, но не оптимизированы |
| Память | 7 | Нет IDisposable, утечки кэша, allocation per frame |
| Error handling | 8 | Хорошая диагностика, 1 P2 inconsistency |
| Тесты | 8 | Excellent parity, но есть пробелы в edge cases |
| Samples | 8 | Хорошее покрытие, некоторые edge cases не покрыты |
| Документация | 9 | High quality, 3 minor discrepancies |
| IDE | 7 | Работает, но есть проблемы с потокобезопасностью и DPI |
| Maintainability | 8 | Хорошо структурированный код, чёткие responsibilities |
| Reliability | 8 | Стабильная работа, minor edge cases |
| Future readiness | 9 | Отличная база для дальнейшего развития |

**Общий Health Score: 8.1/10**

---

## Главный контрольный вопрос

> Можно ли считать подсистему GRAPHICS/SCREEN достаточно стабильной, корректной и архитектурно понятной для перехода к следующей подсистеме?

## **YES, WITH CONDITIONS**

### Основные blockers

1. **SYS-03 (P1):** DRAWIMAGE/DRAWSPRITE validation fallthrough в VM — изображения не рисуются при определённых условиях. Это баг корректности, который нужно исправить немедленно.

### Что нужно сделать в первую очередь

1. Исправить SYS-03 (DRAWIMAGE/DRAWSPRITE VM validation fallthrough)
2. Исправить SYS-01 (DRAWTILEMAP/DRAWTILELAYER SetGraphicsCurrentPosition)
3. Исправить SYS-02 (PALETTE USING error handling)
4. Добавить недостающие тесты для PUT XOR/AND/OR и type mismatches

### Что можно отложить

1. Оптимизации производительности (ЭТАП 4)
2. DPI awareness (ЭТАП 7)
3. Glyph atlas и dirty-region tracking (ЭТАП 8)
4. Документационные мелочи (ЭТАП 6)

### Что не следует трогать

1. Parser — полностью корректен
2. Binder — полностью корректен
3. Lowering — полностью корректен
4. IGraphicsHost контракт — стабилен
5. RuntimeValue типы — immutable и корректны
6. Тестовая стратегия parity — excellent
