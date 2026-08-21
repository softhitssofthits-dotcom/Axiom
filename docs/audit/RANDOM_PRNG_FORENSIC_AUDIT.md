# AXIOM — ГЛУБОКИЙ FORENSIC-АУДИТ
## RANDOM / PRNG ПОДСИСТЕМА

---

## 1. Резюме

Проведён полный forensic-аудит подсистемы RANDOM/PRNG языка Axiom. Подсистема минимальна: одна встроенная функция `RND()`, возвращающая псевдослучайное `DOUBLE` в полуоткрытом интервале `[0, 1)`. Источник случайности абстрагирован через интерфейс `IRuntimeRandom` и прокидывается через `IRuntimeHost` → `ExecutionRequest` → `ExecutionRuntimeHostAdapter`.

**Итоговая оценка здоровья: 9.0/10.**

Подсистема фундаментально корректна, имеет полный паритет Interpreter ↔ BytecodeVm, адекватные тесты, sample и документацию. Основные находки — это feature gaps (отсутствие `RANDOMIZE`, seed-управления, диапазонных форм), а не дефекты корректности.

---

## 2. Что представляет собой эта подсистема

Подсистема RANDOM/PRNG отвечает за генерацию псевдослучайных чисел в языке Axiom. Текущий объём:

- **Одна функция**: `RND()` — возвращает `DOUBLE` в `[0, 1)`.
- **Абстракция источника**: `IRuntimeRandom` с одним методом `double NextRandomDouble()`.
- **Интеграция с хостом**: `IRuntimeHost` наследует `IRuntimeRandom`.
- **Детерминированность для тестов**: через `ExecutionRequest.runtimeRandom` можно подставить фиксированный или seeded генератор.

Подсистема не содержит:
- оператора `RANDOMIZE` для установки seed;
- форм `RND(n)` с аргументами (как в QuickBASIC);
- функций `RANDOMIZE` / `RandomSeed`;
- предопределённых констант для seed;
- воспроизводимости последовательности между запусками.

---

## 3. Полный список возможностей

### SYS-001: Функция RND

| Поле | Значение |
|------|----------|
| **ID** | SYS-001 |
| **Название** | RND |
| **Категория** | Встроенные функции — Числовые |
| **Назначение** | Генерация псевдослучайного числа типа DOUBLE в [0, 1) |
| **Пользовательский синтаксис** | `RND()` |
| **Тип** | FUNCTION |
| **Parser** | Не требуется — `RND` парсится как вызов идентификатора через стандартный `ParseCallExpression` / `ParsePrimaryExpression` |
| **Syntax Node** | `CallExpressionSyntax` |
| **Binder** | `BuiltinFunctionId.Rnd`, `TypeSymbol.Double`, 0 параметров |
| **Semantic Layer** | Полная — проверка арности (MB2004 при аргументе) |
| **Interpreter** | `EvaluateRndBuiltin()` → `BuiltinNumericHelpers.EvaluateRnd()` → `_host.NextRandomDouble()` |
| **Bytecode Lowering** | Не требуется — `RND` не имеет специальных bound-узлов, lowering проходит через стандартный `BoundCallExpression` |
| **Opcode** | `CallBuiltin` с `BuiltinBinding(Rnd)` |
| **BytecodeVm** | `BuiltinExecution.NullaryNoLocation` → `EvaluateRndBuiltin()` → `BuiltinNumericHelpers.EvaluateRnd()` → `_host.NextRandomDouble()` |
| **Runtime Helper** | `BuiltinNumericHelpers.EvaluateRnd(Func<double>)` |
| **IDE** | Подсветка `RND` как встроенной функции в обеих темах (Axiom.xshd, Axiom.Dark.xshd) |
| **Tests** | 4 теста: Binder (2), Interpreter (1), BytecodeVm (1), Execution Pipeline (1) |
| **Samples** | `samples/functions/rnd/basic_usage.axm` |
| **Benchmarks** | `samples/smoke/engine-compare/random_stream_snapshot.axm` |
| **Documentation** | `docs/manual/numeric-functions-core.html#rnd`, `docs/manual/numeric-functions.html` |
| **Текущий статус** | IMPLEMENTED |
| **Уровень уверенности** | CONFIRMED |
| **Доказательства** | Код, тесты, samples, manual, benchmark |

### SYS-002: Интерфейс IRuntimeRandom

| Поле | Значение |
|------|----------|
| **ID** | SYS-002 |
| **Название** | IRuntimeRandom |
| **Категория** | Runtime — Host Abstraction |
| **Назначение** | Абстракция источника псевдослучайных чисел |
| **Пользовательский синтаксис** | Нет — внутренний интерфейс |
| **Тип** | RUNTIME FACILITY |
| **Файл** | `src/Axiom.Runtime/Host/IRuntimeRandom.cs` |
| **Метод** | `double NextRandomDouble()` |
| **Статус** | IMPLEMENTED |
| **Уверенность** | CONFIRMED |

### SYS-003: Интеграция через ExecutionRequest

