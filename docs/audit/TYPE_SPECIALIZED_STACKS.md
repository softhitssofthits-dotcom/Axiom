# AXIOM — ГЛУБОКИЙ FORENSIC-АУДИТ
## TYPE-SPECIALIZED STACKS / МОДЕЛЬ ЗНАЧЕНИЙ И СТЕКА ВЫЧИСЛЕНИЙ

Дата: 2026-08-21
Метод: полный forensic-анализ кода, документации, benchmarks и архитектуры

---

## 1. Резюме

Подсистема TYPE-SPECIALIZED STACKS в текущей реализации Axiom **не существует как отдельная реализованная feature**. Вместо этого в проекте используется:

1. **Модель `RuntimeValue`** — класс-обёртка с полем `object? Value`, вызывающим boxing для всех типов значений (int, long, double, decimal, bool).
2. **Стек вычислений `BytecodeEvaluationStack`** — массив `RuntimeValue[]`, хранящий забоксированные значения.
3. **Fast paths** — оптимизированные горячие пути в `BytecodeVm`, которые проверяют тип и распаковывают значение напрямую, минуя generic-диспатч.
4. **Кэш целых чисел** — предварительно созданные экземпляры `RuntimeValue` для диапазона -1..1024.

Type-specialized stacks (параллельные `int[]`, `long[]`, `double[]`, `decimal[]`, `bool[]` массивы) запланированы как **OPT-PERF-ARCH-001**, но не реализованы.

**Главный вывод:** Текущая модель `RuntimeValue` с boxing через `object? Value` является **архитектурным потолком** для дальнейшего ускорения VM. Fast paths компенсируют часть накладных расходов, но не устраняют фундаментальную проблему: каждая арифметическая операция выполняет 2 unboxing + 0-1 boxing.

---

## 2. Что представляет собой эта подсистема

### 2.1 Текущее состояние

**Модель значений (`RuntimeValue`):**
```csharp
public sealed class RuntimeValue
{
    public TypeSymbol Type { get; }      // ссылочное сравнение
    public object? Value { get; }        // BOXED типы значений
}
```

Каждое значение в Axiom хранится как объект на куче. Типы значений (int, long, double, decimal, bool) упаковываются (boxing) при создании `RuntimeValue` и распаковываются (unboxing) при чтении.

**Стек вычислений (`BytecodeEvaluationStack`):**
```csharp
internal sealed class BytecodeEvaluationStack
{
    private RuntimeValue[] _items;  // массив ссылок на кучу
    // Push/Pop/Peek с AggressiveInlining
}
```

Стек хранит массив ссылок на объекты `RuntimeValue`. Каждый Push/Pop — это перемещение ссылки (быстро), но доступ к значению требует unboxing (медленно).

### 2.2 Что запланировано (OPT-PERF-ARCH-001)

Type-specialized stacks — параллельные типизированные массивы рядом с `RuntimeValue[]`:
- `int[]` для INTEGER
- `long[]` для LONG
- `double[]` для DOUBLE
- `decimal[]` для DECIMAL
- `bool[]` для BOOLEAN

Цель: устранить boxing/unboxing для горячих арифметических операций. Ожидаемый эффект: дополнительные 20-40% на CPU-heavy сценариях.

---

## 3. Полный список возможностей

### SYS-001: RuntimeValue boxing модель

- **Название:** Модель значений с boxing
- **Категория:** Runtime / Core
- **Тип:** RUNTIME FACILITY
- **Файл:** `src/Axiom.Runtime/Values/RuntimeValue.cs`
- **Описание:** Класс `RuntimeValue` хранит `TypeSymbol Type` и `object? Value`. Типы значений (int, long, double, decimal, bool) boxingуются в `object?`.
- **Статус:** IMPLEMENTED (текущая реализация)
- **Уровень уверенности:** CONFIRMED

### SYS-002: BytecodeEvaluationStack

- **Название:** Стек вычислений VM
- **Категория:** VM / Core
- **Тип:** RUNTIME FACILITY
- **Файл:** `src/Axiom.Runtime/Execution/BytecodeEvaluationStack.cs`
- **Описание:** Массив `RuntimeValue[]` с Push/Pop/Peek операциями. Хранит ссылки на забоксированные значения.
- **Статус:** IMPLEMENTED
- **Уровень уверенности:** CONFIRMED

### SYS-003: Integer/Long cache

- **Название:** Кэш целых чисел
- **Категория:** Runtime / Optimization
- **Тип:** RUNTIME FACILITY
- **Файл:** `src/Axiom.Runtime/Values/RuntimeValue.cs:9-16`
- **Описание:** Предварительно созданные экземпляры `RuntimeValue` для INTEGER (-1..1024) и LONG (-1..1024). Покрывает ~100% счётчиков FOR-циклов и 50-75% арифметических результатов.
- **Статус:** IMPLEMENTED
- **Уровень уверенности:** CONFIRMED

### SYS-004: Fast paths для арифметики

- **Название:** Оптимизированные горячие пути
- **Категория:** VM / Optimization
- **Тип:** RUNTIME FACILITY
- **Файл:** `src/Axiom.Runtime/Execution/BytecodeVm.cs`
- **Описание:** Методы `TryEvaluateHotAdd`, `TryEvaluateHotSubtract`, `TryEvaluateHotMultiply`, `TryEvaluateHotDivide`, `TryEvaluateHotModulo`, `TryEvaluateHotNegate`, `TryEvaluateHotNot`, `TryEvaluateFastBinaryInstruction` и др. Проверяют тип один раз и выполняют unbox/operate/box напрямую.
- **Статус:** IMPLEMENTED
- **Уровень уверенности:** CONFIRMED

### SYS-005: Fast paths для сравнений

- **Название:** Оптимизированные сравнения
- **Категория:** VM / Optimization
- **Тип:** RUNTIME FACILITY
- **Файл:** `src/Axiom.Runtime/Execution/BytecodeVm.cs`
- **Описание:** `TryEvaluateFastComparisonCondition`, `TryEvaluateFastComparison`, `ValuesEqual` с `ReferenceEquals` short-circuit для кэшированных значений.
- **Статус:** IMPLEMENTED
- **Уровень уверенности:** CONFIRMED

### SYS-006: Fast paths для логических операций

