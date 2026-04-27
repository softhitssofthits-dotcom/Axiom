namespace Axiom.Semantics;

public static partial class BuiltinFunctions
{
    private static readonly IReadOnlyList<BuiltinFunctionDescriptor> AllDescriptors =
    [
        .. GetArrayDescriptors(),
        .. GetBasicStringDescriptors(),
        .. GetCharStringDescriptors(),
        .. GetTrimStringDescriptors(),
        .. GetSearchStringDescriptors(),
        .. GetFillStringDescriptors(),
        .. GetEditStringDescriptors(),
        .. GetStringArrayDescriptors(),
        .. GetRegexDescriptors(),
        .. GetDateTimeDescriptors(),
        .. GetFileIoDescriptors(),
        .. GetFileSystemDescriptors(),
        .. GetConversionDescriptors(),
        .. GetNumericDescriptors()
    ];

    private static readonly IReadOnlyDictionary<BuiltinFunctionId, BuiltinFunctionDescriptor> DescriptorsById =
        AllDescriptors.ToDictionary(static descriptor => descriptor.Id);

    private static readonly IReadOnlyDictionary<string, IReadOnlyList<BuiltinFunctionDescriptor>> DescriptorsByName =
        AllDescriptors
            .GroupBy(static descriptor => descriptor.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                static group => group.Key,
                static group => (IReadOnlyList<BuiltinFunctionDescriptor>)group.ToArray(),
                StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyList<FunctionSymbol> All { get; } =
        DescriptorsByName.Values
            .Select(static family => family[0].CreateSymbol())
            .ToArray();

    public static bool TryGetDescriptor(FunctionSymbol function, out BuiltinFunctionDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(function);

        if (!function.IsBuiltIn || function.BuiltinId is null)
        {
            descriptor = null!;
            return false;
        }

        return DescriptorsById.TryGetValue(function.BuiltinId.Value, out descriptor!);
    }

    public static bool TryGetDescriptor(BuiltinFunctionId id, out BuiltinFunctionDescriptor descriptor)
    {
        return DescriptorsById.TryGetValue(id, out descriptor!);
    }

    public static bool TryGetDescriptors(string name, out IReadOnlyList<BuiltinFunctionDescriptor> descriptors)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return DescriptorsByName.TryGetValue(name, out descriptors!);
    }

    private static BuiltinFunctionDescriptor CreateDefault(
        BuiltinFunctionId id,
        string name,
        TypeSymbol returnType,
        IReadOnlyList<BuiltinParameterDescriptor> parameters)
    {
        return new BuiltinFunctionDescriptor(id, name, parameters, returnType);
    }

    private static BuiltinFunctionDescriptor CreateNumeric(
        BuiltinFunctionId id,
        string name,
        TypeSymbol returnType,
        BuiltinOperandBindingPolicy operandBindingPolicy,
        BuiltinResultTypePolicy resultTypePolicy,
        IReadOnlyList<BuiltinParameterDescriptor> parameters)
    {
        return new BuiltinFunctionDescriptor(
            id,
            name,
            parameters,
            returnType,
            bindingKind: BuiltinBindingKind.Numeric,
            operandBindingPolicy: operandBindingPolicy,
            resultTypePolicy: resultTypePolicy);
    }

    private static BuiltinFunctionDescriptor CreateArrayInfo(
        BuiltinFunctionId id,
        string name,
        TypeSymbol returnType)
    {
        return new BuiltinFunctionDescriptor(
            id,
            name,
            [Parameter("array", ArrayTypeSymbol.Create(TypeSymbol.Integer))],
            returnType,
            bindingKind: BuiltinBindingKind.Array);
    }

    private static BuiltinFunctionDescriptor CreateArrayBoolean(
        BuiltinFunctionId id,
        string name,
        IReadOnlyList<BuiltinParameterDescriptor> additionalParameters)
    {
        return new BuiltinFunctionDescriptor(
            id,
            name,
            [Parameter("array", ArrayTypeSymbol.Create(TypeSymbol.Integer)), .. additionalParameters],
            TypeSymbol.Boolean,
            bindingKind: BuiltinBindingKind.Array);
    }

    private static BuiltinFunctionDescriptor CreateArrayInteger(
        BuiltinFunctionId id,
        string name,
        IReadOnlyList<BuiltinParameterDescriptor> additionalParameters)
    {
        return new BuiltinFunctionDescriptor(
            id,
            name,
            [Parameter("array", ArrayTypeSymbol.Create(TypeSymbol.Integer)), .. additionalParameters],
            TypeSymbol.Integer,
            bindingKind: BuiltinBindingKind.Array);
    }

    private static BuiltinFunctionDescriptor CreateArrayIntegerArray(
        BuiltinFunctionId id,
        string name,
        IReadOnlyList<BuiltinParameterDescriptor> additionalParameters)
    {
        return new BuiltinFunctionDescriptor(
            id,
            name,
            [Parameter("array", ArrayTypeSymbol.Create(TypeSymbol.Integer)), .. additionalParameters],
            ArrayTypeSymbol.Create(TypeSymbol.Integer),
            bindingKind: BuiltinBindingKind.Array);
    }

    private static BuiltinFunctionDescriptor CreateArrayVoid(
        BuiltinFunctionId id,
        string name)
    {
        return CreateArrayVoid(id, name, []);
    }

    private static BuiltinFunctionDescriptor CreateArrayVoid(
        BuiltinFunctionId id,
        string name,
        IReadOnlyList<BuiltinParameterDescriptor> additionalParameters)
    {
        return new BuiltinFunctionDescriptor(
            id,
            name,
            [Parameter("array", ArrayTypeSymbol.Create(TypeSymbol.Integer)), .. additionalParameters],
            TypeSymbol.Void,
            bindingKind: BuiltinBindingKind.Array);
    }

    private static BuiltinFunctionDescriptor CreateArrayElementType(
        BuiltinFunctionId id,
        string name)
    {
        return CreateArrayElementType(id, name, []);
    }

    private static BuiltinFunctionDescriptor CreateArrayElementType(
        BuiltinFunctionId id,
        string name,
        IReadOnlyList<BuiltinParameterDescriptor> additionalParameters)
    {
        return new BuiltinFunctionDescriptor(
            id,
            name,
            [Parameter("array", ArrayTypeSymbol.Create(TypeSymbol.Integer)), .. additionalParameters],
            TypeSymbol.Integer,
            bindingKind: BuiltinBindingKind.Array,
            resultTypePolicy: BuiltinResultTypePolicy.ElementTypeOfFirstArrayArgument);
    }

    private static BuiltinFunctionDescriptor CreateArraySameType(
        BuiltinFunctionId id,
        string name)
    {
        return CreateArraySameType(id, name, []);
    }

    private static BuiltinFunctionDescriptor CreateArraySameType(
        BuiltinFunctionId id,
        string name,
        IReadOnlyList<BuiltinParameterDescriptor> additionalParameters)
    {
        return new BuiltinFunctionDescriptor(
            id,
            name,
            [Parameter("array", ArrayTypeSymbol.Create(TypeSymbol.Integer)), .. additionalParameters],
            ArrayTypeSymbol.Create(TypeSymbol.Integer),
            bindingKind: BuiltinBindingKind.Array,
            resultTypePolicy: BuiltinResultTypePolicy.SameAsFirstArgument);
    }

    private static BuiltinParameterDescriptor Parameter(string name, TypeSymbol type)
    {
        return new BuiltinParameterDescriptor(name, type);
    }
}
