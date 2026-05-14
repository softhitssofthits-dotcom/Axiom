(function () {
  const nav = [
    {
      title: "Начало работы и среда",
      items: [
        { id: "home", href: "index.html", label: "Главная" },
        { id: "getting-started", href: "getting-started.html", label: "Быстрый старт" },
        { id: "reference", href: "reference.html", label: "Алфавитный указатель" },
        { id: "search", href: "search.html", label: "Поиск по справочнику" }
      ]
    },
    {
      title: "Основы языка",
      groups: [
        {
          title: "Типы данных",
          items: [
            { id: "data-types", href: "data-types.html", label: "Типы данных" },
            { id: "data-types-scalars", href: "data-types-scalars.html", label: "Скалярные типы" },
            { id: "data-types-strings-and-files", href: "data-types-strings-and-files.html", label: "STRING и FILE" },
            { id: "data-types-records-and-arrays", href: "data-types-records-and-arrays.html", label: "Записи TYPE и массивы" },
            { id: "data-types-conversions", href: "data-types-conversions.html", label: "Преобразования типов" }
          ]
        },
        {
          title: "Переменные и константы",
          items: [
            { id: "variables-constants", href: "variables-and-constants.html", label: "Переменные и константы" }
          ]
        }
      ]
    },
    {
      title: "Операторы описания и объявления",
      items: [
        { id: "dim", href: "dim.html", label: "Оператор DIM" },
        { id: "const", href: "const.html", label: "Оператор CONST" },
        { id: "type", href: "type.html", label: "Оператор TYPE" }
      ]
    },
    {
      title: "Выражения и операции",
      items: [
        { id: "expressions-and-branching", href: "expressions-and-branching.html", label: "Выражения" },
        { id: "arithmetic-logic", href: "arithmetic-and-logic.html", label: "Арифметика, сравнения и логика" }
      ]
    },
    {
      title: "Операторы передачи управления",
      groups: [
        {
          title: "Условные операторы",
          items: [
            { id: "if", href: "if.html", label: "Оператор IF" },
            { id: "select-case", href: "select-case.html", label: "Оператор SELECT CASE" }
          ]
        },
        {
          title: "Циклы",
          items: [
            { id: "loops", href: "loops.html", label: "Циклы" },
            { id: "for", href: "for.html", label: "FOR ... NEXT" },
            { id: "while", href: "while.html", label: "WHILE ... END WHILE" },
            { id: "do-loop", href: "do-loop.html", label: "DO ... LOOP" }
          ]
        },
        {
          title: "Остановка выполнения",
          items: [
            { id: "sleep", href: "sleep.html", label: "Оператор SLEEP" }
          ]
        }
      ]
    },
    {
      title: "Процедуры и функции",
      items: [
        { id: "procedures-and-functions", href: "procedures-and-functions.html", label: "Процедуры и функции" },
        { id: "sub", href: "sub.html", label: "Процедура SUB" },
        { id: "function", href: "function.html", label: "Функция FUNCTION" },
        { id: "return", href: "return.html", label: "Оператор RETURN" }
      ]
    },
    {
      title: "Массивы и составные данные",
      groups: [
        {
          title: "Форма и индексы",
          items: [
            { id: "arrays", href: "arrays.html", label: "Массивы" },
            { id: "arrays-1d", href: "arrays-1d.html", label: "Одномерные массивы" },
            { id: "arrays-2d", href: "arrays-2d.html", label: "Двумерные массивы" },
            { id: "arrays-multidimensional", href: "arrays-multidimensional.html", label: "Многомерные массивы" },
            { id: "arrays-dynamic", href: "arrays-dynamic.html", label: "Динамические массивы" },
            { id: "arrays-indexing", href: "arrays-indexing.html", label: "Индексирование" },
            { id: "arrays-slices", href: "arrays-slices.html", label: "Срезы и представления" }
          ]
        },
        {
          title: "Функции и диагностика",
          items: [
            { id: "arrays-functions", href: "arrays-functions.html", label: "Базовые ARRAY-функции" },
            { id: "arrays-and-builtins", href: "arrays-and-builtins.html", label: "Функции массивов" },
            { id: "arrays-diagnostics", href: "arrays-diagnostics.html", label: "Ошибки массивов" }
          ]
        },
        {
          title: "Наглядные схемы",
          items: [
            { id: "arrays-1d-visual", href: "arrays-1d-visual.html", label: "Одномерный массив" },
            { id: "arrays-2d-visual", href: "arrays-2d-visual.html", label: "Двумерный массив" }
          ]
        }
      ]
    },
    {
      title: "Ввод и вывод",
      items: [
        { id: "data-and-input", href: "data-and-input.html", label: "Данные, ввод и вывод" },
        { id: "print", href: "print.html", label: "Оператор PRINT" },
        { id: "input", href: "input.html", label: "Оператор INPUT" }
      ]
    },
    {
      title: "Файлы и файловая система",
      groups: [
        {
          title: "Открытые файлы",
          items: [
            { id: "open", href: "open.html", label: "Оператор OPEN" },
            { id: "close", href: "close.html", label: "Оператор CLOSE" },
            { id: "file-system-functions-open-files", href: "file-system-functions-open-files.html", label: "Функции FILE" }
          ]
        },
        {
          title: "Пути, текст и операции",
          items: [
            { id: "file-system-functions", href: "file-system-functions.html", label: "Файловая система" },
            { id: "file-system-functions-text", href: "file-system-functions-text.html", label: "Текстовые файлы" },
            { id: "file-system-functions-paths", href: "file-system-functions-paths.html", label: "Пути и сведения" },
            { id: "file-system-functions-management", href: "file-system-functions-management.html", label: "Операции с файлами" }
          ]
        }
      ]
    },
    {
      title: "Графика и экран",
      items: [
        { id: "graphics", href: "graphics.html", label: "Графика" },
        { id: "screen", href: "screen.html", label: "Оператор SCREEN" },
        { id: "graphics-text", href: "graphics-text.html", label: "Текст на экране" },
        { id: "graphics-primitives", href: "graphics-primitives.html", label: "Операторы рисования" },
        { id: "screen-functions", href: "screen-functions.html", label: "Функции состояния SCREEN" }
      ]
    },
    {
      title: "Клавиатура, мышь и события",
      groups: [
        {
          title: "Клавиатура",
          items: [
            { id: "inkey", href: "inkey.html", label: "Функция INKEY" },
            { id: "keydown", href: "keydown.html", label: "Функция KEYDOWN" },
            { id: "keypress", href: "keypress.html", label: "Функция KEYPRESS" },
            { id: "on-key", href: "on-key.html", label: "Оператор ON KEY" }
          ]
        },
        {
          title: "Мышь",
          items: [
            { id: "mousex", href: "mousex.html", label: "Функция MOUSEX" },
            { id: "mousey", href: "mousey.html", label: "Функция MOUSEY" },
            { id: "mousebutton", href: "mousebutton.html", label: "Функция MOUSEBUTTON" },
            { id: "mousepress", href: "mousepress.html", label: "Функция MOUSEPRESS" },
            { id: "on-mouse", href: "on-mouse.html", label: "Оператор ON MOUSE" }
          ]
        }
      ]
    },
    {
      title: "Встроенные функции",
      groups: [
        {
          title: "Строки и символы",
          items: [
            { id: "string-functions", href: "string-functions.html", label: "Строковые функции" },
            { id: "string-functions-slices-and-length", href: "string-functions-slices-and-length.html", label: "Длина и фрагменты" },
            { id: "string-functions-trim-and-case", href: "string-functions-trim-and-case.html", label: "Обрезка и регистр" },
            { id: "string-functions-search-and-replace", href: "string-functions-search-and-replace.html", label: "Поиск и замена" },
            { id: "string-functions-build-and-split", href: "string-functions-build-and-split.html", label: "Построение и массивы строк" },
            { id: "characters", href: "characters.html", label: "CHR, ASC, CHRW и ASCW" }
          ]
        },
        {
          title: "Числа",
          items: [
            { id: "numeric-functions", href: "numeric-functions.html", label: "Числовые функции" },
            { id: "numeric-functions-conversion-and-legacy", href: "numeric-functions-conversion-and-legacy.html", label: "INT, STR и VAL" },
            { id: "numeric-functions-core", href: "numeric-functions-core.html", label: "Базовая математика" },
            { id: "numeric-functions-trigonometry", href: "numeric-functions-trigonometry.html", label: "Тригонометрия" },
            { id: "numeric-functions-roots-rounding", href: "numeric-functions-roots-rounding.html", label: "Корни и округление" }
          ]
        },
        {
          title: "Дата и время",
          items: [
            { id: "date-time-functions", href: "date-time-functions.html", label: "Дата и время" },
            { id: "date-time-functions-current", href: "date-time-functions-current.html", label: "Текущие значения" },
            { id: "date-time-functions-parts-and-serial", href: "date-time-functions-parts-and-serial.html", label: "Части и сборка" },
            { id: "date-time-functions-arithmetic-and-validation", href: "date-time-functions-arithmetic-and-validation.html", label: "Арифметика и проверка" },
            { id: "date-time-functions-time-zones", href: "date-time-functions-time-zones.html", label: "UTC и часовые пояса" }
          ]
        },
        {
          title: "Регулярные выражения",
          items: [
            { id: "regex-functions", href: "regex-functions.html", label: "Регулярные выражения" },
            { id: "regex-functions-match-and-extract", href: "regex-functions-match-and-extract.html", label: "Проверка и извлечение" },
            { id: "regex-functions-arrays-and-split", href: "regex-functions-arrays-and-split.html", label: "Массивы и разбиение" },
            { id: "regex-functions-replace-and-count", href: "regex-functions-replace-and-count.html", label: "Замена и подсчёт" }
          ]
        }
      ]
    },
    {
      title: "Обработка ошибок и диагностика",
      items: [
        { id: "error-handling", href: "error-handling.html", label: "Обработка ошибок" },
        { id: "try", href: "try.html", label: "Оператор TRY" },
        { id: "error", href: "error.html", label: "Оператор ERROR" },
        { id: "diagnostics", href: "diagnostics.html", label: "Диагностика" }
      ]
    },
    {
      title: "Примеры и справочные приложения",
      items: [
        { id: "data-type-limits", href: "data-type-limits.html", label: "Диапазоны и ограничения типов" },
        { id: "examples", href: "examples.html", label: "Примеры программ" },
        { id: "cheat-sheet", href: "cheat-sheet.html", label: "Краткая справка" }
      ]
    }
  ];

  const currentPageId = document.body?.dataset.page ?? "";
  const navHost = document.getElementById("manual-nav");
  const pagerHost = document.getElementById("page-pager");

  function collectItems(section) {
    if (Array.isArray(section.items)) {
      return section.items;
    }

    if (Array.isArray(section.groups)) {
      return section.groups.flatMap((group) => group.items);
    }

    return [];
  }

  const flatNavItems = nav.flatMap(collectItems);

  function sectionHasCurrent(section) {
    return collectItems(section).some((item) => item.id === currentPageId);
  }

  function groupHasCurrent(group) {
    return group.items.some((item) => item.id === currentPageId);
  }

  function renderLink(item) {
    const listItem = document.createElement("li");
    const link = document.createElement("a");
    link.className = "manual-nav__link";

    if (item.id === currentPageId) {
      link.classList.add("is-active");
      link.setAttribute("aria-current", "page");
    }

    link.href = item.href;
    link.textContent = item.label;
    listItem.appendChild(link);
    return listItem;
  }

  function renderItemList(items) {
    const listElement = document.createElement("ul");
    listElement.className = "manual-nav__list";

    for (const item of items) {
      listElement.appendChild(renderLink(item));
    }

    return listElement;
  }

  function renderGroup(group) {
    const groupElement = document.createElement("div");
    groupElement.className = "manual-nav__group";

    const groupTitle = document.createElement("h4");
    groupTitle.className = "manual-nav__group-title";
    groupTitle.textContent = group.title;
    groupElement.appendChild(groupTitle);
    groupElement.appendChild(renderItemList(group.items));

    if (groupHasCurrent(group)) {
      groupElement.classList.add("is-current");
    }

    return groupElement;
  }

  function renderNav() {
    if (!navHost) {
      return;
    }

    navHost.className = "manual-nav";
    navHost.innerHTML = "";

    for (const section of nav) {
      const sectionElement = document.createElement("details");
      sectionElement.className = "manual-nav__section";
      sectionElement.open = sectionHasCurrent(section);

      const titleElement = document.createElement("summary");
      titleElement.className = "manual-nav__section-title";
      titleElement.textContent = section.title;
      sectionElement.appendChild(titleElement);

      if (Array.isArray(section.groups)) {
        for (const group of section.groups) {
          sectionElement.appendChild(renderGroup(group));
        }
      } else {
        sectionElement.appendChild(renderItemList(section.items));
      }

      navHost.appendChild(sectionElement);
    }
  }

  function renderPager() {
    if (!pagerHost) {
      return;
    }

    const currentIndex = flatNavItems.findIndex((item) => item.id === currentPageId);
    if (currentIndex < 0) {
      pagerHost.innerHTML = "";
      return;
    }

    const previous = currentIndex > 0 ? flatNavItems[currentIndex - 1] : null;
    const next = currentIndex < flatNavItems.length - 1 ? flatNavItems[currentIndex + 1] : null;

    pagerHost.innerHTML = "";

    if (previous) {
      const previousLink = document.createElement("a");
      previousLink.className = "pager__link pager__link--prev";
      previousLink.href = previous.href;
      previousLink.textContent = "< " + previous.label;
      pagerHost.appendChild(previousLink);
    }

    if (next) {
      const nextLink = document.createElement("a");
      nextLink.className = "pager__link pager__link--next";
      nextLink.href = next.href;
      nextLink.textContent = next.label + " >";
      pagerHost.appendChild(nextLink);
    }
  }

  function setupNavToggle() {
    const toggle = document.querySelector("[data-nav-toggle]");
    if (!(toggle instanceof HTMLButtonElement)) {
      return;
    }

    toggle.addEventListener("click", () => {
      document.body.classList.toggle("nav-open");
    });

    navHost?.addEventListener("click", (event) => {
      const target = event.target;
      if (target instanceof HTMLAnchorElement) {
        document.body.classList.remove("nav-open");
      }
    });
  }

  renderNav();
  renderPager();
  setupNavToggle();
})();