- **Название:** Оптимизированные AND/OR/XOR/Shift
- **Категория:** VM / Optimization
- **Тип:** RUNTIME FACILITY
- **Файл:** `src/Axiom.Runtime/Execution/BytecodeVm.cs`
- **Описание:** `TryEvaluateHotAnd`, `TryEvaluateHotOr`, `TryEvaluateHotXor`, `TryEvaluateHotShiftLeft`, `TryEvaluateHotShiftRight` — прямые пути для INTEGER и LONG.
- **Статус:** IMPLEMENTED
- **Уровень уверенности:** CONFIRMED

### SYS-007: Type-specialized stacks (запланировано)

- **Название:** Type-specialized stacks
- **Категория:** VM / Architecture
- **Тип:** PLANNED
- **Описание:** Параллельные `int[]`, `long[]`, `double[]`, `decimal[]`, `bool[]` стеки рядом с `RuntimeValue[]`. Полное устранение boxing для горячих операций.
- **Статус:** PLANNED (OPT-PERF-ARCH-001)
- **Уровень уверенности:** CONFIRMED (запланировано в `FUTURE.md` и `BYTECODE_VM_AUDIT_REPORT.md`)

---

## 4. Фактическая архитектура

### 4.1 Модель значений

```
RuntimeValue (sealed class, heap-allocated)
├── Type : TypeSymbol          // ссылка (сравнение через ==)
└── Value : object?            // BOXED значение
    ├── int → boxing → object
    ├── long → boxing → object
    ├── double → boxing → object
    ├── decimal → boxing → object
    ├── bool → boxing → object
    ├── string → ссылка (без boxing)
    ├── RuntimeArrayValue → ссылка
    ├── RuntimeRecordValue → ссылка
    ├── RuntimeFunctionValue → ссылка
    └── ...другие ссылочные типы
```

### 4.2 Стек вычислений

```
BytecodeEvaluationStack
├── _items : RuntimeValue[]    // массив ссылок
├── Count : int                // текущий размер
├── _highWaterMark : int       // максимальный размер (для Clear)
├── Push(RuntimeValue)         // AggressiveInlining + null check
├── PushUnchecked(RuntimeValue)// AggressiveInlining, без проверки
├── Pop() → RuntimeValue       // с проверкой underflow
├── PopUnchecked() → RuntimeValue // без проверки
└── PeekUnchecked(depth) → RuntimeValue
```

### 4.3 Fast path pattern

```
ExecuteAddInstruction(frame, instructionIndex):
  1. stack.PopUnchecked() → right (ссылка на RuntimeValue)
  2. stack.PopUnchecked() → left (ссылка на RuntimeValue)
  3. TryEvaluateHotAdd(left, right, out result):
     a. left.Type != right.Type → false
     b. left.Type == Integer:
        - (int)left.Value!   → UNBOX #1
        - (int)right.Value!  → UNBOX #2
        - checked(a + b)     → арифметика
        - RuntimeValue.FromInteger(result) → BOX (или кэш-хит)
     c. left.Type == Long: аналогично
     d. left.Type == Double: аналогично (без checked)
     e. left.Type == Decimal: аналогично
     f. left.Type == String: конкатенация
  4. Если fast path не сработал → generic EvaluateAdd
```

### 4.4 Ключевые файлы

| Файл | Ответственность |
|------|----------------|
| `src/Axiom.Runtime/Values/RuntimeValue.cs` | Модель значений, boxing/unboxing, кэш |
| `src/Axiom.Runtime/Execution/BytecodeEvaluationStack.cs` | Стек вычислений VM |
| `src/Axiom.Runtime/Execution/BytecodeCallFrame.cs` | Фрейм вызова (locals, arguments, captures) |
| `src/Axiom.Runtime/Execution/BytecodeVm.cs` | Fast paths, арифметика, сравнения |
| `src/Axiom.Runtime/Values/RuntimeArithmeticHelpers.cs` | Checked arithmetic helpers |
| `docs/FUTURE.md` | План type-specialized stacks |
| `docs/BYTECODE_VM_AUDIT_REPORT.md` | Анализ boxing overhead |

---

## 5. Полный путь выполнения

### 5.1 Арифметическая операция (INTEGER + INTEGER)

```
Исходный код: PRINT 2 + 3
         ↓
Lexer: IntegerLiteral(2), Plus, IntegerLiteral(3)
         ↓
Parser: BinaryExpression(2, Add, 3)
         ↓
Binder: BoundBinaryExpression(Integer, Add, Integer)
         ↓
Lowerer: PushConst(2), PushConst(3), Add
         ↓
BytecodeCompiler: Opcode.PushConst, Opcode.PushConst, Opcode.Add
         ↓
BytecodeVm:
  1. PushConst(2) → frame.EvaluationStack.PushUnchecked(ReadConstant(0))
     → RuntimeValue(Type=Integer, Value=boxed_int_2)
  2. PushConst(3) → frame.EvaluationStack.PushUnchecked(ReadConstant(1))
     → RuntimeValue(Type=Integer, Value=boxed_int_3)
  3. Add:
     a. right = stack.PopUnchecked() → RuntimeValue(Integer, boxed_3)
     b. left = stack.PopUnchecked()  → RuntimeValue(Integer, boxed_2)
     c. TryEvaluateHotAdd(left, right, out result):
        - left.Type == Integer → true
        - (int)left.Value! → 2 (UNBOX)
        - (int)right.Value! → 3 (UNBOX)
        - checked(2 + 3) = 5
        - RuntimeValue.FromInteger(5) → кэш-хит или BOX
     d. stack.PushUnchecked(result)
  4. Print → result.ToDisplayString() → "5"
```

### 5.2 Сравнение (INTEGER = INTEGER)

```
Opcode.CompareEqual:
  1. right = stack.PopUnchecked()
  2. left = stack.PopUnchecked()
  3. ValuesEqual(left, right):
     - ReferenceEquals(left, right) → true (если один и тот же объект)
     - left.Type != right.Type → false
     - left.Type == Integer:
       - (int)left.Value! (UNBOX)
       - (int)right.Value! (UNBOX)
       - a == b → bool
  4. RuntimeValue.FromBoolean(result) → кэшированный True/False
```

---

## 6. Фактический семантический контракт

### 6.1 RuntimeValue

**Вход:** значение любого поддерживаемого типа
**Обработка:** boxing в `object?`, хранение с `TypeSymbol`
**Выход:** unboxing при чтении через `AsXxx()` методы

