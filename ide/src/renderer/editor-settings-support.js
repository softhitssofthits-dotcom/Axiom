/* Возможности штатного языка Axiom. Каталог сохраняется для будущих провайдеров. */
(function(root){
 const reasons={};
 const add=(keys,ru,en)=>keys.split(' ').forEach(key=>reasons[key]={ru:'Пока не поддерживается: '+ru,en:'Not supported yet: '+en});
 add('formatOnType formatOnPaste','не зарегистрирован провайдер форматирования Monaco. Команда форматирования Axiom работает отдельно.','no Monaco formatting provider is registered. Axiom formatting commands work separately.');
 add('linkedEditing renameOnType','нет провайдера связанного редактирования.','no linked editing provider.');
 add('codeLens codeLensFontFamily codeLensFontSize','нет провайдера CodeLens.','no CodeLens provider.');
 add('lightbulb.enabled','нет провайдера действий исправления.','no code action provider.');
 add('inlayHints.enabled inlayHints.fontSize inlayHints.fontFamily inlayHints.padding inlayHints.maximumLength','нет провайдера встроенных пояснений.','no inlay hint provider.');
 // Monaco подключает общие провайдеры продолжений и подсветки слов самостоятельно.
 add('definitionLinkOpensInPeek peekWidgetDefaultFocus stablePeek gotoLocation.multiple gotoLocation.multipleDefinitions gotoLocation.multipleTypeDefinitions gotoLocation.multipleDeclarations gotoLocation.multipleImplementations gotoLocation.multipleReferences gotoLocation.multipleTests','нет провайдера соответствующих переходов по символам.','no corresponding symbol navigation provider.');
 add('autoClosingComments','в Axiom нет парных блочных комментариев.','Axiom has no paired block comments.');
 add('showDeprecated','диагностики Axiom пока не помечают сущности как устаревшие.','Axiom diagnostics do not mark deprecated entities yet.');
 add('minimap.showRegionSectionHeaders','в конфигурации Axiom пока не заданы маркеры областей сворачивания.','Axiom folding region markers are not configured yet.');
 add('inlineCompletionsAccessibilityVerbose','в IDE пока не подключена команда доступного просмотра продолжений.','the IDE does not expose the accessible view command for inline completions.');
 add('inlineSuggest.suppressSuggestions','независимый провайдер продолжений Axiom пока не подключён; штатные продолжения работают через режим Inline автоматических подсказок.','no independent Axiom inline provider is connected; built-in continuations use Inline quick suggestions.');
 add('dropIntoEditor.showDropSelector','штатные провайдеры сейчас предлагают один способ вставки; связь Monaco с каталогом проекта для относительных путей пока не подключена.','built-in providers currently offer one edit; Monaco workspace integration for relative paths is not connected yet.');
 add('pasteAs.showPasteSelector','нет нескольких способов автоматической вставки; команда Paste As в текущем режиме ввода IDE вставляет обычный текст.','automatic paste has no alternative edits; Paste As uses plain text in the current IDE input mode.');
 add('suggest.shareSuggestSelections','в IDE пока не подключено постоянное хранилище общей истории дополнений Monaco.','the IDE does not connect persistent shared Monaco suggestion history.');
 add('overtypeOnPaste overtypeCursorStyle','в IDE не подключена команда переключения режима замещения.','the IDE does not expose an overtype mode command.');
 add('foldingImportsByDefault','штатное сворачивание Axiom не выделяет группы импортов.','Axiom folding does not identify import groups.');
 add('bracketPairColorization.independentColorPoolPerBracketType','в Axiom зарегистрирован только один вид скобок: круглые.','only parentheses are registered in Axiom.');
 add('suggestOnTriggerCharacters','провайдер дополнений Axiom не задаёт символы вызова.','the Axiom completion provider defines no trigger characters.');
 add('acceptSuggestionOnCommitCharacter','провайдер дополнений Axiom не задаёт символы принятия.','the Axiom completion provider defines no commit characters.');
 const kinds='Methods Constructors Fields Interfaces Modules Properties Events Operators Units Values Constants Enums EnumMembers Colors Files References Folders TypeParameters Issues Users Deprecated';
 add(kinds.split(' ').map(k=>'suggest.show'+k).join(' '),'провайдер дополнений Axiom пока не выдаёт эту категорию.','the Axiom completion provider does not emit this category yet.');
 const api={reasons,apply(entries){for(const e of entries)if(reasons[e.key]){e.available=false;e.unavailableRu=reasons[e.key].ru;e.unavailableEn=reasons[e.key].en;e.unsupportedByAxiom=true;}return entries;}};
 root.EditorSettingsSupport=api;if(typeof module!=='undefined')module.exports=api;
})(typeof window==='undefined'?globalThis:window);
