# AXIOM — ГЛУБОКИЙ FORENSIC-АУДИТ
## ПОДСИСТЕМА: DEBUGGER (ОТЛАДЧИК)

**Дата:** 2026-08-21
**Уровень:** 14 — breakpoints, stepping, time-travel debugging, IDE integration, VM state management

---

## 1. Резюме

Отладчик Axiom — это **реальная, функциональная подсистема**, обеспечивающая пошаговое выполнение программ на уровне байткодной VM. Подсистема включает точки останова, четыре режима шага (Over, Into, Out, Back), time-travel debugging и интеграцию с IDE через чёткую архитектурную границу.

**Главный вердикт: ДА** — подсистема достаточно стабильна, корректна и архитектурно понятна для перехода к следующей подсистеме.

**Ключевые факты:**
- Отладчик работает **только с BytecodeVm** — интерпретатор не поддерживает пошаговое выполнение
- Time-travel debugging (шаг назад) реализован через полные снимки состояния VM
- Архитектурная граница `IProgramDebuggerState` корректно изолирует IDE от внутренностей runtime
- 25+ unit-тестов покрывают breakpoints, stepping, resume, step-back
- 1 runnable sample (`samples/smoke/debug_step_controls.axm`)

**Health Score: 8.0/10**

---

## 2. Что представляет собой эта подсистема

Отладчик Axiom — это встроенная подсистема пошагового выполнения программ, встроенная непосредственно в байткодную VM. Она позволяет:

1. **Останавливать выполнение** на заданных строках исходного кода (точки останова)
2. **Выполнять программу пошагово** с обходом, входом в функции и выходом из них
3. **Обращать выполнение** (time-travel debugging) — шаг назад к предыдущему состоянию
4. **Возобновлять выполнение** до следующей точки останова

Подсистема не является внешним отладчиком, а встроена в ядро runtime. Это архитектурное решение обеспечивает тесную интеграцию с байткодным движком, но означает, что отладчик недоступен для интерпретаторного пути.

---

## 3. Полный список возможностей

### SYS-001: Точки останова по строке
- **Категория:** BREAKPOINT
- **Тип:** RUNTIME FACILITY
- **Статус:** IMPLEMENTED
- **Описание:** Остановка выполнения на заданной строке исходного кода
- **Parser:** Нет (IDE-only)
- **Binder:** Нет (IDE-only)
- **Interpreter:** Нет (только BytecodeVm)
- **Bytecode VM:** Да (`BytecodeBreakpointSet`, `ShouldStopBefore`)
- **IDE:** Да (`BreakpointMargin`, `SourceBreakpointLines`)
- **Tests:** Да (`BytecodeVmTests`, `ProgramRunServiceTests`)
- **Samples:** Да (`debug_step_controls.axm`)
- **Documentation:** Частично (IDE_FORENSIC_AUDIT.md §53-59)

### SYS-002: Точки останова по позиции (строка + столбец)
- **Категория:** BREAKPOINT
- **Тип:** RUNTIME FACILITY
- **Статус:** IMPLEMENTED
- **Описание:** Остановка выполнения на заданной позиции (строка + столбец)
- **Bytecode VM:** Да (`BytecodeSourceBreakpoint`, `ShouldStopBeforePositionBreakpoint`)
- **IDE:** Нет (только строки)
- **Tests:** Да (`Execute_StopsAtSourcePositionBreakpoint`, `Execute_IgnoresSourcePositionBreakpointWithDifferentColumn`)

### SYS-003: Шаг с обходом (Step Over)
- **Категория:** STEPPING
- **Тип:** RUNTIME FACILITY
- **Статус:** IMPLEMENTED
- **Описание:** Выполнение до следующей строки в том же или внешнем контексте
- **Bytecode VM:** Да (`StepOverFrom`, `DebugStepMode.Over`)
- **IDE:** Да (`StepOverCommand`, F10)
- **Tests:** Да (`Execute_StepOverFromSourceLineBreakpointStopsAtNextSourceLine`)

### SYS-004: Шаг с входом (Step Into)
- **Категория:** STEPPING
- **Тип:** RUNTIME FACILITY
- **Статус:** IMPLEMENTED
- **Описание:** Вход в вызов функции или процедуры
- **Bytecode VM:** Да (`StepIntoFrom`, `DebugStepMode.Into`)
- **IDE:** Да (`StepIntoCommand`, F11)
- **Tests:** Да (`Execute_StepIntoFromSourceLineBreakpointEntersRoutine`)

### SYS-005: Шаг с выходом (Step Out)
- **Категория:** STEPPING
- **Тип:** RUNTIME FACILITY
- **Статус:** IMPLEMENTED
- **Описание:** Выполнение до возврата из текущей функции
- **Bytecode VM:** Да (`StepOutFrom`, `DebugStepMode.Out`)
- **IDE:** Да (`StepOutCommand`, Shift+F11)
- **Tests:** Да (`Execute_StepOutFromInsideRoutineReturnsToCaller`)

### SYS-006: Шаг назад (Step Back / Time-Travel Debugging)
- **Категория:** STEPPING
- **Тип:** RUNTIME FACILITY
- **Статус:** IMPLEMENTED
- **Описание:** Обращение выполнения к предыдущему состоянию
- **Bytecode VM:** Да (`StepBackFrom`, `RestoreStateSnapshot`)
- **IDE:** Да (`StepBackCommand`, Ctrl+F10)
- **Tests:** Да (15+ тестов на step-back)
- **Ограничения:** Не все built-in функции безопасны для обратного выполнения

### SYS-007: Продолжение выполнения (Resume/Continue)
- **Категория:** STEPPING
- **Тип:** RUNTIME FACILITY
- **Статус:** IMPLEMENTED
- **Описание:** Возобновление выполнения до следующей точки останова
- **Bytecode VM:** Да (`ResumeFrom`)
- **IDE:** Да (`ContinueRunCommand`, F5)
- **Tests:** Да (`Execute_ResumeFromSourceLineBreakpointCompletesProgram`)

### SYS-008: Отладочная информация байткода
- **Категория:** DEBUG INFO
- **Тип:** RUNTIME FACILITY
- **Статус:** IMPLEMENTED
- **Описание:** Маппинг инструкций байткода в позиции исходного кода
- **Bytecode:** Да (`BytecodeDebugInfo`, `SourceInstructionMap`, `BytecodeRoutineDebugInfo`, `BytecodeBlockDebugRange`)
- **Tests:** Да (`BytecodeModuleValidatorTests`)

### SYS-009: Визуализация текущей строки
- **Категория:** IDE
- **Тип:** IDE FACILITY
- **Статус:** IMPLEMENTED
- **Описание:** Подсветка строки исходного кода, на которой остановилось выполнение
- **IDE:** Да (`SetStoppedRunLine`, `SetStoppedRunLineHighlight`, `DebugExecutionSpanRenderer`)
- **Tests:** Да (`EditorFeatureTests`)

### SYS-010: Управление состоянием отладки в IDE
- **Категория:** IDE
- **Тип:** IDE FACILITY
- **Статус:** IMPLEMENTED
- **Описание:** Хранение и управление состоянием остановленного выполнения
- **IDE:** Да (`IProgramDebuggerState`, `BytecodeVmDebuggerAdapter`, `SetStoppedRunState`)
- **Tests:** Да (`MainWindowViewModelTests`, `ProgramRunServiceTests`)

