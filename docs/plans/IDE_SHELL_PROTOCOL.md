# Договор новой оболочки IDE и служебного процесса

Дата: 2026-10-10. Статус: **проект договора**, реализация отсутствует. Применяется вместе с IDE_SHELL_REPLACEMENT_PLAN.md; не описывает существующий консольный CLI. Слова «команда» и «событие» ниже обозначают сообщения IDE, а не новые операторы языка Axiom.

## 1. Владельцы и каналы

Renderer не получает файловый дескриптор процесса или доступ к Node. Именованные методы preload вызывают проверяемые обработчики main; main владеет единственным DesktopHost и его каналами. DesktopHost владеет ProgramRunService, компиляционным кэшем, VM StopState, вводом, графикой и сессией.

Команды и события: stdin/stdout DesktopHost, NDJSON UTF-8 без BOM, одна JSON-запись на строку LF. Встроенная сериализация экранирует переводы строки в пользовательском тексте. stderr — инфраструктурный журнал, его строки не являются выводом программы и не содержат событий, обязательных для управления.

Кадры CANVAS: отдельный именованный канал Windows, создаваемый DesktopHost с именем, переданным main при запуске. Main подключается через \\.\pipe\axiom-ide-<случайный GUID>. Имя выбирается main, не renderer; доступ ограничивается текущим пользователем. Канал используется только для кадров. SCREEN рисуется существующим нативным окном и не обязан пересылать все кадры в Electron.

Ограничения управляющего сообщения: 16 MiB UTF-8, без неограниченного накопления до LF. Если исходник не помещается, вернуть MESSAGE_TOO_LARGE; не обрезать текст и не выдавать успех. Это ограничение интерфейсного обмена, не новый предел языка. Разбор JSON сначала проверяет версию, тип и размер, затем полезную нагрузку. Файловые ресурсы Axiom остаются доступными по прежнему договору, не вводить скрытый запрет путей программы.

## 2. Оболочка сообщения

Запрос:

```json
{"protocolVersion":1,"kind":"request","id":"q17","method":"execution.stop","sessionId":"s3","payload":{}}
```

Успешный ответ:

```json
{"protocolVersion":1,"kind":"response","id":"q17","ok":true,"payload":{"accepted":true}}
```

Ошибка:

```json
{"protocolVersion":1,"kind":"response","id":"q17","ok":false,"error":{"code":"INVALID_SESSION","message":"Указанная сессия уже завершена."}}
```

Событие:

```json
{"protocolVersion":1,"kind":"event","event":"execution.state","sessionId":"s3","sequence":"12","payload":{"state":"Cancelling"}}
```

id — уникальная строка запроса в пределах соединения, максимум 128 символов. sessionId — случайный идентификатор конкретного запуска; sequence — монотонный Int64 внутри сессии, представленный десятичной строкой. frameId и outputVersion также передаются десятичными строками, чтобы не терять точность Number JavaScript. Клиент сравнивает такие счётчики через BigInt, а не лексикографически.

Порядок ответов разных запросов может различаться. События каждой сессии упорядочены. Main не пересылает события закрытой сессии в новый запуск; renderer также проверяет sessionId и sequence. Ошибка запроса не завершает программу автоматически, если состояние допускает продолжение.

## 3. Установление связи и возможности

Host отправляет hello после готовности Dispatcher и служб:

```json
{"protocolVersion":1,"kind":"event","event":"host.ready","payload":{"executionEngine":"BytecodeVm","capabilities":["compile","analyse","format","run","input","screen","canvas","keyboard","mouse","debug","stepBack"],"sourceRevision":"<git hash>","sourceFingerprint":"<sha256 входных исходников>","hostVersion":"1"}}
```

Значения sourceRevision/sourceFingerprint — настоящие значения сборки; угловые скобки здесь обозначают формат поля, не строку для отправки. Fingerprint вычисляется при подготовке сборки из содержимого src-проектов, project references и файлов конфигурации сборки, без bin/obj/.artifacts. Произвольное поле timestamp не заменяет его. В поставке сверяется manifest совместимых версий и контрольных сумм ресурсов.

Main ждёт ready до 10 секунд после создания процесса. При ошибке/тайм-ауте выводит понятную инфраструктурную ошибку, закрывает свой процесс и не запускает старый CLI как неявную замену. capabilities — объявление доступности интерфейса, а не доказательство правильности функции. Версия протокола несовместима → PROTOCOL_VERSION_MISMATCH.

