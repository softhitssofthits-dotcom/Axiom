(function () {
  const chapters = window.AxiomManualChapters || [];
  const pageName = location.pathname.split('/').pop() || 'index.html';
  const navHost = document.getElementById('manual-nav');
  const pagerHost = document.getElementById('page-pager');
  const items = chapters.flatMap(chapter => chapter.items);
  const current = items.find(item => item.href.split('#')[0] === pageName);
  const chapter = chapters.find(chapter => chapter.items.includes(current));
  function link(item, className = 'manual-nav__link') {
    const a = document.createElement('a');
    a.href = item.href; a.textContent = item.label; a.className = className;
    if (item === current) a.setAttribute('aria-current', 'page');
    return a;
  }
  if (navHost) {
    navHost.className = 'manual-nav';
    const home = link({ href: 'index.html', label: 'Справочник Axiom' });
    if (pageName === 'index.html') home.setAttribute('aria-current', 'page');
    navHost.append(home);
    for (const section of chapters) {
      const details = document.createElement('details');
      details.className = 'manual-nav__section';
      if (section.number) details.dataset.chapter = section.number;
      details.open = section === chapter;
      const summary = document.createElement('summary');
      summary.textContent = (section.number ? section.number + '. ' : '') + section.title;
      const list = document.createElement('ul');
      for (const item of section.items) { const li = document.createElement('li'); li.append(link(item)); list.append(li); }
      details.append(summary, list); navHost.append(details);
    }
  }
  const breadcrumbs = document.querySelector('.breadcrumbs');
  if (chapter && current && breadcrumbs) {
    breadcrumbs.replaceChildren(link({ href: 'index.html', label: 'Справочник' }, 'breadcrumb-link'));
    if (chapter.items[0] !== current) breadcrumbs.append(document.createTextNode(' / '), link(chapter.items[0], 'breadcrumb-link'));
    const name = document.createElement('span'); name.textContent = current.label;
    breadcrumbs.append(document.createTextNode(' / '), name);
  }
  if (pagerHost && current) {
    const index = items.indexOf(current);
    for (const [item, direction] of [[items[index - 1], 'Предыдущая тема'], [items[index + 1], 'Следующая тема']]) {
      if (!item) continue;
      const a = document.createElement('a'); a.href = item.href; a.textContent = direction + ': ' + item.label; a.className = 'pager__link'; pagerHost.append(a);
    }
  }
  const toc = document.querySelector('.article-toc');
  if (toc) {
    const headings = [...document.querySelectorAll('main > section > h2')];
    if (headings.length > 1 && headings.length < 40) {
      const title = document.createElement('strong'); title.textContent = 'В этой статье'; toc.append(title);
      const list = document.createElement('ul');
      headings.forEach((heading, index) => {
        if (!heading.parentElement.id) heading.parentElement.id = 'section-' + (index + 1);
        const li = document.createElement('li'); li.append(link({ href: '#' + heading.parentElement.id, label: heading.textContent })); list.append(li);
      }); toc.append(list);
    } else toc.hidden = true;
  }
  const toggle = document.querySelector('[data-nav-toggle]');
  function closeNav() { document.body.classList.remove('nav-open'); toggle?.setAttribute('aria-expanded', 'false'); }
  toggle?.addEventListener('click', () => { const open = document.body.classList.toggle('nav-open'); toggle.setAttribute('aria-expanded', String(open)); });
  navHost?.addEventListener('click', event => { if (event.target.closest('a')) closeNav(); });
  document.addEventListener('keydown', event => {
    if (event.key === 'Escape' && document.body.classList.contains('nav-open')) { closeNav(); toggle?.focus(); }
    if (event.altKey && event.code === 'Slash') { event.preventDefault(); document.getElementById('manual-search-input')?.focus(); }
  });
})();