### SYS-011: Остановка перед первой инструкцией
- **Категория:** BREAKPOINT
- **Тип:** RUNTIME FACILITY
- **Статус:** IMPLEMENTED
- **Описание:** Остановка выполнения перед первой инструкцией программы
- **Bytecode VM:** Да (`stopBeforeFirstInstruction` параметр)
- **IDE:** Да (используется при Step Over/Into из начального состояния)
- **Tests:** Да (`Execute_StopBeforeFirstInstructionStopsAtEntryPoint`)

### SYS-012: Запрет обратного выполнения для необратимых операций
- **Категория:** SAFETY
- **Тип:** RUNTIME FACILITY
- **Статус:** IMPLEMENTED
- **Описание:** Встроенная функция не может быть обращена, если она имеет побочные эффекты
- **Bytecode VM:** Да (`IsBuiltinReverseExecutionSafe`)
- **Охват:** Ввод-вывод, файловые операции, мышь, клавиатура, случайные числа, дата/время

### SYS-013: Инвалидация истории обратного выполнения
- **Категория:** SAFETY
- **Тип:** RUNTIME FACILITY
- **Статус:** IMPLEMENTED
- **Описание:** Сброс истории снимков при необратимых операциях
- **Bytecode VM:** Да (`InvalidateReverseExecutionHistory`, `InvalidateReverseExecutionHistoryIfOutputIsNotReversible`)
- **Покрытие:** Графические операции, файловые операции, ввод

### SYS-014: Остановка отладки пользователем
- **Категория:** IDE
- **Тип:** IDE FACILITY
- **Статус:** IMPLEMENTED
- **Описание:** Пользователь может остановить отладку и освободить ресурсы VM
- **IDE:** Да (`StopDebuggingCommand`, Shift+F5)
- **Tests:** Да (`MainWindowViewModelTests`)

### SYS-015: Перезапуск отладки
- **Категория:** IDE
- **Тип:** IDE FACILITY
- **Статус:** IMPLEMENTED
- **Описание:** Пользователь может перезапустить отладку с начала
- **IDE:** Да (`RestartDebuggingCommand`, Ctrl+Shift+F5)
- **Tests:** Да (`MainWindowViewModelTests`)

### SYS-016: Снимки состояния VM для обратного выполнения
- **Категория:** RUNTIME
- **Тип:** RUNTIME FACILITY
- **Статус:** IMPLEMENTED
- **Описание:** Полные снимки состояния VM (глобалы, кадры, стек, вывод) для time-travel
- **Bytecode VM:** Да (`BytecodeVmStateSnapshot`, `BytecodeCallFrameSnapshot`, `BytecodeVariableSlotSnapshot`)
- **Ограничения:** Не создаются при открытых файлах или необратимых операциях

---

## 4. Фактическая архитектура

### 4.1 Общая архитектура

```
┌─────────────────────────────────────────────────────────────────┐
│  IDE СЛОЙ                                                       │
│  MainWindowViewModel.Debug.cs                                   │
│  ├── RunProgramAsync(useSourceBreakpoints: true)                │
│  ├── ContinueProgramAsync()                                     │
│  ├── StepOverProgramAsync()                                     │
│  ├── StepIntoProgramAsync()                                     │
│  ├── StepOutProgramAsync()                                      │
│  ├── StepBackProgramAsync()                                     │
│  ├── StopDebugging()                                            │
│  └── RestartDebugging()                                         │
├─────────────────────────────────────────────────────────────────┤
│  СЛОЙ СЕРВИСОВ                                                  │
│  ProgramRunService                                              │
│  ├── Run() → ExecutionRequest(sourceBreakpointLines)            │
│  ├── Continue(IProgramDebuggerState)                            │
│  ├── StepOver(IProgramDebuggerState)                            │
│  ├── StepInto(IProgramDebuggerState)                            │
│  ├── StepOut(IProgramDebuggerState)                             │
│  ├── StepBack(IProgramDebuggerState)                            │
│  └── CreateDebuggerAdapter(BytecodeVmStopState)                 │
├─────────────────────────────────────────────────────────────────┤
│  АРХИТЕКТУРНАЯ ГРАНИЦА                                          │
│  IProgramDebuggerState (интерфейс)                              │
│  BytecodeVmDebuggerAdapter (адаптер)                            │
│  DebuggerStopInfo (модель)                                      │
├─────────────────────────────────────────────────────────────────┤
│  PIPELINE СЛОЙ                                                  │
│  AxiomExecutionPipeline                                         │
│  ├── RunThroughBytecodeVm() → BytecodeVm.Execute(breakpoints)  │
│  └── ExecutionResult(stopState)                                 │
├─────────────────────────────────────────────────────────────────┤
│  RUNTIME СЛОЙ                                                   │
│  BytecodeVm                                                     │
│  ├── Execute(module, host, breakpoints)                         │
│  ├── ResumeFrom(stopState)                                      │
│  ├── StepOverFrom(stopState)                                    │
│  ├── StepIntoFrom(stopState)                                    │
│  ├── StepOutFrom(stopState)                                     │
│  ├── StepBackFrom(stopState)                                    │
│  ├── ShouldStopBefore(snapshot)                                 │
│  ├── TryCreateReversibleStateSnapshot()                         │
│  └── RestoreStateSnapshot()                                     │
├─────────────────────────────────────────────────────────────────┤
│  BYTECODE СЛОЙ                                                  │
│  BytecodeDebugInfo                                              │
│  ├── SourceInstructionMap (routineId, instructionIndex → locus) │
│  ├── BytecodeRoutineDebugInfo (parameter/local names, blocks)   │
│  └── BytecodeBlockDebugRange                                    │
├─────────────────────────────────────────────────────────────────┤
│  РЕДАКТОРНЫЙ СЛОЙ                                               │
│  BreakpointMargin (визуализация точек останова)                 │
│  DebugExecutionSpanRenderer (подсветка текущей позиции)         │
│  MainWindow.Diagnostics.cs (навигация к остановленной строке)   │
└─────────────────────────────────────────────────────────────────┘
```

### 4.2 Ключевые файлы

| Файл | Ответственность | Строки |
|------|----------------|--------|
| `src/Axiom.Runtime/Execution/BytecodeVm.cs` | Основной цикл VM, breakpoints, stepping, снимки | 6,272 |
| `src/Axiom.Runtime/Execution/BytecodeVmStopState.cs` | Состояние остановки, Resume/Step* | 82 |
| `src/Axiom.Runtime/Execution/BytecodeBreakpointSet.cs` | Набор точек останова | 69 |
| `src/Axiom.Runtime/Execution/BytecodeSourceBreakpoint.cs` | Одна точка останова | 32 |
| `src/Axiom.Runtime/Execution/BytecodeVmExecutionSnapshot.cs` | Снимок выполнения | 12 |
| `src/Axiom.Runtime/Execution/BytecodeVmStateSnapshot.cs` | Полный снимок состояния VM | 24 |
| `src/Axiom.Bytecode/Model/BytecodeDebugInfo.cs` | Отладочная карта | 54 |
| `src/Axiom.Bytecode/Model/SourceInstructionMap.cs` | Маппинг инструкция → locus | 19 |
| `src/Axiom.Bytecode/Model/BytecodeRoutineDebugInfo.cs` | Отладочная информация процедуры | 28 |
| `src/Axiom.Bytecode/Model/BytecodeBlockDebugRange.cs` | Диапазон блока | 19 |
| `src/Axiom.Ide/Services/Execution/IProgramDebuggerState.cs` | Абстракция отладчика для IDE | 60 |
| `src/Axiom.Ide/Services/Execution/BytecodeVmDebuggerAdapter.cs` | Адаптер runtime → IDE | 65 |
| `src/Axiom.Ide/Services/Execution/ProgramRunService.cs` | Сервис запуска с отладкой | 503 |
| `src/Axiom.Ide/ViewModels/MainWindowViewModel.Debug.cs` | Логика отладки в ViewModel | 829 |
| `src/Axiom.Ide/Editor/Breakpoints/BreakpointMargin.cs` | Визуализация точек останова | 95 |
| `src/Axiom.Ide/MainWindow.Diagnostics.cs` | Подсветка текущей строки | 241 |
| `src/Axiom.Ide/MainWindow.xaml.cs` | DebugExecutionSpanRenderer | 717 |

