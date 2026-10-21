import fs from 'node:fs';
import path from 'node:path';
import {spawnSync} from 'node:child_process';
import {fileURLToPath} from 'node:url';

const manual = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const root = path.resolve(manual, '..', '..');
const dir = path.join(root, '.artifacts/manual2-examples');
fs.mkdirSync(dir, {recursive: true});
const dll = path.join(root, '.artifacts/bin/Axiom.Cli/Release/net10.0/axiom.dll');

const cases = [
  ['start-first', `DIM name AS STRING = "Анна"
DIM age AS INTEGER = 27
PRINT "Имя: "; name
PRINT "Возраст: "; age`, 'Имя: Анна\nВозраст: 27'],
  ['start-comments', `' Имя и возраст.
DIM name AS STRING = "Анна"
PRINT name  ' Комментарий после инструкции.
REM Ещё один комментарий.`, 'Анна'],
  ['start-region', `' #region Приветствие
PRINT "Привет"
' #endregion`, 'Привет'],
  ['language-block', `DIM count AS INTEGER = 2
count = count + 1
IF count > 0 THEN
    PRINT "Есть предметы"
    PRINT count
END IF`, 'Есть предметы\n3'],
  ['language-expr', `DIM price AS DECIMAL = 199.90
DIM quantity AS INTEGER = 3
PRINT price * quantity`, '599.70'],
  ['control-if', `DIM score AS INTEGER = 75
IF score >= 90 THEN
    PRINT "Отлично"
ELSEIF score >= 70 THEN
    PRINT "Хорошо"
ELSE
    PRINT "Повторите тему"
END IF`, 'Хорошо'],
  ['control-single', `DIM n AS INTEGER = 1
IF n = 1 THEN PRINT "один" ELSE PRINT "другое"`, 'один'],
  ['control-select', `DIM code AS INTEGER = 5
SELECT CASE code
    CASE 1, 2
        PRINT "Малая ошибка"
    CASE IS >= 10
        PRINT "Серьёзная ошибка"
    CASE 3 TO 9
        PRINT "Средняя ошибка"
    CASE ELSE
        PRINT "Неизвестный код"
END SELECT`, 'Средняя ошибка'],
  ['control-for', `FOR i = 1 TO 3
    PRINT i
NEXT
FOR j = 5 TO 1 STEP -2
    PRINT j
NEXT j`, '1\n2\n3\n5\n3\n1'],
  ['control-while', `DIM n AS INTEGER = 0
WHILE n < 3
    n = n + 1
    PRINT n
END WHILE`, '1\n2\n3'],
  ['control-do', `DIM n AS INTEGER = 0
DO
    n = n + 1
    PRINT n
LOOP UNTIL n = 2`, '1\n2'],
  ['control-exit', `FOR i = 1 TO 5
    IF i = 2 THEN CONTINUE FOR
    IF i = 4 THEN EXIT FOR
    PRINT i
NEXT`, '1\n3'],
  ['types-fixed', `DIM code AS STRING * 4 = "АБ"
PRINT "[" & code & "]"
code = "АБВГД"
PRINT code`, '[АБ  ]\nАБВГ'],
  ['routines-byref', `SUB Increase(BYREF value AS INTEGER)
    value = value + 1
END SUB

DIM count AS INTEGER = 4
Increase(count)
PRINT count`, '5'],
  ['routines-function', `FUNCTION Square(value AS INTEGER) AS INTEGER
    RETURN value * value
END FUNCTION

PRINT Square(7)`, '49'],
  ['routines-value', `FUNCTION Twice(value AS INTEGER) AS INTEGER
    RETURN value * 2
END FUNCTION

DIM f AS FUNCTION(INTEGER) AS INTEGER
f = Twice
PRINT f(5)`, '10'],
  ['routines-closure', `FUNCTION MakeReader() AS FUNCTION() AS INTEGER
    DIM value AS INTEGER = 41
    FUNCTION Read() AS INTEGER
        RETURN value + 1
    END FUNCTION
    RETURN Read
END FUNCTION

DIM reader AS FUNCTION() AS INTEGER
reader = MakeReader()
PRINT reader()`, '42'],
  ['records-basic', `TYPE Point
    X AS INTEGER
    Y AS INTEGER
END TYPE

DIM p AS Point
p.X = 10
p.Y = 20
PRINT p.X, p.Y`, '10 20'],
  ['records-default', `TYPE Box
    Label AS STRING = "пусто"
    Size AS INTEGER = 3
END TYPE
DIM b AS Box
PRINT b.Label, b.Size`, 'пусто 3'],
  ['records-method', `TYPE Counter
    Value AS INTEGER
    SUB Add(amount AS INTEGER)
        SELF.Value = SELF.Value + amount
    END SUB
END TYPE

DIM c AS Counter
c.Add(5)
PRINT c.Value`, '5'],
  ['records-literal', `TYPE Point
    X AS INTEGER
    Y AS INTEGER
END TYPE
DIM p AS Point = Point(X := 3, Y := 4)
PRINT p.X + p.Y`, '7'],
  ['records-equal', `TYPE Point
    X AS INTEGER
    Y AS INTEGER
END TYPE
DIM a AS Point = Point(X := 1, Y := 2)
DIM b AS Point = Point(X := 1, Y := 2)
PRINT a = b`, 'TRUE'],
  ['errors-try', `DIM divisor AS INTEGER = 0
TRY
    PRINT 10 / divisor
CATCH err
    PRINT "Ошибка "; err.Code
FINALLY
    PRINT "Завершение"
END TRY`, 'Ошибка MB3001\nЗавершение'],
  ['errors-error', `TRY
    ERROR "APP001", "Собственная ошибка"
CATCH err
    PRINT err.Code
    PRINT err.Message
END TRY`, 'APP001\nСобственная ошибка'],
  ['operators-math', `PRINT 7 / 2
PRINT 7 \\ 2
PRINT 7 MOD 2
PRINT 2 ^ 10`, '3.5\n3\n1\n1024'],
  ['operators-logic', `PRINT TRUE AND FALSE
PRINT FALSE OR TRUE
PRINT NOT FALSE
PRINT 5 AND 3
PRINT NOT 0`, 'FALSE\nTRUE\nTRUE\n1\n-1'],
  ['operators-text', `PRINT "Количество: " & 3
PRINT "А" < "Б"`, 'Количество: 3\nTRUE'],
  ['operators-short', `PRINT FALSE ANDALSO (1 / 0 > 0)`, 'FALSE'],
  ['arrays-shared', `DIM a(1 TO 2) AS INTEGER
DIM b() AS INTEGER
b = a
b(1) = 99
PRINT a(1)`, '99'],
  ['arrays-preserve', `DIM items(1 TO 2) AS INTEGER
items(1) = 7
items(2) = 8
REDIM PRESERVE items(1 TO 3)
items(3) = 9
PRINT ARRAYLENGTH(items)
PRINT items(1), items(3)`, '3\n7 9'],
  ['strings-basic', `DIM t AS STRING = "Привет"
PRINT LEN(t)
PRINT LEFT(t, 3)
PRINT MID(t, 2, 3)
PRINT UCASE("abc")
PRINT TRIM("  x  ")`, '6\nПри\nрив\nABC\nx'],
  ['dates-basic', `DIM d AS DATE = DATEVALUE("2024-02-29")
PRINT d
PRINT YEAR(d), MONTH(d), DAY(d)
DIM t AS TIME = TIMEVALUE("12:34:56")
PRINT t`, '2024-02-29\n2024 2 29\n12:34:56'],
  ['regex-basic', `PRINT REGEXISMATCH("abc123", "\\d+")
PRINT REGEXEXTRACT("abc123", "(\\d+)", 1)
PRINT REGEXREPLACE("a-b-c", "-", "+")
PRINT REGEXCOUNT("a1b22", "\\d+")`, 'TRUE\n123\na+b+c\n2'],
  ['files-roundtrip', `DIM path AS STRING = "manual2-roundtrip.txt"
DIM ok AS BOOLEAN
ok = WRITEFILE(path, "строка")
PRINT ok
PRINT READFILE(path)
PRINT DELETEFILE(path)`, 'TRUE\nстрока\nTRUE'],
  ['paths', `PRINT FILENAME("C:\\temp\\a.txt")
PRINT EXTNAME("C:\\temp\\a.txt")
PRINT COMBINEPATH("C:\\temp", "a.txt")`, 'a.txt\n.txt\nC:\\temp\\a.txt'],
  ['interp', `DIM n AS INTEGER = 42
PRINT $"Число: {n:F2}"
PRINT $"Шестнадцатеричное: {n:X}"`, 'Число: 42.00\nШестнадцатеричное: 2A'],
  ['const', `CONST LIMIT AS INTEGER = 10
DIM count AS INTEGER = 7
PRINT count <= LIMIT`, 'TRUE'],
  ['regex-modes', `PRINT REGEXISMATCH("ABC", "abc", "i")`, 'TRUE'],
  ['numeric-literals', `DIM count AS INTEGER = 42
DIM big AS LONG = 5000000000
DIM ratio AS DOUBLE = 0.75
DIM price AS DECIMAL = 199.90
PRINT count, big, ratio, price`, '42 5000000000 0.75 199.90'],
  ['string-quotes', `PRINT "Он сказал: ""привет"""`, 'Он сказал: "привет"'],
  ['array-rank', `DIM grid(1 TO 2, 1 TO 3) AS INTEGER
grid(2, 3) = 7
PRINT grid(2, 3)
PRINT LBOUND(grid, 1), UBOUND(grid, 2)`, '7\n1 3'],
  ['array-redim', `DIM a() AS INTEGER
PRINT ARRAYISALLOCATED(a)
REDIM a(1 TO 3)
PRINT ARRAYISALLOCATED(a), ARRAYLENGTH(a)`, 'FALSE\nTRUE 3'],
  ['string-head', `PRINT "Первая строка" & " и вторая"`, 'Первая строка и вторая'],
  ['const-snapshot', `DIM source AS INTEGER = 5
CONST SNAPSHOT AS INTEGER = source + 1
source = 99
PRINT SNAPSHOT`, '6'],
  ['for-negative-step', `FOR i = 5 TO 1 STEP -2
    PRINT i
NEXT i`, '5\n3\n1'],
  ['do-while', `DIM n AS INTEGER = 0
DO WHILE n < 2
    n = n + 1
    PRINT n
LOOP`, '1\n2'],
  ['sub-return', `SUB Test()
    PRINT "начало"
    RETURN
    PRINT "не выполнится"
END SUB
Test()`, 'начало'],
  ['function-nextid', `FUNCTION NextId(BYREF counter AS INTEGER) AS INTEGER
    counter = counter + 1
    RETURN counter
END FUNCTION

DIM current AS INTEGER = 0
PRINT NextId(current)
PRINT current`, '1\n1'],
  ['function-print-header', `SUB PrintHeader(title AS STRING)
    PRINT "== "; title; " =="
END SUB

PrintHeader("Отчёт")`, '== Отчёт =='],
];

