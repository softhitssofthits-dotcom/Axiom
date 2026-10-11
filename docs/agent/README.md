# Краткий вход в проект

Это вспомогательная навигация после обязательного чтения проектных документов. [CURRENT_STATE.md](CURRENT_STATE.md) указывает точку остановки, [ARCHITECTURE_MAP.md](ARCHITECTURE_MAP.md) — исходники, [PLAYBOOKS.md](PLAYBOOKS.md) — типовые действия.

`powershell -NoProfile -File scripts/ai-context.ps1 -StdoutOnly` читает действующие PLANS4/WORKLOG5 и строит краткую сводку. Тема задаётся параметром `-Topic`. Временные результаты сохраняются только в `.artifacts/`.

Этот слой не заменяет PROJECT, SPECIFICATION3, ARCHITECTURE, PLANS4 и WORKLOG5 и не создаёт отдельной очереди задач.