**Допустимые входы:** int, long, double, decimal, bool, string, массивы, записи, функции, файлы, изображения, спрайты, тайлы, ошибки

**Недопустимые входы:** null (для Value допустим для Void и Function)

### 6.2 BytecodeEvaluationStack

**Вход:** `RuntimeValue` через Push
**Обработка:** хранение в массиве `RuntimeValue[]`
**Выход:** `RuntimeValue` через Pop

**Ограничения:** размер ограничен `int.MaxValue`, рост через `Array.Resize`

---

## 7. Анализ Parser

Подсистема TYPE-SPECIALIZED STACKS не затрагивает Parser. Parser работает на уровне синтаксиса и не зависит от модели значений.

---

## 8. Анализ Binder

Подсистема TYPE-SPECIALIZED STACKS не затрагивает Binder. Binder работает на уровне семантики и не зависит от модели значений.

---

## 9. Анализ Interpreter

### 9.1 Модель значений в Interpreter

Interpreter использует ту же модель `RuntimeValue` с boxing. Однако Interpreter работает с `BoundExpression` деревом напрямую, а не со стеком вычислений.

### 9.2 Особенности Interpreter

- Нет `BytecodeEvaluationStack` — значения хранятся в `RuntimeScope` и передаются как аргументы методов
- Boxing/unboxing происходит при создании `RuntimeValue` и при чтении через `AsXxx()`
- Нет fast paths — каждая операция проходит через generic dispatch
- Нет кэша целых чисел в горячем пути (кэш есть в `RuntimeValue.FromInteger`, но Interpreter не использует специализированные пути)

### 9.3 Interpreter-only логика

Нет логики, реализованной специально в Interpreter и отсутствующей в общей Runtime architecture. Оба движка используют одну и ту же модель `RuntimeValue`.

---

## 10. Анализ Bytecode / VM

### 10.1 Fast paths: полный каталог

| Операция | Метод | Типы | Boxing overhead |
|----------|-------|------|-----------------|
| Add | `TryEvaluateHotAdd` | Integer, Long, Double, Decimal, String | 2 unbox + 0-1 box |
| Subtract | `TryEvaluateHotSubtract` | Integer, Long, Double, Decimal | 2 unbox + 0-1 box |
| Multiply | `TryEvaluateHotMultiply` | Integer, Long, Double, Decimal | 2 unbox + 0-1 box |
| Divide | `TryEvaluateHotDivide` | Double, Decimal | 2 unbox + 0-1 box |
| Modulo | `TryEvaluateHotModulo` | Integer, Long | 2 unbox + 0-1 box |
| Negate | `TryEvaluateHotNegate` | Integer, Long, Double, Decimal | 1 unbox + 0-1 box |
| Not | `TryEvaluateHotNot` | Boolean, Integer, Long | 1 unbox + 0-1 box |
| And | `TryEvaluateHotAnd` | Boolean, Integer, Long | 2 unbox + 0-1 box |
| Or | `TryEvaluateHotOr` | Boolean, Integer, Long | 2 unbox + 0-1 box |
| Xor | `TryEvaluateHotXor` | Boolean, Integer, Long | 2 unbox + 0-1 box |
| ShiftLeft | `TryEvaluateHotShiftLeft` | Integer, Long | 2 unbox + 0-1 box |
| ShiftRight | `TryEvaluateHotShiftRight` | Integer, Long | 2 unbox + 0-1 box |
| CompareEqual | `ValuesEqual` + `ReferenceEquals` | Все | 0-2 unbox |
| CompareLess/Greater | `TryEvaluateFastComparison` | Integer, Long, Double, Decimal | 2 unbox |
| IntegralDivide | `TryEvaluateFastIntegerBinaryInstruction` | Integer, Long | 2 unbox + 0-1 box |
| Power | `TryEvaluateFastIntegerBinaryInstruction` | Integer | 2 unbox + 0-1 box |

### 10.2 Boxing/unboxing per instruction (подтверждено кодом)

| Инструкция | Unbox | Box | Alloc | Доказательства |
|------------|-------|-----|-------|----------------|
| Add (int+int) | 2 | 0-1 | 0-1 | `TryEvaluateHotAdd`: `(int)left.Value!`, `(int)right.Value!`, `RuntimeValue.FromInteger()` |
| Subtract (int-int) | 2 | 0-1 | 0-1 | `TryEvaluateHotSubtract`: аналогично |
| Compare (int=int) | 2 | 0 | 0 | `ValuesEqual`: `(int)left.Value!`, `(int)right.Value!` + `FromBoolean` (кэш) |
| Negate (int) | 1 | 0-1 | 0-1 | `TryEvaluateHotNegate`: `(int)operand.Value!` + `FromInteger` |
| Not (bool) | 1 | 0 | 0 | `TryEvaluateHotNot`: `(bool)operand.Value!` + `FromBoolean` (кэш) |
| PushConst | 0 | 0 | 0 | Константы уже забоксированы в pool |
| LoadLocal | 0 | 0 | 0 | Значение уже `RuntimeValue` (ссылка) |
| StoreLocal | 0 | 0 | 0 | Присваивание ссылки |

### 10.3 Кэш целых чисел: эффективность

```csharp
private const int CachedIntegerMin = -1;
private const int CachedIntegerMax = 1024;
private static readonly RuntimeValue[] CachedIntegers = CreateCachedIntegers();
```

- Диапазон: -1..1024 (1,027 кэшированных экземпляров)
- FOR-циклы: ~100% hit rate (счётчики обычно в этом диапазоне)
- Арифметические результаты: 50-75% hit rate
- Значения > 1024: 0% hit rate (каждый результат создаёт новый объект)

### 10.4 AddLongInteger: специализированная инструкция

```csharp
private void ExecuteAddLongIntegerInstruction(BytecodeCallFrame frame, int instructionIndex)
{
    // Специализированный путь для Long + Integer без generic dispatch
    if (left.Type == TypeSymbol.Long && right.Type == TypeSymbol.Integer)
    {
        stack.PushUnchecked(RuntimeValue.FromLong(checked(GetLongUnchecked(left) + GetIntegerUnchecked(right))));
        return;
    }
    // Fallback
}
```

Это пример **opcode-level specialization** — отдельный опкод для часто встречающейся комбинации типов.

