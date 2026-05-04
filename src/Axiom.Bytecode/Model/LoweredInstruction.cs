using Axiom.Core;
using Axiom.Semantics;
using Axiom.Syntax;

namespace Axiom.Bytecode;

public sealed class LoweredInstruction
{
    private LoweredInstruction(
        LoweredInstructionKind kind,
        TextLocation location,
        object? literalValue = null,
        TypeSymbol? valueType = null,
        VariableSymbol? variable = null,
        FunctionSymbol? function = null,
        SyntaxKind? operatorKind = null,
        PrintSeparatorKind? separatorKind = null,
        BoundFileOpenMode? fileOpenMode = null,
        IReadOnlyList<VariableSymbol>? variables = null,
        IReadOnlyList<int>? integerOperands = null,
        string? label = null,
        string? displayName = null,
        string? promptText = null,
        int valueCount = 0,
        bool preserve = false,
        int indexRangeMask = 0)
    {
        Kind = kind;
        Location = location;
        LiteralValue = literalValue;
        ValueType = valueType;
        Variable = variable;
        Function = function;
        OperatorKind = operatorKind;
        SeparatorKind = separatorKind;
        FileOpenMode = fileOpenMode;
        Variables = variables ?? Array.Empty<VariableSymbol>();
        IntegerOperands = integerOperands ?? Array.Empty<int>();
        Label = label;
        DisplayName = displayName;
        PromptText = promptText;
        ValueCount = valueCount;
        Preserve = preserve;
        IndexRangeMask = indexRangeMask;
    }

    public LoweredInstructionKind Kind { get; }

    public TextLocation Location { get; }

    public object? LiteralValue { get; }

    public TypeSymbol? ValueType { get; }

    public VariableSymbol? Variable { get; }

    public FunctionSymbol? Function { get; }

    public SyntaxKind? OperatorKind { get; }

    public PrintSeparatorKind? SeparatorKind { get; }

    public BoundFileOpenMode? FileOpenMode { get; }

    public IReadOnlyList<VariableSymbol> Variables { get; }

    public IReadOnlyList<int> IntegerOperands { get; }

    public string? Label { get; }

    public string? DisplayName { get; }

    public string? PromptText { get; }

    public int ValueCount { get; }

    public bool Preserve { get; }

    public int IndexRangeMask { get; }

