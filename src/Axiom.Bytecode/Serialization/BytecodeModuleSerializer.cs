using System.Text.Json;
using System.Text.Json.Serialization;
using Axiom.Core;
using Axiom.Semantics;

namespace Axiom.Bytecode;

public static class BytecodeModuleSerializer
{
    public const int CurrentArtifactSchemaVersion = 3;

    /// <summary>
    /// Снимок версии 2 не содержал идентичности TYPE и восстанавливается по
    /// коротким именам. Версия 3 добавляет идентичность, поэтому два закрытых
    /// TYPE с одинаковым именем из разных модулей не сливаются.
    /// </summary>
    private const int LegacyArtifactSchemaVersion = 2;

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
        var userDefinedTypes = CreateUserDefinedTypes(artifact.TypeDefinitions, artifact.ArtifactSchemaVersion is null or <= LegacyArtifactSchemaVersion);

        // Регистрируем параметры процедур до таблиц захваченных переменных:
        // один и тот же SymbolId может сначала встретиться в замыкании вложенной процедуры.
        foreach (var routine in artifact.Routines)
        {
            if (routine.Symbol is not null)
            {
                GetOrCreateFunctionSymbol(routine.Symbol, functionSymbols, variableSymbols, userDefinedTypes);
            }
        }

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
        if (artifact.ArtifactSchemaVersion is int schemaVersion
            && schemaVersion != CurrentArtifactSchemaVersion
            && schemaVersion != LegacyArtifactSchemaVersion)
        {
            throw new InvalidOperationException(
                $"JSON-снимок байткода имеет неподдерживаемую версию схемы {schemaVersion}. Поддерживаются версии {LegacyArtifactSchemaVersion} и {CurrentArtifactSchemaVersion}.");
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
            routine.ParameterVariables
                .Select((variable, index) => GetOrCreateParameterSymbol(variable, index, variableSymbols, userDefinedTypes))
                .ToArray(),
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
        return new TextLocation(new TextSpan(location.SpanStart, location.SpanLength), location.Line, location.Column, location.Document);
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
        // Идентичность записывается только когда она отличается от короткого
        // имени: снимок программы без модулей остаётся прежним.
        var identity = TypeIdentityFormat.Equals(type.TypeIdentity, type.Name)
            ? null
            : type.TypeIdentity;

        return new BytecodeTypeDefinitionArtifact(
            type.Name,
            type.Fields.Select(ToArtifact).ToArray(),
            identity,
            (type as TupleTypeSymbol)?.ElementNames);
    }

    private static BytecodeTypeFieldArtifact ToArtifact(TypeFieldSymbol field)
    {
        return new BytecodeTypeFieldArtifact(
            field.Name,
            TypeReference(field.Type),
            field.FixedStringLength,
            field.FixedArrayLowerBounds,
            field.FixedArrayUpperBounds,
            field.HasDefaultValue,
            NormalizeTypeFieldDefaultValue(field));
    }

    /// <summary>
    /// Текстовая ссылка на тип для снимка. Пользовательский TYPE записывается
    /// идентичностью, поэтому два закрытых TYPE с одинаковым именем не
    /// сливаются; массивы и функциональные типы сохраняют идентичность элемента.
    /// </summary>
    private static string TypeReference(TypeSymbol type)
    {
        ArgumentNullException.ThrowIfNull(type);

        return type switch
        {
            UserDefinedTypeSymbol userDefinedType => userDefinedType.TypeIdentity,
            DictionaryTypeSymbol dictionaryType => CreateDictionaryTypeReference(dictionaryType),
            SetTypeSymbol setType => CreateSetTypeReference(setType),
            ArrayTypeSymbol { ElementType: FunctionTypeSymbol } arrayType => $"({TypeReference(arrayType.ElementType)})({new string(',', arrayType.Rank - 1)})",
            ArrayTypeSymbol arrayType => $"{TypeReference(arrayType.ElementType)}({new string(',', arrayType.Rank - 1)})",
            FunctionTypeSymbol functionType => CreateFunctionTypeReference(functionType),
            _ => type.Name
        };
    }

