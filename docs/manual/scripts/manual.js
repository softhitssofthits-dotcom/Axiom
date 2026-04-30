(function () {
  const nav = [
    {
      title: "Старт",
      items: [
        { id: "home", href: "index.html", label: "Главная" },
        { id: "getting-started", href: "getting-started.html", label: "Быстрый старт" },
        { id: "reference", href: "reference.html", label: "Алфавитный указатель" },
        { id: "search", href: "search.html", label: "Поиск по справочнику" }
      ]
    },
    {
      title: "Основы языка",
      items: [
        { id: "data-types", href: "data-types.html", label: "Типы данных" },
        { id: "data-types-scalars", href: "data-types-scalars.html", label: "Скалярные типы" },
        { id: "data-types-strings-and-files", href: "data-types-strings-and-files.html", label: "STRING и FILE" },
        { id: "data-types-records-and-arrays", href: "data-types-records-and-arrays.html", label: "TYPE и массивы" },
        { id: "data-types-conversions", href: "data-types-conversions.html", label: "Преобразования типов" },
        { id: "variables-constants", href: "variables-and-constants.html", label: "Переменные и константы" },
        { id: "arithmetic-logic", href: "arithmetic-and-logic.html", label: "Арифметика и логика" }
      ]
    },
    {
      title: "Ввод и вывод",
      items: [
        { id: "print", href: "print.html", label: "Оператор PRINT" },
        { id: "input", href: "input.html", label: "Оператор INPUT" },
        { id: "open", href: "open.html", label: "Оператор OPEN" },
        { id: "close", href: "close.html", label: "Оператор CLOSE" }
      ]
    },
    {
      title: "Управление выполнением",
      items: [
        { id: "if", href: "if.html", label: "Оператор IF" },
        { id: "select-case", href: "select-case.html", label: "Оператор SELECT CASE" }
      ]
    },
    {
      title: "Циклы",
      items: [
        { id: "for", href: "for.html", label: "Оператор цикла FOR" },
        { id: "while", href: "while.html", label: "Оператор цикла WHILE" },
        { id: "do-loop", href: "do-loop.html", label: "Оператор цикла DO ... LOOP" }
      ]
    },
    {
      title: "Процедуры и функции",
      items: [
        { id: "sub", href: "sub.html", label: "Процедура SUB" },
        { id: "function", href: "function.html", label: "Функция FUNCTION" },
        { id: "return", href: "return.html", label: "Оператор RETURN" }
      ]
    },
    {
      title: "Массивы",
      items: [
        { id: "arrays", href: "arrays.html", label: "Массивы" },
        { id: "arrays-1d", href: "arrays-1d.html", label: "Одномерные массивы" },
        { id: "arrays-2d", href: "arrays-2d.html", label: "Двумерные массивы" },
        { id: "arrays-multidimensional", href: "arrays-multidimensional.html", label: "Многомерные массивы" },
        { id: "arrays-dynamic", href: "arrays-dynamic.html", label: "Динамические массивы" },
        { id: "arrays-indexing", href: "arrays-indexing.html", label: "Индексирование" },
        { id: "arrays-slices", href: "arrays-slices.html", label: "Срезы и представления" },
        { id: "arrays-functions", href: "arrays-functions.html", label: "ARRAY-функции" },
        { id: "arrays-diagnostics", href: "arrays-diagnostics.html", label: "Ошибки массивов" },
        { id: "arrays-1d-visual", href: "arrays-1d-visual.html", label: "1D наглядно" },
        { id: "arrays-2d-visual", href: "arrays-2d-visual.html", label: "2D наглядно" }
      ]
    },
    {
      title: "Встроенные функции",
      items: [
        { id: "string-functions", href: "string-functions.html", label: "Строковые" },
        { id: "string-functions-slices-and-length", href: "string-functions-slices-and-length.html", label: "Строковые: длина и фрагменты" },
        { id: "string-functions-trim-and-case", href: "string-functions-trim-and-case.html", label: "Строковые: обрезка и регистр" },
        { id: "string-functions-search-and-replace", href: "string-functions-search-and-replace.html", label: "Строковые: поиск и замена" },
        { id: "string-functions-build-and-split", href: "string-functions-build-and-split.html", label: "Строковые: построение и массивы" },
        { id: "numeric-functions", href: "numeric-functions.html", label: "Числовые" },
        { id: "numeric-functions-conversion-and-legacy", href: "numeric-functions-conversion-and-legacy.html", label: "Числовые: INT/STR/VAL" },
        { id: "numeric-functions-core", href: "numeric-functions-core.html", label: "Числовые: базовая математика" },
        { id: "numeric-functions-trigonometry", href: "numeric-functions-trigonometry.html", label: "Числовые: тригонометрия" },
        { id: "numeric-functions-roots-rounding", href: "numeric-functions-roots-rounding.html", label: "Числовые: корни и округление" },
        { id: "regex-functions", href: "regex-functions.html", label: "Регулярные выражения" },
        { id: "regex-functions-match-and-extract", href: "regex-functions-match-and-extract.html", label: "Регулярные: проверка и извлечение" },
        { id: "regex-functions-arrays-and-split", href: "regex-functions-arrays-and-split.html", label: "Регулярные: массивы и разбиение" },
        { id: "regex-functions-replace-and-count", href: "regex-functions-replace-and-count.html", label: "Регулярные: замена и подсчёт" },
        { id: "date-time-functions", href: "date-time-functions.html", label: "Дата и время" },
        { id: "date-time-functions-current", href: "date-time-functions-current.html", label: "Дата и время: текущие значения" },
        { id: "date-time-functions-parts-and-serial", href: "date-time-functions-parts-and-serial.html", label: "Дата и время: части и сборка" },
        { id: "date-time-functions-arithmetic-and-validation", href: "date-time-functions-arithmetic-and-validation.html", label: "Дата и время: арифметика и проверка" },
        { id: "date-time-functions-time-zones", href: "date-time-functions-time-zones.html", label: "Дата и время: UTC и пояса" },
        { id: "file-system-functions", href: "file-system-functions.html", label: "Файловая система" },
        { id: "file-system-functions-text", href: "file-system-functions-text.html", label: "Файловая система: текст" },
        { id: "file-system-functions-open-files", href: "file-system-functions-open-files.html", label: "Файловая система: FILE" },
        { id: "file-system-functions-paths", href: "file-system-functions-paths.html", label: "Файловая система: пути и сведения" },
        { id: "file-system-functions-management", href: "file-system-functions-management.html", label: "Файловая система: операции" }
      ]
    },
    {
      title: "Практика",
      items: [
        { id: "diagnostics", href: "diagnostics.html", label: "Диагностика" },
        { id: "examples", href: "examples.html", label: "Примеры" },
        { id: "cheat-sheet", href: "cheat-sheet.html", label: "Шпаргалка" }
      ]
    }
  ];

  const currentPageId = document.body?.dataset.page ?? "";
  const navHost = document.getElementById("manual-nav");
  const pagerHost = document.getElementById("page-pager");
  const flatNavItems = nav.flatMap((section) => section.items);

  function renderNav() {
    if (!navHost) {
      return;
    }

    navHost.className = "manual-nav";
    navHost.innerHTML = "";

    for (const section of nav) {
      const sectionElement = document.createElement("section");
      sectionElement.className = "manual-nav__section";

      const titleElement = document.createElement("h3");
      titleElement.className = "manual-nav__section-title";
      titleElement.textContent = section.title;
      sectionElement.appendChild(titleElement);

      const listElement = document.createElement("ul");
      listElement.className = "manual-nav__list";

      for (const item of section.items) {
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
        listElement.appendChild(listItem);
      }

      sectionElement.appendChild(listElement);
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
