using System.Text.Json;
using System.Text.Json.Serialization;
using Axiom.Core;
using Axiom.Semantics;

namespace Axiom.Bytecode;

public static class BytecodeModuleSerializer
{
    public const int CurrentArtifactSchemaVersion = 2;

    private const string SupportedCompatibilityToken = "axiom-bytecode-v1";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static BytecodeModuleArtifact ToArtifact(BytecodeModule module)
    {
        ArgumentNullException.ThrowIfNull(module);
        var context = new SerializationContext();

        return new BytecodeModuleArtifact(
            ArtifactSchemaVersion: CurrentArtifactSchemaVersion,
            Header: ToArtifact(module.Header),
            ConstantPool: module.ConstantPool.Entries.Select(ToArtifact).ToArray(),
            Routines: module.Routines.Select(routine => ToArtifact(routine, context)).ToArray(),
            Globals: module.Globals.Select(global => ToArtifact(global, context)).ToArray(),
            Builtins: module.Builtins.Select(builtin => ToArtifact(builtin, context)).ToArray(),
            TypeDefinitions: context.UserDefinedTypes.Select(ToArtifact).ToArray(),
            DebugInfo: module.DebugInfo is null ? null : ToArtifact(module.DebugInfo),
            EntryPointRoutineId: module.EntryPointRoutineId);
    }

    public static string SerializeToJson(BytecodeModule module)
    {
        var artifact = ToArtifact(module);
        return JsonSerializer.Serialize(artifact, JsonOptions);
    }

    public static BytecodeModuleArtifact DeserializeArtifactFromJson(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        return JsonSerializer.Deserialize<BytecodeModuleArtifact>(json, JsonOptions)
            ?? throw new InvalidOperationException("JSON-снимок байткода не содержит корневой объект модуля.");
    }

    public static BytecodeModuleArtifact DeserializeArtifactFromJson(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        return JsonSerializer.Deserialize<BytecodeModuleArtifact>(stream, JsonOptions)
            ?? throw new InvalidOperationException("JSON-снимок байткода не содержит корневой объект модуля.");
    }

    public static BytecodeModule DeserializeModuleFromJson(string json)
    {
        return FromArtifact(DeserializeArtifactFromJson(json));
    }

    public static BytecodeModule DeserializeModuleFromJson(Stream stream)
    {
        return FromArtifact(DeserializeArtifactFromJson(stream));
    }

    public static BytecodeModule FromArtifact(BytecodeModuleArtifact artifact)
    {
        ArgumentNullException.ThrowIfNull(artifact);
        ValidateArtifactCompatibility(artifact);

        var functionSymbols = new Dictionary<string, FunctionSymbol>(StringComparer.Ordinal);
        var variableSymbols = new Dictionary<string, VariableSymbol>(StringComparer.Ordinal);
        var userDefinedTypes = CreateUserDefinedTypes(artifact.TypeDefinitions);

        return new BytecodeModule(
            FromArtifact(artifact.Header),
            new ConstantPool(artifact.ConstantPool.OrderBy(static entry => entry.Index).Select(FromArtifact).ToArray()),
            artifact.Routines.Select(routine => FromArtifact(routine, functionSymbols, variableSymbols, userDefinedTypes)).ToArray(),
            artifact.Globals.Select(global => FromArtifact(global, userDefinedTypes)).ToArray(),
            artifact.Builtins.Select(builtin => FromArtifact(builtin, functionSymbols, variableSymbols, userDefinedTypes)).ToArray(),
            artifact.DebugInfo is null ? null : FromArtifact(artifact.DebugInfo),
            artifact.EntryPointRoutineId);
    }

    private static void ValidateArtifactCompatibility(BytecodeModuleArtifact artifact)
    {
        if (artifact.ArtifactSchemaVersion is int schemaVersion && schemaVersion != CurrentArtifactSchemaVersion)
        {
            throw new InvalidOperationException(
                $"JSON-снимок байткода имеет неподдерживаемую версию схемы {schemaVersion}. Поддерживается версия {CurrentArtifactSchemaVersion}.");
        }

        if (artifact.Header is null)
        {
            throw new InvalidOperationException("JSON-снимок байткода не содержит заголовок модуля.");
        }

        if (!Enum.TryParse<BytecodeFormatVersion>(artifact.Header.FormatVersion, out var formatVersion) ||
            formatVersion != BytecodeFormatVersion.V1)
        {
            throw new InvalidOperationException(
                $"JSON-снимок байткода имеет неподдерживаемую версию формата '{artifact.Header.FormatVersion}'.");
        }

        if (!string.Equals(artifact.Header.CompatibilityToken, SupportedCompatibilityToken, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"JSON-снимок байткода имеет несовместимый токен '{artifact.Header.CompatibilityToken}'.");
        }
    }