const errorCases = [
  ['err-colon', `PRINT 1: PRINT 2`, 'MB0001'],
  ['err-undeclared', `count = 1`, 'MB2001'],
  ['err-dim-no-type', `DIM count`, 'MB2017'],
  ['err-const-write', `CONST LIMIT AS INTEGER = 10
LIMIT = 11`, 'MB2010'],
  ['err-const-byref', `CONST LIMIT AS INTEGER = 10
Change(LIMIT)
SUB Change(BYREF value AS INTEGER)
    value = 12
END SUB`, 'MB2010'],
  ['err-for-step-zero', `FOR i = 1 TO 3 STEP 0
    PRINT i
NEXT`, 'MB3004'],
  ['err-type-mismatch', `DIM n AS INTEGER = "12"`, 'MB2003'],
  ['err-array-fraction', `DIM items(1 TO 3) AS INTEGER
PRINT items(1.5)`, 'MB3007'],
  ['err-expr-stmt', `(1 + 1)`, 'MB2040'],
  ['err-print-using-type', `PRINT USING 5; 1`, 'MB2003'],
  ['err-mouse-mode', `PRINT MOUSEX()`, 'MB2046'],
  ['err-regex-mode', `PRINT REGEXISMATCH("ABC", "abc", "IgnoreCase")`, 'MB3007'],
  ['err-array-descending', `DIM items(4 TO 2) AS INTEGER`, 'MB3005'],
  ['err-array-min-lower', `DIM items(-2147483648 TO -2147483647) AS INTEGER`, 'MB3007'],
];