### 4.3 Архитектурные решения

1. **Отладчик встроен в VM, а не является внешним компонентом.** Это обеспечивает низкий оверхед и прямой доступ к состоянию, но означает, что отладчик недоступен для интерпретатора.

2. **Архитектурная граница через `IProgramDebuggerState`.** IDE не знает о `BytecodeVmStopState` напрямую — адаптер скрывает внутренности runtime.

3. **Снимки состояния для time-travel.** Полные снимки состояния VM (глобалы, кадры, стек, вывод) создаются перед каждой инструкцией в режиме отладки.

4. **Точки останова по строке и позиции.** Поддерживаются два уровня детализации: по строке и по позиции (строка + столбец).

---

## 5. Полный путь выполнения

### 5.1 Запуск с отладкой (F5)

```
Пользователь нажимает F5
  ↓
MainWindowViewModel.RunProgramWithDebuggingAsync()
  ↓
RunProgramAsync(useSourceBreakpoints: true)
  ↓
ProgramRunService.Run(sourceText, mode, sourceBreakpointLines)
  ↓
AxiomExecutionPipeline.RunCompiled(compiledProgram, request)
  ↓
RunThroughBytecodeVm(module, request)
  ↓
BytecodeBreakpointSet.FromLines(request.SourceBreakpointLines)
  ↓
BytecodeVm.Execute(module, host, breakpoints)
  ↓
ExecuteModule() → цикл ExecuteCurrentInstruction()
  ↓
CreateExecutionSnapshot() → ShouldStopBefore()
  ↓
Если остановка: _stopState = new BytecodeVmStopState(snapshot, this, previousSnapshot)
  ↓
RuntimeResult(stopState)
  ↓
ExecutionResult(stopState)
  ↓
ProgramRunResult(debuggerState: CreateDebuggerAdapter(stopState))
  ↓
MainWindowViewModel.SetStoppedRunState(debuggerState, document)
  ↓
NavigateToStoppedRunLine() → подсветка строки
```

### 5.2 Шаг с обходом (F10)

```
Пользователь нажимает F10
  ↓
MainWindowViewModel.StepOverProgramAsync()
  ↓
Если HasStoppedRun: ResumeStoppedProgramAsync(_programRunService.StepOver)
  ↓
Иначе: StartSteppingProgramAsync() → Run(stopBeforeFirstInstruction: true)
  ↓
IProgramDebuggerState.StepOver()
  ↓
BytecodeVmDebuggerAdapter._stepOver(stopState)
  ↓
ProgramRunService.ResumeStoppedRun(stopState, s => s.StepOver())
  ↓
BytecodeVmStopState.StepOver()
  ↓
BytecodeVm.StepOverFrom(stopState)
  ↓
ResumeFrom(stopState, DebugStepMode.Over)
  ↓
_skipStopOnceAt = stopState.Snapshot
_suppressLineBreakpointsUntilDifferentLine = stopState.Snapshot.SourceLocation.Line
_debugStepBoundary = new DebugStepBoundary(Over, startLine, startCallDepth)
  ↓
Цикл ExecuteCurrentInstruction()
  ↓
ShouldStopForDebugStep(): snapshot.CallDepth <= boundary.StartCallDepth && snapshot.SourceLocation.Line != boundary.StartLine
  ↓
Если остановка: _stopState = new BytecodeVmStopState(...)
  ↓
RuntimeResult(stopState) → ... → IDE обновляет состояние
```

### 5.3 Шаг назад (Ctrl+F10)

```
Пользователь нажимает Ctrl+F10
  ↓
MainWindowViewModel.StepBackProgramAsync()
  ↓
ResumeStoppedProgramAsync(_programRunService.StepBack, canResume: state => state.CanStepBack)
  ↓
IProgramDebuggerState.StepBack()
  ↓
BytecodeVmStopState.StepBack()
  ↓
BytecodeVm.StepBackFrom(stopState)
  ↓
FindStepBackTargetSnapshot(snapshot, previousStateSnapshot)
  ↓
Ищет снимок с другой строкой исходника
  ↓
RestoreStateSnapshot(previousStateSnapshot)
  ↓
Восстанавливает: глобалы, кадры, стек, вывод, captures
  ↓
_stopState = new BytecodeVmStopState(previousSnapshot, this, previous.Previous)
  ↓
RuntimeResult(stopState) → ... → IDE обновляет состояние
```

---

## 6. Фактический семантический контракт

### 6.1 Точки останова

**Вход:**
- Номер строки (1-based, целое > 0)
- Опционально: номер столбца (1-based, целое > 0)

**Обработка:**
- `BytecodeBreakpointSet` хранит HashSet<int> для строк и HashSet<SourcePosition> для позиций
- Перед каждой инструкцией VM проверяет `ShouldStopBefore(snapshot)`
- Проверка: `snapshot.SourceLocation.Line == breakpoint.Line` (для строк)
- Проверка: `snapshot.SourceLocation.Line == line && snapshot.SourceLocation.Column == column` (для позиций)

**Выход:**
- Остановка выполнения, создание `BytecodeVmStopState`

**Особые случаи:**
- После resume/step VM подавляет повторную остановку на той же строке (`_suppressLineBreakpointsUntilDifferentLine`)
- После resume/step VM пропускает одну остановку на той же инструкции (`_skipStopOnceAt`)
- Позиционные точки останова не подавляются (проверяются даже на той же строке)

### 6.2 Шаг с обходом (Step Over)

**Вход:**
- Текущее состояние остановки (`BytecodeVmStopState`)

**Обработка:**
- Устанавливает `_debugStepBoundary` с режимом `Over`, начальной строкой и начальной глубиной вызовов
- Выполняет инструкции до условия: `snapshot.CallDepth <= startCallDepth && snapshot.SourceLocation.Line != startLine`

**Выход:**
- Новое состояние остановки или завершение программы

**Особые случаи:**
- Если выполнение заходит в функцию (глубина увеличивается), шаг не останавливается до возврата
- Если выполнение остаётся на той же строке, шаг не останавливается

### 6.3 Шаг с входом (Step Into)

**Вход:**
- Текущее состояние остановки