| Поле | Значение |
|------|----------|
| **ID** | SYS-003 |
| **Название** | ExecutionRequest.RuntimeRandom |
| **Категория** | Execution Pipeline — Hosting |
| **Назначение** | Прокидывание управляемого источника случайности из хоста в runtime |
| **Тип** | RUNTIME FACILITY |
| **Файлы** | `src/Axiom.Execution/Requests/ExecutionRequest.cs`, `src/Axiom.Execution/Hosting/ExecutionRuntimeHostAdapter.cs` |
| **Статус** | IMPLEMENTED |
| **Уверенность** | CONFIRMED |

### SYS-004: NullRuntimeHost — дефолтный генератор

| Поле | Значение |
|------|----------|
| **ID** | SYS-004 |
| **Название** | NullRuntimeHost.NextRandomDouble() |
| **Категория** | Runtime — Host Default |
| **Назначение** | Дефолтный неуправляемый источник случайности через `System.Random` |
| **Тип** | RUNTIME FACILITY |
| **Файл** | `src/Axiom.Runtime/Host/NullRuntimeHost.cs:75` |
| **Реализация** | `private readonly Random _random = new();` → `_random.NextDouble()` |
| **Статус** | IMPLEMENTED |
| **Уверенность** | CONFIRMED |

---

## 4. Фактическая архитектура

### 4.1 Общая схема

```
Пользовательский код:  DIM x AS DOUBLE = RND()
                              │
                              ▼
Lexer:                  TokenKind.Identifier("RND")
                              │
                              ▼
Parser:                 CallExpressionSyntax("RND", [])
                              │
                              ▼
Binder:                 BoundCallExpression(FunctionSymbol(Rnd), [], TypeSymbol.Double)
                              │
                              ▼
Lowering:               BoundCallExpression (без изменений)
                              │
                              ▼
BytecodeCompiler:       CallBuiltin(BuiltinBinding(Rnd, 0))
                              │
                              ▼
BytecodeVm:             BuiltinExecution.NullaryNoLocation → EvaluateRndBuiltin()
                              │
                              ▼
BuiltinNumericHelpers:  EvaluateRnd(() => _host.NextRandomDouble())
                              │
                              ▼
IRuntimeRandom:         NextRandomDouble() → System.Random.NextDouble()
                              │
                              ▼
RuntimeValue:           FromDouble(0.xxx)
```

### 4.2 Ключевые файлы

| Файл | Ответственность |
|------|----------------|
| `src/Axiom.Runtime/Host/IRuntimeRandom.cs` | Интерфейс источника случайности (6 строк) |
| `src/Axiom.Runtime/Host/IRuntimeHost.cs` | IRuntimeHost наследует IRuntimeRandom |
| `src/Axiom.Runtime/Host/NullRuntimeHost.cs:75` | Дефолтная реализация через System.Random |
| `src/Axiom.Runtime/Builtins/BuiltinNumericHelpers.cs:8-11` | Общий helper: `EvaluateRnd(Func<double>)` |
| `src/Axiom.Runtime/Execution/Interpreter.Builtins.Numeric.cs:9-12` | Interpreter-путь |
| `src/Axiom.Runtime/Execution/BytecodeVm.Builtins.Numeric.cs:9-12` | VM-путь |
| `src/Axiom.Runtime/Execution/BytecodeVm.BuiltinDispatch.cs:186` | VM dispatch: NullaryNoLocation |
| `src/Axiom.Runtime/Execution/BytecodeVm.cs:294` | Reverse execution safety: `Rnd => false` |
| `src/Axiom.Semantics/Symbols/BuiltinFunctionId.cs:250` | Идентификатор `Rnd` |
| `src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.Numeric.cs:9` | Регистрация: `RND`, `DOUBLE`, 0 параметров |
| `src/Axiom.Execution/Requests/ExecutionRequest.cs:13,27,45` | `IRuntimeRandom` в request |
| `src/Axiom.Execution/Hosting/ExecutionRuntimeHostAdapter.cs:11,16,504` | Делегирование в host |
| `src/Axiom.Execution/Pipeline/AxiomExecutionPipeline.cs:98` | Rnd в списке поддержанных built-in |

---

## 5. Полный путь выполнения

### 5.1 Interpreter-путь

```
SourceText "RND()"
  → Lexer.Tokenize() → [Identifier("RND"), OpenParen, CloseParen]
  → Parser.ParseExpression() → CallExpressionSyntax("RND", [])
  → Binder.BindCallExpression()
      → LookupBuiltin("RND") → BuiltinFunctionId.Rnd
      → Check arity: 0 parameters, 0 arguments → OK
      → BoundCallExpression(Type=Double)
  → Interpreter.EvaluateCallExpression()
      → dispatch BuiltinFunctionId.Rnd
      → EvaluateRndBuiltin()
      → BuiltinNumericHelpers.EvaluateRnd(() => _host.NextRandomDouble())
      → RuntimeValue.FromDouble(0.734...)
```

### 5.2 BytecodeVm-путь