    public static LoweredInstruction LabelMarker(TextLocation location, string label)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        return new LoweredInstruction(LoweredInstructionKind.Label, location, label: label);
    }

    public static LoweredInstruction LoadLiteral(TextLocation location, object? value, TypeSymbol valueType)
    {
        ArgumentNullException.ThrowIfNull(valueType);
        return new LoweredInstruction(LoweredInstructionKind.LoadLiteral, location, literalValue: value, valueType: valueType);
    }

    public static LoweredInstruction LoadFunction(TextLocation location, FunctionSymbol function)
    {
        ArgumentNullException.ThrowIfNull(function);
        return new LoweredInstruction(LoweredInstructionKind.LoadFunction, location, function: function);
    }

    public static LoweredInstruction LoadVariable(TextLocation location, VariableSymbol variable)
    {
        ArgumentNullException.ThrowIfNull(variable);
        return new LoweredInstruction(LoweredInstructionKind.LoadVariable, location, variable: variable);
    }

    public static LoweredInstruction StoreVariable(TextLocation location, VariableSymbol variable)
    {
        ArgumentNullException.ThrowIfNull(variable);
        return new LoweredInstruction(LoweredInstructionKind.StoreVariable, location, variable: variable);
    }

    public static LoweredInstruction Dup(TextLocation location)
    {
        return new LoweredInstruction(LoweredInstructionKind.Dup, location);
    }

    public static LoweredInstruction UnaryOperation(TextLocation location, SyntaxKind operatorKind)
    {
        return new LoweredInstruction(LoweredInstructionKind.UnaryOperation, location, operatorKind: operatorKind);
    }

    public static LoweredInstruction BinaryOperation(TextLocation location, SyntaxKind operatorKind)
    {
        return new LoweredInstruction(LoweredInstructionKind.BinaryOperation, location, operatorKind: operatorKind);
    }

    public static LoweredInstruction Convert(TextLocation location, TypeSymbol valueType)
    {
        ArgumentNullException.ThrowIfNull(valueType);
        return new LoweredInstruction(LoweredInstructionKind.Convert, location, valueType: valueType);
    }

    public static LoweredInstruction CallFunction(TextLocation location, FunctionSymbol function)
    {
        ArgumentNullException.ThrowIfNull(function);
        return new LoweredInstruction(LoweredInstructionKind.CallFunction, location, function: function);
    }

    public static LoweredInstruction CallFunctionByRef(
        TextLocation location,
        FunctionSymbol function,
        int byValArgumentCount,
        IReadOnlyList<int> argumentIndices,
        IReadOnlyList<VariableSymbol> byRefVariables)
    {
        ArgumentNullException.ThrowIfNull(function);
        ArgumentNullException.ThrowIfNull(argumentIndices);
        ArgumentNullException.ThrowIfNull(byRefVariables);
        return new LoweredInstruction(
            LoweredInstructionKind.CallFunctionByRef,
            location,
            function: function,
            variables: byRefVariables,
            integerOperands: argumentIndices,
            valueCount: byValArgumentCount);
    }

    public static LoweredInstruction CallValue(TextLocation location, int argumentCount, TypeSymbol returnType)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(argumentCount);
        ArgumentNullException.ThrowIfNull(returnType);
        return new LoweredInstruction(LoweredInstructionKind.CallValue, location, valueType: returnType, valueCount: argumentCount);
    }

    public static LoweredInstruction CreateRecord(TextLocation location, UserDefinedTypeSymbol type)
    {
        ArgumentNullException.ThrowIfNull(type);
        return new LoweredInstruction(LoweredInstructionKind.CreateRecord, location, valueType: type, displayName: type.Name);
    }

    public static LoweredInstruction RedimArray(TextLocation location, string displayName, bool preserve, int dimensionCount)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(dimensionCount);
        return new LoweredInstruction(
            LoweredInstructionKind.RedimArray,
            location,
            displayName: displayName,
            valueCount: dimensionCount,
            preserve: preserve);
    }

    public static LoweredInstruction EraseArray(TextLocation location, string displayName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        return new LoweredInstruction(LoweredInstructionKind.EraseArray, location, displayName: displayName);
    }

    public static LoweredInstruction LoadElement(TextLocation location, string displayName, int dimensionCount, int indexRangeMask = 0)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(dimensionCount);
        return new LoweredInstruction(
            LoweredInstructionKind.LoadElement,
            location,
            displayName: displayName,
            valueCount: dimensionCount,
            indexRangeMask: indexRangeMask);
    }

    public static LoweredInstruction StoreElement(TextLocation location, string displayName, int dimensionCount)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(dimensionCount);
        return new LoweredInstruction(
            LoweredInstructionKind.StoreElement,
            location,
            displayName: displayName,
            valueCount: dimensionCount);
    }

    public static LoweredInstruction LoadField(TextLocation location, string displayName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        return new LoweredInstruction(LoweredInstructionKind.LoadField, location, displayName: displayName);
    }

    public static LoweredInstruction StoreField(TextLocation location, string displayName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        return new LoweredInstruction(LoweredInstructionKind.StoreField, location, displayName: displayName);
    }

    public static LoweredInstruction OpenFile(TextLocation location, BoundFileOpenMode mode, string displayName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        return new LoweredInstruction(LoweredInstructionKind.OpenFile, location, fileOpenMode: mode, displayName: displayName);
    }

    public static LoweredInstruction CloseFile(TextLocation location)
    {
        return new LoweredInstruction(LoweredInstructionKind.CloseFile, location);
    }

    public static LoweredInstruction ValidateForStep(TextLocation location)
    {
        return new LoweredInstruction(LoweredInstructionKind.ValidateForStep, location);
    }

    public static LoweredInstruction PrintValue(TextLocation location)
    {
        return new LoweredInstruction(LoweredInstructionKind.PrintValue, location);
    }

    public static LoweredInstruction PrintUsing(TextLocation location, int valueCount, PrintSeparatorKind? separatorKind)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(valueCount);
        return new LoweredInstruction(LoweredInstructionKind.PrintUsing, location, separatorKind: separatorKind, valueCount: valueCount);
    }

    public static LoweredInstruction PrintSeparator(TextLocation location, PrintSeparatorKind separatorKind)
    {
        return new LoweredInstruction(LoweredInstructionKind.PrintSeparator, location, separatorKind: separatorKind);
    }

    public static LoweredInstruction PrintLine(TextLocation location)
    {
        return new LoweredInstruction(LoweredInstructionKind.PrintLine, location);
    }

    public static LoweredInstruction Input(TextLocation location, IReadOnlyList<VariableSymbol> variables, string? promptText)
    {
        ArgumentNullException.ThrowIfNull(variables);
        return new LoweredInstruction(
            LoweredInstructionKind.Input,
            location,
            variables: variables,
            promptText: promptText,
            valueCount: variables.Count);
    }

    public static LoweredInstruction Jump(TextLocation location, string label)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        return new LoweredInstruction(LoweredInstructionKind.Jump, location, label: label);
    }

    public static LoweredInstruction JumpIfTrue(TextLocation location, string label)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        return new LoweredInstruction(LoweredInstructionKind.JumpIfTrue, location, label: label);
    }

    public static LoweredInstruction JumpIfFalse(TextLocation location, string label)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        return new LoweredInstruction(LoweredInstructionKind.JumpIfFalse, location, label: label);
    }

    public static LoweredInstruction Pop(TextLocation location)
    {
        return new LoweredInstruction(LoweredInstructionKind.Pop, location);
    }

    public static LoweredInstruction Return(TextLocation location)
    {
        return new LoweredInstruction(LoweredInstructionKind.Return, location);
    }
}
