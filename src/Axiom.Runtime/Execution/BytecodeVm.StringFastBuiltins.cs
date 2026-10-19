using Axiom.Semantics;

namespace Axiom.Runtime;

public sealed partial class BytecodeVm
{
    private static bool TryExecuteFastStringBuiltin(
        BytecodeCallFrame frame,
        BuiltinExecutionInfo executionInfo,
        out RuntimeValue result)
    {
        result = RuntimeValue.Void;
        var stack = frame.EvaluationStack;

        switch (executionInfo.CanonicalId)
        {
            case BuiltinFunctionId.Len:
            {
                if (stack.Count < 1)
                {
                    return false;
                }

                var value = stack.PeekUnchecked();
                if (value.Type != TypeSymbol.String)
                {
                    return false;
                }

                stack.PopUnchecked();
                result = RuntimeValue.FromInteger(GetStringUnchecked(value).Length);
                return true;
            }

            case BuiltinFunctionId.TrimWhitespace:
            {
                if (stack.Count < 1)
                {
                    return false;
                }

                var value = stack.PeekUnchecked();
                if (value.Type != TypeSymbol.String)
                {
                    return false;
                }

                stack.PopUnchecked();
                result = RuntimeValue.FromString(GetStringUnchecked(value).Trim());
                return true;
            }

            case BuiltinFunctionId.LTrimWhitespace:
            {
                if (stack.Count < 1)
                {
                    return false;
                }

                var value = stack.PeekUnchecked();
                if (value.Type != TypeSymbol.String)
                {
                    return false;
                }

                stack.PopUnchecked();
                result = RuntimeValue.FromString(GetStringUnchecked(value).TrimStart());
                return true;
            }

            case BuiltinFunctionId.RTrimWhitespace:
            {
                if (stack.Count < 1)
                {
                    return false;
                }

                var value = stack.PeekUnchecked();
                if (value.Type != TypeSymbol.String)
                {
                    return false;
                }

                stack.PopUnchecked();
                result = RuntimeValue.FromString(GetStringUnchecked(value).TrimEnd());
                return true;
            }

            case BuiltinFunctionId.Contains:
            {
                if (stack.Count < 2)
                {
                    return false;
                }

                var right = stack.PeekUnchecked();
                var left = stack.PeekUnchecked(1);
                if (left.Type != TypeSymbol.String || right.Type != TypeSymbol.String)
                {
                    return false;
                }

                stack.PopUnchecked();
                stack.PopUnchecked();
                result = RuntimeValue.FromBoolean(GetStringUnchecked(left).Contains(GetStringUnchecked(right)));
                return true;
            }

            case BuiltinFunctionId.StartsWith:
            {
                if (stack.Count < 2)
                {
                    return false;
                }

                var right = stack.PeekUnchecked();
                var left = stack.PeekUnchecked(1);
                if (left.Type != TypeSymbol.String || right.Type != TypeSymbol.String)
                {
                    return false;
                }

                stack.PopUnchecked();
                stack.PopUnchecked();
                result = RuntimeValue.FromBoolean(GetStringUnchecked(left).StartsWith(GetStringUnchecked(right), StringComparison.Ordinal));
                return true;
            }

            case BuiltinFunctionId.EndsWith:
            {
                if (stack.Count < 2)
                {
                    return false;
                }

                var right = stack.PeekUnchecked();
                var left = stack.PeekUnchecked(1);
                if (left.Type != TypeSymbol.String || right.Type != TypeSymbol.String)
                {
                    return false;
                }

                stack.PopUnchecked();
                stack.PopUnchecked();
                result = RuntimeValue.FromBoolean(GetStringUnchecked(left).EndsWith(GetStringUnchecked(right), StringComparison.Ordinal));
                return true;
            }

            case BuiltinFunctionId.LCase:
            {
                if (stack.Count < 1)
                {
                    return false;
                }

                var value = stack.PeekUnchecked();
                if (value.Type != TypeSymbol.String)
                {
                    return false;
                }

                stack.PopUnchecked();
                var text = GetStringUnchecked(value);
                var lowered = text.ToLowerInvariant();
                result = ReferenceEquals(text, lowered) ? value : RuntimeValue.FromString(lowered);
                return true;
            }

            case BuiltinFunctionId.UCase:
            {
                if (stack.Count < 1)
                {
                    return false;
                }

                var value = stack.PeekUnchecked();
                if (value.Type != TypeSymbol.String)
                {
                    return false;
                }

                stack.PopUnchecked();
                var text = GetStringUnchecked(value);
                var uppered = text.ToUpperInvariant();
                result = ReferenceEquals(text, uppered) ? value : RuntimeValue.FromString(uppered);
                return true;
            }

            case BuiltinFunctionId.InStr:
            {
                if (stack.Count < 2)
                {
                    return false;
                }

                var right = stack.PeekUnchecked();
                var left = stack.PeekUnchecked(1);
                if (left.Type != TypeSymbol.String || right.Type != TypeSymbol.String)
                {
                    return false;
                }

                stack.PopUnchecked();
                stack.PopUnchecked();
                result = RuntimeValue.FromInteger(GetStringUnchecked(left).IndexOf(GetStringUnchecked(right), StringComparison.Ordinal) + 1);
                return true;
            }

            default:
                return false;
        }
    }
}