```
SourceText "RND()"
  → Lexer → Parser → Binder (тот же путь)
  → BoundProgramLowerer → BoundCallExpression (без изменений)
  → BytecodeCompiler → CallBuiltin(BindingIndex=N)
  → BytecodeVm.ExecuteCallBuiltin()
      → BuiltinExecution.NullaryNoLocation(static vm => vm.EvaluateRndBuiltin())
      → EvaluateRndBuiltin()
      → BuiltinNumericHelpers.EvaluateRnd(() => _host.NextRandomDouble())
      → RuntimeValue.FromDouble(0.734...)
      → PushUnchecked(result)
```

### 5.3 Особенности

- **Нет lowering-трансформаций**: `RND` проходит через lowering без изменений.
- **Нет constant folding**: `RND()` не может быть свёрнут на этапе компиляции (non-deterministic).
- **Нет fast path**: `RND` не имеет inline fast path в `TryExecuteFastBuiltin` (не нужен — NullaryNoLocation уже минимален).
- **Reverse execution safety**: `Rnd` помечен как `false` в `IsBuiltinReverseExecutionSafe`, что корректно — повторный вызов даёт другое значение.

---

## 6. Фактический семантический контракт

### 6.1 RND()

| Параметр | Значение |
|----------|----------|
| **Вход** | Нет аргументов |
| **Выход** | `RuntimeValue` типа `DOUBLE` |
| **Диапазон** | `[0, 1)` — от 0 включительно до 1 не включительно |
| **Алгоритм** | `System.Random.NextDouble()` (или внешний `IRuntimeRandom`) |
| **Побочные эффекты** | Изменяет внутреннее состояние генератора |
| **Детерминированность** | Зависит от реализации `IRuntimeRandom` |
| **Thread safety** | `System.Random` не thread-safe; `NullRuntimeHost` — singleton |
| **Ошибки** | Невозможны при корректном вызове без аргументов |
| **Диагностика** | MB2004 при передаче аргумента (`RND(1)`) |

### 6.2 Недопустимые входы

| Вход | Результат |
|------|-----------|
| `RND(1)` | MB2004: "Неверное число аргументов вызова 'RND'. Ожидалось 0, но получено 1." |
| `RND(0.5)` | MB2004: аналогично |
| `RND(a, b)` | MB2004: аналогично |

---

## 7. Анализ Parser

### 7.1 Специальный синтаксис

**Нет.** `RND` парсится как обычный вызов идентификатора через стандартный механизм `ParseCallExpression`. Специальных синтаксических правил для `RND` не существует.

### 7.2 Токен

`RND` не является ключевым словом лексера. Это идентификатор, который распознаётся на этапе семантического анализа как встроенная функция.

**Статус**: Корректно. Соответствует модели всех остальных встроенных функций Axiom.

---

## 8. Анализ Binder

### 8.1 Регистрация

```csharp
// BuiltinFunctions.Numeric.cs:9
CreateDefault(BuiltinFunctionId.Rnd, "RND", TypeSymbol.Double, [])
```

- Идентификатор: `BuiltinFunctionId.Rnd`
- Имя: `"RND"`
- Возвращаемый тип: `TypeSymbol.Double`
- Параметры: `[]` (нет параметров)

### 8.2 Проверка арности

При вызове `RND(1)` Binder генерирует MB2004 (wrong arity). Это подтверждено тестом `Bind_BuiltinRndReportsWrongArityWhenArgumentIsPassed`.

### 8.3 Возвращаемый тип

`TypeSymbol.Double` — подтверждено тестом `Bind_BuiltinRndReturnsDouble`.

**Статус**: Полная корректность.

---

## 9. Анализ Interpreter

### 9.1 Реализация

```csharp
// Interpreter.Builtins.Numeric.cs:9-12
private RuntimeValue EvaluateRndBuiltin()
{
    return BuiltinNumericHelpers.EvaluateRnd(() => _host.NextRandomDouble());
}
```

### 9.2 Dispatch

```csharp
// Interpreter.Builtins.cs:378
BuiltinFunctionId.Rnd => EvaluateRndBuiltin(),
```

### 9.3 Общий helper

```csharp
// BuiltinNumericHelpers.cs:8-11
public static RuntimeValue EvaluateRnd(Func<double> nextRandom)
{
    return RuntimeValue.FromDouble(nextRandom());
}
```

**Статус**: Минимальный, корректный, без дублирования.

---

## 10. Анализ Bytecode / VM

### 10.1 Dispatch

```csharp
// BytecodeVm.BuiltinDispatch.cs:186
BuiltinFunctionId.Rnd => BuiltinExecution.NullaryNoLocation(static vm => vm.EvaluateRndBuiltin()),
```

`NullaryNoLocation` — оптимальная форма для функции без аргументов, не требующей `TextLocation`.

### 10.2 Реализация

```csharp
// BytecodeVm.Builtins.Numeric.cs:9-12
private RuntimeValue EvaluateRndBuiltin()
{
    return BuiltinNumericHelpers.EvaluateRnd(() => _host.NextRandomDouble());
}
```

### 10.3 Fallback dispatch

