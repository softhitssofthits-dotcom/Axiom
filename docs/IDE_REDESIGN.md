# IDE Redesign — Thonny-style shell for Axiom.Ide

## Status

**Active** — основной документ для всех IDE-redesign сессий.  
Любой новый агент, работающий над `Axiom.Ide`, обязан читать этот файл вместе с `AGENTS.md`, `PROJECT.md` и `docs/ARCHITECTURE.md`.

### Current implementation checkpoint

- Phases 1-8 по этому blueprint уже закрыты в репозитории: выделены child viewmodels и focused services, введены document tabs, собран новый shell layout, доведён editor-command surface, завершена миграция recent files в меню, реализовано рабочее files-tree представление и зафиксирован shell-level run/input UX.
- В `MainWindow` уже подключены window-level shortcuts для `New`, `Open`, `Save`, `Save As`, `Close Tab`, `Run`, `Find`, `Find Next` и `Find Previous`.
- Поиск переведён на custom modeless `Find/Replace` dialog: `Find...` работает через активный editor, `F3`/`Shift+F3` заведены на shell-команды, у editor есть context menu с базовыми edit/search действиями.
- `File > Open Recent` теперь является основной UI-поверхностью для recent files: меню связано с `RecentFiles` коллекцией shell-level viewmodel, успешное open обновляет snapshot, а missing recent entry удаляется из persistence без отдельной sidebar-панели.
- Phase 7 закрыта: `File Manager` pane теперь работает как lazy tree по дискам/папкам, соблюдает порядок directories first / files second, открывает файл по клику и не использует refresh-кнопку.
- Phase 8 закрыта: при ожидании `INPUT` shell принудительно удерживает `Output` активным, code-behind фокусирует `ConsoleInputTextBox`, а `Run` и document-navigation команды блокируются на время активного run/`INPUT`.
- Следующий узкий шаг для продолжения: **закрыть Phase 9**, то есть добрать финальные shell regression checks и manual smoke pass без расширения scope redesign.

## Цель редизайна

Перевести `Axiom.Ide` в полноценный редактор для повседневной работы с кодом, визуально и по UX максимально близкий к **Thonny**, но реализованный нативно для стека **C# + WPF + AvalonEdit**.

Язык, runtime, execution pipeline и semantics в этот milestone **не меняются**; вся работа ограничена IDE-слоем и его документацией.

## Locked decisions

Следующие решения зафиксированы и не подлежат пересмотру без отдельного осознанного изменения в документации:

1. **UX референс**: почти копия Thonny, адаптированная под Axiom.
2. **Document model**: editor tabs.
3. **Bottom pane**: один pane с вкладками `Output` и `Diagnostics`.
4. **INPUT UX**: inline inside Output transcript, без modal/panel UI.
5. **Recent Files**: уходят из боковой панели в `File > Open Recent`.
6. **File Manager pane**: левая панель дерева дисков/папок/файлов в lazy-режиме, не project/workspace model.
7. **Дополнительные panes** (`Variables`, `Assistant` и т.п.) в первый redesign **не входят**.
8. **Тема**: light theme, близкий к Thonny; multiple themes и theme switching в этот milestone не вводятся.
9. **Языковые слои** (`Axiom.Execution`, `Axiom.Runtime`, language-core проекты) **не меняются**.

## Внешние референсы