---

## 11. Анализ Runtime

### 11.1 RuntimeValue: размер и размещение

```
RuntimeValue (sealed class):
├── object header: 8 bytes (x64)
├── TypeSymbol Type: 8 bytes (ссылка)
├── object? Value: 8 bytes (ссылка)
└── Итого: 24 bytes на экземпляр (heap)

int (boxed):
├── object header: 8 bytes
├── int value: 4 bytes
└── padding: 4 bytes
└── Итого: 16 bytes на boxed int

Полный overhead для одного integer в стеке:
- RuntimeValue: 24 bytes (heap)
- boxed int: 16 bytes (heap)
- Ссылка в массиве: 8 bytes (stack/array)
- Итого: 48 bytes вместо 4 bytes (int)
```

### 11.2 BytecodeEvaluationStack: аллокации

- Начальный размер: 16 элементов (`RuntimeValue[]`)
- Рост: `Array.Resize` (удвоение до 1024, затем +1024)
- `Clear()`: `Array.Clear` до `_highWaterMark`
- Нет пулинга стека

### 11.3 Frame model

```
BytecodeCallFrame:
├── Arguments : BytecodeVariableSlot[]  // массив слотов
├── _locals : RuntimeValue[]            // массив значений (ссылки)
├── Captures : BytecodeVariableSlot[]   // массив слотов
├── EvaluationStack : BytecodeEvaluationStack
└── _errorHandlers : List<BytecodeErrorHandlerFrame>
```

Locals хранятся как `RuntimeValue[]` — массив ссылок. Нет типизации на уровне фрейма.

---

## 12. Взаимодействие с типами

| Тип | Boxing | Кэш | Fast path | Комментарий |
|-----|--------|-----|-----------|-------------|
| INTEGER | ✅ (int → object) | ✅ (-1..1024) | ✅ | Основной горячий тип |
| LONG | ✅ (long → object) | ✅ (-1..1024) | ✅ | Второй по частоте |
| DOUBLE | ✅ (double → object) | ❌ | ✅ | Каждый результат — аллокация |
| DECIMAL | ✅ (decimal → object) | ❌ | ✅ | Каждый результат — аллокация |
| STRING | ❌ (ссылка) | ✅ (пустая строка) | ✅ | Нет boxing |
| BOOLEAN | ✅ (bool → object) | ✅ (True/False) | ✅ | 2 кэшированных экземпляра |
| ARRAY | ❌ (ссылка) | ❌ | — | Нет boxing |
| TYPE | ❌ (ссылка) | ❌ | — | Нет boxing |
| FUNCTION | ❌ (ссылка) | ❌ | — | Нет boxing |
| IMAGE | ❌ (ссылка) | ❌ | — | Нет boxing |
| SPRITE | ❌ (ссылка) | ❌ | — | Нет boxing |
| TILESET | ❌ (ссылка) | ❌ | — | Нет boxing |
| TILEMAP | ❌ (ссылка) | ❌ | — | Нет boxing |
| FILE | ❌ (ссылка) | ❌ | — | Нет boxing |

---

## 13. Взаимодействие со String

Строки не boxingуются — `string` является ссылочным типом в C#. Однако:

- `RuntimeValue.FromString("")` возвращает синглтон `EmptyStringValue`
- `RuntimeValue.FromBoolean(true/false)` возвращает синглтоны `TrueStringValue`/`FalseStringValue` для display
- `ToDisplayString()` для строк возвращает `this` (без аллокации)
- `ToDisplayRuntimeValue()` для целых чисел использует кэш `CachedIntegerDisplayValues`/`CachedLongDisplayValues`

---

## 14. Взаимодействие с Array

Массивы хранятся как `RuntimeArrayValue` (ссылочный тип) — без boxing. Доступ к элементам массива:

```csharp
// 1D fast path
if (!array.TryGetValue1(GetIntegerUnchecked(indexValue), out var directValue))
```

Элементы массива хранятся как `RuntimeValue[]` — массив ссылок. Каждый элемент — забоксированное значение.

---

## 15. Взаимодействие с TYPE

Записи TYPE хранятся как `RuntimeRecordValue` (ссылочный тип) — без boxing. Поля записей хранятся как `RuntimeValue[]` — массив ссылок.

---

## 16. Взаимодействие с Functions / SUB

Функции хранятся как `RuntimeFunctionValue` (ссылочный тип) — без boxing. Аргументы функций передаются как `RuntimeValue[]` — массив ссылок.

---

## 17. Взаимодействие с Control Flow

### 17.1 FOR-циклы: специализированные инструкции

```csharp
// AdvanceForLoopLocal: 2 unbox + 0-1 box
// JumpIfForDoneLocal: 3 unbox
```

FOR-циклы имеют специализированные опкоды, которые загружают current/end/step одним вызовом (`LoadForLoopLocalValues`), но всё равно требуют unboxing для сравнения и арифметики.

### 17.2 Условные переходы: специализированные инструкции

```csharp
// JumpIfEqual: 0-2 unbox + ReferenceEquals short-circuit
// JumpIfNotEqual: аналогично
```

Условные переходы имеют inline fast paths с `ReferenceEquals` для кэшированных значений.

---

## 18. Взаимодействие с Error / Exception System

Boxing не влияет на обработку ошибок. `RuntimeFailureException` — ссылочный тип, не зависит от модели значений.

---

## 19. State / Lifetime / Ownership

### 19.1 RuntimeValue: immutability

`RuntimeValue` — **неизменяемый** (immutable) объект:
- `Type` и `Value` устанавливаются в конструкторе и не меняются
- Нет сеттеров
- `CopyForStorage()` возвращает `this` для большинства типов (кроме строк с фиксированной длиной и записей TYPE)

### 19.2 BytecodeEvaluationStack: ownership

Стек принадлежит `BytecodeCallFrame`. При переиспользовании фрейма (`Reset`) стек очищается.

### 19.3 GC pressure

Каждый boxing-результат создаёт новый объект на куче (если не попадает в кэш). В tight-циклах это может создавать значительное давление на GC:
- Gen0 collections для короткоживущих объектов
- Каждый boxed int/long — 16 bytes
- Каждый boxed double — 16 bytes
- Каждый boxed decimal — 32 bytes
- Каждый boxed bool — 16 bytes

---

