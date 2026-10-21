export const diagnosticsPage = {
  title: 'Диагностика',
  lead: 'Диагностика содержит код, текст и место в исходнике. Коды имеют постоянные диапазоны: лексика MB0, синтаксис MB1, семантика MB2, выполнение MB3, конвейер MB4.',
  body: `
<section><h2>Как читать сообщение</h2>
<p>Код начинается с MB. Ошибка блокирует запуск или прерывает выполнение. Предупреждение и совет не препятствуют работе: MB2047 сообщает о неиспользуемом значении, MB2045 — о совпадении имени значения и подпрограммы. После исправления исходника диагностика снимается следующей проверкой.</p>
<p>Диагностики времени выполнения перехватываются TRY/CATCH: в обработчик попадают код, текст и позиция. Синтаксические и семантические диагностики возникают до исполнения и в CATCH не попадают.</p>
</section>
<section><h2>Каталог кодов</h2>
<table class="codes"><thead><tr><th>Код</th><th>Условие</th></tr></thead><tbody>
<tr><td><code>MB0001</code></td><td>Недопустимый символ в исходном тексте.</td></tr>
<tr><td><code>MB0002</code></td><td>Строковый литерал не закрыт до конца строки.</td></tr>
<tr><td><code>MB0003</code></td><td>Неверная запись числового литерала или примыкающий суффикс.</td></tr>
<tr><td><code>MB0004</code></td><td>Числовой литерал выходит за допустимый диапазон.</td></tr>
<tr><td><code>MB1001</code></td><td>В конструкции не хватает ожидаемого ключевого слова или знака.</td></tr>
<tr><td><code>MB1002</code></td><td>Ожидается идентификатор.</td></tr>
<tr><td><code>MB1003</code></td><td>Ожидается выражение.</td></tr>
<tr><td><code>MB1004</code></td><td>Токен не допускается в текущем месте конструкции.</td></tr>
<tr><td><code>MB1005</code></td><td>Второй блок ELSE в одном IF.</td></tr>
<tr><td><code>MB1006</code></td><td>Второй блок CASE ELSE в одном SELECT CASE.</td></tr>
<tr><td><code>MB2001</code></td><td>Имя не объявлено или недоступно в текущей области.</td></tr>
<tr><td><code>MB2002</code></td><td>Повторное объявление имени в той же области.</td></tr>
<tr><td><code>MB2003</code></td><td>Несовместимый тип значения.</td></tr>
<tr><td><code>MB2004</code></td><td>Неверное число аргументов вызова.</td></tr>
<tr><td><code>MB2005</code></td><td>Имя используется как вызываемое, но не объявлено.</td></tr>
<tr><td><code>MB2006</code></td><td>RETURN со значением в SUB.</td></tr>
<tr><td><code>MB2007</code></td><td>RETURN без выражения в FUNCTION.</td></tr>
<tr><td><code>MB2008</code></td><td>Условие не имеет тип BOOLEAN.</td></tr>
<tr><td><code>MB2009</code></td><td>Неподдерживаемый тип счётчика FOR.</td></tr>
<tr><td><code>MB2010</code></td><td>Присваивание константе или передача её BYREF.</td></tr>
<tr><td><code>MB2011</code></td><td>REDIM или ERASE применён к не-массиву.</td></tr>
<tr><td><code>MB2012</code></td><td>RETURN вне SUB или FUNCTION.</td></tr>
<tr><td><code>MB2013</code></td><td>EXIT FOR вне цикла FOR.</td></tr>
<tr><td><code>MB2014</code></td><td>EXIT DO вне цикла DO.</td></tr>
<tr><td><code>MB2015</code></td><td>Неверное значение или диапазон CASE.</td></tr>
<tr><td><code>MB2016</code></td><td>Новое имя перекрывает доступное внешнее имя.</td></tr>
<tr><td><code>MB2017</code></td><td>В DIM нет ни типа, ни инициализатора.</td></tr>
<tr><td><code>MB2018</code></td><td>Константа не помещается в указанный тип.</td></tr>
<tr><td><code>MB2019</code></td><td>SUB используется как значение.</td></tr>
<tr><td><code>MB2020</code></td><td>Значение не является вызываемым.</td></tr>
<tr><td><code>MB2021</code></td><td>Тип после AS не объявлен.</td></tr>
<tr><td><code>MB2022</code></td><td>Недопустимое поле TYPE или цикл вложенности типов.</td></tr>
<tr><td><code>MB2023</code></td><td>В TYPE нет указанного поля.</td></tr>
<tr><td><code>MB2024</code></td><td>Доступ через точку к значению, не являющемуся записью.</td></tr>
<tr><td><code>MB2025</code></td><td>REDIM или ERASE у фиксированного массивного поля.</td></tr>
<tr><td><code>MB2026</code></td><td>Недопустимая фиксированная строка STRING * N.</td></tr>
<tr><td><code>MB2027</code></td><td>Значение по умолчанию поля TYPE несовместимо с типом.</td></tr>
<tr><td><code>MB2028</code></td><td>Аргумент BYREF не является простой переменной подходящего типа.</td></tr>
<tr><td><code>MB2029</code></td><td>Функция с BYREF-параметром используется как значение.</td></tr>
<tr><td><code>MB2031</code></td><td>Смешение позиционных и именованных аргументов в литерале записи.</td></tr>
<tr><td><code>MB2032</code></td><td>Неверное число позиционных аргументов литерала записи.</td></tr>
<tr><td><code>MB2033</code></td><td>Неоднозначный конструктор TYPE.</td></tr>
<tr><td><code>MB2034</code></td><td>Неверный обработчик ON KEY.</td></tr>
<tr><td><code>MB2035</code></td><td>Неверный обработчик ON MOUSE.</td></tr>
<tr><td><code>MB2036</code></td><td>Неверное событие ON MOUSE.</td></tr>
<tr><td><code>MB2037</code></td><td>CONTINUE FOR вне цикла FOR.</td></tr>
<tr><td><code>MB2038</code></td><td>CONTINUE WHILE вне цикла WHILE.</td></tr>
<tr><td><code>MB2039</code></td><td>CONTINUE DO вне цикла DO.</td></tr>
<tr><td><code>MB2040</code></td><td>Выражение-инструкция не выполняет действия.</td></tr>
<tr><td><code>MB2044</code></td><td>Поле TYPE указано без SELF.</td></tr>
<tr><td><code>MB2045</code></td><td>Имя значения совпадает с именем подпрограммы.</td></tr>
<tr><td><code>MB2046</code></td><td>Операция требует графического режима.</td></tr>
<tr><td><code>MB2047</code></td><td>Значение не используется.</td></tr>
<tr><td><code>MB2048</code></td><td>Имя после NEXT не совпадает со счётчиком.</td></tr>
<tr><td><code>MB2049</code></td><td>Неизвестный модуль IMPORT.</td></tr>
<tr><td><code>MB2050</code></td><td>Повтор имени модуля.</td></tr>
<tr><td><code>MB2051</code></td><td>Отсутствует точка входа набора.</td></tr>
<tr><td><code>MB2052</code></td><td>Цикл импортов или самоимпорт.</td></tr>
<tr><td><code>MB2053</code></td><td>Обращение к закрытому имени другого модуля.</td></tr>
<tr><td><code>MB2054</code></td><td>Неоднозначный импортированный член.</td></tr>
<tr><td><code>MB2055</code></td><td>Конфликт импортированного имени.</td></tr>
<tr><td><code>MB2056</code></td><td>PUBLIC или PRIVATE в недопустимом месте.</td></tr>
<tr><td><code>MB3001</code></td><td>Деление на ноль.</td></tr>
<tr><td><code>MB3002</code></td><td>Индекс за границей массива.</td></tr>
<tr><td><code>MB3003</code></td><td>Ответ INPUT не удалось разобрать.</td></tr>
<tr><td><code>MB3004</code></td><td>Нулевой шаг FOR.</td></tr>
<tr><td><code>MB3005</code></td><td>Недопустимое состояние ресурса или значения.</td></tr>
<tr><td><code>MB3006</code></td><td>Функция завершилась без обязательного результата.</td></tr>
<tr><td><code>MB3007</code></td><td>Неверный аргумент встроенной функции.</td></tr>
<tr><td><code>MB3008</code></td><td>Переполнение целого результата.</td></tr>
<tr><td><code>MB3009</code></td><td>Пустое функциональное значение.</td></tr>
<tr><td><code>MB3010</code></td><td>Графический режим не выбран.</td></tr>
<tr><td><code>MB3011</code></td><td>Переполнение стека вызовов.</td></tr>
<tr><td><code>MB3012</code></td><td>Недопустимая область значения.</td></tr>
<tr><td><code>MB3013</code></td><td>Отказ выделения массива.</td></tr>
<tr><td><code>MB3014</code></td><td>Представление массива устарело.</td></tr>
<tr><td><code>MB4001</code></td><td>Неподдерживаемая инструкция байт-кода.</td></tr>
<tr><td><code>MB4002</code></td><td>Ошибка захвата значения.</td></tr>
<tr><td><code>MB4003</code></td><td>Ошибка снимка байт-кода.</td></tr>
<tr><td><code>MB4004</code></td><td>Зависимость точки входа не прочитана.</td></tr>
</tbody></table>
</section>`,
};
