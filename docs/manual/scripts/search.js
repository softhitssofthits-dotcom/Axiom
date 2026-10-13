(function () {
  const normalize = value => String(value || '').toLocaleLowerCase('ru').replaceAll('ё', 'е').trim();
  function search(entries, query) {
    const normalized = normalize(query);
    const words = normalized.split(/\s+/).filter(Boolean);
    if (!words.length) return [];
    return entries.map(entry => {
      const title = normalize(entry.title);
      const text = [title, entry.kind, entry.category, entry.description, ...(entry.keywords || [])].map(normalize).join(' ');
      if (!words.every(word => text.includes(word))) return { entry, rank: 0 };
      const exact = title === normalized || (entry.aliases || []).some(alias => normalize(alias) === normalized);
      const rank = (exact ? 100 + (entry.kind === 'Функция' ? 10 : 0) : 0) + (title.startsWith(normalized) ? 20 : 0) + words.reduce((sum, word) => sum + (title.includes(word) ? 5 : 1), 0);
      return { entry, rank };
    }).filter(item => item.rank > 0).sort((a, b) => b.rank - a.rank || a.entry.title.localeCompare(b.entry.title, 'ru')).map(item => item.entry);
  }
  const input = document.getElementById('manual-search-input');
  const results = document.getElementById('manual-search-results');
  const status = document.getElementById('manual-search-status');
  const entries = window.AxiomSearchIndex;
  if (!(input instanceof HTMLInputElement) || !Array.isArray(entries)) return;
  if (!results) return;
  input.value = new URLSearchParams(location.search).get('q') || '';
  function render() {
    const matches = search(entries, input.value); results.replaceChildren();
    if (status) status.textContent = !input.value.trim() ? 'Введите название или несколько слов из описания.' : matches.length ? 'Найдено: ' + matches.length : 'Ничего не найдено. Попробуйте другое название или более короткий запрос.';
    for (const item of matches) {
      const article = document.createElement('article'); article.className = 'search-result';
      const heading = document.createElement('h2'); const link = document.createElement('a'); link.href = item.href; link.textContent = item.title; heading.append(link);
      const meta = document.createElement('p'); meta.className = 'search-result__meta'; meta.textContent = item.kind + ' · ' + item.category;
      const description = document.createElement('p'); description.textContent = item.description; article.append(heading, meta, description); results.append(article);
    }
  }
  input.addEventListener('input', render); render();
})();