const results = [];
for (const [name, source, expected] of cases) {
  const file = path.join(dir, name + '.axm');
  fs.writeFileSync(file, source + '\n');
  const r = spawnSync('dotnet', [dll, 'run', file], {cwd: dir, encoding: 'utf8', timeout: 30000});
  const out = (r.stdout || '').replaceAll('\r\n', '\n').replace(/\n$/, '');
  const pass = r.status === 0 && out === expected;
  results.push({name, expected, out, err: (r.stderr || '').trim(), pass});
  console.log(`${pass ? 'PASS' : 'FAIL'} ${name}${pass ? '' : ': ' + JSON.stringify({out, err: (r.stderr || '').split('\n')[0]})}`);
}

for (const [name, source, code] of errorCases) {
  const file = path.join(dir, name + '.axm');
  fs.writeFileSync(file, source + '\n');
  const r = spawnSync('dotnet', [dll, 'run', file], {cwd: dir, encoding: 'utf8', timeout: 30000});
  const err = (r.stderr || '') + (r.error?.message || '');
  const pass = r.status !== 0 && err.includes(code);
  results.push({name, code, err: err.trim(), pass});
  console.log(`${pass ? 'PASS' : 'FAIL'} ${name}${pass ? '' : ': ' + JSON.stringify({err: err.split('\n')[0]})}`);
}

fs.writeFileSync(path.join(dir, 'results.json'), JSON.stringify(results, null, 2) + '\n');
const failed = results.filter(r => !r.pass);
console.log(`${results.length - failed.length}/${results.length}`);
if (failed.length) process.exitCode = 1;
