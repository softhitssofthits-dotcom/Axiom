// Axiom регистрирует собственный язык; встроенные языки Monaco не требуются.
import * as monaco from 'monaco-editor/editor';
import 'monaco-editor/features/register.all';
// Дополнительные регистрации editor.main сохраняют команды и семантические цвета.
import 'monaco-editor/editor/browser/coreCommands';
import 'monaco-editor/editor/contrib/caretOperations/browser/caretOperations';
import 'monaco-editor/editor/contrib/dropOrPasteInto/browser/copyPasteContribution';
import 'monaco-editor/editor/contrib/find/browser/findController';
import 'monaco-editor/editor/contrib/gotoSymbol/browser/goToCommands';
import 'monaco-editor/editor/contrib/gotoError/browser/markerSelectionStatus';
import 'monaco-editor/editor/contrib/semanticTokens/browser/documentSemanticTokens';
import 'monaco-editor/editor/contrib/suggest/browser/suggestController';
import 'monaco-editor/editor/common/standaloneStrings';
import '../../node_modules/monaco-editor/esm/vs/base/browser/ui/codicons/codicon/codicon-modifiers.css';

self.MonacoEnvironment = {
  getWorker() {
    return new Worker(new URL('./editor.worker.js', import.meta.url), { type: 'module' });
  },
};

export { monaco };