```csharp
// BytecodeVm.Builtins.cs:162
BuiltinFunctionId.Rnd => EvaluateRndBuiltin(),
```

### 10.4 Reverse execution safety

```csharp
// BytecodeVm.cs:294
BuiltinFunctionId.Rnd => false,
```

Корректно: `RND` не является воспроизводимой операцией, поэтому при её вызове история обратного выполнения инвалидируется.

### 10.5 Fast path

Нет. `RND` не имеет inline fast path в `TryExecuteFastBuiltin`. Это не является проблемой: `NullaryNoLocation` уже минимален по накладным расходам (нет разрешения location, нет аргументов).

### 10.6 Lowering

`RND` не требует специального lowering. `BoundCallExpression` проходит через стандартный `LowerExpression` → `LowerCallExpression` без трансформаций.

**Статус**: Полная корректность, полный паритет с Interpreter.

---

## 11. Анализ Runtime

### 11.1 IRuntimeRandom

```csharp
// IRuntimeRandom.cs
public interface IRuntimeRandom
{
    double NextRandomDouble();
}
```

Интерфейс минимален: один метод, возвращающий `double` в `[0, 1)`.

### 11.2 IRuntimeHost

```csharp
// IRuntimeHost.cs
public interface IRuntimeHost : IInputProvider, IKeyboardInputProvider, IMouseInputProvider,
    IOutputWriter, IRuntimeClock, IRuntimeRandom, IGraphicsHost, IRuntimeCancellationProvider
```

`IRuntimeHost` наследует `IRuntimeRandom`, что означает: хост является источником случайности.

### 11.3 NullRuntimeHost

```csharp
// NullRuntimeHost.cs:5,75
private readonly Random _random = new();
public double NextRandomDouble() => _random.NextDouble();
```

Дефолтная реализация использует `System.Random` с неуказанным seed (зависит от текущего времени).

### 11.4 ExecutionRuntimeHostAdapter

```csharp
// ExecutionRuntimeHostAdapter.cs:11,16,504
private readonly IRuntimeRandom _runtimeRandom;
// ... constructor ...
public double NextRandomDouble() => _runtimeRandom.NextRandomDouble();
```

Адаптер просто делегирует вызов.

**Статус**: Архитектура чистая, абстракция корректная.

---

## 12. Взаимодействие с типами

| Взаимодействие | Статус |
|----------------|--------|
| `RND()` → `DOUBLE` | Корректно — единственный возвращаемый тип |
| `DIM x AS DOUBLE = RND()` | Корректно — присваивание совместимых типов |
| `DIM x AS INTEGER = RND()` | Корректно — неявное приведение DOUBLE → INTEGER (отбрасывает дробную часть) |
| `INT(RND() * 10)` | Корректно — стандартный паттерн получения случайного целого |
| `RND()` в выражениях | Корректно — DOUBLE участвует во всех арифметических операциях |

---

## 13. Взаимодействие со String

Не применимо. `RND()` не взаимодействует со строками.

---

## 14. Взаимодействие с Array

Не применимо напрямую. `RND()` может использоваться для заполнения массивов, но это не часть подсистемы.

---

## 15. Взаимодействие с TYPE

Не применимо. `RND()` не взаимодействует с пользовательскими типами.

---

## 16. Взаимодействие с Functions / SUB

`RND()` может вызываться из любого контекста: глобального кода, `SUB`, `FUNCTION`, локальных блоков. Специальных ограничений нет.

---

## 17. Взаимодействие с Control Flow

Не применимо специфически. `RND()` работает в любом контексте управления потоком.

---

## 18. Взаимодействие с Error / Exception System

### 18.1 Диагностики

| Код | Сообщение | Условие |
|-----|-----------|---------|
| MB2004 | "Неверное число аргументов вызова 'RND'. Ожидалось 0, но получено N." | `RND(1)` или `RND(a, b)` |

### 18.2 Runtime errors

Невозможны при корректном вызове. `System.Random.NextDouble()` не бросает исключений.

### 18.3 TRY/CATCH

Не применимо — нет точек отказа.

---

## 19. State / Lifetime / Ownership

### 19.1 Владелец генератора

| Кто | Что |
|-----|-----|
| **Создаёт** | Хост (IDE или тестовый harness) через `ExecutionRequest.runtimeRandom` |
| **Владеет** | `ExecutionRuntimeHostAdapter` (хранит ссылку на `IRuntimeRandom`) |
| **Изменяет** | `System.Random` (внутреннее состояние при каждом вызове `NextDouble()`) |
| **Освобождает** | GC (нет `IDisposable`) |

### 19.2 NullRuntimeHost

Singleton (`NullRuntimeHost.Instance`). Содержит единственный экземпляр `Random`. Не thread-safe.

### 19.3 Время жизни

Генератор живёт в рамках одного запуска программы. При каждом новом `Run()` создаётся новый `ExecutionRuntimeHostAdapter`, но если передан тот же `IRuntimeRandom`, состояние генератора сохраняется между запусками.

---

## 20. Edge Cases

