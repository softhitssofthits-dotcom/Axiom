# Системный поиск зон: Unicode и NUL

Session1299. SystemTimeZoneUnicodeEvidenceTests добавляет24 случая:4 неверных имени для UTCOFFSET/TOUTC/FROMUTC в обоих движках. Часы/resolver не подменяются, используется системный поиск Windows. Конечный NUL после UTC не отбрасывается; эмодзи, одиночный суррогат и несуществующее имя с составным акцентом дают MB3007, FINALLY сохраняет исходный DATETIME.

Совместно26/26 с TimeZoneUnicodeBoundaryEvidenceTests, ошибок/пропусков0, запасной интерпретатор не использован. Результат: .artifacts/test-results/system-zone-1299/system-zone-1299.trx. Дефект не найден, Runtime/руководство/поведение не менялись, новый пример не требуется. Проверка ограничена Windows и четырьмя неверными именами, не всеми платформами/зонами. NUM26-CAND-001/IDE26-CAND-001 открыты.
