// Клиентская часть: оглавление по группам, меню и поиск по локальному индексу.
(function () {
  'use strict';

  const groups = window.AxiomManual2Nav || [];
  const entries = window.AxiomManual2Index || [];
  const pageId = document.body.dataset.page;

  function buildNavigation() {
    const host = document.getElementById('nav');
    if (!host) return;
    host.textContent = '';
    for (const group of groups) {
      const title = document.createElement('div');
      title.className = 'nav-title';
      title.textContent = group.title;
      host.appendChild(title);
      for (const [id, label] of group.items) {
        const link = document.createElement('a');
        link.href = id + '.html';
        link.textContent = label;
        if (id === pageId) link.setAttribute('aria-current', 'page');
        host.appendChild(link);
      }
    }
  }

  function setupMenu() {
    const button = document.querySelector('.menu-btn');
    const side = document.getElementById('side');
    if (!button || !side) return;
    button.addEventListener('click', () => {
      const open = side.classList.toggle('open');
      button.setAttribute('aria-expanded', open ? 'true' : 'false');
    });
    document.addEventListener('keydown', event => {
      if (event.key === 'Escape' && side.classList.contains('open')) {
        side.classList.remove('open');
        button.setAttribute('aria-expanded', 'false');
        button.focus();
      }
      if (event.altKey && event.code === 'Slash') {
        const input = document.getElementById('q');
        if (input) {
          event.preventDefault();
          input.focus();
        }
      }
    });
  }

  function normalize(value) {
    return String(value || '').toLocaleLowerCase('ru-RU').replace(/\s+/g, ' ').trim();
  }

  function score(entry, query) {
    const title = normalize(entry.title);
    if (title === query) return 1000;
    if (title.startsWith(query)) return 500 - title.length;
    if (title.includes(query)) return 300 - title.length;
    const text = normalize(entry.text);
    const at = text.indexOf(query);
    if (at >= 0) return 100 - Math.min(at, 60);
    return 0;
  }

  function escapeHtml(value) {
    return String(value)
      .replaceAll('&', '&amp;')
      .replaceAll('<', '&lt;')
      .replaceAll('>', '&gt;')
      .replaceAll('"', '&quot;');
  }

  function snippet(entry, query) {
    const text = String(entry.text || '');
    const at = normalize(text).indexOf(query);
    if (at < 0) return '';
    const from = Math.max(0, at - 40);
    return (from ? '…' : '') + text.slice(from, at + query.length + 70).trim() + '…';
  }

  function mark(text, query) {
    const safe = escapeHtml(text);
    if (!query) return safe;
    const pattern = new RegExp('(' + query.replace(/[.*+?^${}()|[\]\\]/g, '\\$&') + ')', 'iu');
    return safe.replace(pattern, '<mark>$1</mark>');
  }

  function runSearch() {
    const params = new URLSearchParams(location.search);
    const raw = params.get('q') || '';
    const query = normalize(raw);
    const input = document.getElementById('q');
    if (input && raw) input.value = raw;

    const status = document.getElementById('status');
    const results = document.getElementById('results');
    if (!status || !results) return;

    if (!query) {
      status.textContent = 'Введите запрос в поле поиска.';
      results.textContent = '';
      return;
    }

    const found = entries
      .map(entry => ({entry, rank: score(entry, query)}))
      .filter(item => item.rank > 0)
      .sort((a, b) => b.rank - a.rank || a.entry.title.localeCompare(b.entry.title, 'en'))
      .slice(0, 80);

    status.textContent = found.length
      ? `Найдено: ${found.length}`
      : `Ничего не найдено по запросу «${raw}».`;

    results.textContent = '';
    for (const {entry} of found) {
      const link = document.createElement('a');
      link.className = 'result';
      link.href = entry.href;
      const kind = `<span class="kind">${escapeHtml(entry.kind || '')}</span>`;
      const name = `<div class="name">${mark(entry.title, query)}</div>`;
      const snip = snippet(entry, query);
      link.innerHTML = kind + name + (snip ? `<div class="snippet">${mark(snip, query)}</div>` : '');
      results.appendChild(link);
    }
  }

  buildNavigation();
  setupMenu();
  if (pageId === 'search') runSearch();
})();
