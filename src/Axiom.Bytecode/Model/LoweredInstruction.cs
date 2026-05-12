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
        string? catchLabel = null,
        string? finallyLabel = null,
        string? endLabel = null,
        string? displayName = null,
        string? promptText = null,
        int valueCount = 0,
        bool preserve = false,
        bool handlerReceivesKeyText = false,
        int mouseEventKind = 0,
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
        CatchLabel = catchLabel;
        FinallyLabel = finallyLabel;
        EndLabel = endLabel;
        DisplayName = displayName;
        PromptText = promptText;
        ValueCount = valueCount;
        Preserve = preserve;
        HandlerReceivesKeyText = handlerReceivesKeyText;
        MouseEventKind = mouseEventKind;
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

    public string? CatchLabel { get; }

    public string? FinallyLabel { get; }

    public string? EndLabel { get; }

    public string? DisplayName { get; }

    public string? PromptText { get; }

    public int ValueCount { get; }

    public bool Preserve { get; }

    public bool HandlerReceivesKeyText { get; }

    public int MouseEventKind { get; }

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

    public static LoweredInstruction AppendStringVariable(TextLocation location, VariableSymbol variable, bool useDisplayString)
    {
        ArgumentNullException.ThrowIfNull(variable);
        return new LoweredInstruction(
            LoweredInstructionKind.AppendStringVariable,
            location,
            variable: variable,
            preserve: useDisplayString);
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

    public static LoweredInstruction ValidateTypeInvariant(TextLocation location, string displayName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        return new LoweredInstruction(LoweredInstructionKind.ValidateTypeInvariant, location, displayName: displayName);
    }

    public static LoweredInstruction ValidateForStep(TextLocation location)
    {
        return new LoweredInstruction(LoweredInstructionKind.ValidateForStep, location);
    }

    public static LoweredInstruction StatementBoundary(TextLocation location)
    {
        return new LoweredInstruction(LoweredInstructionKind.StatementBoundary, location);
    }

    public static LoweredInstruction RegisterKeyHandler(TextLocation location, bool handlerReceivesKeyText)
    {
        return new LoweredInstruction(LoweredInstructionKind.RegisterKeyHandler, location, handlerReceivesKeyText: handlerReceivesKeyText);
    }

    public static LoweredInstruction UnregisterKeyHandler(TextLocation location)
    {
        return new LoweredInstruction(LoweredInstructionKind.UnregisterKeyHandler, location);
    }

    public static LoweredInstruction PauseKeyHandler(TextLocation location)
    {
        return new LoweredInstruction(LoweredInstructionKind.PauseKeyHandler, location);
    }

    public static LoweredInstruction ResumeKeyHandler(TextLocation location)
    {
        return new LoweredInstruction(LoweredInstructionKind.ResumeKeyHandler, location);
    }

    public static LoweredInstruction RegisterMouseHandler(TextLocation location, bool handlerReceivesEvent, BoundMouseEventKind eventKind)
    {
        return new LoweredInstruction(
            LoweredInstructionKind.RegisterMouseHandler,
            location,
            handlerReceivesKeyText: handlerReceivesEvent,
            mouseEventKind: (int)eventKind);
    }

    public static LoweredInstruction UnregisterMouseHandler(TextLocation location, BoundMouseEventKind eventKind)
    {
        return new LoweredInstruction(LoweredInstructionKind.UnregisterMouseHandler, location, mouseEventKind: (int)eventKind);
    }

    public static LoweredInstruction PauseMouseHandler(TextLocation location, BoundMouseEventKind eventKind)
    {
        return new LoweredInstruction(LoweredInstructionKind.PauseMouseHandler, location, mouseEventKind: (int)eventKind);
    }

    public static LoweredInstruction ResumeMouseHandler(TextLocation location, BoundMouseEventKind eventKind)
    {
        return new LoweredInstruction(LoweredInstructionKind.ResumeMouseHandler, location, mouseEventKind: (int)eventKind);
    }

    public static LoweredInstruction PrintValue(TextLocation location)
    {
        return new LoweredInstruction(LoweredInstructionKind.PrintValue, location);
    }

    public static LoweredInstruction Screen(TextLocation location)
    {
        return new LoweredInstruction(LoweredInstructionKind.Screen, location);
    }

    public static LoweredInstruction Cls(TextLocation location)
    {
        return new LoweredInstruction(LoweredInstructionKind.Cls, location);
    }

    public static LoweredInstruction Color(TextLocation location, bool hasBackground)
    {
        return new LoweredInstruction(LoweredInstructionKind.Color, location, valueCount: hasBackground ? 2 : 1);
    }

    public static LoweredInstruction Locate(TextLocation location)
    {
        return new LoweredInstruction(LoweredInstructionKind.Locate, location);
    }

    public static LoweredInstruction PSet(TextLocation location, bool reset, bool hasColor)
    {
        return new LoweredInstruction(LoweredInstructionKind.PSet, location, valueCount: hasColor ? 3 : 2, preserve: reset);
    }

    public static LoweredInstruction Line(TextLocation location, BoundLineBoxKind boxKind, bool hasColor)
    {
        return new LoweredInstruction(LoweredInstructionKind.Line, location, valueCount: hasColor ? 5 : 4, mouseEventKind: (int)boxKind);
    }

    public static LoweredInstruction Circle(TextLocation location, bool hasColor)
    {
        return new LoweredInstruction(LoweredInstructionKind.Circle, location, valueCount: hasColor ? 4 : 3);
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

    public static LoweredInstruction Sleep(TextLocation location, bool hasDuration)
    {
        return new LoweredInstruction(LoweredInstructionKind.Sleep, location, valueCount: hasDuration ? 1 : 0);
    }

    public static LoweredInstruction PushErrorHandler(
        TextLocation location,
        string? catchLabel,
        string? finallyLabel,
        string endLabel,
        VariableSymbol? catchVariable)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endLabel);
        return new LoweredInstruction(
            LoweredInstructionKind.PushErrorHandler,
            location,
            variable: catchVariable,
            catchLabel: catchLabel,
            finallyLabel: finallyLabel,
            endLabel: endLabel);
    }

    public static LoweredInstruction PopErrorHandler(TextLocation location)
    {
        return new LoweredInstruction(LoweredInstructionKind.PopErrorHandler, location);
    }

    public static LoweredInstruction RaiseError(TextLocation location, bool hasMessage)
    {
        return new LoweredInstruction(LoweredInstructionKind.RaiseError, location, valueCount: hasMessage ? 1 : 0);
    }

    public static LoweredInstruction RaiseExistingError(TextLocation location)
    {
        return new LoweredInstruction(LoweredInstructionKind.RaiseError, location, valueCount: 2);
    }

    public static LoweredInstruction EndFinally(TextLocation location)
    {
        return new LoweredInstruction(LoweredInstructionKind.EndFinally, location);
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