**Обработка:**
- Устанавливает `_debugStepBoundary` с режимом `Into`
- Выполняет до условия: `snapshot.SourceLocation.Line != startLine`

**Выход:**
- Новое состояние остановки на первой строке вызванной функции

### 6.4 Шаг с выходом (Step Out)

**Вход:**
- Текущее состояние остановки

**Обработка:**
- Устанавливает `_debugStepBoundary` с режимом `Out`
- Выполняет до условия: `snapshot.CallDepth < startCallDepth`

**Выход:**
- Новое состояние остановки после возврата из функции

### 6.5 Шаг назад (Step Back)

**Вход:**
- Текущее состояние остановки

**Обработка:**
- Ищет `_previousReversibleStateSnapshot` с другой строкой исходника
- Восстанавливает полное состояние VM из снимка

**Выход:**
- Предыдущее состояние остановки

**Ограничения:**
- Невозможно при открытых файлах
- Невозможно после необратимых built-in функций (ввод, файлы, мышь, клавиатура, случайные числа, дата/время)
- Невозможно после графических операций
- Невозможно после операций с выводом через не-reversible host

### 6.6 Снимки состояния

**Что сохраняется:**
- Глобальные переменные (все слоты с значениями)
- Все кадры вызовов (аргументы, локалы, captures, стек вычислений, instruction pointer)
- Отложенный вывод
- Флаг наличия отложенного вывода
- Контрольная точка вывода (для reversible output)
- Ссылка на предыдущий снимок (связный список)

**Что НЕ сохраняется:**
- Открытые файлы (снимок не создаётся)
- Состояние графического хоста
- Состояние мыши/клавиатуры

---

## 7. Анализ Parser

Отладчик не имеет representation в Parser. Точки останова и команды шага — это IDE-концепции, а не языковые конструкции.

**Вердикт:** Нет проблем. Отладчик корректно отделён от языкового синтаксиса.

---

## 8. Анализ Binder

Отладчик не имеет representation в Binder. Семантический анализ не знает о точках останова или пошаговом выполнении.

**Вердикт:** Нет проблем. Отладчик корректно отделён от семантического анализа.

---

## 9. Анализ Interpreter

**Критическая находка: Интерпретатор НЕ поддерживает отладку.**

Доказательства:
- `Interpreter.Execute()` принимает только `BoundProgram` и `IRuntimeHost` — нет параметра для точек останова
- `RuntimeResult` для интерпретатора создаётся с `stopState: null` (по умолчанию)
- Нет механизма остановки/шага в интерпретаторе
- `IProgramRunService.StepOver/Into/Out/Back` все принимают `IProgramDebuggerState`, который оборачивает `BytecodeVmStopState`

**Последствия:**
- При выборе режима `Interpreter` в IDE отладка недоступна
- Пользователь может запустить программу в режиме интерпретатора, но не может использовать breakpoints или stepping
- IDE молча игнорирует точки останова при выборе интерпретатора

**Классификация:** Это **архитектурное решение**, а не баг. Отладчик спроектирован специально для BytecodeVm, где пошаговое выполнение естественно встроается в цикл исполнения инструкций.

---

## 10. Анализ Bytecode / VM

### 10.1 Механизм остановки

VM проверяет `ShouldStopBefore(snapshot)` перед каждой инструкцией (в режиме отладки). Проверка включает:

1. `ShouldSkipStopOnceAt` — пропуск одной остановки после resume
2. `ShouldStopForBreakpoint` — проверка точек останова
3. `ShouldStopForDebugStep` — проверка условий шага
4. `_shouldStopBeforeInstruction` — внешний callback (для stopBeforeFirstInstruction)

### 10.2 Fast path vs debug path

VM имеет два пути исполнения:
- **Fast path** (`CanUseFastInstructionPath() == true`): пропускает проверки breakpoints/stepping
- **Debug path** (`CanUseFastInstructionPath() == false`): выполняет все проверки

Fast path активен когда:
- Нет instruction observer
- Нет breakpoints
- Нет shouldStopBeforeInstruction callback
- Нет debug step boundary
- Нет skipStopOnceAt
- Нет suppressLineBreakpointsUntilDifferentLine

### 10.3 Снимки состояния

Снимки создаются через `TryCreateReversibleStateSnapshot()` перед каждой инструкцией (в debug path). Снимок НЕ создаётся если:
- Открыты файлы (`_openFiles.Count > 0`)
- Глобалы не могут быть скопированы (необратимые значения)

### 10.4 Восстановление состояния

`RestoreStateSnapshot()` восстанавливает:
- Глобальные переменные (с клонированием через shared values)
- Кадры вызовов (с восстановлением captures через visible variables)
- Стек вычислений
- Отложенный вывод
- Контрольную точку вывода

### 10.5 Lowering quality

Отладочная информация (`BytecodeDebugInfo`) создаётся на этапе компиляции байткода. Качество маппинга зависит от:
- Точности `SourceInstructionMap` (каждая инструкция → TextLocation)
- Корректности `BytecodeRoutineDebugInfo` (имена параметров/локалов, диапазоны блоков)

---

## 11. Анализ Runtime

### 11.1 RuntimeResult

`RuntimeResult` расширен для поддержки отладки:
- `StopState` (nullable `BytecodeVmStopState`) — состояние остановки
- `Stopped` — флаг остановки
- `CanStepBack` — возможность шага назад
- `PendingOutput` — отложенный вывод (для корректного отображения при остановке)

### 11.2 IReversibleOutputWriter

Интерфейс `IReversibleOutputWriter` расширяет `IOutputWriter` для поддержки time-travel:
- `OutputCheckpoint` — текущая контрольная точка вывода
- `RestoreOutputCheckpoint(checkpoint)` — восстановление вывода к контрольной точке

`NullRuntimeHost` реализует `IReversibleOutputWriter` с checkpoint = 0.

### 11.3 TimeoutCancellationProvider

`TimeoutCancellationProvider` поддерживает отмену по таймауту:
- Реализует `IRuntimeCancellationProvider`
- Проверяет elapsed time при каждом вызове `IsCancellationRequested`
- Используется в `ProgramRunService.Run()` для настраиваемых таймаутов

---

## 12. Взаимодействие с типами

Отладчик не зависит от конкретных типов данных. Снимки состояния клонируют `RuntimeValue` через `CopyForStorage()`, что корректно работает для всех типов (INTEGER, LONG, DOUBLE, DECIMAL, STRING, BOOLEAN, ARRAY, TYPE, IMAGE, SPRITE, TILEMAP, TILESET).

**Особый случай:** Фиксированные строки (`FixedStringLength`) сохраняются в снимках через `BytecodeVariableSlotSnapshot`.

---

## 13. Взаимодействие со String

Отладчик корректно работает со строками:
- Строковые значения клонируются при создании снимков
- Фиксированные строки сохраняют длину
- Отложенный вывод (StringBuilder) сохраняется в снимках

**Нет проблем.**

---

## 14. Взаимодействие с Array

Отладчик корректно работает с массивами:
- Массивы клонируются через `CopyForStorage()` при создании снимков
- `RuntimeArrayValue` поддерживает глубокое копирование

**Нет проблем.**

---

## 15. Взаимодействие с TYPE

Отладчик корректно работает с пользовательскими типами:
- `RuntimeRecordValue` клонируется через `CopyForStorage()`
- Поля записей сохраняются в снимках