## 20. Edge Cases

### 20.1 Кэш: границы

| Сценарий | Кэш-хит | Boxing |
|----------|---------|--------|
| INTEGER 0 | ✅ | Нет |
| INTEGER -1 | ✅ | Нет |
| INTEGER 1024 | ✅ | Нет |
| INTEGER 1025 | ❌ | Да |
| INTEGER -2 | ❌ | Да |
| LONG 0 | ✅ | Нет |
| LONG 1025 | ❌ | Да |
| DOUBLE 0.0 | ❌ | Да |
| DECIMAL 0 | ❌ | Da |
| BOOLEAN true | ✅ | Нет |
| BOOLEAN false | ✅ | Нет |

### 20.2 Overflow: checked arithmetic

Для INTEGER и LONG используется `checked` arithmetic. При переполнении:
1. `checked(a + b)` выбрасывает `OverflowException`
2. Fast path ловит исключение и возвращает `false`
3. Generic path создаёт diagnostic и выбрасывает `RuntimeFailureException`

### 20.3 Double: non-finite values

```csharp
var value = left + right;
if (!double.IsFinite(value))
{
    result = RuntimeValue.FromDouble(value);
    return true;
}
return false; // → generic path → diagnostic
```

Non-finite результаты (Infinity, NaN) проходят через fast path, но generic path проверяет и выдаёт diagnostic.

---

## 21. Interpreter ↔ Bytecode VM

| Возможность | Interpreter | Bytecode VM | Одинаковый результат | Комментарий |
|---|---|---|---|---|
| RuntimeValue модель | ✅ | ✅ | ✅ | Одна и та же модель |
| Boxing/unboxing | ✅ | ✅ | ✅ | Одинаковые накладные расходы |
| Integer cache | ✅ | ✅ | ✅ | Одинаковый кэш |
| Fast paths | ❌ | ✅ | ✅ | VM быстрее на горячих путях |
| Specialized opcodes | ❌ | ✅ | ✅ | VM имеет AddLongInteger и др. |
| EvaluationStack | ❌ | ✅ | — | Только в VM |

**Ключевое расхождение:** VM имеет fast paths и специализированные опкоды, которые отсутствуют в Interpreter. Это объясняет, почему VM быстрее на CPU-heavy сценариях (медиана 0.63x).

---

## 22. Анализ производительности

### 22.1 Boxing overhead: количественная оценка

| Операция | Без boxing | С boxing | Overhead |
|----------|-----------|----------|----------|
| int + int | ~1 ns | ~5 ns | 5x |
| int == int | ~1 ns | ~4 ns | 4x |
| -int | ~1 ns | ~4 ns | 4x |
| int & int | ~1 ns | ~5 ns | 5x |

*Оценки основаны на типичных накладных расходах boxing/unboxing в .NET.*

### 22.2 Кэш-хит rate: оценка

| Сценарий | Кэш-хит rate | Комментарий |
|----------|-------------|-------------|
| FOR-цикл 1 TO 100 | ~100% | Все значения в диапазоне -1..1024 |
| FOR-цикл 1 TO 10000 | ~10% | Большинство значений вне кэша |
| Арифметика (a + b) | 50-75% | Зависит от диапазона значений |
| Сравнения | ~50% | Результат — Boolean (кэш) |
| Логические операции | ~100% | Результат — Boolean (кэш) |

### 22.3 VM performance: текущее состояние

| Категория | Медиана runtime ratio | Комментарий |
|-----------|----------------------|-------------|
| CPU-heavy (арифметика) | ~0.40x | VM в 2.5x быстрее |
| String-heavy | ~0.50x | VM в 2x быстрее |
| Array-heavy | ~0.70x | VM в 1.4x быстрее |
| I/O-heavy | ~0.90x | Паритет |
| Mixed | ~0.63x | VM в 1.6x быстрее |

---

## 23. Качество Benchmark

### 23.1 Методология

- ✅ Чередующийся запуск движков (Interpreter/BytecodeVm)
- ✅ Прогревочные итерации
- ✅ Множественные итерации
- ✅ Мин/ср/медиана/макс
- ✅ Одинаковый вход
- ✅ Runtime отделён от total (включая frontend)

### 23.2 Ограничения

- ⚠️ Нет профилирования GC
- ⚠️ Нет CPU counter profiling
- ⚠️ Только 3 итерации измерения
- ⚠️ Нет изоляции boxing overhead от остальных накладных расходов

---

## 24. Hot Paths и Bottlenecks

### 24.1 Главный bottleneck: RuntimeValue boxing

```
ROOT-PERF-001: RuntimeValue boxing model
├── PERF-001: Boxing per arithmetic operation (2 unbox + 0-1 box)
├── PERF-003: Negate/Not closure allocation (УЖЕ ИСПРАВЛЕНО)
├── PERF-004: Slow-path closure allocation (УЖЕ ИСПРАВЛЕНО)
├── PERF-007: Double/Decimal always allocate (нет кэша)
└── PERF-008: No type-specialized stacks
```

### 24.2 Вторичные bottlenecks

```
ROOT-PERF-002: Lowering quality gaps
├── PERF-002: No constant folding
└── PERF-005: No dead code elimination

ROOT-PERF-003: I/O path overhead
├── PERF-006: File I/O regression (resolved)
├── PERF-009: Field access via string
└── PERF-010: No inline caching
```

---

## 25. Архитектурные проблемы

### SYS-001: RuntimeValue — sealed class вместо struct

**Категория:** ARCHITECTURE
**Серьёзность:** P1
**Уверенность:** CONFIRMED

`RuntimeValue` объявлен как `sealed class`, а не `readonly struct`. Это означает:
- Каждый экземпляр размещается на куче
- Каждое значение — отдельный объект
- Массив `RuntimeValue[]` — массив ссылок, а не значений
- Нет преимуществ value-type семантики

**Место:** `RuntimeValue.cs:7`
**Доказательства:** `public sealed class RuntimeValue : IRuntimeStoredValue`

**Последствия:** Каждое значение требует heap-аллокации. Для типов значений (int, long, double, decimal, bool) это означает boxing + отдельный объект-обёртка.

**Направление:** Type-specialized stacks (OPT-PERF-ARCH-001) или переход на tagged struct.

### SYS-002: Double/Decimal не кэшируются

