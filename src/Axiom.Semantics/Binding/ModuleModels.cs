using Axiom.Syntax;

namespace Axiom.Semantics;

/// <summary>
/// Исходная единица модуля: имя модуля и разобранное дерево файла.
/// Имя модуля — имя файла без расширения; сравнение регистронезависимое.
/// </summary>
public sealed class ModuleSyntax
{
    public ModuleSyntax(string name, ProgramSyntax syntax)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Module name must be provided.", nameof(name));
        }

        ArgumentNullException.ThrowIfNull(syntax);

        Name = name;
        Syntax = syntax;
    }

    public string Name { get; }

    public ProgramSyntax Syntax { get; }

    public IReadOnlyList<ImportStatementSyntax> Imports =>
        Syntax.Members.OfType<ImportStatementSyntax>().ToArray();
}

/// <summary>
/// Связанный модуль: имя, объявленные символы и их доступность.
/// </summary>
public sealed class ModuleSymbol
{
    public ModuleSymbol(
        string name,
        IReadOnlyDictionary<string, FunctionSymbol> publicFunctions,
        IReadOnlyDictionary<string, UserDefinedTypeSymbol> publicTypes,
        IReadOnlyDictionary<string, VariableSymbol> publicVariables,
        IReadOnlySet<string> privateNames,
        IReadOnlyDictionary<string, long>? publicIntegralConstants = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(publicFunctions);
        ArgumentNullException.ThrowIfNull(publicTypes);
        ArgumentNullException.ThrowIfNull(publicVariables);
        ArgumentNullException.ThrowIfNull(privateNames);

        Name = name;
        PublicFunctions = publicFunctions;
        PublicTypes = publicTypes;
        PublicVariables = publicVariables;
        PrivateNames = privateNames;
        PublicIntegralConstants = publicIntegralConstants ?? new Dictionary<string, long>();
    }

    public string Name { get; }

    public IReadOnlyDictionary<string, FunctionSymbol> PublicFunctions { get; }

    public IReadOnlyDictionary<string, UserDefinedTypeSymbol> PublicTypes { get; }

    public IReadOnlyDictionary<string, VariableSymbol> PublicVariables { get; }

    public IReadOnlySet<string> PrivateNames { get; }

    public IReadOnlyDictionary<string, long> PublicIntegralConstants { get; }

    public bool IsPrivateName(string name) => PrivateNames.Contains(name);
}