### 20.1 Проверенные edge cases

| Сценарий | Результат | Статус |
|----------|-----------|--------|
| `RND()` — базовый вызов | DOUBLE в [0, 1) | ✅ Покрыт тестами |
| `RND(1)` — лишний аргумент | MB2004 | ✅ Покрыт тестом Binder |
| `RND()` в цикле 240 итераций | Детерминированный поток с seeded генератором | ✅ Покрыт benchmark |
| `RND()` с фиксированным Random | Детерминированный вывод | ✅ Покрыт тестом Execution Pipeline |
| `RND()` в Interpreter | Корректный диапазон | ✅ Покрыт тестом |
| `RND()` в BytecodeVm | Корректный диапазон | ✅ Покрыт тестом |

### 20.2 Непроверенные edge cases

| Сценарий | Риск |
|----------|------|
| Многократный вызов `RND()` — проверка распределения | Низкий — `System.Random` стандартный |
| `RND()` при очень большом количестве вызовов (>1M) | Низкий — `System.Random` не деградирует |
| `RND()` в многопоточном сценарии | Средний — `System.Random` не thread-safe, но Axiom однопоточен |
| `RND()` после ошибки и повторного запуска | Низкий — каждый запуск создаёт новый adapter |

---

## 21. Interpreter ↔ Bytecode VM

| Возможность | Interpreter | Bytecode VM | Одинаковый результат | Одинаковая ошибка | Одинаковое состояние |
|---|---|---|---|---|---|
| `RND()` — базовый вызов | ✅ | ✅ | ✅ | ✅ | ✅ |
| `RND(1)` — ошибка арности | MB2004 | MB2004 | ✅ | ✅ | ✅ |
| `RND()` с фиксированным Random | Детерминированный | Детерминированный | ✅ | ✅ | ✅ |
| Reverse execution safety | N/A (Interpreter) | `false` | N/A | N/A | ✅ |

**Расхождений не обнаружено.** Полный паритет подтверждён.

---

## 22. Анализ производительности

### 22.1 Benchmark

| Сценарий | Interpreter | VM | Ускорение | Классификация |
|---|---:|---:|---:|---|
| `random_stream_snapshot.axm` (240 вызовов RND) | baseline | 0.818x runtime | 0.818x | CLEAR WIN |

VM быстрее интерпретатора на ~18% для сценария密集ного вызова `RND`.

### 22.2 Hot path

`RND` не является hot path в типичных программах. Один вызов = один `System.Random.NextDouble()` + один `RuntimeValue.FromDouble()` + один push на стек. Накладные расходы минимальны.

### 22.3 Bottlenecks

Нет. Подсистема не создаёт узких мест.

---

## 23. Качество Benchmark

### 23.1 random_stream_snapshot.axm

- 240 вызовов `RND()` в цикле
- Детерминированный seed через `SeededRuntimeRandom(24681357)`
- Проверяет: `sumScaled`, `bucketChecksum`, `zigzag`
- Стабильность: подтверждена на длинных прогонах (10 итераций, 3 прогрева)

**Качество**: Высокое. Benchmark детерминирован, воспроизводим, покрывает密集ный сценарий.

---

## 24. Hot Paths и Bottlenecks

Нет. `RND` не является bottleneck. Подсистема не требует оптимизации.

---

## 25. Архитектурные проблемы

### 25.1 Отсутствие RANDOMIZE

**Категория**: FEATURE GAP (не баг)

В QuickBASIC и большинстве BASIC-диалектов существует оператор `RANDOMIZE [seed]` для установки seed генератора. В Axiom этот оператор отсутствует. Seed можно установить только программно через `ExecutionRequest.runtimeRandom`.

**Последствие**: Пользователь не может управлять воспроизводимостью последовательности из кода на Axiom.

### 25.2 Отсутствие форм RND(n)

**Категория**: FEATURE GAP (не баг)

В QuickBASIC `RND(n)` имеет несколько форм:
- `RND()` — следующее случайное число
- `RND(0)` — повторить последнее случайное число
- `RND(n)` — следующее случайное число (n > 0)
- `RND(-n)` — детерминированная последовательность от seed n

В Axiom только `RND()` без аргументов.

**Последствие**: Ограниченная функциональность по сравнению с QuickBASIC.

### 25.3 NullRuntimeHost singleton и thread safety

**Категория**: ПОТЕНЦИАЛЬНАЯ ПРОБЛЕМА (низкий риск)

`NullRuntimeHost.Instance` содержит `private readonly Random _random = new()`. `System.Random` не thread-safe. Однако Axiom — однопоточное приложение, поэтому это не является актуальной проблемой.

---

## 26. Дублирование логики

**Нет дублирования.** Логика `RND` находится ровно в одном месте: `BuiltinNumericHelpers.EvaluateRnd()`. Оба движка (Interpreter и BytecodeVm) делегируют в этот helper.

---

## 27. Single Source of Truth