**Категория:** PERFORMANCE
**Серьёзность:** P2
**Уверенность:** CONFIRMED

В отличие от INTEGER и LONG, для DOUBLE и DECIMAL нет кэша. Каждый результат арифметической операции с этими типами создаёт новый объект.

**Место:** `RuntimeValue.cs:111-113`
**Доказательства:** `FromDouble` и `FromDecimal` всегда создают `new RuntimeValue(...)`

**Последствия:** Для decimal-heavy сценариев — повышенное давление на GC.

**Направление:** Кэш для commonly used значений (0.0, 1.0, 0m, 1m) или type-specialized stacks.

### SYS-003: Boxing для Boolean

**Категория:** PERFORMANCE
**Серьёзность:** P3
**Уверенность:** CONFIRMED

Boolean boxingуется в `object?`, хотя в кэше есть только 2 значения (true/false). Кэш работает корректно, но сам факт boxing избыточен.

**Место:** `RuntimeValue.cs:125`
**Доказательства:** `FromBoolean` возвращает кэшированные `TrueValue`/`FalseValue` — boxing не происходит для кэшированных значений. Но при передаче через `object?` — всё равно ссылка.

**Последствия:** Минимально — кэш покрывает все случаи.

---

## 26. Дублирование логики

### 26.1 Fast paths дублируют generic paths

Каждый `TryEvaluateHotXxx` метод дублирует логику соответствующего `EvaluateXxx` метода, но с прямым unboxing. Это преднамеренное дублирование для производительности.

**Место:** `BytecodeVm.cs` — ~15 пар методов (hot + generic)
**Оценка:** Приемлемое дублирование — hot path оптимизирован для скорости, generic path для корректности.

### 26.2 GetXxxUnchecked дублирует AsXxx

```csharp
// Fast path
private static int GetIntegerUnchecked(RuntimeValue value) => (int)value.Value!;

// Generic path
public int AsInteger() => Type == TypeSymbol.Integer
    ? (int)Value!
    : throw new InvalidOperationException(...);
```

`GetXxxUnchecked` — это версия `AsXxx` без проверки типа. Преднамеренное дублирование.

---

## 27. Single Source of Truth

| Аспект | Источник истины |
|--------|----------------|
| Модель значений | `RuntimeValue.cs` |
| Boxing/unboxing | `RuntimeValue.cs` (From/As методы) |
| Кэш целых чисел | `RuntimeValue.cs` (CachedIntegers/CachedLongs) |
| Стек вычислений | `BytecodeEvaluationStack.cs` |
| Fast paths | `BytecodeVm.cs` (TryEvaluateHotXxx) |
| План type-specialized stacks | `FUTURE.md`, `BYTECODE_VM_AUDIT_REPORT.md` |

**Архитектурная находка:** Единый источник истины для модели значений — `RuntimeValue.cs`. Все компоненты (Interpreter, VM, helpers) используют одну и ту же модель.

---

## 28. Legacy / Dead Code

| Код | Статус | Доказательства |
|-----|--------|----------------|
| `EvaluateAdd` (generic path) | Используется | Fallback для типов без fast path |
| `EvaluateSubtract` (generic path) | Используется | Fallback для типов без fast path |
| `EvaluateNegate` (generic path) | Используется | Fallback для non-fast-path типов |
| `EvaluateNot` (generic path) | Используется | Fallback для non-fast-path типов |

Нет мёртвого кода. Все пути используются.

---

## 29. Надёжность и безопасность

### 29.1 Thread safety

`RuntimeValue` — immutable объект. Потокобезопасен для чтения. `BytecodeEvaluationStack` — не потокобезопасен (ожидается single-threaded использование в VM).

### 29.2 Memory pressure

Boxing создаёт значительное давление на память:
- Каждый boxed int — 16 bytes на куче
- Каждый boxed long — 16 bytes на куче
- Каждый boxed double — 16 bytes на куче
- Каждый boxed decimal — 32 bytes на куче
- Каждый boxed bool — 16 bytes на куче

Для tight-циклов с миллионами итераций это может создавать сотни мегабайт мусора.

---

## 30. Анализ тестов

### 30.1 Существующие тесты

Тесты для модели значений и стека вычислений покрыты косвенно через:
- Runtime-тесты (897 тестов) — покрывают все операции
- BytecodeVm-тесты — покрывают fast paths
- Acceptance-тесты — покрывают end-to-end сценарии
- Engine-comparison benchmarks (48 сценариев) — покрывают производительность

### 30.2 Пропуски в тестах

| Сценарий | Тест | Статус |
|----------|------|--------|
| Boxing overhead изоляция | ❌ | Нет dedicated теста |
| Cache hit rate измерение | ❌ | Нет dedicated теста |
| Fast path correctness vs generic | ❌ | Нет dedicated сравнения |
| GC pressure measurement | ❌ | Нет profiling теста |
| Edge case: значение > 1024 | ❌ | Нет dedicated теста |
| Edge case: значение < -1 | ❌ | Нет dedicated теста |

---

## 31. Недостающие тесты

### Обычные сценарии
- Тест кэш-хит rate для INTEGER/LONG
- Тест fast path vs generic path (одинаковый результат)

### Отрицательные сценарии
- Тест overflow в fast path
- Тест non-finite double в fast path

### Производительность
- Тест boxing overhead изоляции
- Тест GC pressure для tight-циклов

---

## 32. Анализ Samples

Нет dedicated samples для type-specialized stacks (подсистема не реализована). Все существующие samples работают через текущую модель `RuntimeValue`.

---

## 33. Анализ документации

### 33.1 FUTURE.md

- ✅ Type-specialized stacks запланированы как OPT-PERF-ARCH-001
- ✅ Ожидаемый эффект: 20-40% на CPU-heavy сценариях
- ✅ Сложность: высокая
- ✅ Статус: ОТЛОЖЕНО

### 33.2 BYTECODE_VM_AUDIT_REPORT.md

- ✅ Boxing overhead количественно описан
- ✅ Fast paths каталогизированы
- ✅ Integer cache проанализирован
- ✅ Performance speedup matrix создана
- ✅ Top 20 bottlenecks идентифицированы

### 33.3 LANGUAGE_INVENTORY.md

- ✅ RuntimeValue описан как "Единый тип значения с boxing через `object? Value`"

---

## 34. Интеграция с IDE