host.ping проверяет транспорт; host.shutdown запускает отмену сессии/закрытие окон/очистку и завершение процесса. Потеря stdin означает уход владельца и вызывает ту же очистку. Main умеет ограниченно завершить собственный процесс, если штатное завершение не состоялось.

## 4. Документ, диагностика и форматирование

Поля снимка документа:

```json
{"documentId":"d1","documentRevision":8,"sourceText":"PRINT \"Привет\"\r\n","sourceFilePath":"C:\\Axiom\\samples\\program.axm","resourceBaseDirectory":"C:\\Axiom\\samples"}
```

documentRevision — неотрицательный безопасный Integer JavaScript, рост в пределах открытого документа; при достижении предела создаётся новый documentId с явно обновлённой моделью. Для analyse/format путь может быть null, для запуска подготовленный сохранённый путь обязателен согласно договору текущей WPF-IDE. Main получает такой путь после реального сохранения; не использовать фиктивный временный файл, меняющий базу ресурсов.

| method | payload | ответ |
|---|---|---|
| language.metadata | пустой объект | текущие metadata из реестра, без второго списка имён |
| document.analyse | снимок документа | documentId/revision, полный статус компиляции и диагностики, без исполнения и записи исходника |
| document.compile | подготовленный снимок | полный статус, diagnostics, сведения времени/пригодности кэша |
| document.format | снимок + formattingStyle=Classic/Modern, smartIdentifier=true/false | documentId/revision, новый текст и карта изменения выделения либо диапазоны замен |

Форматирование одного исходника даёт детерминированный результат существующей службы. Изменения применяются только к совпавшей revision, одной undo-операцией. Не заменять ошибочный ответ исходной пустой строкой. Для структурного перехода строки предусмотрен document.editTransition: снимок + previousLineNumber/currentLineNumber/caretOffset/formattingStyle; результат — edits[{start,length,newText}], newCaretOffset и исходная revision. Реализация использует общий BlockEditPlanner из плана, не AvalonEdit внутри DesktopHost.

Диагностика содержит code, stage, severity, message, sourceFilePath/documentId/revision, line, column, spanStart, spanLength. Координаты line/column 1-based; offsets/length UTF-16. Отсутствие позиции передавать как null, не выдавать за линию 1/столбец 1. BackendFailure и InfrastructureFailure различаются. Полный compile Success требует готового байт-кода.

Запрос analyse может быть отменён или его ответ проигнорирован; даже после отмены он не может изменить маркеры новой revision. Во время исполняющейся сессии анализ использует отдельный экземпляр компиляционного pipeline, не трогает кэш текущего ProgramRunService; document.compile, способный обновить исполняемый кэш, блокируется до окончания активной сессии. Не выполнять MSBuild из хоста ради анализа.

## 5. Запуск и состояния

execution.start: payload = prepared document snapshot + launchKind=run/debug + breakpointLines + stopBeforeFirstInstruction. sessionId задаётся main. launchKind — намерение оболочки, не выбор исполнителя: в обоих случаях работает BytecodeVm.

Состояния:

```text
Idle -> Preparing -> Compiling -> Running -> Finished
                              -> Paused -> Running / Paused
Running -> WaitingForInput -> Running
Running / WaitingForInput / Paused -> Cancelling -> Finished
Любое активное состояние -> Failed при потере/ошибке инфраструктуры
Finished / Failed -> новый start с новым sessionId
```

Preparing относится к сохранению/снимку в Electron; Host начинает Compiling после проверки запроса. Finished содержит итоговый статус Succeeded/CompilationFailed/RuntimeFailed/Cancelled; Failed используется для инфраструктуры соединения. Не считать RuntimeFailed успешным завершением по exitCode процесса: сам долгоживущий Host продолжает жить.

execution.start отвечает accepted после валидации, не после конца программы. Ответ accepted не означает успешной компиляции. Один активный запуск; параллельный start → BUSY. Отладочная пауза считается активной сессией. Ошибка компиляции порождает diagnostics и finished(CompilationFailed) без running.

execution.state сообщает изменение состояния; execution.diagnostics — ошибки по снимку; execution.finished — одно окончательное событие с status, actualEngine/null, timings и финальной версией вывода. После finished никакой поздний worker не возвращает Running или Paused.

execution.stop отвечает accepted и переводит состояние Cancelling. Реальный finished(Cancelled) приходит после завершения кооперативной отмены и освобождения INPUT/окон. Если до 5 секунд finished не получен, main показывает принудительное прекращение и закрывает собственный Host. Это не Succeeded и не доказательство выполнения FINALLY при принудительном завершении. Следующий запуск создаёт новый Host при необходимости.