- **Thonny official site**: [https://thonny.org/](https://thonny.org/)
- **Thonny repository**: [https://github.com/thonny/thonny](https://github.com/thonny/thonny)
- **AvalonEdit documentation**: [https://avalonedit.net/documentation/](https://avalonedit.net/documentation/)
- **Notepad++ UI manual**: [https://npp-user-manual.org/docs/user-interface/](https://npp-user-manual.org/docs/user-interface/)

## Целевая компоновка окна

```
┌──────────────────────────────────────────────────────────────┐
│  Menu Bar (File, Edit, Search, Run, View, Help)              │
├──────────────────────────────────────────────────────────────┤
│  ToolBar (New, Open, Save, Run, Find) — компактный           │
├─────────┬────────────────────────────────────────────────────┤
│         │  Document Tabs (TabControl)                        │
│  Files  │  ┌──────────────────────────────────────────────┐  │
│  Pane   │  │                                              │  │
│  ~220px │  │          Editor (AvalonEdit)                 │  │
│  │      │  │                                              │  │
│  Split  │  └──────────────────────────────────────────────┘  │
│  ter    │                                                    │
│         ├────────────────────────────────────────────────────┤
│         │  Bottom Pane: Output / Diagnostics (TabControl)    │
│         │  ~200-260px, GridSplitter для высоты               │
├─────────┴────────────────────────────────────────────────────┤
│  Status Bar (line/col, path, dirty, run state)               │
└──────────────────────────────────────────────────────────────┘
```

### Что убирается из текущего shell

- постоянная правая колонка `Recent Files`;
- `GroupBox`-стилистику как основной визуальный каркас;
- текущую композицию "editor + random side pane + output + diagnostics" без editor-first приоритета.

### Что сохраняется по смыслу

- live output transcript;
- diagnostics grid;
- navigation from diagnostic to source location;
- syntax highlighting через централизованный AvalonEdit highlighting definition.

### Стартовое состояние окна

Окно стартует с **одной пустой вкладкой** (`Untitled-1.axm`) и editor-focus на ней.  
Если последняя вкладка закрыта, shell сразу создаёт новую пустую вкладку, а не остаётся без editor surface.

## Menu map

### File
- `New` (`Ctrl+N`)
- `Open...` (`Ctrl+O`)
- `Open Recent` (submenu)
- `Save` (`Ctrl+S`)
- `Save As...` (`Ctrl+Shift+S`)
- `Save All`
- `Close Tab` (`Ctrl+W`)
- `Exit`

### Edit
- `Undo` (`Ctrl+Z`)
- `Redo` (`Ctrl+Y`)
- `Cut` (`Ctrl+X`)
- `Copy` (`Ctrl+C`)
- `Paste` (`Ctrl+V`)
- `Delete` (`Del`)
- `Select All` (`Ctrl+A`)

### Search
- `Find...` (`Ctrl+F`)
- `Find Next` (`F3`)
- `Find Previous` (`Shift+F3`)

### Run
- `Run` (`F5`)

### View
- `File Manager`
- `Output` (checkable toggle)
- `Diagnostics` (checkable toggle)
- `Status Bar`

### Help
- `About`
- `Open Language Guide`
- `Open Samples Folder`

## Command map и hotkeys

Стандартные WPF/AvalonEdit команды, без собственной текстовой механики:

| Команда        | Shortcut       |
|----------------|----------------|
| New            | Ctrl+N         |
| Open           | Ctrl+O         |
| Save           | Ctrl+S         |
| Save As        | Ctrl+Shift+S   |
| Close Tab      | Ctrl+W         |
| Find           | Ctrl+F         |
| Find Next      | F3             |
| Find Previous  | Shift+F3       |
| Run            | F5             |
| Undo           | Ctrl+Z         |
| Redo           | Ctrl+Y         |
| Cut            | Ctrl+X         |
| Copy           | Ctrl+C         |
| Paste          | Ctrl+V         |
| Select All     | Ctrl+A         |

Поиск реализуется через отдельный modeless dialog `Find/Replace` (в стиле Thonny) и editor-side engine; встроенный **AvalonEdit SearchPanel** UI не используется.

### Editor baseline behaviors

- выделение текста мышью и клавиатурой;
- drag-and-drop текста;
- highlight current line;
- корректный caret/selection reporting для status bar;
- нормальное scroll behavior;
- контекстное меню по правой кнопке с теми же командами, что в `Edit`.

## Pane model

### Document tabs

Каждая вкладка хранит:

- `FilePath?` — путь к файлу, null для untitled;
- `DisplayName` — отображаемое имя вкладки;
- `SourceText` — текущий текст документа;
- `IsDirty` — флаг несохранённых изменений;
- `IsUntitled` — флаг untitled документа;
- текущие editor state values, реально нужные shell-уровню.

#### Правила document workflow

- `Open...` открывает файл в новой вкладке;
- если файл уже открыт, новая вкладка не создаётся, активируется существующая;
- `Save` для untitled docs ведёт в `Save As...`;
- `Save All` сохраняет все dirty tabs;
- `Close Tab` для dirty tab показывает prompt: `Save / Discard / Cancel`;
- `Exit` при наличии dirty tabs делает multi-document save/discard/cancel flow;
- session restore в этот milestone **не вводится**.

### Files pane / File Manager

`Files` pane показывает дерево **текущей рабочей папки**:

- root-level = список доступных дисков;
- раскрытие папок выполняется лениво при expand узла.

#### Первый milestone для files pane

- directories first, files second;
- показывать папки и текстовые файлы, пригодные для открытия редактором;
- клик по файлу открывает его в tab;
- `.axm`, `.txt`, `.md` открываются как редактируемые текстовые документы;
- остальные текстовые расширения также открываются в editable режиме;
- `.exe` не открываются в редакторе;
- expander дерева использует `+/-` glyph, узлы имеют иконки папки/файла;
- в заголовке панели есть отдельная кнопка `X` для скрытия `File Manager`;
- у узлов дерева есть контекстное меню в стиле file manager: рабочие команды `Открыть в Axiom IDE` / `Перейти в`, `Открыть во внешнем приложении`, `Показать скрытые файлы`, `Свойства`;
- неподдержанные file-operations (`New`, `Rename`, `Cut/Copy/Paste`, `Recycle Bin`) пока остаются disabled placeholders;
- selection узла должен быть визуально контрастным и не сливаться с обычным фоном дерева;
- refresh command отсутствует;
- **без** rename/delete/create/new folder;
- **без** project model, solution model и workspace metadata.

### Bottom pane

Единый pane с двумя вкладками:

- `Output` — transcript программы (PRINT output, INPUT prompts);
- `Diagnostics` — список ошибок с навигацией к source location.

Правила:
- `Run` всегда работает для активной вкладки;
- bottom pane автоматически переключается на `Output` при запуске;
- если run завершается diagnostics failure, bottom pane автоматически переключается на `Diagnostics`;
- `INPUT` остаётся внутри `Output` transcript и получает focus автоматически;
- вкладки `Output` и `Diagnostics` можно скрывать/показывать как через `View`, так и через `X` в заголовке вкладки;
- если `Output`/`Diagnostics` были скрыты вручную, следующий `Run` снова открывает нужную вкладку автоматически;
- когда обе вкладки скрыты, нижняя область вместе со splitter полностью схлопывается;
- нижняя область поддерживает ручной resize по высоте через вертикальный `GridSplitter`;
- `Output` и `Diagnostics` являются **global shell panes**, а не отдельными на каждую вкладку;
- параллельные runs **не поддерживаются**;
- пока программа запущена или ждёт `INPUT`, запрещены повторный `Run` и destructive document actions.

## Visual system

Вместо одного растущего `ShellResources.xaml` — несколько focused resource dictionaries:

1. **Colors/Brushes** — цветовая палитра light theme;
2. **Spacing/Sizing** — layout constants (pane widths, heights, margins);
3. **Menu/Toolbar styles** — стили для меню и тулбара;
4. **Pane and Tab styles** — стили для файлов панелей и вкладок;
5. **Status Bar styles** — стили для статус бара.

### Визуальный курс

- light theme, близкий по духу к Thonny;
- `Segoe UI` для shell chrome;
- `Consolas` или `Cascadia Mono` для editor/output;
- ровная desktop-spacing and separators;
- без тяжёлых `GroupBox` и "форменного" вида раннего WPF.

## Shell architecture

`MainWindowViewModel` остаётся root shell VM, но перестаёт быть монолитом. Вводится композиция:

### Child viewmodels

- `DocumentTabViewModel` — одна вкладка редактора;
- `FilesPaneViewModel` — дерево текущей папки;
- `BottomPaneViewModel` — Output/Diagnostics и active bottom tab;
- `StatusBarViewModel` — line/column, path, dirty state, run state.

### Focused services

- **Document workspace service** — управление вкладками, активацией, dedupe по пути, save/save-all, close rules;
- **Files pane service** — чтение lazy дерева дисков/каталогов и файлов для левой панели file manager;
- **Prompt/dialog service** — save-discard-cancel prompts и другие shell-level user prompts;
- **Recent files service** — persistence recent files для `File > Open Recent`.

### Единственное допустимое взаимодействие с execution

```
Axiom.Ide -> ProgramRunService -> AxiomExecutionPipeline
```

Слои `Axiom.Execution`, `Axiom.Runtime` и language-core проекты **не меняются**.

## Milestone roadmap

| #  | Шаг | Описание |
|----|-----|----------|
| 1  | Documentation-first baseline | Создать этот документ, обновить `ARCHITECTURE.md`, `PLANS.md`, `WORKLOG.md` |
| 2  | Shell architecture refactor | Разбить текущий IDE state на child viewmodels и focused services, не меняя внешний вид радикально |
| 3  | Document tabs | Ввести document tabs и document-workspace flow |
| 4  | New shell layout | Перестроить `MainWindow` в новый shell: menu, toolbar, files pane, editor tabs, bottom tabbed pane, status bar |
| 5  | Editor commands | Подключить editor-command surface, context menu и search |
| 6  | Recent files migration | Перенести recent files из sidebar в `File > Open Recent` |
| 7  | Files pane | Перевести левую панель в file-manager режим: lazy tree, click-to-open, editable текстовые форматы, блокировка `.exe` |
| 8  | Run/output/diagnostics UX | Довести auto-switching и blocking rules during active run |
| 9  | Tests & smoke pass | Расширить tests и выполнить manual shell smoke pass |

## Agent protocol

### Что читать перед каждым slice

Обязательный набор документов:

1. `AGENTS.md`
2. `PROJECT.md`
3. `README.md`
4. `docs/ARCHITECTURE.md`
5. `docs/PLANS3.md`
6. `docs/WORKLOG4.md`
7. **Этот файл** (`docs/IDE_REDESIGN.md`)

### Что обновлять после каждого slice

- `docs/WORKLOG4.md` — что сделано, touched paths, manual checks, exact next step;
- `docs/PLANS3.md` — если следующий шаг изменился;
- `docs/ARCHITECTURE.md` — если изменилась техническая структура IDE.

### Какие тесты гонять

После каждого изменения IDE:

```powershell
dotnet build Axiom.sln
dotnet test Axiom.sln
```

Для ручного smoke pass:

```powershell
dotnet run --project src/Axiom.Ide/Axiom.Ide.csproj
```

### Что не трогать

- `Axiom.Execution` — execution pipeline;
- `Axiom.Runtime` — runtime values, interpreter, host abstraction;
- `Axiom.Semantics` — binder, symbols, scopes;
- `Axiom.Parsing` — parser, AST;
- `Axiom.Lexing` — lexer;
- `Axiom.Core` — базовые types;
- `samples/` — language behavior samples не меняются, потому что language behavior не меняется.

## Ручная приёмка

После завершения redesign должны работать следующие сценарии:

### Editor

- feels like normal text editor;
- работает мышиное выделение;
- `Ctrl+C/X/V` работают;
- `Ctrl+Z/Y` работают;
- right-click context menu работает;
- `Ctrl+F` открывает modeless dialog `Find/Replace`; `F3`/`Shift+F3` продолжают искать вперёд/назад с текущими опциями dialog.

### Documents

- можно открыть несколько файлов во вкладках;
- open уже открытого файла активирует существующую вкладку;
- dirty tabs показывают корректный save/discard/cancel при закрытии;
- exit из приложения с dirty tabs делает multi-document flow;
- last-tab закрытие создаёт новую пустую вкладку.

### Files & Recent

- `File > Open Recent` работает;
- missing recent entry удаляется из persistence;
- `Files` pane открывает файлы текущей папки;
- клик в file manager открывает файл во вкладке;
- `.axm`/`.txt`/`.md`/`.json`/`.css` и прочие текстовые форматы открываются как editable tabs;
- `.exe` не открываются в редакторе;
- expander дерева — `+/-`, узлы папок/файлов имеют разные иконки;
- в шапке панели есть `X` для быстрого закрытия `File Manager`;
- `View -> File Manager` сохраняет своё текущее состояние между перезапусками IDE.
- закрытие IDE в `Minimized` или `Maximized` состоянии восстанавливает тот же `WindowState` при следующем запуске.

### Run & Output

- запуск программы корректно пишет в `Output`;
- bottom pane автоматически переключается на `Output` при запуске;
- runtime/syntax/semantic diagnostics открывают `Diagnostics`;
- `INPUT` идёт в нижнем `Output` без modal windows;
- во время run или INPUT запрещён повторный Run.

### Visual

- light theme, близкий к Thonny;
- Segoe UI для shell;
- Consolas/Cascadia Mono для editor;
- без GroupBox и тяжёлого WPF chrome;
- у line-number gutter отсутствуют декоративные пунктирные вертикальные линии.

