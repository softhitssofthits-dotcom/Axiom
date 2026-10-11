(function () {
  function normalize(text) {
    return String(text || "").toLowerCase();
  }

  function score(entry, query) {
    let value = 0;
    const tokens = [entry.title, entry.kind, entry.category, entry.description, ...(entry.keywords || [])].map(normalize);
    if (normalize(entry.title) === query) {
      value += 100;
      // DATE и TIME также являются именами типов: точное имя функции ведёт к её карточке.
      if (entry.kind === "Функция") {
        value += 10;
      }
    }
    if (normalize(entry.title).startsWith(query)) {
      value += 5;
    }
    for (const token of tokens) {
      if (token.includes(query)) {
        value += 1;
      }
    }
    return value;
  }

  function search(entries, query) {
    if (!query.trim()) {
      return [];
    }

    const normalized = normalize(query);
    return entries
      .map((entry) => ({ entry, rank: score(entry, normalized) }))
      .filter((item) => item.rank > 0)
      .sort((a, b) => b.rank - a.rank || a.entry.title.localeCompare(b.entry.title, "ru"))
      .map((item) => item.entry);
  }

  function render(resultsHost, matches) {
    resultsHost.innerHTML = "";
    if (matches.length === 0) {
      resultsHost.innerHTML = '<p class="empty-state">Ничего не найдено.</p>';
      return;
    }

    for (const item of matches) {
      const card = document.createElement("article");
      card.className = "reference-card";
      card.innerHTML = `
        <h3 class="reference-card__title"><a href="${item.href}">${item.title}</a></h3>
        <p class="reference-card__meta">${item.kind} • ${item.category}</p>
        <p class="reference-card__description">${item.description}</p>
      `;
      resultsHost.appendChild(card);
    }
  }

  const input = document.getElementById("manual-search-input");
  const results = document.getElementById("manual-search-results");
  const entries = window.AxiomSearchIndex;

  if (!(input instanceof HTMLInputElement) || !results || !Array.isArray(entries)) {
    return;
  }

  const onSearch = () => {
    const matches = search(entries, input.value);
    render(results, matches);
  };

  input.addEventListener("input", onSearch);
  onSearch();
})();