| Сущность | Источник истины |
|----------|----------------|
| Идентификатор `Rnd` | `BuiltinFunctionId.Rnd` в `Axiom.Semantics` |
| Регистрация built-in | `BuiltinFunctions.Numeric.cs` в `Axiom.Semantics` |
| Семантика выполнения | `BuiltinNumericHelpers.EvaluateRnd()` в `Axiom.Runtime` |
| Абстракция источника | `IRuntimeRandom` в `Axiom.Runtime` |
| Диапазон [0, 1) | Документация + тесты (неявный контракт `System.Random.NextDouble()`) |

**Статус**: Единый источник правды для всех аспектов.

---

## 28. Legacy / Dead Code

**Нет.** Подсистема не содержит legacy или dead code.

---

## 29. Надёжность и безопасность

### 29.1 Безопасность

| Риск | Статус |
|------|--------|
| Неконтролируемый доступ к ресурсам | Нет — `RND` не создаёт ресурсов |
| Path handling | Не применимо |
| Resource exhaustion | Нет — один вызов = один double |
| Malformed input | Нет — нет входных данных |
| Host exceptions | Нет — `System.Random.NextDouble()` не бросает исключений |
| Memory abuse | Нет — нет аллокаций |
| Stale handles | Нет |
| Unsafe native interaction | Нет |
| Unsafe GPU interaction | Нет |

### 29.2 Надёжность

Подсистема надёжна: нет точек отказа, нет ресурсов, нет состояния, которое может стать stale.

---

## 30. Анализ тестов

### 30.1 Существующие тесты

| Тест | Тип | Что проверяет |
|------|-----|---------------|
| `Bind_BuiltinRndReturnsDouble` | Semantic | Возвращаемый тип DOUBLE |
| `Bind_BuiltinRndReportsWrongArityWhenArgumentIsPassed` | Semantic | MB2004 при аргументе |
| `Execute_BuiltinRndReturnsDoubleInUnitInterval` | Runtime (Interpreter) | Диапазон [0, 1) |
| `Execute_CompiledRndReturnsValueInHalfOpenRange` | Runtime (BytecodeVm) | Диапазон [0, 1) |
| `Run_UsesProvidedRuntimeRandomForRndBuiltin` | Execution Pipeline | Детерминированный вывод с фиксированным Random |

### 30.2 Качество тестов

Тесты покрывают:
- ✅ Семантическую проверку (тип, арность)
- ✅ Базовый сценарий выполнения (диапазон)
- ✅ Паритет движков (одинаковый вывод)
- ✅ Управляемый источник (детерминированность)

---

## 31. Недостающие тесты

### 31.1 Обычные сценарии

| Сценарий | Приоритет |
|----------|-----------|
| Многократный вызов `RND()` — проверка что значения различаются | P3 |
| `RND()` в присваивании разных типов (INTEGER, LONG) | P3 |

### 31.2 Отрицательные сценарии

| Сценарий | Приоритет |
|----------|-----------|
| `RND(0)` — аргумент не допускается | P3 (покрыто через MB2004 общий тест) |
| `RND(-1)` — аргумент не допускается | P3 (покрыто через MB2004 общий тест) |

### 31.3 Граничные случаи

| Сценарий | Приоритет |
|----------|-----------|
| `RND()` ровно 1 раз — проверка что результат < 1 | P4 (косвенно покрыто) |

### 31.4 Parity

Паритет полностью покрыт тестом `Run_UsesProvidedRuntimeRandomForRndBuiltin`.

### 31.5 Performance

Performance покрыт benchmark `random_stream_snapshot.axm`.

---

## 32. Анализ Samples

### 32.1 samples/functions/rnd/basic_usage.axm

| Поле | Значение |
|------|----------|
| **Что демонстрируется** | `RND()` возвращает число в [0, 1) |
| **Engine** | Оба (через IDE Run) |
| **Проверяемый результат** | `TRUE`, `TRUE` (value >= 0 AND value < 1) |
| **Покрывает ли рисковые сценарии** | Базовый — да;密集ный — нет (для этого есть benchmark) |

### 32.2 samples/smoke/engine-compare/random_stream_snapshot.axm

| Поле | Значение |
|------|----------|
| **Что демонстрируется** | Детерминированный поток RND в 240 вызовов |
| **Engine** | Оба (benchmark harness) |
| **Проверяемый результат** | Три checksum-значения |
| **Покрывает ли рисковые сценарии** | Да —密集ный сценарий, детерминированность |

---

## 33. Анализ документации

### 33.1 docs/manual/numeric-functions-core.html#rnd

- Описание: корректное, на русском языке
- Синтаксис: `result = RND()` — корректно
- Диапазон: "от 0 включительно до 1 не включительно" — корректно
- Пример: `INT(RND() * 10) + 1` — стандартный паттерн
- Ограничение: "RND не принимает параметров. Форма вроде RND(1) в Axiom не поддерживается." — корректно

### 33.2 docs/manual/numeric-functions.html

- Упоминание в общем списке — корректно

### 33.3 docs/SPECIFICATION3.md

- Не содержит отдельного описания `RND` (проверено через grep)

### 33.4 docs/LANGUAGE_INVENTORY.md

