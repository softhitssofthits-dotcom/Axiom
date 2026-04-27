namespace Axiom.Bytecode;

public enum LoweredInstructionKind
{
    Label,
    LoadLiteral,
    LoadFunction,
    LoadVariable,
    StoreVariable,
    UnaryOperation,
    BinaryOperation,
    Convert,
    CallFunction,
    CallValue,
    RedimArray,
    EraseArray,
    LoadElement,
    StoreElement,
    LoadField,
    StoreField,
    OpenFile,
    CloseFile,
    ValidateForStep,
    PrintValue,
    PrintUsing,
    PrintSeparator,
    PrintLine,
    Input,
    Jump,
    JumpIfTrue,
    JumpIfFalse,
    Pop,
    Return
}