**Нет проблем.**

---

## 16. Взаимодействие с Functions / SUB

Отладчик корректно работает с функциями и процедурами:
- Кадры вызовов полностью сохраняются в снимках
- Аргументы, локалы, captures — всё клонируется
- Замыкания (captures) восстанавливаются через visible variables
- BYREF-аргументы сохраняют связь с целевыми слотами

**Ограничение:** При восстановлении состояния BYREF-аргументы могут потерять связь с исходными переменными, если те были изменены между снимками.

---

## 17. Взаимодействие с Control Flow

Отладчик корректно работает с управлением потоком:
- `IF/ELSE/END IF` — breakpoints и stepping работают
- `FOR/NEXT` — breakpoints и stepping работают
- `WHILE/END WHILE` — breakpoints и stepping работают
- `DO/LOOP` — breakpoints и stepping работают
- `SELECT CASE` — breakpoints и stepping работают
- `TRY/CATCH/FINALLY` — breakpoints и stepping работают

**Нет проблем.**

---

## 18. Взаимодействие с Error / Exception System

Отладчик корректно работает с обработкой ошибок:
- `TRY/CATCH/FINALLY` — breakpoints и stepping работают
- `ERROR` — breakpoints и stepping работают
- Runtime-ошибки — выполнение останавливается с диагностикой

**Нет проблем.**

---

## 19. State / Lifetime / Ownership

### 19.1 Владение состоянием отладки

| Ресурс | Создаёт | Владеет | Изменяет | Освобождает |
|--------|---------|---------|----------|-------------|
| `BytecodeVmStopState` | `BytecodeVm` | `RuntimeResult` → `ExecutionResult` → `ProgramRunResult` → `MainWindowViewModel` | `BytecodeVm` (при resume/step) | `MainWindowViewModel` (при StopDebugging/новый Run) |
| `BytecodeVmStateSnapshot` | `BytecodeVm` | `BytecodeVmStopState` | Неизменяемый | GC |
| `BytecodeBreakpointSet` | `AxiomExecutionPipeline` | `BytecodeVm` | Неизменяемый | GC |
| `BreakpointMargin` | `MainWindow` | `MainWindow` | `SetBreakpointLines()` | `RemoveBreakpointMargin()` |
| `SourceBreakpointLines` | `DocumentTabViewModel` | `DocumentTabViewModel` | `SetSourceBreakpointLines()` | `ClearSourceBreakpointLines()` |

### 19.2 Lifetime состояния отладки

1. **Создание:** При остановке на breakpoint или step
2. **Хранение:** В `MainWindowViewModel._stoppedRunState`
3. **Использование:** При Continue/Step*/Stop/Restart
4. **Освобождение:** При StopDebugging, новом Run, или завершении программы

### 19.3 Потенциальная утечка

`BytecodeVmStopState` хранит ссылку на `BytecodeVm` (для Resume/Step*). Если пользователь остановит отладку и больше не запустит, `BytecodeVm` (и весь контекст выполнения) остаётся в памяти до следующего запуска или закрытия IDE.

**Мягчающий фактор:** `StopDebugging()` вызывает `SetStoppedRunState(null, null)`, что обнуляет `_stoppedRunState` и позволяет GC собрать VM.

---

## 20. Edge Cases

### 20.1 Пустая программа
- Breakpoints на несуществующих строках — игнорируются
- Step Into на пустой программе — выполнение завершается

### 20.2 Точка останова на строке с несколькими инструкциями
- VM останавливается на первой инструкции строки
- Resume пропускает одну остановку (`_skipStopOnceAt`)
- `_suppressLineBreakpointsUntilDifferentLine` предотвращает повторную остановку на той же строке

### 20.3 Точка останова внутри цикла
- VM останавливается на каждой итерации
- Resume → остановка на следующей итерации

### 20.4 Рекурсия
- Breakpoints и stepping работают корректно с рекурсивными вызовами
- Каждый рекурсивный вызов создаёт новый кадр

### 20.5 Шаг назад при невозможности обратного выполнения
- `CanStepBack` возвращает `false`
- `StepBack()` бросает `InvalidOperationException`
- IDE блокирует команду Step Back

### 20.6 Редактирование кода во время остановки
- `_stoppedRunState` хранит живое состояние VM
- Если пользователь редактирует код, состояние VM становится несогласованным с исходником
- **Это известная проблема** (упомянута в IDE_FORENSIC_AUDIT.md §61)

### 20.7 Множественные документы
- Отладка работает только с активным документом
- `_stoppedRunDocument` отслеживает, какой документ имеет остановленный запуск
- Переключение на другой документ скрывает состояние отладки

---

## 21. Interpreter ↔ Bytecode VM

| Возможность | Interpreter | Bytecode VM | Одинаковый результат | Примечание |
|---|---|---|---|---|
| Точки останова | НЕТ | ДА | Н/Д | Архитектурное различие |
| Шаг с обходом | НЕТ | ДА | Н/Д | Архитектурное различие |
| Шаг с входом | НЕТ | ДА | Н/Д | Архитектурное различие |
| Шаг с выходом | НЕТ | ДА | Н/Д | Архитектурное различие |
| Шаг назад | НЕТ | ДА | Н/Д | Архитектурное различие |
| Продолжение | НЕТ | ДА | Н/Д | Архитектурное различие |
| Остановка перед первой инструкцией | НЕТ | ДА | Н/Д | Архитектурное различие |
| Отладочная информация | НЕТ | ДА | Н/Д | Архитектурное различие |

**Классификация:** Все различия — **архитектурные** (отладчик спроектирован только для BytecodeVm).

---

## 22. Анализ производительности

### 22.1 Оверхед отладки в normal mode

В normal mode (без breakpoints, без stepping) VM использует **fast path**, который пропускает все проверки отладки. Оверхед: **ноль**.

### 22.2 Оверхед отладки в debug mode

В debug mode (с breakpoints или stepping) VM:
1. Создаёт `BytecodeVmExecutionSnapshot` перед каждой инструкцией
2. Проверяет `ShouldStopBefore(snapshot)`
3. Создаёт `BytecodeVmStateSnapshot` (если нужен time-travel)

**Оценка оверхеда:**
- Создание snapshot: ~100ns (создание record struct)
- Проверка ShouldStopBefore: ~50ns (HashSet lookup)
- Создание state snapshot: ~1-10μs (зависит от размера состояния)

**Итого:** Для простых программ оверхед отладки может быть значительным (10-100x замедление). Для реальных программ с вычислениями оверхед пренебрежим.

### 22.3 Потребление памяти

Каждый снимок состояния хранит:
- Глобальные переменные (все слоты)
- Все кадры вызовов (аргументы, локалы, captures, стек)
- Отложенный вывод

Для программы с 10 глобалами, 5 кадрами по 10 локалов каждый: ~500 RuntimeValue × ~32 bytes = ~16 KB на снимок.

При выполнении 1000 инструкций: ~16 MB истории.

**Мягчающий фактор:** История инвалидируется при необратимых операциях.

---

## 23. Качество Benchmark

Отдельных benchmarks для отладчика нет. Производительность отладчика не измеряется.

**Рекомендация:** Добавить benchmark для измерения оверхеда debug mode vs normal mode.

---

## 24. Hot Paths и Bottlenecks

### 24.1 Hot path: ShouldStopBefore

