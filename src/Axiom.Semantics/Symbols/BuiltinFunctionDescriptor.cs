namespace Axiom.Semantics;

public enum BuiltinBindingKind
{
    Default,
    Numeric,
    Array
}

public enum BuiltinOperandBindingPolicy
{
    Declared,
    PreserveNumericArguments,
    PromoteNumericArguments,
    ConvertNumericArgumentsToDouble
}

public enum BuiltinResultTypePolicy
{
    Declared,
    SameAsFirstArgument,
    ElementTypeOfFirstArrayArgument,
    ArrayMapSelectorReturnArray,
    ArrayReduceAccumulator,
    PromotedNumeric,
    Integer,
    Double,
    String
}

public sealed class BuiltinParameterDescriptor
{
    public BuiltinParameterDescriptor(string name, TypeSymbol type)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Parameter name must be provided.", nameof(name));
        }

        ArgumentNullException.ThrowIfNull(type);
        Name = name;
        Type = type;
    }

    public string Name { get; }

    public TypeSymbol Type { get; }
}

public sealed class BuiltinFunctionDescriptor
{
    public BuiltinFunctionDescriptor(
        BuiltinFunctionId id,
        string name,
        IReadOnlyList<BuiltinParameterDescriptor> parameters,
        TypeSymbol declaredReturnType,
        BuiltinBindingKind bindingKind = BuiltinBindingKind.Default,
        BuiltinOperandBindingPolicy operandBindingPolicy = BuiltinOperandBindingPolicy.Declared,
        BuiltinResultTypePolicy resultTypePolicy = BuiltinResultTypePolicy.Declared)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Builtin name must be provided.", nameof(name));
        }

        ArgumentNullException.ThrowIfNull(parameters);
        ArgumentNullException.ThrowIfNull(declaredReturnType);

        Id = id;
        Name = name;
        Parameters = parameters;
        DeclaredReturnType = declaredReturnType;
        BindingKind = bindingKind;
        OperandBindingPolicy = operandBindingPolicy;
        ResultTypePolicy = resultTypePolicy;
    }

    public BuiltinFunctionId Id { get; }

    public string Name { get; }

    public IReadOnlyList<BuiltinParameterDescriptor> Parameters { get; }

    public TypeSymbol DeclaredReturnType { get; }

    public BuiltinBindingKind BindingKind { get; }

    public BuiltinOperandBindingPolicy OperandBindingPolicy { get; }

    public BuiltinResultTypePolicy ResultTypePolicy { get; }

    public FunctionSymbol CreateSymbol()
    {
        return new FunctionSymbol(
            Name,
            CreateParameterSymbols(Parameters.Select(static descriptor => descriptor.Type).ToArray()),
            DeclaredReturnType,
            isBuiltIn: true,
            builtInId: Id);
    }

    public FunctionSymbol CreateResolvedSymbol(IReadOnlyList<TypeSymbol> parameterTypes, TypeSymbol returnType)
    {
        ArgumentNullException.ThrowIfNull(parameterTypes);
        ArgumentNullException.ThrowIfNull(returnType);

        if (parameterTypes.Count != Parameters.Count)
        {
            throw new ArgumentException("Resolved parameter count must match descriptor.", nameof(parameterTypes));
        }

        return new FunctionSymbol(
            Name,
            CreateParameterSymbols(parameterTypes),
            returnType,
            isBuiltIn: true,
            builtInId: Id);
    }

    private IReadOnlyList<ParameterSymbol> CreateParameterSymbols(IReadOnlyList<TypeSymbol> parameterTypes)
    {
        var parameters = new List<ParameterSymbol>(Parameters.Count);

        for (var index = 0; index < Parameters.Count; index++)
        {
            parameters.Add(new ParameterSymbol(Parameters[index].Name, parameterTypes[index], index));
        }

        return parameters;
    }
}