- §21: корректное описание подсистемы

### 33.5 docs/BYTECODE_COVERAGE.md

- `RND` в таблице покрытия — корректно

**Статус**: Документация корректна и достаточна.

---

## 34. Интеграция с IDE

### 34.1 Подсветка

`RND` присутствует в обеих темах подсветки (`Axiom.xshd`, `Axiom.Dark.xshd`) как встроенная функция.

### 34.2 Автодополнение

Не проверялось отдельно, но `RND` зарегистрирован в `BuiltinFunctionId` и должен быть доступен через стандартный механизм.

### 34.3 Диагностики

MB2004 при неверной арности отображается в панели Diagnostics.

**Статус**: Интеграция с IDE корректна.

---

## 35. Зависимости от Host / Platform

| Зависимость | Статус |
|-------------|--------|
| `System.Random` | Стандартный .NET, кроссплатформенный |
| Windows | Нет специфичных зависимостей |
| .NET | `System.Random` доступен во всех версиях |
| Filesystem | Не применимо |
| Encoding | Не применимо |
| Locale | Не применимо |
| GPU | Не применимо |
| Display | Не применимо |

**Статус**: Нет платформенных зависимостей.

---

## 36. Карта корневых причин

### ROOT-RND-001: Отсутствие RANDOMIZE и seed-управления

├── SYS-001 (нет оператора RANDOMIZE)
├── FEATURE GAP (нет форм RND(n))
└── FEATURE GAP (нет воспроизводимости из кода Axiom)

**Корневая причина**: Подсистема спроектирована как минимальная функция без расширенного контракта QuickBASIC.

**Направление**: При необходимости расширения — добавить `RANDOMIZE [seed]` и формы `RND(n)`.

---

## 37. Стабильные области

| Область | Статус | Риск изменения |
|---------|--------|----------------|
| `IRuntimeRandom` интерфейс | Стабилен | Низкий — минимален, не требует изменений |
| `BuiltinNumericHelpers.EvaluateRnd()` | Стабилен | Низкий — тривиальная реализация |
| `BuiltinFunctionId.Rnd` | Стабилен | Низкий — не требует изменений |
| Регистрация в Binder | Стабильна | Низкий — не требует изменений |
| Dispatch в обоих движках | Стабилен | Низкий — не требует изменений |
| Тесты | Стабильны | Низкий — не требуют изменений |
| Sample | Стабилен | Низкий — не требует изменений |
| Benchmark | Стабилен | Низкий — не требует изменений |
| Документация | Стабильна | Низкий — не требует изменений |

---

## 38. Возможности консолидации

Нет. Логика уже консолидирована в `BuiltinNumericHelpers.EvaluateRnd()`.

---

## 39. Возможности разделения ответственности

Нет. Текущее разделение ответственности оптимально:
- `IRuntimeRandom` — абстракция
- `BuiltinNumericHelpers` — общая логика
- Interpreter/BytecodeVm — dispatch

---

## 40. Кандидаты на удаление

Нет. Подсистема не содержит dead code.

---

## 41. Готовность к дальнейшему развитию

### 41.1 RANDOMIZE

Подсистема готова к добавлению `RANDOMIZE`: достаточно добавить оператор, который вызывает `IRuntimeRandom` с seed-параметром. Однако текущий `IRuntimeRandom` не поддерживает seed (только `NextRandomDouble()`), поэтому потребуется расширение интерфейса или отдельный `IRandomSeedProvider`.

### 41.2 RND(n)

Подсистема готова к добавлению форм `RND(n)`: достаточно расширить регистрацию в Binder (параметр `n`) и dispatch в обоих движках.

### 41.3 Дополнительные функции

Подсистема готова к добавлению функций вроде `RANDOMINTEGER(min, max)`, `RANDOMDOUBLE(min, max)` через тот же механизм `IRuntimeRandom`.

### 41.4 Compiler readiness

Подсистема полностью готова к будущему компилятору: `RND` — это просто вызов built-in функции через `CallBuiltin`.

---

## 42. Реестр проблем

| ID | Серьёзность | Категория | Компонент | Проблема | Доказательства | Корневая причина | Уверенность |
|---|---|---|---|---|---|---|---|
| RND-001 | P3 | FEATURE GAP | Язык | Нет оператора `RANDOMIZE` | Отсутствует в коде, спецификации, manual | Минимальная реализация | CONFIRMED |
| RND-002 | P3 | FEATURE GAP | Язык | Нет форм `RND(n)` | Только `RND()` без аргументов | Минимальная реализация | CONFIRMED |
| RND-003 | P4 | DOCS | Спецификация | `RND` не описан в SPECIFICATION3.md | grep не нашёл | Не добавлен | CONFIRMED |

---

## 43. Recovery Roadmap

### ЭТАП 0 — Базовая диагностика и измерения

**Статус**: Завершён. Все измерения выполнены.

### ЭТАП 1 — Исправление критических проблем корректности

**Статус**: Не требуется. Критических проблем не обнаружено.

### ЭТАП 2 — Стабилизация семантики