Читатель stdin не ждёт выполнения VM. VM работает отдельно; Dispatcher продолжает графику. Изменяющие VM debug-команды сериализуются, input/cancel поступают через отдельные сигналы. Запрещено удерживать общий lock во время ожидания INPUT и Dispatcher.Invoke.

## 6. Вывод и ввод

output.change: sessionId, sequence, outputVersion и операция append/replace/clear. append содержит точный текст, replace — полный текст восстановленной контрольной точки, clear — явную очистку. Подсказка INPUT и введённый текст следуют существующему ConsoleOutputService/InputPromptService, не дублируются в двух местах. Unicode-текст не отправляется через ANSI-терминал как эталон языка.

Для больших потоков вывода: пакет не более 64 KiB UTF-8, не разделять surrogate pair, упорядоченная доставка, максимум 1 MiB отправленного неполученного вывода. Main подтверждает output.ack(outputVersion). При заполнении буфера VM может ожидать доставку с проверкой cancel; Stop обрабатывается независимым читателем. Не отбрасывать PRINT, не приписывать вывод после finished. Ограничение отображаемого scrollback требует отдельного объяснения/экспорта; исходная WPF не должна молча потерять строки при переносе.

input.request: inputRequestId, promptText, inputKind, sourceLocation, surface=console/screen. Main показывает консольное поле только для console; SCREEN использует существующий графический ввод. Поверхности не должны конкурировать за один запрос.

input.submit: sessionId, inputRequestId, text. Строка может быть пустой. Принять один раз; устаревший/повторный запрос → INVALID_INPUT_REQUEST. Ввод до input.request не применяется скрытно. input.cancel освобождает ожидание как отмена сессии; не превращать его в пустое успешно введённое значение.

keyboard.event: keydown/keyup/text + canonicalKey/text + modifiers; mouse.event: move/down/up/wheel + координаты/кнопка/delta. Main принимает события только от своей активной поверхности. SCREEN в основном получает их напрямую из NativeGraphicsScreenWindow; Electron не дублирует те же события. При blur сбросить удержанные клавиши/кнопки по прежнему договору.

## 7. Отладчик

Команды debug.continue / debug.stepOver / debug.stepInto / debug.stepOut / debug.stepBack требуют Paused, sessionId и stopId. debug.setBreakpoints требует список положительных строк и идентичность выполняемого снимка. Удаление точки обновляет этот список, а не перезапускает программу.

debug.paused: stopId, sourceLocation, line, column, opcode, callDepth, canResume, canStepBack. stopId меняется на каждой остановке. Internal VM StopState, RuntimeValue, захваченные слоты и дескрипторы кадров не сериализуются наружу.

StepBack при canStepBack=false → STEP_BACK_UNAVAILABLE; никаких выдуманных снимков. После успешного шага назад отправляются согласованные output replace и frame, затем debug.paused. При ошибке команды сессия не перескакивает в Running. Обратимость внешних файлов/ввода ограничивается существующим runtime-договором, не обещается интерфейсом.

Restart — orchestration main: Stop текущей сессии → подтверждённый finished → сохранение/подготовка нового документа → start с новым sessionId. Это не переиспользование старого StopState. Изменение файла при Paused не меняет запущенный снимок.

## 8. Графические кадры и поверхности

graphics.surface: sessionId, surfaceId, kind=screen/canvas, state=opened/changed/closed, logicalWidth/Height, frameWidth/Height, coordinateTransform. Для screen указывать наблюдаемый размер, не выдавать HWND renderer как разрешение управлять любым окном системы.

Канал кадров CANVAS имеет пакет: UInt32 little-endian headerLength → UTF-8 JSON header → указанное число байтов pixels. Header не более 4096 байтов; содержит sessionId, frameId, surfaceId, width, height, stride, format=BGRA8, byteLength. frameId передаётся строкой. Размер byteLength должен совпадать с проверенным stride*height и не превышать 256 MiB; превысивший размер возвращает FRAME_TRANSPORT_LIMIT с явным объяснением, а не меняет размер/семантику CANVAS. Это предел транспорта, достаточность проверяется по текущим допустимым размерам на этапе 9.

BGRA8 — представление готовых пикселей, включая палитру. При передаче в HTML ImageData преобразовать BGRA → RGBA, сохраняя альфа-канал и физический размер. Индексы POINT остаются внутри .NET, не вычисляются из экранного RGB.