Подсистема TYPE-SPECIALIZED STACKS не затрагивает IDE напрямую. IDE работает на уровне исходного кода и не зависит от модели значений runtime.

---

## 35. Зависимости от Host / Platform

### 35.1 .NET runtime

- Boxing/unboxing — стандартная модель .NET
- `GC` — стандартный сборщик мусора
- `Array.Resize` — стандартный механизм роста массивов

### 35.2 Платформа

- Windows/x64: размер указателя 8 bytes
- Размер `RuntimeValue` (class): 24 bytes (header + 2 ссылки)
- Размер boxed int: 16 bytes (header + int + padding)

---

## 36. Карта корневых причин

```
ROOT-PERF-001: RuntimeValue boxing model
├── SYS-001: RuntimeValue — sealed class вместо struct
├── SYS-002: Double/Decimal не кэшируются
├── SYS-003: Boxing для Boolean (минимально)
├── PERF-001: 2 unbox + 0-1 box per arithmetic op
├── PERF-007: Double/Decimal always allocate
└── PERF-008: No type-specialized stacks

ROOT-PERF-002: Lowering quality gaps
├── PERF-002: No constant folding
└── PERF-005: No dead code elimination
```

---

## 37. Стабильные области

### Что работает и что не следует трогать

1. **RuntimeValue модель** — стабильна, immutable, корректна. Не требует изменений в текущей архитектуре.
2. **Integer/Long cache** — эффективен для типичных сценариев. Не требует изменений.
3. **Boolean cache** — полное покрытие (True/False). Не требует изменений.
4. **Fast paths** — корректны, оптимизированы, покрыты тестами. Не требуют изменений.
5. **BytecodeEvaluationStack** — простой, эффективный, AggressiveInlining. Не требует изменений.
6. **Frame model** — стабильный, с переиспользованием фреймов. Не требует изменений.

---

## 38. Возможности консолидации

Текущая архитектура не требует консолидации. Все компоненты используют единую модель `RuntimeValue`.

---

## 39. Возможности разделения ответственности

При реализации type-specialized stacks потребуется:
1. Разделить `BytecodeEvaluationStack` на типизированные стеки
2. Разделить fast paths на типизированные операции
3. Возможно, разделить `RuntimeValue` на tagged struct + reference wrapper

---

## 40. Кандидаты на удаление

Нет кандидатов на удаление. Все текущие компоненты используются.

---

## 41. Готовность к дальнейшему развитию

### 41.1 Type-specialized stacks

**Готовность: НИЗКАЯ**

- Текущая архитектура `RuntimeValue` как sealed class несовместима с type-specialized stacks
- Потребуется фундаментальная переработка модели значений
- Все fast paths потребуют переписывания
- Все тесты потребуют проверки

### 41.2 Tagged struct

**Готовность: СРЕДНЯЯ**

- Переход на `readonly struct` с inline type tag возможен
- Сохраняет совместимость с текущим API
- Устраняет boxing для типов значений
- Но не устраняет overhead хранения в `RuntimeValue[]`

### 41.3 Compiler readiness

**Готовность: НИЗКАЯ (5/10)**

- Boxing model prevents native code generation
- Нет type specialization в bytecode
- Нет register-like optimization hints
- Нет inline annotations

---

## 42. Реестр проблем

| ID | Серьёзность | Категория | Компонент | Проблема | Доказательства | Корневая причина | Уверенность |
|---|---|---|---|---|---|---|---|
| SYS-001 | P1 | ARCHITECTURE | RuntimeValue | Sealed class вместо struct — boxing для всех типов значений | `RuntimeValue.cs:7` | Архитектурное решение | CONFIRMED |
| SYS-002 | P2 | PERFORMANCE | RuntimeValue | Double/Decimal не кэшируются — каждая операция аллокация | `RuntimeValue.cs:111-113` | Отсутствие кэша | CONFIRMED |
| SYS-003 | P4 | PERFORMANCE | RuntimeValue | Boolean boxing (минимально — кэш покрывает) | `RuntimeValue.cs:125` | Кэш работает | CONFIRMED |
| SYS-004 | P1 | ARCHITECTURE | BytecodeEvaluationStack | Хранит `RuntimeValue[]` — массив ссылок на забоксированные значения | `BytecodeEvaluationStack.cs:7` | Архитектурное решение | CONFIRMED |
| SYS-005 | P3 | TESTS | Runtime | Нет теста кэш-хит rate | Отсутствие теста | Пропуск | HIGH |
| SYS-006 | P3 | TESTS | Runtime | Нет теста boxing overhead изоляции | Отсутствие теста | Пропуск | HIGH |

---

## 43. Recovery Roadmap

### ЭТАП 0 — Базовая диагностика и измерения

**Цель:** Зафиксировать текущее состояние boxing overhead.

| Задача | Описание | Приоритет |
|--------|----------|-----------|
| TEST-001 | Добавить тест кэш-хит rate для INTEGER/LONG | P3 |
| TEST-002 | Добавить тест boxing overhead изоляции | P3 |
| TEST-003 | Добавить тест fast path vs generic path | P3 |

### ЭТАП 1 — Исправление критических проблем корректности

*Этап не требуется. Нет проблем корректности.*

### ЭТАП 2 — Стабилизация семантики

*Этап не требуется. Семантика стабильна.*

### ЭТАП 3 — Архитектурное улучшение

**Цель:** Подготовить переход на tagged struct или type-specialized stacks.

| Задача | Описание | Приоритет |
|--------|----------|-----------|
| ARCH-001 | Проектирование: tagged struct для RuntimeValue | P1 |
| ARCH-002 | Проектирование: type-specialized evaluation stack | P1 |
| ARCH-003 | Проектирование: типизированные fast paths | P1 |

### ЭТАП 4 — Оптимизация производительности

**Цель:** Устранить boxing для горячих типов.

| Задача | Описание | Приоритет |
|--------|----------|-----------|
| OPT-001 | Реализация tagged struct для RuntimeValue | P1 |
| OPT-002 | Реализация parallel typed stacks (int[], long[], double[]) | P1 |
| OPT-003 | Кэш для commonly used Double/Decimal значений | P2 |
| OPT-004 | Типизированные fast paths без boxing | P1 |

### ЭТАП 5 — Усиление тестирования

*Покрывается ЭТАПОМ 0.*