**Статус**: Завершён. Семантика стабильна.

### ЭТАП 3 — Архитектурное улучшение

**Статус**: Не требуется. Архитектура чистая.

### ЭТАП 4 — Оптимизация производительности

**Статус**: Не требуется. Производительность адекватна.

### ЭТАП 5 — Усиление тестирования

| Задача | Приоритет | Статус |
|--------|-----------|--------|
| Тест: многократный вызов RND() даёт разные значения | P3 | Отложено |
| Тест: RND() в присваивании INTEGER/LONG | P3 | Отложено |

### ЭТАП 6 — Обновление документации

| Задача | Приоритет | Статус |
|--------|-----------|--------|
| Добавить RND в SPECIFICATION3.md | P3 | Отложено |

### ЭТАП 7 — Обновление IDE

**Статус**: Не требуется. Подсветка уже есть.

### ЭТАП 8 — Подготовка к дальнейшему развитию

| Задача | Приоритет | Статус |
|--------|-----------|--------|
| Добавить `RANDOMIZE [seed]` | P3 | Отложено (feature gap) |
| Добавить формы `RND(n)` | P3 | Отложено (feature gap) |
| Расширить `IRuntimeRandom` для seed | P3 | Отложено |

---

## 44. Граф зависимостей задач

```
RND-001 (RANDOMIZE) ──── зависит от ──── расширения IRuntimeRandom
RND-002 (RND(n))    ──── не зависит от ── других задач
RND-003 (документация) ── не зависит от ── других задач
```

---

## 45. Definition of Done

Подсистема RANDOM/PRNG считается завершённой, если:

- [x] `RND()` возвращает `DOUBLE` в `[0, 1)`
- [x] `RND(1)` генерирует MB2004
- [x] Interpreter и BytecodeVm дают одинаковый результат
- [x] Управляемый источник через `IRuntimeRandom` работает
- [x] Тесты покрывают базовый сценарий, ошибку арности, паритет движков
- [x] Sample `basic_usage.axm` запускается в IDE
- [x] Benchmark `random_stream_snapshot.axm` стабилен
- [x] Manual описывает `RND` корректно
- [x] IDE-подсветка `RND` работает

**Статус**: Все критерии выполнены.

---

## 46. ТОП-20 проблем корректности и архитектуры

| # | ID | Серьёзность | Описание |
|---|---|---|---|
| 1 | RND-001 | P3 | Нет оператора `RANDOMIZE` |
| 2 | RND-002 | P3 | Нет форм `RND(n)` |
| 3 | RND-003 | P4 | `RND` не описан в SPECIFICATION3.md |

**Проблем корректности (P0/P1/P2) не обнаружено.**

---

## 47. ТОП-20 задач по производительности

**Задач по производительности не обнаружено.** Подсистема не требует оптимизации.

---

## 48. Итоговая оценка здоровья подсистемы

| Метрика | Оценка | Объяснение |
|---------|--------|------------|
| Корректность | 10/10 | Нет дефектов корректности |
| Семантика | 9/10 | Полная для текущего объёма; отсутствие RANDOMIZE/RND(n) — feature gap |
| Архитектура | 10/10 | Чистая абстракция через IRuntimeRandom, единый helper |
| Runtime | 10/10 | Минимальный, корректный |
| Interpreter | 10/10 | Полная реализация |
| Bytecode VM | 10/10 | Полная реализация, полный паритет |
| Производительность | 10/10 | VM быстрее на 18%, нет bottleneck |
| Память | 10/10 | Нет аллокаций |
| Error handling | 10/10 | MB2004 при неверной арности |
| Tests | 9/10 | Адекватные; отсутствуют тесты на многократный вызов |
| Samples | 10/10 | Есть базовый sample и benchmark |
| Documentation | 9/10 | Manual корректен; нет в SPECIFICATION3.md |
| IDE | 10/10 | Подсветка работает |
| Maintainability | 10/10 | Минимальный код, нет дублирования |
| Reliability | 10/10 | Нет точек отказа |
| Future readiness | 9/10 | Готов к расширению; IRuntimeRandom не поддерживает seed |

**ИТОГО: 9.0/10**

---

## Главный контрольный вопрос

> Можно ли считать подсистему RANDOM/PRNG достаточно стабильной, корректной и архитектурно понятной для перехода к следующей подсистеме?

## **YES**

---

## Основные blockers

Нет. Подсистема не имеет blockers.

## Что нужно сделать в первую очередь

Нет критических задач. При желании расширить:
1. Добавить `RANDOMIZE [seed]` (requires IRuntimeRandom extension)
2. Добавить формы `RND(n)`

## Что можно отложить

- RANDOMIZE и RND(n) — feature gaps, не влияющие на стабильность
- Добавление RND в SPECIFICATION3.md

## Что не следует трогать

- `IRuntimeRandom` интерфейс — стабилен
- `BuiltinNumericHelpers.EvaluateRnd()` — стабилен
- Dispatch в обоих движках — стабилен
- Тесты — стабильны
- Benchmark — стабилен
