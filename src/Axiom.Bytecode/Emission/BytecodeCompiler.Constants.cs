using Axiom.Core;
using Axiom.Semantics;
using Axiom.Syntax;

namespace Axiom.Bytecode;

public sealed partial class BytecodeCompiler
{
    private int RegisterConstant(BytecodeConstantKind kind, object? value)
    {
        // Равенство DOUBLE объединяет +0 и -0, но знак наблюдаем через SETSIGN/ATN2.
        var keyValue = kind == BytecodeConstantKind.Double && value is double number
            ? (object)BitConverter.DoubleToInt64Bits(number)
            : value;
        var key = new ConstantKey(kind, keyValue);
        if (_constantIndices.TryGetValue(key, out var index))
        {
            return index;
        }

        index = _constants.Count;
        _constants.Add(new BytecodeConstant(kind, value));
        _constantIndices[key] = index;
        return index;
    }

    private int RegisterGlobal(VariableSymbol variable)
    {
        if (_globalSlots.TryGetValue(variable, out var slot))
        {
            return slot;
        }

        slot = _globals.Count;
        _globals.Add(new BytecodeGlobal(
            slot,
            variable.Name,
            variable.Type,
            variable.IsReadOnly,
            GlobalInitializationStrategy.Deferred,
            variable.FixedStringLength));
        _globalSlots[variable] = slot;
        return slot;
    }

    private int RegisterBuiltin(FunctionSymbol function, BuiltinFunctionId builtinId)
    {
        RegisterConstant(BytecodeConstantKind.BuiltinName, function.Name);

        var key = BuiltinBindingKey.Create(function, builtinId);
        if (_builtinIndices.TryGetValue(key, out var index))
        {
            return index;
        }

        index = _builtins.Count;
        _builtins.Add(new BuiltinBinding(
            index,
            builtinId,
            function.Name,
            function.Parameters.Count,
            returnsValue: function.ReturnType != TypeSymbol.Void,
            function));
        _builtinIndices[key] = index;
        return index;
    }

    private static bool TryGetConstantKind(TypeSymbol? type, out BytecodeConstantKind kind)
    {
        if (type == TypeSymbol.Integer)
        {
            kind = BytecodeConstantKind.Integer;
            return true;
        }

        if (type == TypeSymbol.Long)
        {
            kind = BytecodeConstantKind.Long;
            return true;
        }

        if (type == TypeSymbol.Double)
        {
            kind = BytecodeConstantKind.Double;
            return true;
        }

        if (type == TypeSymbol.Decimal)
        {
            kind = BytecodeConstantKind.Decimal;
            return true;
        }

        if (type == TypeSymbol.String)
        {
            kind = BytecodeConstantKind.String;
            return true;
        }

        if (type == TypeSymbol.Boolean)
        {
            kind = BytecodeConstantKind.Boolean;
            return true;
        }

        kind = BytecodeConstantKind.Null;
        return false;
    }

    private static object CreateZeroLiteral(TypeSymbol type)
    {
        if (type == TypeSymbol.Double)
        {
            return 0d;
        }

        if (type == TypeSymbol.Long)
        {
            return 0L;
        }

        if (type == TypeSymbol.Decimal)
        {
            return 0m;
        }

        return 0;
    }

    private void Report(string code, string message, TextLocation location)
    {
        _diagnostics.Add(new BytecodeDiagnostic(code, message, location));
    }

}
