using Axiom.Core;

namespace Axiom.Semantics;

// Граф импортов, порядок обхода и обнаружение циклов. Таблица модулей и
// координация проходов остаются в Binder.Modules.cs.
public sealed partial class Binder
{
    /// <summary>
    /// Разрешает IMPORT каждого модуля в существующие модули и сообщает
    /// о самоимпорте и о неизвестном модуле. Повтор одного IMPORT в списке
    /// зависимостей не создаёт второе ребро.
    /// </summary>
    private static Dictionary<string, IReadOnlyList<ModuleSyntax>> BuildImportGraph(
        IReadOnlyDictionary<string, ModuleSyntax> modulesByName,
        DiagnosticBag result)
    {
        var importGraph = new Dictionary<string, IReadOnlyList<ModuleSyntax>>(StringComparer.OrdinalIgnoreCase);
        foreach (var module in modulesByName.Values)
        {
            var resolved = new List<ModuleSyntax>();
            foreach (var import in module.Imports)
            {
                if (string.Equals(import.ModuleName, module.Name, StringComparison.OrdinalIgnoreCase))
                {
                    result.Report(
                        DiagnosticCodes.CyclicModuleImport,
                        $"Модуль '{module.Name}' импортирует сам себя.",
                        import.NameToken.Location);
                    continue;
                }

                if (!modulesByName.TryGetValue(import.ModuleName, out var imported))
                {
                    result.Report(
                        DiagnosticCodes.UnknownModule,
                        $"Модуль '{module.Name}' импортирует несуществующий модуль '{import.ModuleName}'.",
                        import.NameToken.Location);
                    continue;
                }

                if (!resolved.Contains(imported))
                {
                    resolved.Add(imported);
                }
            }

            importGraph[module.Name] = resolved;
        }

        return importGraph;
    }

    /// <summary>
    /// Возвращает порядок инициализации: зависимости раньше импортирующих
    /// модулей. Обход начинается от точки входа, поэтому достижимость считается
    /// до посещения остальных модулей; недостижимые модули тоже проверяются,
    /// но их операторы не попадают в общий BoundProgram.
    /// </summary>
    private static List<ModuleSyntax> OrderModules(
        ModuleSyntax entryModule,
        IReadOnlyDictionary<string, ModuleSyntax> modulesByName,
        IReadOnlyDictionary<string, IReadOnlyList<ModuleSyntax>> importGraph,
        DiagnosticBag result,
        out HashSet<string> reachableModules)
    {
        var order = new List<ModuleSyntax>();
        var states = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        Visit(entryModule);
        reachableModules = order.Select(module => module.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var module in modulesByName.Values)
        {
            Visit(module);
        }

        return order;

        void Visit(ModuleSyntax module)
        {
            if (states.TryGetValue(module.Name, out var state))
            {
                if (state == 1)
                {
                    result.Report(
                        DiagnosticCodes.CyclicModuleImport,
                        $"Циклический импорт модулей: '{module.Name}' уже находится в цепочке инициализации.",
                        GetModuleLocation(module));
                }

                return;
            }

            states[module.Name] = 1;
            foreach (var dependency in importGraph[module.Name])
            {
                Visit(dependency);
            }

            states[module.Name] = 2;
            order.Add(module);
        }
    }
}