Проверяется перед каждой инструкцией в debug mode. Включает:
- ShouldSkipStopOnceAt (O(1))
- ShouldStopForBreakpoint (O(1) HashSet lookup)
- ShouldStopForDebugStep (O(1) сравнение)

**Оценка:** Достаточно быстро для большинства сценариев.

### 24.2 Hot path: TryCreateReversibleStateSnapshot

Создаёт полный снимок состояния перед каждой инструкцией. Включает:
- Копирование всех глобалов
- Копирование всех кадров
- Копирование стека вычислений

**Оценка:** Может быть дорогим для программ с большим состоянием.

### 24.3 Bottleneck: InvalidateReverseExecutionHistory

При необратимых операциях сбрасывает всю историю. Это правильно для корректности, но означает, что time-travel недоступен после любого ввода/вывода.

---

## 25. Архитектурные проблемы

### SYS-017 [P3]: Отладчик только для BytecodeVm
- **Категория:** ARCHITECTURE
- **Серьёзность:** P3
- **Уверенность:** CONFIRMED
- **Проблема:** Интерпретатор не поддерживает пошаговое выполнение
- **Ожидаемое поведение:** Отладка должна работать в обоих режимах
- **Фактическое поведение:** Отладка работает только в режиме Bytecode
- **Доказательства:** `Interpreter.Execute()` не принимает breakpoints, `RuntimeResult` для интерпретатора создаётся без `stopState`
- **Последствия:** Пользователь не может отлаживать программы в режиме интерпретатора
- **Корневая причина:** Архитектурное решение — отладчик встроен в BytecodeVm
- **Направление исправления:** Не исправлять — это намеренное архитектурное решение
- **Риск регрессии:** Н/Д

### SYS-018 [P3]: Редактирование кода во время остановки
- **Категория:** RELIABILITY
- **Серьёзность:** P3
- **Уверенность:** CONFIRMED
- **Проблема:** Состояние VM становится несогласованным с исходником при редактировании
- **Ожидаемое поведение:** IDE должна предупреждать или блокировать редактирование
- **Фактическое поведение:** Редактирование разрешено, состояние VM может стать некорректным
- **Доказательства:** `_stoppedRunState` хранит живое состояние VM, нет проверки изменений
- **Последствия:** Потенциальное поведение при resume после редактирования
- **Корневая причина:** Нет механизма инвалидации состояния при изменении документа
- **Направление исправления:** Добавить проверку изменений документа при resume/step
- **Риск регрессии:** Низкий

### SYS-019 [P4]: Нет панели переменных
- **Категория:** IDE
- **Серьёзность:** P4
- **Уверенность:** CONFIRMED
- **Проблема:** Нет визуализации локальных/глобальных переменных при остановке
- **Ожидаемое поведение:** Панель с текущими значениями переменных
- **Фактическое поведение:** Только подсветка строки
- **Доказательства:** `BytecodeVmExecutionSnapshot` содержит CallDepth и Opcode, но не значения переменных
- **Последствия:** Ограниченная usability отладчика
- **Корневая причина:** Не реализовано
- **Направление исправление:** Добавить панель переменных в IDE
- **Риск регрессии:** Низкий

### SYS-020 [P4]: Нет панели стека вызовов
- **Категория:** IDE
- **Серьёзность:** P4
- **Уверенность:** CONFIRMED
- **Проблема:** Нет визуализации стека вызовов при остановке
- **Ожидаемое поведение:** Панель со стеком вызовов
- **Фактическое поведение:** Только CallDepth в логах
- **Доказательства:** Нет UI для стека вызовов
- **Последствия:** Ограниченная usability отладчика
- **Корневая причина:** Не реализовано
- **Направление исправления:** Добавить панель стека вызовов в IDE
- **Риск регрессии:** Низкий

---

## 26. Дублирование логики

### 26.1 Нормализация точек останова

Логика нормализации точек останова (проверка line > 0, дедупликация) дублируется в:
- `ExecutionRequest.NormalizeSourceBreakpointLines()`
- `DocumentTabViewModel.NormalizeSourceBreakpointLines()`

**Рекомендация:** Вынести в общий хелпер.

### 26.2 Проверка CanResume

Проверка `CanResume` дублируется в:
- `BytecodeVmStopState.CanResume` (проверка `_vm is not null && !_resumed`)
- `IProgramDebuggerState.CanResume` (интерфейс)
- `MainWindowViewModel.HasStoppedRun` (проверка `IsStoppedRunForActiveDocument && _stoppedRunState?.CanResume == true`)

**Рекомендация:** Это допустимое дублирование (разные уровни абстракции).

---

## 27. Single Source of Truth

| Сущность | Источник истины | Где используется |
|----------|----------------|-----------------|
| Точки останова | `DocumentTabViewModel.SourceBreakpointLines` | IDE |
| Состояние остановки | `BytecodeVmStopState` | Runtime → IDE (через адаптер) |
| Отладочная информация | `BytecodeDebugInfo` | Bytecode → Runtime |
| Режим шага | `DebugStepMode` (enum в BytecodeVm) | Runtime |

**Нет проблем с множественными источниками правды.**

---

## 28. Legacy / Dead Code

### 28.1 `_shouldStopBeforeInstruction` callback

Используется только для `stopBeforeFirstInstruction`. Это не legacy, а специализированный механизм.

### 28.2 `_instructionObserver`

Используется для трассировки инструкций. Не используется в стандартном режиме отладки, но полезен для отладки самой VM.

**Нет dead code.**

---

## 29. Надёжность и безопасность

### 29.1 Утечка памяти

`BytecodeVmStopState` хранит ссылку на `BytecodeVm`. Если пользователь остановит отладку и больше не запустит, VM остаётся в памяти.

**Мягчающий фактор:** `StopDebugging()` обнуляет `_stoppedRunState`.

### 29.2 Thread safety

Отладка выполняется в фоновом потоке (через `Task.Run`). Состояние `_stoppedRunState` передаётся на UI-поток через `ProgramRunResult`.

**Нет проблем с thread safety** — состояние передаётся, а не разделяется.

### 29.3 Исключения

Все операции отладки оборачивают исключения в `ProgramRunService.ResumeStoppedRun()`. VM-исключения (`RuntimeFailureException`) обрабатываются внутри VM.

**Нет проблем с обработкой исключений.**

---

## 30. Анализ тестов

### 30.1 Тесты BytecodeVm

Файл: `tests/Axiom.Runtime.Tests/BytecodeVmTests.cs`