    /// <summary>
    /// Ссылка на словарь: DICTIONARY(ключ, значение). Вложенные массивы и
    /// словари значений сохраняют собственную запись, поэтому ссылка остаётся
    /// однозначной при восстановлении.
    /// </summary>
    private static string CreateDictionaryTypeReference(DictionaryTypeSymbol dictionaryType)
    {
        return $"DICTIONARY({TypeReference(dictionaryType.KeyType)}, {TypeReference(dictionaryType.ValueType)})";
    }

    /// <summary>
    /// Ссылка на множество: HASHSET(элемент). Тип элемента записывается той же
    /// ссылкой, поэтому вложенные записи и массивы остаются однозначными.
    /// </summary>
    private static string CreateSetTypeReference(SetTypeSymbol setType)
    {
        return $"HASHSET({TypeReference(setType.ElementType)})";
    }

    private static string CreateFunctionTypeReference(FunctionTypeSymbol functionType)
    {
        var parameters = functionType.ParameterTypes
            .Select((parameterType, index) => functionType.ParameterPassingModes[index] == ParameterPassingMode.ByRef
                ? $"BYREF {TypeReference(parameterType)}"
                : TypeReference(parameterType));

        return $"FUNCTION({string.Join(", ", parameters)}) AS {TypeReference(functionType.ReturnType)}";
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
            ResultType: TypeReference(routine.ResultType),
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
            Type: TypeReference(global.Type),
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
            ReturnType: TypeReference(function.ReturnType),
            IsBuiltIn: function.IsBuiltIn,
            BuiltinId: function.BuiltinId?.ToString(),
            ContainingRoutineSymbolId: function.ContainingRoutine is null ? null : context.GetFunctionId(function.ContainingRoutine),
            ContainingRoutineName: function.ContainingRoutine?.Name,
            Parameters: function.Parameters.Select(parameter => ToArtifact(parameter, context)).ToArray(),
            HasImplicitSelfReceiver: function.HasImplicitSelfReceiver);
    }

    private static BytecodeVariableSymbolArtifact ToArtifact(VariableSymbol variable, SerializationContext context)
    {
        return new BytecodeVariableSymbolArtifact(
            SymbolId: context.GetVariableId(variable),
            Name: variable.Name,
            Type: TypeReference(variable.Type),
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
            Column: location.Column,
            Document: location.Document);
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
            function.BuiltinId is null ? null : Enum.Parse<BuiltinFunctionId>(function.BuiltinId),
            function.HasImplicitSelfReceiver);

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
        private readonly Dictionary<string, UserDefinedTypeSymbol> _userDefinedTypes = new(StringComparer.OrdinalIgnoreCase);

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
                    if (!_userDefinedTypes.TryAdd(userDefinedType.TypeIdentity, userDefinedType))
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

                // Тип словаря ссылается на типы ключа и значения: без их
                // регистрации определение TYPE, используемого только внутри
                // словаря, отсутствовало бы в снимке.
                case DictionaryTypeSymbol dictionaryType:
                    RegisterType(dictionaryType.KeyType);
                    RegisterType(dictionaryType.ValueType);
                    break;

                // Множество ссылается на тип элемента: без его регистрации
                // определение TYPE, используемого только внутри HASHSET,
                // отсутствовало бы в снимке.
                case SetTypeSymbol setType:
                    RegisterType(setType.ElementType);
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

    private sealed class TypeResolutionContext(bool legacy) : Dictionary<string, UserDefinedTypeSymbol>(StringComparer.OrdinalIgnoreCase)
    {
        public bool IsLegacy { get; } = legacy;
    }

    private static Dictionary<string, UserDefinedTypeSymbol> CreateUserDefinedTypes(
        IReadOnlyList<BytecodeTypeDefinitionArtifact>? definitions, bool legacy)
    {
        var types = new TypeResolutionContext(legacy);

        // Ключ словаря — идентичность типа. Снимок версии 2 не содержал
        // идентичности, поэтому там ключом остаётся короткое имя.
        foreach (var definition in definitions ?? [])
        {
            var identity = GetTypeIdentity(definition);
            TypeIdentityFormat.TrySplit(identity, out var name, out var containingModule);
            if (definition.TupleElementNames is { } tupleNames)
            {
                var allNamed = tupleNames.All(static n => !string.IsNullOrWhiteSpace(n));
                var allUnnamed = tupleNames.All(static n => n is null);
                var unique = tupleNames.Where(static n => n is not null).Distinct(StringComparer.OrdinalIgnoreCase).Count();
                var fieldsValid = definition.Fields.Select((field, index) =>
                    string.Equals(field.Name, "Item" + (index + 1), StringComparison.Ordinal)).All(static valid => valid);
                if (tupleNames.Count < 2 || tupleNames.Count != definition.Fields.Count ||
                    (!allNamed && !allUnnamed) || (allNamed && unique != tupleNames.Count) || !fieldsValid)
                    throw new InvalidOperationException($"Неверное определение кортежа '{definition.Name}'.");
            }
            UserDefinedTypeSymbol restored = definition.TupleElementNames is { } names
                ? new TupleTypeSymbol(definition.Name, names)
                : new UserDefinedTypeSymbol(name, containingModule);
            if (!types.TryAdd(identity, restored))
            {
                throw new InvalidOperationException($"Идентичность TYPE '{identity}' объявлена несколько раз.");
            }
        }

        foreach (var definition in definitions ?? [])
        {
            var type = types[GetTypeIdentity(definition)];
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

        // Снимок недоверенный: циклическое определение кортежа нельзя
        // передавать структурному сравнению или созданию начального значения.
        var active = new HashSet<UserDefinedTypeSymbol>(ReferenceEqualityComparer.Instance);
        var complete = new HashSet<UserDefinedTypeSymbol>(ReferenceEqualityComparer.Instance);
        foreach (var type in types.Values) ValidateValueTypeGraph(type);

        void ValidateValueTypeGraph(UserDefinedTypeSymbol type)
        {
            if (complete.Contains(type)) return;
            if (!active.Add(type)) throw new InvalidOperationException($"Циклическое определение типа '{type.Name}'.");
            foreach (var field in type.Fields)
            {
                var nested = field.Type is ArrayTypeSymbol array ? array.ElementType : field.Type;
                if (nested is UserDefinedTypeSymbol record) ValidateValueTypeGraph(record);
            }
            active.Remove(type);
            complete.Add(type);
        }

        return types;
    }

    private static string GetTypeIdentity(BytecodeTypeDefinitionArtifact definition)
    {
        return string.IsNullOrWhiteSpace(definition.Identity)
            ? definition.Name
            : definition.Identity;
    }

    private static TypeSymbol ResolveType(
        string name,
        IReadOnlyDictionary<string, UserDefinedTypeSymbol> userDefinedTypes)
    {
        // Старый формат проверял окончание массива первым. Схема 3 явно
        // группирует массивы функций и отличает их от функции, возвращающей массив.
        if (userDefinedTypes is TypeResolutionContext { IsLegacy: true }
            && TryResolveArrayType(name, userDefinedTypes, out var legacyArray)) return legacyArray;

        return ResolveStructuredType(name.Trim(), userDefinedTypes);
    }

    /// <summary>
    /// Разбирает ссылку типа слева направо, поэтому внешние суффиксы массива
    /// отделяются раньше содержимого: DICTIONARY(STRING, INTEGER)() — это
    /// массив словарей, а не словарь со скобкой в конце. Функциональный тип
    /// проверяется первым: в ссылке FUNCTION() AS T[,] суффикс относится к
    /// результату функции, а не к самой функции.
    /// </summary>
    private static TypeSymbol ResolveStructuredType(
        string name,
        IReadOnlyDictionary<string, UserDefinedTypeSymbol> userDefinedTypes)
    {
        name = name.Trim();

        if (name.StartsWith("FUNCTION(", StringComparison.Ordinal))
        {
            return ResolveFunctionType(name, userDefinedTypes);
        }

        // Словарь и множество разбираются одним помощником: внешний суффикс
        // массива отделяется в одном месте, поэтому DICTIONARY(STRING, INTEGER)()
        // и HASHSET(STRING)[,] не путают закрывающую скобку коллекции со
        // скобкой суффикса.
        if (TryResolveCollectionType(
                name,
                "DICTIONARY(",
                inner => ResolveDictionaryType(name, inner, userDefinedTypes),
                "тип словаря",
                out var dictionaryType))
        {
            return dictionaryType;
        }

        if (TryResolveCollectionType(
                name,
                "HASHSET(",
                inner => ResolveSetType(name, inner, userDefinedTypes),
                "тип множества",
                out var setType))
        {
            return setType;
        }

        if (TryPeelArraySuffix(name, out var elementName, out var rank))
        {
            // Массив функционального типа записывается в скобках: (FUNCTION(...) AS T)().
            var element = IsWrappedWholeExpression(elementName)
                && elementName.AsSpan(1).StartsWith("FUNCTION(", StringComparison.Ordinal)
                    ? elementName[1..^1] : elementName;
            return ArrayTypeSymbol.Create(ResolveStructuredType(element, userDefinedTypes), rank);
        }

        return ResolveScalarType(name, userDefinedTypes);
    }

    /// <summary>
    /// Разбирает ссылку коллекции с внешним суффиксом массива. Внутреннее
    /// содержимое передаётся помощнику конкретной коллекции. Разбор суффикса
    /// остаётся единственным для всех коллекций.
    /// </summary>
    private static bool TryResolveCollectionType(
        string name,
        string prefix,
        Func<string, TypeSymbol> resolveInner,
        string typeDescription,
        out TypeSymbol resolved)
    {
        resolved = TypeSymbol.Error;

        if (!name.StartsWith(prefix, StringComparison.Ordinal))
        {
            return false;
        }

        var closing = FindClosingParen(name, prefix.Length - 1);
        if (closing < 0)
        {
            throw new InvalidOperationException($"Неверная ссылка на {typeDescription} '{name}'.");
        }

        var inner = name[prefix.Length..closing];
        var collectionType = resolveInner(inner);
        var tail = name[(closing + 1)..];

        if (tail.Length == 0)
        {
            resolved = collectionType;
            return true;
        }

        if (!TryParseArraySuffix(tail, out var rank))
        {
            throw new InvalidOperationException($"Неверная ссылка на {typeDescription} '{name}'.");
        }

        resolved = ArrayTypeSymbol.Create(collectionType, rank);
        return true;
    }

    /// <summary>
    /// Разбирает суффикс массива, оставшийся после закрывающей скобки типа:
    /// () для ранга 1 и (,), (,,) для следующих рангов.
    /// </summary>
    private static bool TryParseArraySuffix(string suffix, out int rank)
    {
        rank = 0;

        if (suffix.Length < 2 || suffix[0] != '(' || suffix[^1] != ')')
        {
            return false;
        }

        var rankToken = suffix[1..^1];
        if (rankToken.Any(static ch => ch != ','))
        {
            return false;
        }

        rank = rankToken.Length + 1;
        return true;
    }

    /// <summary>
    /// Отделяет внешний суффикс массива: закрывающая скобка, перед ней только
    /// запятые и открывающая скобка.
    /// </summary>
    private static bool TryPeelArraySuffix(string name, out string elementName, out int rank)
    {
        elementName = string.Empty;
        rank = 0;

        if (!name.EndsWith(")", StringComparison.Ordinal))
        {
            return false;
        }

        var open = name.LastIndexOf('(');
        if (open <= 0)
        {
            return false;
        }

        var rankToken = name[(open + 1)..^1];
        if (rankToken.Any(static ch => ch != ','))
        {
            return false;
        }

        elementName = name[..open];
        rank = rankToken.Length + 1;
        return true;
    }

    /// <summary>
    /// Проверяет, что внешние скобки охватывают всё выражение целиком, а не
    /// только его начало.
    /// </summary>
    private static bool IsWrappedWholeExpression(string name)
    {
        var depth = 0;
        for (var index = 0; index < name.Length; index++)
        {
            if (name[index] == '(') depth++;
            else if (name[index] == ')' && --depth == 0) return index == name.Length - 1;
        }

        return false;
    }

    private static TypeSymbol ResolveScalarType(
        string name,
        IReadOnlyDictionary<string, UserDefinedTypeSymbol> userDefinedTypes)
    {
        return name switch
        {
            "INTEGER" => TypeSymbol.Integer,
            "LONG" => TypeSymbol.Long,
            "DOUBLE" => TypeSymbol.Double,
            "DECIMAL" => TypeSymbol.Decimal,
            "STRING" => TypeSymbol.String,
            "BOOLEAN" => TypeSymbol.Boolean,
            "FILE" => TypeSymbol.File,
            "DATE" => TypeSymbol.Date,
            "TIME" => TypeSymbol.Time,
            "DATETIME" => TypeSymbol.DateTime,
            "UTCDATETIME" => TypeSymbol.UtcDateTime,
            "IMAGE" => TypeSymbol.Image,
            "SPRITE" => TypeSymbol.Sprite,
            "TILEMAP" => TypeSymbol.Tilemap,
            "TILESET" => TypeSymbol.Tileset,
            "ERROR" => TypeSymbol.RuntimeError,
            "VOID" => TypeSymbol.Void,
            "<error>" => TypeSymbol.Error,
            _ when TryResolveUserDefinedType(name, userDefinedTypes, out var userDefinedType) => userDefinedType,
            _ => throw new InvalidOperationException($"Неизвестная или неоднозначная ссылка на тип '{name}'.")
        };
    }

    /// <summary>
    /// Сначала ссылка разрешается по идентичности. Короткое имя остаётся
    /// совместимым путём для снимков версии 2 и для ручной сборки модуля, но
    /// применяется только при однозначном совпадении.
    /// </summary>
    private static bool TryResolveUserDefinedType(
        string name,
        IReadOnlyDictionary<string, UserDefinedTypeSymbol> userDefinedTypes,
        out UserDefinedTypeSymbol userDefinedType)
    {
        if (userDefinedTypes.TryGetValue(name, out userDefinedType!))
        {
            return true;
        }

        if (name.Contains(TypeIdentityFormat.Separator, StringComparison.Ordinal))
        {
            return false;
        }

        UserDefinedTypeSymbol? match = null;
        foreach (var candidate in userDefinedTypes.Values)
        {
            if (!TypeIdentityFormat.Equals(candidate.Name, name))
            {
                continue;
            }

            if (match is not null)
            {
                // Два закрытых TYPE с одинаковым коротким именем: выбор наугад
                // потерял бы различие, поэтому ссылка считается неоднозначной.
                return false;
            }

            match = candidate;
        }

        userDefinedType = match!;
        return match is not null;
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
        if (elementTypeName.StartsWith('(') && elementTypeName.EndsWith(')'))
            elementTypeName = elementTypeName[1..^1];
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
        var depth = 1;
        var closing = prefix.Length;
        for (; closing < name.Length; closing++)
        {
            if (name[closing] == '(') depth++;
            else if (name[closing] == ')' && --depth == 0) break;
        }
        if (closing >= name.Length || !name.AsSpan(closing).StartsWith(separator, StringComparison.Ordinal))
            throw new InvalidOperationException($"Неверная ссылка на функциональный тип '{name}'.");

        var parameterText = name[prefix.Length..closing];
        var returnText = name[(closing + separator.Length)..];
        var segments = new List<string>();
        var start = 0;
        depth = 0;
        for (var index = 0; index < parameterText.Length; index++)
        {
            if (parameterText[index] == '(') depth++;
            else if (parameterText[index] == ')') depth--;
            else if (parameterText[index] == ',' && depth == 0)
            {
                segments.Add(parameterText[start..index].Trim());
                start = index + 1;
            }
        }
        if (!string.IsNullOrWhiteSpace(parameterText)) segments.Add(parameterText[start..].Trim());
        var parameters = new List<TypeSymbol>(segments.Count);
        var passingModes = new List<ParameterPassingMode>(segments.Count);
        foreach (var segment in segments)
        {
            var byRef = segment.StartsWith("BYREF ", StringComparison.Ordinal);
            passingModes.Add(byRef ? ParameterPassingMode.ByRef : ParameterPassingMode.ByVal);
            parameters.Add(ResolveType(byRef ? segment["BYREF ".Length..] : segment, userDefinedTypes));
        }
        return FunctionTypeSymbol.Create(parameters, ResolveType(returnText, userDefinedTypes), passingModes);
    }

    /// <summary>
    /// Восстанавливает тип словаря из содержимого ссылки: тип ключа и тип
    /// значения. Разделитель ищется на верхнем уровне, поэтому вложенные
    /// массивы и функциональные типы не путаются с разделителем аргументов.
    /// </summary>
    private static TypeSymbol ResolveDictionaryType(
        string name,
        string inner,
        IReadOnlyDictionary<string, UserDefinedTypeSymbol> userDefinedTypes)
    {
        var separator = FindTopLevelComma(inner);
        if (separator < 0)
        {
            throw new InvalidOperationException($"Неверная ссылка на тип словаря '{name}'.");
        }

        var keyType = ResolveStructuredType(inner[..separator].Trim(), userDefinedTypes);
        var valueType = ResolveStructuredType(inner[(separator + 1)..].Trim(), userDefinedTypes);

        var dictionaryType = DictionaryTypeSymbol.Create(keyType, valueType);
        if (dictionaryType == TypeSymbol.Error)
        {
            throw new InvalidOperationException($"Недопустимый тип словаря '{name}'.");
        }

        return dictionaryType;
    }

    /// <summary>
    /// Восстанавливает тип множества из содержимого ссылки: единственный тип
    /// элемента. Второй аргумент недопустим так же, как неподдержанный элемент.
    /// </summary>
    private static TypeSymbol ResolveSetType(
        string name,
        string inner,
        IReadOnlyDictionary<string, UserDefinedTypeSymbol> userDefinedTypes)
    {
        if (FindTopLevelComma(inner) >= 0)
        {
            throw new InvalidOperationException($"Неверная ссылка на тип множества '{name}': ожидается один тип элемента.");
        }

        var elementType = ResolveStructuredType(inner.Trim(), userDefinedTypes);
        var setType = SetTypeSymbol.Create(elementType);
        if (setType == TypeSymbol.Error)
        {
            throw new InvalidOperationException($"Недопустимый тип множества '{name}'.");
        }

        return setType;
    }

    private static int FindClosingParen(string text, int openIndex)
    {
        var depth = 0;
        for (var index = openIndex; index < text.Length; index++)
        {
            if (text[index] == '(') depth++;
            else if (text[index] == ')' && --depth == 0) return index;
        }

        return -1;
    }

    private static int FindTopLevelComma(string text)
    {
        var depth = 0;
        for (var index = 0; index < text.Length; index++)
        {
            if (text[index] == '(') depth++;
            else if (text[index] == ')') depth--;
            else if (text[index] == ',' && depth == 0) return index;
        }

        return -1;
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