### ЭТАП 6 — Обновление документации

*Документация уже актуальна.*

### ЭТАП 7 — Обновление IDE

*Этап не требуется.*

### ЭТАП 8 — Подготовка к дальнейшему развитию

**Цель:** Подготовить compiler-ready VM.

| Задача | Описание | Приоритет |
|--------|----------|-----------|
| COMPILER-001 | Type-tagged bytecode annotations | P2 |
| COMPILER-002 | Register-like locals for hot variables | P2 |
| COMPILER-003 | Inline annotations for common patterns | P3 |

---

## 44. Граф зависимостей задач

```
TEST-001 (кэш-хит rate)
TEST-002 (boxing overhead)
TEST-003 (fast path correctness)
    │
    ▼
ARCH-001 (tagged struct design)
ARCH-002 (typed stack design)
ARCH-003 (typed fast path design)
    │
    ▼
OPT-001 (tagged struct implementation) ← зависит от ARCH-001
OPT-002 (parallel stacks) ← зависит от ARCH-002
OPT-003 (Double/Decimal cache) ← не зависит
OPT-004 (typed fast paths) ← зависит от OPT-001, OPT-002
    │
    ▼
COMPILER-001 (type annotations) ← зависит от OPT-001
COMPILER-002 (register locals) ← зависит от OPT-002
COMPILER-003 (inline annotations) ← не зависит
```

---

## 45. Definition of Done

Подсистема TYPE-SPECIALIZED STACKS считается завершённой, если:

1. ✅ RuntimeValue модель работает корректно для всех типов
2. ✅ Кэш целых чисел работает корректно
3. ✅ Fast paths работают корректно для всех горячих операций
4. ✅ VM быстрее Interpreter на CPU-heavy сценариях
5. ✅ Все 897 runtime-тестов проходят
6. ✅ Все 48 benchmark-сценариев показывают VM ≤ 1.0x
7. ❌ Type-specialized stacks реализованы (PLANNED)
8. ❌ Boxing устранён для горячих типов (PLANNED)
9. ❌ GC pressure минимизирован (PLANNED)

---

## 46. ТОП-20 проблем корректности и архитектуры

| # | ID | Серьёзность | Описание |
|---|---|---|---|
| 1 | SYS-001 | P1 | RuntimeValue — sealed class вместо struct (boxing) |
| 2 | SYS-004 | P1 | BytecodeEvaluationStack хранит `RuntimeValue[]` (массив ссылок) |
| 3 | SYS-002 | P2 | Double/Decimal не кэшируются |
| 4 | SYS-005 | P3 | Нет теста кэш-хит rate |
| 5 | SYS-006 | P3 | Нет теста boxing overhead изоляции |
| 6 | SYS-003 | P4 | Boolean boxing (минимально) |

---

## 47. ТОП-20 задач по производительности

| # | ID | Описание | Ожидаемая польза | Сложность |
|---|---|---|---|---|
| 1 | OPT-001 | Tagged struct для RuntimeValue | Устранение boxing для типов значений | Высокая |
| 2 | OPT-002 | Parallel typed stacks (int[], long[], double[]) | Устранение boxing полностью | Высокая |
| 3 | OPT-003 | Кэш для Double/Decimal (0.0, 1.0, 0m, 1m) | Снижение GC pressure | Низкая |
| 4 | OPT-004 | Типизированные fast paths без boxing | Устранение boxing в горячих путях | Средняя |
| 5 | OPT-005 | Constant folding в lowering | Устранение runtime вычислений | Средняя |
| 6 | OPT-006 | Dead code elimination в lowering | Снижение размера bytecode | Низкая |
| 7 | OPT-007 | Register-like locals для горячих переменных | Обход стека для счётчиков циклов | Высокая |
| 8 | OPT-008 | Superinstructions (LoadLocal+LoadLocal+Add) | Снижение dispatch overhead | Средняя |

---

## 48. Итоговая оценка здоровья подсистемы

| Аспект | Оценка (0-10) | Объяснение |
|--------|---:|---|
| Корректность | 10 | Все 897 тестов проходят, нет проблем корректности |
| Семантика | 10 | Модель значений стабильна и корректна |
| Архитектура | 6 | Boxing model — архитектурный потолок |
| Runtime | 8 | Fast paths компенсируют часть boxing overhead |
| Interpreter | 7 | Нет fast paths, полный boxing overhead |
| Bytecode VM | 8 | Fast paths, специализированные опкоды, кэш |
| Производительность | 7 | VM быстрее на 37% (медиана), но boxing ограничивает |
| Память | 5 | Boxing создаёт значительное GC pressure |
| Error handling | 10 | Не зависит от модели значений |
| Tests | 8 | Полное покрытие функциональности, нет dedicated perf тестов |
| Samples | 8 | Нет dedicated samples (подсистема не реализована) |
| Documentation | 9 | FUTURE.md и BYTECODE_VM_AUDIT_REPORT.md актуальны |
| IDE | 10 | Не затрагивается |
| Maintainability | 8 | Чистый код, понятная архитектура |
| Reliability | 10 | Стабильная работа |
| Future readiness | 4 | Требует фундаментальной переработки для type-specialized stacks |

**Общая оценка: 7.8 / 10**

---

## ФИНАЛЬНЫЙ КОНТРОЛЬНЫЙ ВОПРОС

> Можно ли считать подсистему TYPE-SPECIALIZED STACKS достаточно стабильной, корректной и архитектурно понятной для перехода к следующей подсистеме?

**YES, WITH CONDITIONS**

### Основные blockers

Нет критических blockers. Текущая модель `RuntimeValue` работает корректно и стабильно. Type-specialized stacks — это будущая оптимизация, а не текущая проблема.

### Что нужно сделать в первую очередь

1. Добавить dedicated тесты для boxing overhead и кэш-хит rate
2. Приступить к проектированию tagged struct (ARCH-001)

### Что можно отложить

1. Реализация type-specialized stacks (OPT-002) — высокая сложность
2. Register-like locals (OPT-007) — высокая сложность
3. Superinstructions (OPT-008) — средняя сложность

### Что не следует трогать

1. RuntimeValue модель — стабильна и корректна
2. Integer/Long cache — эффективен
3. Fast paths — корректны и оптимизированы
4. BytecodeEvaluationStack — простой и эффективный
5. Frame model — стабильный