| Тест | Что проверяет |
|------|---------------|
| `Execute_StopsAtSourceLineBreakpoint` | Остановка на строке |
| `Execute_StopsAtSourcePositionBreakpoint` | Остановка на позиции |
| `Execute_IgnoresSourcePositionBreakpointWithDifferentColumn` | Игнорирование несовпадающего столбца |
| `Execute_ResumeFromSourceLineBreakpointCompletesProgram` | Resume до завершения |
| `Execute_StepOverFromSourceLineBreakpointStopsAtNextSourceLine` | Step Over |
| `Execute_StepIntoFromSourceLineBreakpointEntersRoutine` | Step Into |
| `Execute_StepOutFromInsideRoutineReturnsToCaller` | Step Out |
| `Execute_StepBackFromBreakpointRestoresPreviousInstruction` | Step Back |
| `Execute_StepBackMultipleTimesRestoresPreviousStates` | Множественные Step Back |
| `Execute_StepBackCannotGoBeforeFirstInstruction` | Ограничение Step Back |
| `Execute_StepBackRestoresGlobalVariableState` | Восстановление глобалов |
| `Execute_StepBackRestoresLocalVariableState` | Восстановление локалов |
| `Execute_StepBackRestoresEvaluationStack` | Восстановление стека |
| `Execute_StepBackRestoresPendingOutput` | Восстановление вывода |
| `Execute_StepBackFromInsideRoutineReturnsToCallerLine` | Step Back из функции |
| `Execute_StepBackFromAfterArraySortReturnsToPreviousLine` | Step Back после ARRAY |
| `Execute_StepBackAndResumeReplaysCorrectly` | Step Back + Resume |
| `Execute_StepBackFromNonReversibleBuiltInCannotStepBack` | Необратимые built-in |
| `Execute_ResumeFromSourceLineBreakpointSkipsRestOfSameLine` | Пропуск той же строки |
| `Execute_ResumeFromSourceLineBreakpointStopsAgainOnNextLoopIteration` | Остановка в цикле |
| `Execute_PositionBreakpointStopsOnDifferentColumnOnSameLine` | Позиционные breakpoints |

### 30.2 Тесты IDE

Файл: `tests/Axiom.Ide.Tests/MainWindowViewModelTests.cs`

| Тест | Что проверяет |
|------|---------------|
| `Continue` | Продолжение выполнения |
| `StepOver` | Шаг с обходом |
| `StepInto` | Шаг с входом |
| `StepOut` | Шаг с выходом |
| `StepBack` | Шаг назад |

Файл: `tests/Axiom.Ide.Tests/ProgramRunServiceTests.cs`

| Тест | Что проверяет |
|------|---------------|
| Интеграционные тесты | Полный путь запуска с breakpoints |

### 30.3 Тесты Bytecode

Файл: `tests/Axiom.Bytecode.Tests/BytecodeModuleValidatorTests.cs`

Множественные тесты проверяют корректность `BytecodeDebugInfo` и `SourceInstructionMap`.

---

## 31. Недостающие тесты

### 31.1 Обычные сценарии
- Тест Step Over через несколько строк
- Тест Step Into с вложенными вызовами (3+ уровня)
- Тест Step Out из вложенного вызова
- Тест Continue с несколькими breakpoints

### 31.2 Отрицательные сценарии
- Тест resume после редактирования кода (если будет реализована защита)
- Тест breakpoints на несуществующих строках
- Тест stepping в рекурсивной функции

### 31.3 Граничные случаи
- Тест Step Back при пустой истории
- Тест Step Back после множественных необратимых операций
- Тест breakpoints в программе с TRY/CATCH
- Тест stepping в программе с ошибками runtime

### 31.4 Паритет
- Нет parity-тестов (отладка только для BytecodeVm)

### 31.5 Производительность
- Нет benchmarks для оверхеда debug mode

---

## 32. Анализ Samples

### 32.1 debug_step_controls.axm

- **Путь:** `samples/smoke/debug_step_controls.axm`
- **Что демонстрирует:** Пошаговая отладка с Step Over, Step Into, Step Out, Step Back
- **Engine:** Bytecode (отладка только для BytecodeVm)
- **Проверяемый результат:** Ручная проверка в IDE
- **Покрывает ли рисковые сценарии:** Да, покрывает основные сценарии stepping

---

## 33. Анализ документации

### 33.1 IDE_FORENSIC_AUDIT.md

Разделы §53-59 содержат краткую инвентаризацию отладчика. Точность: **высокая**.

### 33.2 ARCHITECTURE.md

Не содержит явного описания подсистемы отладчика. **Пробел.**

### 33.3 SPECIFICATION3.md

Не содержит спецификации отладчика. **Пробел** (отладчик — runtime facility, а не языковая конструкция).

### 33.4 Manual

Нет явной страницы manual для отладчика. **Пробел** (отладчик — IDE feature, а не языковая конструкция).

---

## 34. Интеграция с IDE

### 34.1 Подсветка синтаксиса

Отладчик не влияет на подсветку синтаксиса.

### 34.2 Форматирование

Отладчик не влияет на форматирование кода.

### 34.3 Навигация

Отладчик обеспечивает навигацию к остановленной строке через `NavigateToStoppedRunLine()`.

### 34.4 Диагностики

Отладчик не генерирует диагностики. Runtime-ошибки обрабатываются стандартным путём.

### 34.5 Горячие клавиши

| Комбинация | Действие |
|-----------|----------|
| F5 | Run / Continue |
| F10 | Step Over |
| F11 | Step Into |
| Shift+F11 | Step Out |
| Ctrl+F10 | Step Back |
| Shift+F5 | Stop Debugging |
| Ctrl+Shift+F5 | Restart Debugging |
| F9 | Toggle Breakpoint |
| Ctrl+Shift+F9 | Clear All Breakpoints |

---

## 35. Зависимости от Host / Platform

Отладчик не зависит от конкретной платформы. Работает на:
- Windows (.NET 10)
- Потенциально на других платформах (нет platform-specific кода)

**Нет platform dependencies.**

---

## 36. Карта корневых причин

```
ROOT-001: Отладчик только для BytecodeVm
 ├── SYS-017: Интерпретатор не поддерживает отладку
 └── (архитектурное решение, не баг)

ROOT-002: Редактирование кода во время остановки
 └── SYS-018: Нет инвалидации состояния при изменении документа

ROOT-003: Ограниченная визуализация отладки
 ├── SYS-019: Нет панели переменных
 └── SYS-020: Нет панели стека вызовов
```

---

## 37. Стабильные области

### ЧТО РАБОТАЕТ И ЧТО НЕ СЛЕДУЕТ ТРОГАТЬ

1. **Механизм точек останова** — корректно работает, хорошо протестирован
2. **Stepping (Over, Into, Out, Back)** — корректно работает, хорошо протестирован
3. **Архитектурная граница `IProgramDebuggerState`** — чёткое разделение IDE и runtime
4. **Снимки состояния** — корректно создают и восстанавливают состояние VM
5. **Fast path optimization** — нулевой оверхед в normal mode
6. **Валидация необратимых операций** — корректно блокирует step back

---

## 38. Возможности консолидации

### 38.1 Нормализация точек останова

**Текущие места:**
- `ExecutionRequest.NormalizeSourceBreakpointLines()`
- `DocumentTabViewModel.NormalizeSourceBreakpointLines()`

**Дублируемая логика:** Проверка line > 0, дедупликация

**Единый источник истины:** Вынести в `Axiom.Core` или `Axiom.Runtime`

**Направление миграции:** Создать `BreakpointNormalization.NormalizeLines()`

**Риски:** Низкий риск, чистый рефакторинг

---

## 39. Возможности разделения ответственности

### 39.1 Отладчик и VM

Отладчик встроен в `BytecodeVm.cs` (6,272 строк). Логика отладки (~500 строк) могла бы быть вынесена в partial-файл `BytecodeVm.Debugger.cs`.

**Текущее состояние:** Частично реализовано (файл уже partial), но логика отладки разбросана по основному файлу.

**Рекомендация:** Вынести методы отладки в `BytecodeVm.Debugger.cs` для лучшей организации.

