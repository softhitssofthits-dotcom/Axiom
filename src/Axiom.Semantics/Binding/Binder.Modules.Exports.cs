using Axiom.Core;
using Axiom.Syntax;

namespace Axiom.Semantics;

// Импорт, экспорт и приватность модульных имён: внедрение открытых имён
// зависимостей, таблица экспорта модуля и диагностика закрытых имён.
public sealed partial class Binder
{
    private void InjectImportedSymbols()
    {
        foreach (var import in _imports)
        {
            foreach (var (name, function) in import.PublicFunctions)
            {
                if (!_injectedFunctionNames.Add(name))
                {
                    Report(
                        DiagnosticCodes.AmbiguousModuleMember,
                        $"Имя '{name}' экспортируется несколькими импортированными модулями. Переименуйте объявление или измените состав импортов.",
                        _moduleLocation);
                    continue;
                }

                if (!_globalScope.TryDeclareFunction(function))
                {
                    Report(
                        DiagnosticCodes.ImportedNameConflict,
                        $"Импортируемое имя '{name}' из модуля '{import.Name}' конфликтует с уже доступным именем.",
                        _moduleLocation);
                }
            }

            foreach (var (name, variable) in import.PublicVariables)
            {
                if (!_injectedVariableNames.Add(name))
                {
                    Report(
                        DiagnosticCodes.AmbiguousModuleMember,
                        $"Имя '{name}' экспортируется несколькими импортированными модулями. Переименуйте объявление или измените состав импортов.",
                        _moduleLocation);
                    continue;
                }

                if (!_globalScope.TryDeclareVariable(variable))
                {
                    Report(
                        DiagnosticCodes.ImportedNameConflict,
                        $"Импортируемое имя '{name}' из модуля '{import.Name}' конфликтует с уже доступным именем.",
                        _moduleLocation);
                }
            }

            foreach (var (name, type) in import.PublicTypes)
            {
                if (!_importedTypes.TryAdd(name, type))
                {
                    Report(
                        DiagnosticCodes.AmbiguousModuleMember,
                        $"Тип '{name}' экспортируется несколькими импортированными модулями. Переименуйте объявление или измените состав импортов.",
                        _moduleLocation);
                }
            }

            foreach (var (name, value) in import.PublicIntegralConstants)
            {
                _globalIntegralConstants.TryAdd(name, value);
            }
        }
    }

    /// <summary>
    /// Запоминает доступность глобальной переменной: модульный путь передаёт
    /// сюда решение из объявления, однофайловый путь эту регистрацию не выполняет.
    /// </summary>
    private void RegisterGlobalVariableVisibility(BoundStatement statement, bool isPublic)
    {
        switch (statement)
        {
            case BoundVariableDeclarationStatement declaration:
                _globalVariableVisibility[declaration.Variable] = isPublic;
                break;

            case BoundVariableDeclarationsStatement declarations:
                foreach (var declaration in declarations.Declarations)
                {
                    _globalVariableVisibility[declaration.Variable] = isPublic;
                }

                break;
        }
    }

    private ModuleSymbol CreateModuleSymbol(ModuleSyntax module)
    {
        var publicFunctions = new Dictionary<string, FunctionSymbol>(StringComparer.OrdinalIgnoreCase);
        var privateNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var publicTypes = new Dictionary<string, UserDefinedTypeSymbol>(StringComparer.OrdinalIgnoreCase);
        var publicVariables = new Dictionary<string, VariableSymbol>(StringComparer.OrdinalIgnoreCase);

        foreach (var member in module.Syntax.Members)
        {
            switch (member)
            {
                case SubDeclarationSyntax subDeclaration:
                    RegisterFunction(subDeclaration.VisibilityKeyword, _declaredFunctions[subDeclaration]);
                    break;

                case FunctionDeclarationSyntax functionDeclaration:
                    RegisterFunction(functionDeclaration.VisibilityKeyword, _declaredFunctions[functionDeclaration]);
                    break;

                case TypeDeclarationSyntax typeDeclaration:
                {
                    if (!_declaredTypes.TryGetValue(typeDeclaration.Identifier.Text, out var type))
                    {
                        break;
                    }

                    if (IsPublic(typeDeclaration.VisibilityKeyword))
                    {
                        publicTypes[type.Name] = type;
                    }
                    else
                    {
                        privateNames.Add(type.Name);
                    }

                    break;
                }
            }
        }

        foreach (var (variable, isPublic) in _globalVariableVisibility)
        {
            if (isPublic)
            {
                publicVariables[variable.Name] = variable;
            }
            else
            {
                privateNames.Add(variable.Name);
            }
        }

        var constants = _boundIntegralConstants.Where(pair => publicVariables.ContainsKey(pair.Key.Name))
            .ToDictionary(pair => pair.Key.Name, pair => pair.Value, StringComparer.OrdinalIgnoreCase);
        return new ModuleSymbol(module.Name, publicFunctions, publicTypes, publicVariables, privateNames, constants);

        void RegisterFunction(SyntaxToken? visibilityKeyword, FunctionSymbol function)
        {
            if (IsPublic(visibilityKeyword))
            {
                publicFunctions[function.Name] = function;
            }
            else
            {
                privateNames.Add(function.Name);
            }
        }
    }

    private static bool IsPublic(SyntaxToken? visibilityKeyword)
    {
        return visibilityKeyword?.Kind == SyntaxKind.PublicKeyword;
    }

    private bool TryReportPrivateModuleMember(string name, TextLocation location)
    {
        foreach (var import in _imports)
        {
            if (import.IsPrivateName(name))
            {
                Report(
                    DiagnosticCodes.PrivateModuleMember,
                    $"Имя '{name}' объявлено в модуле '{import.Name}' как PRIVATE и недоступно снаружи. Объявите его как PUBLIC.",
                    location);
                return true;
            }
        }

        return false;
    }
}
