# Действующий протокол Electron / DesktopHost

Реализация: `ide/src/main/backend-client.js`, `backend-ipc.js`, `src/Axiom.DesktopHost/DesktopHost.cs`, `FrameTransport.cs`. Это служебные сообщения IDE, не операторы языка.

Main запускает свой DesktopHost, проверяет `host.ready` и отпечаток исходников. Renderer использует именованные методы preload. Управляющий поток — UTF-8 NDJSON по stdin/stdout, stderr — служебный журнал. Протокол имеет версию 1; предел управляющего сообщения — 16 MiB.

Запрос содержит `protocolVersion`, `kind: request`, `id`, `method`, необязательный `sessionId` и `payload`. Ответ содержит `kind: response`, тот же id, `ok` и payload либо error. Событие содержит `kind: event`, `event`, sessionId/sequence и payload. Счётчик сессии передаётся строкой и сравнивается клиентом через BigInt. Ответы сопоставляются по id; события с устаревшим счётчиком пропускаются.

Команды действующего диспетчера перечислены ниже; точные полезные нагрузки определяет DesktopHost.cs. `file.save` выполняет условную запись и возвращает saved/conflict/missing/currentHash; конфликт подтверждается после освобождения дескриптора. `execution`/`debug` используют идентификатор сессии; INPUT — идентификатор конкретного запроса.

- `host.ping`
- `host.shutdown`
- `execution.start`
- `execution.stop`
- `graphics.key`
- `graphics.ack`
- `graphics.mouse`
- `output.ack`
- `input.submit`
- `debug.continue`
- `debug.stepOver`
- `debug.stepInto`
- `debug.stepOut`
- `debug.stepBack`
- `file.save`
- `language.metadata`
- `document.compile`
- `document.analyse`
- `document.hover`
- `document.navigation`
- `document.format`
- `document.editTransition`
- `document.normalizeIdentifiers`

CANVAS и SCREEN представляют нативные окна DesktopHost. Событие `graphics.surface` передаёт вид поверхности, состояние, логические размеры, HWND строкой, видимость окна и признак `smooth`; для GPU-CANVAS также передаётся `graphicsDriver` со значением `Hardware` либо `WARP`. Рисование CANVAS и показ готовой текстуры происходят в DesktopHost; обычный кадр не пересылается в Electron. Событие `graphics.error` сообщает об отказе графического устройства и прекращает активное выполнение.

Прежний канал `axiom-ide-<GUID>` сохранён в реализации транспорта, но нативный CANVAS не отправляет через него каждый кадр. Формат сохранённого пути: длина JSON-заголовка (UInt32LE), заголовок и байты BGRA8; клиент проверяет размеры, stride, byteLength и формат, предел данных — 256 MiB, заголовка — 65536 байт.

Смена/закрытие сессии отменяет применение прежних событий и кадров. При аварии процесса интерфейса main прекращает прежнюю сессию и восстанавливает новое окно. Бесконечное зависание renderer обслуживается независимо в main через window-recovery.js: пользователь может продолжить ожидание, остановить с восстановлением либо остановить с выходом. Реализованный объём и проверки описаны в [результате действующей очереди](../reports/electron/ELECTRON_ACTIVE_QUEUE_RESULT.md). Целевые дополнительные гарантии из исторического проекта протокола не считаются реализованными без исходника и проверки.

## Жизненный цикл INPUT — 15 октября 2026 года

Событие input.request содержит inputRequestId, prompt, outputText и target. Значение target=screen означает ввод в активном SCREEN, включая SCREEN 0; Electron не переводит фокус в свой терминал. Значение console сохраняет консольный ввод. CANVAS не получает новый графический текстовый режим: существующий консольный INPUT сохраняется.

Когда ожидание прекращается после Enter или отмены, DesktopHost отправляет input.completed с идентификатором завершённого запроса и текущей сессией. Electron очищает только совпадающий активный запрос. Запоздалое завершение другого запроса или сессии не изменяет новый INPUT. Событие не означает успешного преобразования текста в тип переменной: неверное значение по-прежнему вызывает языковую диагностику; повтор организует сама программа. История исходного дефекта — [ревизия стабильности](../reports/STABILITY_REVIEW_20261015.md).
