// У одной цели поиска одна запись. Имена конструкций сохраняются как варианты запроса.
export function mergeSearchDestinations(entries) {
  const byDestination = new Map();
  for (const entry of entries) {
    const previous = byDestination.get(entry.href);
    if (!previous) {
      byDestination.set(entry.href, { ...entry, keywords: [...(entry.keywords || [])], aliases: [entry.title] });
      continue;
    }
    const article = ['Тема', 'Приложение', 'Оглавление'].includes(previous.kind) ? previous
      : ['Тема', 'Приложение', 'Оглавление'].includes(entry.kind) ? entry : previous;
    byDestination.set(entry.href, {
      ...article,
      keywords: [...new Set([...previous.keywords, ...(entry.keywords || []), previous.title, entry.title, previous.description, entry.description])],
      aliases: [...new Set([...previous.aliases, entry.title])]
    });
  }
  return [...byDestination.values()];
}