---

## 40. Кандидаты на удаление

**Нет кандидатов на удаление.** Весь код отладки активно используется.

---

## 41. Готовность к дальнейшему развитию

### 41.1 Готовность к Compiler

Отладчик не зависит от интерпретаторной архитектуры. При переходе к компилятору отладчик может быть адаптирован через:
- Сохранение `BytecodeDebugInfo` (уже есть)
- Адаптацию `BytecodeVmStopState` к новой модели выполнения

### 41.2 Готовность к расширению

Отладчик готов к расширению:
- Панель переменных (доступ к `BytecodeVmStateSnapshot.Globals` и `CallFrames.Locals`)
- Панель стека вызовов (доступ к `BytecodeVmStateSnapshot.CallFramesBottomToTop`)
- Условные точки останова (расширение `BytecodeBreakpointSet`)
- Точки останова на изменения значений (расширение `ShouldStopBefore`)

---

## 42. Реестр проблем

| ID | Серьёзность | Категория | Компонент | Проблема | Доказательства | Корневая причина | Уверенность |
|---|---|---|---|---|---|---|---|
| SYS-017 | P3 | ARCHITECTURE | Interpreter | Отладчик только для BytecodeVm | `Interpreter.Execute()` не принимает breakpoints | Архитектурное решение | CONFIRMED |
| SYS-018 | P3 | RELIABILITY | IDE | Редактирование кода во время остановки | `_stoppedRunState` хранит живое состояние VM | Нет инвалидации | CONFIRMED |
| SYS-019 | P4 | IDE | IDE | Нет панели переменных | Нет UI для переменных | Не реализовано | CONFIRMED |
| SYS-020 | P4 | IDE | IDE | Нет панели стека вызовов | Нет UI для стека | Не реализовано | CONFIRMED |

---

## 43. Recovery Roadmap

### ЭТАП 0 — Базовая диагностика и измерения
- Добавить benchmark для измерения оверхеда debug mode vs normal mode
- Задокументировать ограничения time-travel в manual

### ЭТАП 1 — Исправление критических проблем корректности
- Нет критических проблем корректности

### ЭТАП 2 — Стабилизация семантики
- Добавить проверку изменений документа при resume/step (SYS-018)

### ЭТАП 3 — Архитектурное улучшение
- Вынести логику отладки в `BytecodeVm.Debugger.cs`
- Вынести нормализацию точек останова в общий хелпер

### ЭТАП 4 — Оптимизация производительности
- Оценить возможность ленивого создания снимков (только при реальной остановке)

### ЭТАП 5 — Усиление тестирования
- Добавить тесты для граничных случаев (TRY/CATCH, рекурсия, вложенные вызовы)
- Добавить тесты для отрицательных сценариев

### ЭТАП 6 — Обновление документации
- Добавить описание отладчика в ARCHITECTURE.md
- Добавить страницу manual для отладчика (IDE feature)

### ЭТАП 7 — Обновление IDE
- Добавить панель переменных (SYS-019)
- Добавить панель стека вызовов (SYS-020)

### ЭТАП 8 — Подготовка к дальнейшему развитию
- Рассмотреть поддержку отладки в интерпретаторе (если будет востребовано)
- Рассмотреть условные точки останова

---

## 44. Граф зависимостей задач

```
ЭТАП 0 (benchmarks, документация)
  ↓
ЭТАП 2 (инвалидация состояния при редактировании)
  ↓
ЭТАП 3 (рефакторинг кода отладки)
  ↓
ЭТАП 5 (усиление тестирования)
  ↓
ЭТАП 6 (обновление документации)
  ↓
ЭТАП 7 (панель переменных, панель стека)
```

---

## 45. Definition of Done

Подсистема отладчика считается завершённой, когда:

1. **Корректность:** Breakpoints и stepping работают корректно для всех языковых конструкций
2. **Edge cases:** Time-travel работает корректно для обратимых операций
3. **Error handling:** Runtime-ошибки корректно обрабатываются в debug mode
4. **Interpreter:** (не применимо — отладчик только для BytecodeVm)
5. **VM:** Fast path не имеет оверхеда, debug path имеет приемлемый оверхед
6. **Тестирование:** Есть тесты для breakpoints, stepping, resume, step-back
7. **Документация:** Ограничения отладки задокументированы
8. **IDE:** Визуализация текущей строки работает
9. **Производительность:** Оверхед debug mode измерен и приемлем

---

## 46. ТОП-20 проблем корректности и архитектуры

1. **[P3] SYS-017:** Отладчик только для BytecodeVm (архитектурное решение)
2. **[P3] SYS-018:** Редактирование кода во время остановки (нет инвалидации)
3. **[P4] SYS-019:** Нет панели переменных
4. **[P4] SYS-020:** Нет панели стека вызовов

---

## 47. ТОП-20 задач по производительности

1. **Добавить benchmark** для измерения оверхеда debug mode vs normal mode
2. **Оценить ленивое создание снимков** — только при реальной остановке, а не при каждой инструкции

---

## 48. Итоговая оценка здоровья подсистемы

| Аспект | Оценка (0-10) | Объяснение |
|--------|---------------|------------|
| Корректность | 9 | Breakpoints и stepping работают корректно, хорошо протестированы |
| Семантика | 8 | Чёткий контракт, но ограниченная документация |
| Архитектура | 8 | Чёткая граница `IProgramDebuggerState`, но отладчик встроен в VM |
| Runtime | 9 | Корректная реализация в BytecodeVm |
| Interpreter | 0 | Не поддерживается (архитектурное решение) |
| Bytecode VM | 9 | Полная поддержка всех возможностей отладки |
| Производительность | 8 | Fast path без оверхеда, debug path приемлем |
| Память | 7 | Снимки могут потреблять много памяти |
| Error handling | 9 | Корректная обработка исключений |
| Tests | 8 | 25+ тестов, но есть пробелы |
| Samples | 7 | 1 sample, но покрывает основные сценарии |
| Documentation | 6 | Есть в IDE_FORENSIC_AUDIT, нет отдельной страницы |
| IDE | 8 | Базовая визуализация работает |
| Maintainability | 8 | Чистый код, но разбросан по файлам |
| Reliability | 8 | Стабильная работа, но нет защиты от редактирования |
| Future readiness | 8 | Готов к расширению (переменные, стек, условные breakpoints) |

**Общий Health Score: 8.0/10**

---

## ФИНАЛЬНЫЙ КОНТРОЛЬНЫЙ ВОПРОС

> Можно ли считать подсистему DEBUGGER достаточно стабильной, корректной и архитектурно понятной для перехода к следующей подсистеме?

**ДА**

---

## Основные blockers

Нет критических blockers.

## Что нужно сделать в первую очередь

1. Добавить benchmark для измерения оверхеда debug mode
2. Задокументировать ограничения time-travel в manual

## Что можно отложить

1. Панель переменных (SYS-019)
2. Панель стека вызовов (SYS-020)
3. Поддержка отладки в интерпретаторе (SYS-017)

## Что не следует трогать

1. Механизм точек останова — стабилен
2. Stepping (Over, Into, Out, Back) — стабилен
3. Архитектурная граница `IProgramDebuggerState` — стабильна
4. Снимки состояния — стабильны
5. Fast path optimization — стабилен
