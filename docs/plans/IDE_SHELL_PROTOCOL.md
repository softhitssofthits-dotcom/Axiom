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
- `document.format`
- `document.editTransition`
- `document.normalizeIdentifiers`

CANVAS передаётся отдельным именованным каналом `axiom-ide-<GUID>`. Перед кадром идут длина JSON-заголовка (UInt32LE), заголовок и байты BGRA8. Клиент проверяет размеры, stride, byteLength и формат; предел данных кадра — 256 MiB, заголовка — 65536 байт. SCREEN представляет нативное окно DesktopHost.

Смена/закрытие сессии отменяет применение прежних событий и кадров. При аварии процесса интерфейса main прекращает прежнюю сессию и восстанавливает новое окно. Обработка бесконечного зависания пока не завершена. Целевые дополнительные гарантии из исторического проекта протокола не считаются реализованными без исходника и проверки.