    public static void SerializeToJson(Stream stream, BytecodeModule module)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var artifact = ToArtifact(module);
        JsonSerializer.Serialize(stream, artifact, JsonOptions);
    }

    private static BytecodeHeader FromArtifact(BytecodeHeaderArtifact header)
    {
        return new BytecodeHeader(
            Enum.Parse<BytecodeFormatVersion>(header.FormatVersion),
            header.LanguageVersion,
            (BytecodeModuleFlags)header.Flags,
            header.CompatibilityToken);
    }

    private static BytecodeConstant FromArtifact(BytecodeConstantArtifact constant)
    {
        var kind = Enum.Parse<BytecodeConstantKind>(constant.Kind);
        return new BytecodeConstant(kind, RestoreConstantValue(kind, constant.Value));
    }

    private static BytecodeRoutine FromArtifact(
        BytecodeRoutineArtifact routine,
        Dictionary<string, FunctionSymbol> functionSymbols,
        Dictionary<string, VariableSymbol> variableSymbols,
        IReadOnlyDictionary<string, UserDefinedTypeSymbol> userDefinedTypes)
    {
        return new BytecodeRoutine(
            routine.Id,
            routine.Name,
            FromArtifact(routine.Location),
            ResolveType(routine.ResultType, userDefinedTypes),
            routine.ParameterCount,
            routine.LocalSlotCount,
            routine.Instructions.OrderBy(static instruction => instruction.Index).Select(FromArtifact).ToArray(),
            routine.IsEntryPoint,
            routine.Symbol is null ? null : GetOrCreateFunctionSymbol(routine.Symbol, functionSymbols, variableSymbols, userDefinedTypes),
            routine.ParameterVariables.Select(variable => GetOrCreateVariableSymbol(variable, variableSymbols, userDefinedTypes)).ToArray(),
            routine.LocalVariables.Select(variable => GetOrCreateVariableSymbol(variable, variableSymbols, userDefinedTypes)).ToArray(),
            routine.CapturedVariables.Select(variable => GetOrCreateVariableSymbol(variable, variableSymbols, userDefinedTypes)).ToArray());
    }

    private static BytecodeInstruction FromArtifact(BytecodeInstructionArtifact instruction)
    {
        return new BytecodeInstruction(
            Enum.Parse<Opcode>(instruction.Opcode),
            instruction.Operands.ToArray());
    }

    private static BytecodeGlobal FromArtifact(
        BytecodeGlobalArtifact global,
        IReadOnlyDictionary<string, UserDefinedTypeSymbol> userDefinedTypes)
    {
        return new BytecodeGlobal(
            global.Slot,
            global.Name,
            ResolveType(global.Type, userDefinedTypes),
            global.IsReadOnly,
            Enum.Parse<GlobalInitializationStrategy>(global.InitializationStrategy),
            global.FixedStringLength);
    }

    private static BuiltinBinding FromArtifact(
        BuiltinBindingArtifact builtin,
        Dictionary<string, FunctionSymbol> functionSymbols,
        Dictionary<string, VariableSymbol> variableSymbols,
        IReadOnlyDictionary<string, UserDefinedTypeSymbol> userDefinedTypes)
    {
        return new BuiltinBinding(
            builtin.Id,
            Enum.Parse<BuiltinFunctionId>(builtin.CanonicalId),
            builtin.Name,
            builtin.ParameterCount,
            builtin.ReturnsValue,
            builtin.Function is null ? null : GetOrCreateFunctionSymbol(builtin.Function, functionSymbols, variableSymbols, userDefinedTypes));
    }

    private static BytecodeDebugInfo FromArtifact(BytecodeDebugInfoArtifact debugInfo)
    {
        return new BytecodeDebugInfo(
            debugInfo.InstructionMap.Select(FromArtifact).ToArray(),
            debugInfo.Routines.Select(FromArtifact).ToArray());
    }

    private static SourceInstructionMap FromArtifact(SourceInstructionMapArtifact map)
    {
        return new SourceInstructionMap(
            map.RoutineId,
            map.InstructionIndex,
            FromArtifact(map.Location));
    }

    private static BytecodeRoutineDebugInfo FromArtifact(BytecodeRoutineDebugInfoArtifact routine)
    {
        return new BytecodeRoutineDebugInfo(
            routine.RoutineId,
            routine.ParameterNames,
            routine.LocalNames,
            routine.BlockRanges.Select(FromArtifact).ToArray());
    }

    private static BytecodeBlockDebugRange FromArtifact(BytecodeBlockDebugRangeArtifact range)
    {
        return new BytecodeBlockDebugRange(
            range.Name,
            range.StartInstructionIndex,
            range.EndInstructionIndexExclusive);
    }

    private static TextLocation FromArtifact(TextLocationArtifact location)
    {
        return new TextLocation(new TextSpan(location.SpanStart, location.SpanLength), location.Line, location.Column);
    }

    private static BytecodeHeaderArtifact ToArtifact(BytecodeHeader header)
    {
        return new BytecodeHeaderArtifact(
            FormatVersion: header.FormatVersion.ToString(),
            LanguageVersion: header.LanguageVersion,
            Flags: (int)header.Flags,
            CompatibilityToken: header.CompatibilityToken);
    }

    private static BytecodeTypeDefinitionArtifact ToArtifact(UserDefinedTypeSymbol type)
    {
        return new BytecodeTypeDefinitionArtifact(
            type.Name,
            type.Fields.Select(ToArtifact).ToArray());
    }

    private static BytecodeTypeFieldArtifact ToArtifact(TypeFieldSymbol field)
    {
        return new BytecodeTypeFieldArtifact(
            field.Name,
            field.Type.Name,
            field.FixedStringLength,
            field.FixedArrayLowerBounds,
            field.FixedArrayUpperBounds,
            field.HasDefaultValue,
            NormalizeTypeFieldDefaultValue(field));
    }

    private static BytecodeConstantArtifact ToArtifact(BytecodeConstant constant, int index)
    {
        return new BytecodeConstantArtifact(
            Index: index,
            Kind: constant.Kind.ToString(),
            Value: NormalizeConstantValue(constant));
    }

    private static BytecodeRoutineArtifact ToArtifact(BytecodeRoutine routine, SerializationContext context)
    {
        context.RegisterType(routine.ResultType);

        return new BytecodeRoutineArtifact(
            Id: routine.Id,
            Name: routine.Name,
            Location: ToArtifact(routine.Location),
            ResultType: routine.ResultType.Name,
            ParameterCount: routine.ParameterCount,
            LocalSlotCount: routine.LocalSlotCount,
            IsEntryPoint: routine.IsEntryPoint,
            Symbol: routine.Symbol is null ? null : ToArtifact(routine.Symbol, context),
            ParameterVariables: routine.ParameterVariables.Select(variable => ToArtifact(variable, context)).ToArray(),
            LocalVariables: routine.LocalVariables.Select(variable => ToArtifact(variable, context)).ToArray(),
            CapturedVariables: routine.CapturedVariables.Select(variable => ToArtifact(variable, context)).ToArray(),
            Instructions: routine.Instructions.Select(ToArtifact).ToArray());
    }

    private static BytecodeInstructionArtifact ToArtifact(BytecodeInstruction instruction, int index)
    {
        return new BytecodeInstructionArtifact(
            Index: index,
            Opcode: instruction.Opcode.ToString(),
            Operands: instruction.Operands.ToArray());
    }

    private static BytecodeGlobalArtifact ToArtifact(BytecodeGlobal global, SerializationContext context)
    {
        context.RegisterType(global.Type);

        return new BytecodeGlobalArtifact(
            Slot: global.Slot,
            Name: global.Name,
            Type: global.Type.Name,
            IsReadOnly: global.IsReadOnly,
            InitializationStrategy: global.InitializationStrategy.ToString(),
            FixedStringLength: global.FixedStringLength);
    }

    private static BuiltinBindingArtifact ToArtifact(BuiltinBinding builtin, SerializationContext context)
    {
        return new BuiltinBindingArtifact(
            Id: builtin.Id,
            CanonicalId: builtin.CanonicalId.ToString(),
            Name: builtin.Name,
            ParameterCount: builtin.ParameterCount,
            ReturnsValue: builtin.ReturnsValue,
            Function: builtin.Function is null ? null : ToArtifact(builtin.Function, context));
    }

    private static BytecodeFunctionSymbolArtifact ToArtifact(FunctionSymbol function, SerializationContext context)
    {
        return new BytecodeFunctionSymbolArtifact(
            SymbolId: context.GetFunctionId(function),
            Name: function.Name,
            ReturnType: function.ReturnType.Name,
            IsBuiltIn: function.IsBuiltIn,
            BuiltinId: function.BuiltinId?.ToString(),
            ContainingRoutineSymbolId: function.ContainingRoutine is null ? null : context.GetFunctionId(function.ContainingRoutine),
            ContainingRoutineName: function.ContainingRoutine?.Name,
            Parameters: function.Parameters.Select(parameter => ToArtifact(parameter, context)).ToArray());
    }

    private static BytecodeVariableSymbolArtifact ToArtifact(VariableSymbol variable, SerializationContext context)
    {
        return new BytecodeVariableSymbolArtifact(
            SymbolId: context.GetVariableId(variable),
            Name: variable.Name,
            Type: variable.Type.Name,
            IsReadOnly: variable.IsReadOnly,
            IsGlobal: variable.IsGlobal,
            FixedStringLength: variable.FixedStringLength,
            PassingMode: variable is ParameterSymbol parameter ? parameter.PassingMode.ToString() : null);
    }

    private static BytecodeDebugInfoArtifact ToArtifact(BytecodeDebugInfo debugInfo)
    {
        return new BytecodeDebugInfoArtifact(
            InstructionMap: debugInfo.InstructionMap.Select(ToArtifact).ToArray(),
            Routines: debugInfo.Routines.Select(ToArtifact).ToArray());
    }

    private static SourceInstructionMapArtifact ToArtifact(SourceInstructionMap map)
    {
        return new SourceInstructionMapArtifact(
            RoutineId: map.RoutineId,
            InstructionIndex: map.InstructionIndex,
            Location: ToArtifact(map.Location));
    }

    private static BytecodeRoutineDebugInfoArtifact ToArtifact(BytecodeRoutineDebugInfo routine)
    {
        return new BytecodeRoutineDebugInfoArtifact(
            RoutineId: routine.RoutineId,
            ParameterNames: new SortedDictionary<int, string>(
                routine.ParameterNames.ToDictionary(static entry => entry.Key, static entry => entry.Value)),
            LocalNames: new SortedDictionary<int, string>(
                routine.LocalNames.ToDictionary(static entry => entry.Key, static entry => entry.Value)),
            BlockRanges: routine.BlockRanges.Select(ToArtifact).ToArray());
    }

    private static BytecodeBlockDebugRangeArtifact ToArtifact(BytecodeBlockDebugRange range)
    {
        return new BytecodeBlockDebugRangeArtifact(
            Name: range.Name,
            StartInstructionIndex: range.StartInstructionIndex,
            EndInstructionIndexExclusive: range.EndInstructionIndexExclusive);
    }

    private static TextLocationArtifact ToArtifact(TextLocation location)
    {
        return new TextLocationArtifact(
            SpanStart: location.Span.Start,
            SpanLength: location.Span.Length,
            Line: location.Line,
            Column: location.Column);
    }

    private static object? NormalizeConstantValue(BytecodeConstant constant)
    {
        if (constant.Kind == BytecodeConstantKind.Decimal && constant.Value is decimal decimalValue)
        {
            return decimalValue.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        return constant.Value;
    }

    private static object? RestoreConstantValue(BytecodeConstantKind kind, object? value)
    {
        if (value is JsonElement json)
        {
            return RestoreConstantValue(kind, json);
        }

        return kind switch
        {
            BytecodeConstantKind.Integer => Convert.ToInt32(value, System.Globalization.CultureInfo.InvariantCulture),
            BytecodeConstantKind.Long => Convert.ToInt64(value, System.Globalization.CultureInfo.InvariantCulture),
            BytecodeConstantKind.Double => Convert.ToDouble(value, System.Globalization.CultureInfo.InvariantCulture),
            BytecodeConstantKind.Decimal => Convert.ToDecimal(value, System.Globalization.CultureInfo.InvariantCulture),
            BytecodeConstantKind.String or BytecodeConstantKind.RoutineName or BytecodeConstantKind.BuiltinName => Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture),
            BytecodeConstantKind.Boolean => Convert.ToBoolean(value, System.Globalization.CultureInfo.InvariantCulture),
            BytecodeConstantKind.Null => null,
            _ => value
        };
    }

    private static object? RestoreConstantValue(BytecodeConstantKind kind, JsonElement value)
    {
        return kind switch
        {
            BytecodeConstantKind.Integer => value.GetInt32(),
            BytecodeConstantKind.Long => value.GetInt64(),
            BytecodeConstantKind.Double => value.GetDouble(),
            BytecodeConstantKind.Decimal => decimal.Parse(value.GetString() ?? "0", System.Globalization.CultureInfo.InvariantCulture),
            BytecodeConstantKind.String or BytecodeConstantKind.RoutineName or BytecodeConstantKind.BuiltinName => value.GetString(),
            BytecodeConstantKind.Boolean => value.GetBoolean(),
            BytecodeConstantKind.Null => null,
            _ => value.ToString()
        };
    }

    private static FunctionSymbol GetOrCreateFunctionSymbol(
        BytecodeFunctionSymbolArtifact function,
        Dictionary<string, FunctionSymbol> functionSymbols,
        Dictionary<string, VariableSymbol> variableSymbols,
        IReadOnlyDictionary<string, UserDefinedTypeSymbol> userDefinedTypes)
    {
        var key = function.SymbolId is int symbolId
            ? $"id:{symbolId.ToString(System.Globalization.CultureInfo.InvariantCulture)}"
            : CreateFunctionKey(function);
        if (functionSymbols.TryGetValue(key, out var existing))
        {
            return existing;
        }

        FunctionSymbol? containingRoutine = null;
        if (function.ContainingRoutineSymbolId is int containingRoutineSymbolId)
        {
            functionSymbols.TryGetValue($"id:{containingRoutineSymbolId.ToString(System.Globalization.CultureInfo.InvariantCulture)}", out containingRoutine);
        }

        if (function.ContainingRoutineName is { Length: > 0 })
        {
            containingRoutine ??= functionSymbols.Values.FirstOrDefault(candidate =>
                string.Equals(candidate.Name, function.ContainingRoutineName, StringComparison.Ordinal));
        }

        var parameters = function.Parameters
            .Select((parameter, index) => GetOrCreateParameterSymbol(parameter, index, variableSymbols, userDefinedTypes))
            .ToArray();
        var symbol = new FunctionSymbol(
            function.Name,
            parameters,
            ResolveType(function.ReturnType, userDefinedTypes),
            containingRoutine,
            function.IsBuiltIn,
            function.BuiltinId is null ? null : Enum.Parse<BuiltinFunctionId>(function.BuiltinId));

        functionSymbols[key] = symbol;
        return symbol;
    }

    private static ParameterSymbol GetOrCreateParameterSymbol(
        BytecodeVariableSymbolArtifact parameter,
        int ordinal,
        Dictionary<string, VariableSymbol> variableSymbols,
        IReadOnlyDictionary<string, UserDefinedTypeSymbol> userDefinedTypes)
    {
        var key = CreateVariableKey(parameter);
        if (variableSymbols.TryGetValue(key, out var existing))
        {
            if (existing is ParameterSymbol parameterSymbol)
            {
                return parameterSymbol;
            }

            return new ParameterSymbol(
                parameter.Name,
                ResolveType(parameter.Type, userDefinedTypes),
                ordinal,
                ParseParameterPassingMode(parameter.PassingMode));
        }

        var symbol = new ParameterSymbol(
            parameter.Name,
            ResolveType(parameter.Type, userDefinedTypes),
            ordinal,
            ParseParameterPassingMode(parameter.PassingMode));
        variableSymbols[key] = symbol;
        return symbol;
    }

    private static VariableSymbol GetOrCreateVariableSymbol(
        BytecodeVariableSymbolArtifact variable,
        Dictionary<string, VariableSymbol> variableSymbols,
        IReadOnlyDictionary<string, UserDefinedTypeSymbol> userDefinedTypes)
    {
        var key = CreateVariableKey(variable);
        if (variableSymbols.TryGetValue(key, out var existing))
        {
            return existing;
        }

        var symbol = new VariableSymbol(
            variable.Name,
            ResolveType(variable.Type, userDefinedTypes),
            variable.IsReadOnly,
            variable.IsGlobal,
            variable.FixedStringLength);
        variableSymbols[key] = symbol;
        return symbol;
    }

    private static string CreateVariableKey(BytecodeVariableSymbolArtifact variable)
    {
        return variable.SymbolId is int symbolId
            ? $"id:{symbolId.ToString(System.Globalization.CultureInfo.InvariantCulture)}"
            : $"{variable.IsGlobal}:{variable.Name}:{variable.Type}:{variable.IsReadOnly}:{variable.FixedStringLength?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "<null>"}:{variable.PassingMode ?? "<null>"}";
    }

    private static string CreateFunctionKey(BytecodeFunctionSymbolArtifact function)
    {
        var parameters = string.Join(",", function.Parameters.Select(static parameter => parameter.Type));
        return $"{function.IsBuiltIn}:{function.BuiltinId ?? "<null>"}:{function.ContainingRoutineName ?? "<root>"}:{function.Name}({parameters}) AS {function.ReturnType}";
    }

    private sealed class SerializationContext
    {
        private readonly Dictionary<FunctionSymbol, int> _functionIds = [];
        private readonly Dictionary<VariableSymbol, int> _variableIds = [];
        private readonly Dictionary<string, UserDefinedTypeSymbol> _userDefinedTypes = new(StringComparer.Ordinal);

        public IReadOnlyList<UserDefinedTypeSymbol> UserDefinedTypes => _userDefinedTypes.Values.ToArray();

        public int GetFunctionId(FunctionSymbol symbol)
        {
            RegisterType(symbol.ReturnType);
            foreach (var parameter in symbol.Parameters)
            {
                RegisterType(parameter.Type);
            }

            if (_functionIds.TryGetValue(symbol, out var id))
            {
                return id;
            }

            id = _functionIds.Count;
            _functionIds.Add(symbol, id);
            return id;
        }

        public int GetVariableId(VariableSymbol symbol)
        {
            RegisterType(symbol.Type);

            if (_variableIds.TryGetValue(symbol, out var id))
            {
                return id;
            }

            id = _variableIds.Count;
            _variableIds.Add(symbol, id);
            return id;
        }

        public void RegisterType(TypeSymbol type)
        {
            switch (type)
            {
                case UserDefinedTypeSymbol userDefinedType:
                    if (!_userDefinedTypes.TryAdd(userDefinedType.Name, userDefinedType))
                    {
                        return;
                    }

                    foreach (var field in userDefinedType.Fields)
                    {
                        RegisterType(field.Type);
                    }
                    break;

                case ArrayTypeSymbol arrayType:
                    RegisterType(arrayType.ElementType);
                    break;

                case FunctionTypeSymbol functionType:
                    RegisterType(functionType.ReturnType);
                    foreach (var parameterType in functionType.ParameterTypes)
                    {
                        RegisterType(parameterType);
                    }
                    break;
            }
        }
    }

    private static Dictionary<string, UserDefinedTypeSymbol> CreateUserDefinedTypes(
        IReadOnlyList<BytecodeTypeDefinitionArtifact>? definitions)
    {
        var types = new Dictionary<string, UserDefinedTypeSymbol>(StringComparer.Ordinal);

        foreach (var definition in definitions ?? [])
        {
            types.TryAdd(definition.Name, new UserDefinedTypeSymbol(definition.Name));
        }

        foreach (var definition in definitions ?? [])
        {
            var type = types[definition.Name];
            if (type.IsCompleted)
            {
                continue;
            }

            type.Complete(definition.Fields
                .Select(field => new TypeFieldSymbol(
                    field.Name,
                    ResolveType(field.Type, types),
                    field.FixedStringLength,
                    field.FixedArrayLowerBounds,
                    field.FixedArrayUpperBounds,
                    field.HasDefaultValue,
                    RestoreTypeFieldDefaultValue(field, types)))
                .ToArray());
        }

        return types;
    }

    private static TypeSymbol ResolveType(
        string name,
        IReadOnlyDictionary<string, UserDefinedTypeSymbol> userDefinedTypes)
    {
        if (TryResolveArrayType(name, userDefinedTypes, out var arrayType))
        {
            return arrayType;
        }

        return name switch
        {
            "INTEGER" => TypeSymbol.Integer,
            "LONG" => TypeSymbol.Long,
            "DOUBLE" => TypeSymbol.Double,
            "DECIMAL" => TypeSymbol.Decimal,
            "STRING" => TypeSymbol.String,
            "BOOLEAN" => TypeSymbol.Boolean,
            "FILE" => TypeSymbol.File,
            "VOID" => TypeSymbol.Void,
            "<error>" => TypeSymbol.Error,
            _ when userDefinedTypes.TryGetValue(name, out var userDefinedType) => userDefinedType,
            _ when name.StartsWith("FUNCTION(", StringComparison.Ordinal) => ResolveFunctionType(name, userDefinedTypes),
            _ => new UserDefinedTypeSymbol(name)
        };
    }

    private static bool TryResolveArrayType(
        string name,
        IReadOnlyDictionary<string, UserDefinedTypeSymbol> userDefinedTypes,
        out TypeSymbol type)
    {
        var openParenIndex = name.LastIndexOf('(');
        if (openParenIndex <= 0 || !name.EndsWith(")", StringComparison.Ordinal))
        {
            type = TypeSymbol.Error;
            return false;
        }

        var rankToken = name[(openParenIndex + 1)..^1];
        if (rankToken.Any(static ch => ch != ','))
        {
            type = TypeSymbol.Error;
            return false;
        }

        var elementTypeName = name[..openParenIndex];
        var elementType = ResolveType(elementTypeName, userDefinedTypes);
        var rank = rankToken.Length + 1;
        type = ArrayTypeSymbol.Create(elementType, rank);
        return true;
    }

    private static TypeSymbol ResolveFunctionType(
        string name,
        IReadOnlyDictionary<string, UserDefinedTypeSymbol> userDefinedTypes)
    {
        const string prefix = "FUNCTION(";
        const string separator = ") AS ";

        var separatorIndex = name.LastIndexOf(separator, StringComparison.Ordinal);
        if (separatorIndex < prefix.Length)
        {
            return TypeSymbol.Error;
        }

        var parameterText = name[prefix.Length..separatorIndex];
        var returnText = name[(separatorIndex + separator.Length)..];
        var parameterSegments = string.IsNullOrWhiteSpace(parameterText)
            ? Array.Empty<string>()
            : parameterText.Split(',', StringSplitOptions.TrimEntries);
        var parameters = new List<TypeSymbol>(parameterSegments.Length);
        var passingModes = new List<ParameterPassingMode>(parameterSegments.Length);

        foreach (var segment in parameterSegments)
        {
            if (segment.StartsWith("BYREF ", StringComparison.Ordinal))
            {
                passingModes.Add(ParameterPassingMode.ByRef);
                parameters.Add(ResolveType(segment["BYREF ".Length..], userDefinedTypes));
            }
            else
            {
                passingModes.Add(ParameterPassingMode.ByVal);
                parameters.Add(ResolveType(segment, userDefinedTypes));
            }
        }

        return FunctionTypeSymbol.Create(parameters, ResolveType(returnText, userDefinedTypes), passingModes);
    }

    private static ParameterPassingMode ParseParameterPassingMode(string? value)
    {
        return Enum.TryParse<ParameterPassingMode>(value, ignoreCase: true, out var mode)
            ? mode
            : ParameterPassingMode.ByVal;
    }

    private static object? NormalizeTypeFieldDefaultValue(TypeFieldSymbol field)
    {
        ArgumentNullException.ThrowIfNull(field);

        if (!field.HasDefaultValue)
        {
            return null;
        }

        if (field.Type == TypeSymbol.Decimal && field.DefaultValue is decimal decimalValue)
        {
            return decimalValue.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        // Временные значения хранятся типизированно, но в артефакт записываются
        // каноническим текстом: это единственная форма, которую одинаково читают
        // JSON-сериализация и восстановление типа поля.
        if (field.DefaultValue is DateOnly dateValue)
        {
            return dateValue.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        }

        if (field.DefaultValue is TimeOnly timeValue)
        {
            return timeValue.Ticks % TimeSpan.TicksPerSecond == 0
                ? timeValue.ToString("HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture)
                : timeValue.ToString("HH:mm:ss.FFFFFFF", System.Globalization.CultureInfo.InvariantCulture);
        }

        if (field.DefaultValue is DateTime instantValue && field.Type == TypeSymbol.UtcDateTime)
        {
            var utc = DateTime.SpecifyKind(instantValue, DateTimeKind.Utc);
            return utc.Ticks % TimeSpan.TicksPerSecond == 0
                ? utc.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", System.Globalization.CultureInfo.InvariantCulture)
                : utc.ToString("yyyy-MM-dd'T'HH:mm:ss.FFFFFFF'Z'", System.Globalization.CultureInfo.InvariantCulture);
        }

        if (field.DefaultValue is DateTime dateTimeValue)
        {
            return dateTimeValue.Ticks % TimeSpan.TicksPerSecond == 0
                ? dateTimeValue.ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture)
                : dateTimeValue.ToString("yyyy-MM-dd HH:mm:ss.FFFFFFF", System.Globalization.CultureInfo.InvariantCulture);
        }

        return field.DefaultValue;
    }

    private static object? RestoreTypeFieldDefaultValue(
        BytecodeTypeFieldArtifact field,
        IReadOnlyDictionary<string, UserDefinedTypeSymbol> userDefinedTypes)
    {
        ArgumentNullException.ThrowIfNull(field);

        if (!field.HasDefaultValue)
        {
            return null;
        }

        var fieldType = ResolveType(field.Type, userDefinedTypes);
        var value = field.DefaultValue;

        if (value is JsonElement json)
        {
            value = fieldType switch
            {
                var type when type == TypeSymbol.Integer => json.GetInt32(),
                var type when type == TypeSymbol.Long => json.GetInt64(),
                var type when type == TypeSymbol.Double => json.GetDouble(),
                var type when type == TypeSymbol.Decimal => decimal.Parse(json.GetString() ?? "0", System.Globalization.CultureInfo.InvariantCulture),
                var type when type == TypeSymbol.String => json.GetString() ?? string.Empty,
                var type when type == TypeSymbol.Boolean => json.GetBoolean(),
                var type when type == TypeSymbol.Date
                    || type == TypeSymbol.Time
                    || type == TypeSymbol.DateTime => json.GetString() ?? string.Empty,
                _ => json.ToString()
            };
        }

        return fieldType switch
        {
            var type when type == TypeSymbol.Integer => Convert.ToInt32(value, System.Globalization.CultureInfo.InvariantCulture),
            var type when type == TypeSymbol.Long => Convert.ToInt64(value, System.Globalization.CultureInfo.InvariantCulture),
            var type when type == TypeSymbol.Double => Convert.ToDouble(value, System.Globalization.CultureInfo.InvariantCulture),
            var type when type == TypeSymbol.Decimal => Convert.ToDecimal(value, System.Globalization.CultureInfo.InvariantCulture),
            var type when type == TypeSymbol.String => Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty,
            var type when type == TypeSymbol.Boolean => Convert.ToBoolean(value, System.Globalization.CultureInfo.InvariantCulture),
            var type when type == TypeSymbol.Date => DateOnly.ParseExact(
                Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty,
                "yyyy-MM-dd",
                System.Globalization.CultureInfo.InvariantCulture),
            var type when type == TypeSymbol.Time => TimeOnly.ParseExact(
                Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty,
                ["HH:mm:ss", "HH:mm:ss.FFFFFFF"],
                System.Globalization.CultureInfo.InvariantCulture),
            var type when type == TypeSymbol.DateTime => DateTime.ParseExact(
                Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty,
                ["yyyy-MM-dd HH:mm:ss", "yyyy-MM-dd HH:mm:ss.FFFFFFF"],
                System.Globalization.CultureInfo.InvariantCulture),
            var type when type == TypeSymbol.UtcDateTime => DateTime.SpecifyKind(
                DateTime.ParseExact(
                    Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty,
                    ["yyyy-MM-dd'T'HH:mm:ss'Z'", "yyyy-MM-dd'T'HH:mm:ss.FFFFFFF'Z'"],
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal),
                DateTimeKind.Utc),
            _ => value
        };
    }
}