У кадр-канала один отправляемый и один последний ожидающий кадр; новый может заменить неотправленный промежуточный. Main после предъявления отправляет graphics.ack(frameId) по управляющему каналу. Смена sessionId/поверхности обнуляет очередь; старый кадр не появляется в новом запуске.

VM/Dispatcher не ждёт медленного renderer под lock графики. Последний кадр перед нормальным завершением должен быть предъявлен либо сохранён в финальном состоянии с возможностью запроса. Падение frame-канала выдаёт видимую ошибку представления и останавливает сессию, если графическая программа не может продолжаться по договору; не объявлять пустую панель успехом.

Не пересылать по IPC каждую DRAW-команду и не переносить clipping/viewport/палитру в JavaScript. При необходимости большей эффективности следующая реализация транспорта изменяется с версией и измерениями, сохраняя общий буфер/семантику.

## 9. Ошибки и проверки

Коды инфраструктуры протокола: INVALID_MESSAGE, MESSAGE_TOO_LARGE, UNKNOWN_METHOD, PROTOCOL_VERSION_MISMATCH, BUSY, INVALID_STATE, INVALID_SESSION, INVALID_STOP, INVALID_INPUT_REQUEST, STEP_BACK_UNAVAILABLE, FRAME_TRANSPORT_LIMIT, HOST_DISCONNECTED, HOST_TIMEOUT. Они не подменяют MBxxxx языка. Сообщение пользователю на выбранном языке, первичная причина/код в журнале.

Запросы без разрешённого метода, неверного размера/типа и из чужого renderer отвергаются. `file://` URL окна сверять с конкретным загруженным приложением/BrowserWindow, не принимать любой file URL как доверенный. Не делать безопасность исходного языка «песочницей», если он по договору работает с файлами пользователя; защита моста и возможности программы — разные границы.

Обязательные процессные проверки: разделение/склейка чтений, Unicode и PRINT похожий на JSON; две конкурирующие start; Stop при INPUT/SLEEP/цикле; повтор input/stop; старый stopId; BackendFailure; восстановление вывода StepBack; несогласованный кадр/размер; конечная очередь при медленном получателе; завершение stdin; следующий чистый запуск после принудительного завершения. Проверки поведения используют настоящий общий backend; подмена допустима только для воспроизведения нарушенного транспорта и переставленных ответов.

## 10. Совместимость и точка расширения

Изменение обязательного поля/типа увеличивает protocolVersion. Добавление необязательного поля допускает игнорирование старым клиентом, если оно не меняет поведение. Electron и DesktopHost поставляются согласованной парой; автоматический переход на устаревший CLI отсутствует. CLI check/run/fmt/metadata остаётся самостоятельным инструментом и не обязан изменять свой потоковый договор ради DesktopHost.

Обновление договора сопровождается Contracts, JS-валидаторами, тестами, manifest и этой страницей. Не вписывать поле ExecutionMode/Interpreter; launchKind=run/debug относится к способу управления одной VM.

## Уточнение реализации — Session 1329, 2026-10-10 09:02

CANVAS показывается самостоятельным окном Electron. Подтверждение graphics.ack допускается для последней графической сессии после execution.finished, пока новый execution.start не сменил её; frameId обязан совпадать с отправленным неподтверждённым кадром. Это позволяет доставить ожидающий финальный кадр короткой программы. Старые идентификаторы после нового запуска отклоняются. Закрытие окна во время выполнения отправляет execution.stop для его собственной активной сессии; закрытие уже завершённого результата новую сессию не отменяет.

## Реализация команд документов — Session 1330, 2026-10-10 09:24

Поле стиля — formattingStyle со значением Classic или Modern. documentRevision в текущих DTO передаётся как обязательный неотрицательный Int32; renderer меняет documentId при достижении 2147483647. document.format возвращает sourceText и исходные documentId/documentRevision; document.editTransition — edits и newCaretOffset; analyse/compile — полный статус и diagnostics. Обработка ограничена восемью незавершёнными запросами, лишний запрос получает BUSY.

Диагностики включают severity, documentId/documentRevision/sourceFilePath, line/column и UTF-16 spanStart/spanLength. Ответ применяется только к исходной версии модели, неизменившемуся пути и последнему запросу соответствующего вида. Переставленные ответы, закрытая вкладка и SaveAs не меняют новый документ. Форматирование и проверка не требуют сохранения; запуск требует действительно сохранённого файла. smartIdentifier пока не реализован в документной команде, полный договор этого режима остаётся незавершённым пунктом плана.
