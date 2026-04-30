using Axiom.Bytecode;
using Axiom.Core;
using Axiom.Semantics;

namespace Axiom.Runtime;

public sealed partial class BytecodeVm
{
    public static bool SupportsBuiltin(BuiltinFunctionId builtinId, int parameterCount)
    {
        return TryCreateBuiltinExecution(new BuiltinBinding(-1, builtinId, builtinId.ToString(), parameterCount, returnsValue: true, function: null)) is not null;
    }

    private static BuiltinExecution CreateBuiltinExecution(BuiltinBinding builtin)
    {
        return TryCreateBuiltinExecution(builtin)
            ?? throw new InvalidOperationException($"Bytecode VM does not support built-in '{builtin.Name}' ({builtin.CanonicalId}).");
    }

    private static BuiltinExecution? TryCreateBuiltinExecution(BuiltinBinding builtin)
    {
        ArgumentNullException.ThrowIfNull(builtin);

        return builtin.CanonicalId switch
        {
            BuiltinFunctionId.Len => BuiltinExecution.Unary(static (vm, value, _) => RuntimeValue.FromInteger(value.AsString().Length)),
            BuiltinFunctionId.Left => BuiltinExecution.Binary(static (vm, text, count, location) => RuntimeValue.FromString(
                BytecodeVm.EvaluateLeft(
                    text.AsString(),
                    vm.RequirePositiveCount("LEFT", "count", count.AsInteger(), location)))),
            BuiltinFunctionId.Right => BuiltinExecution.Binary(static (vm, text, count, location) => RuntimeValue.FromString(
                BytecodeVm.EvaluateRight(
                    text.AsString(),
                    vm.RequirePositiveCount("RIGHT", "count", count.AsInteger(), location)))),
            BuiltinFunctionId.Mid => BuiltinExecution.Ternary(static (vm, text, start, count, location) => RuntimeValue.FromString(
                BytecodeVm.EvaluateMid(
                    text.AsString(),
                    vm.RequirePositiveStart("MID", start.AsInteger(), location),
                    vm.RequirePositiveCount("MID", "count", count.AsInteger(), location)))),
            BuiltinFunctionId.TrimWhitespace => BuiltinExecution.Unary(static (vm, value, _) => RuntimeValue.FromString(BytecodeVm.EvaluateTrimWhitespace(value.AsString()))),
            BuiltinFunctionId.TrimCharacters => BuiltinExecution.Binary(static (vm, value, chars, _) => RuntimeValue.FromString(BytecodeVm.EvaluateTrimCharacters(value.AsString(), chars.AsString()))),
            BuiltinFunctionId.LTrimWhitespace => BuiltinExecution.Unary(static (vm, value, _) => RuntimeValue.FromString(BytecodeVm.EvaluateLeftTrimWhitespace(value.AsString()))),
            BuiltinFunctionId.LTrimCharacters => BuiltinExecution.Binary(static (vm, value, chars, _) => RuntimeValue.FromString(BytecodeVm.EvaluateLeftTrimCharacters(value.AsString(), chars.AsString()))),
            BuiltinFunctionId.RTrimWhitespace => BuiltinExecution.Unary(static (vm, value, _) => RuntimeValue.FromString(BytecodeVm.EvaluateRightTrimWhitespace(value.AsString()))),
            BuiltinFunctionId.RTrimCharacters => BuiltinExecution.Binary(static (vm, value, chars, _) => RuntimeValue.FromString(BytecodeVm.EvaluateRightTrimCharacters(value.AsString(), chars.AsString()))),
            BuiltinFunctionId.Reverse => BuiltinExecution.Unary(static (vm, value, _) => RuntimeValue.FromString(BytecodeVm.EvaluateReverse(value.AsString()))),
            BuiltinFunctionId.Contains => BuiltinExecution.Binary(static (vm, left, right, _) => RuntimeValue.FromBoolean(BytecodeVm.EvaluateContains(left.AsString(), right.AsString()))),
            BuiltinFunctionId.StartsWith => BuiltinExecution.Binary(static (vm, left, right, _) => RuntimeValue.FromBoolean(BytecodeVm.EvaluateStartsWith(left.AsString(), right.AsString()))),
            BuiltinFunctionId.EndsWith => BuiltinExecution.Binary(static (vm, left, right, _) => RuntimeValue.FromBoolean(BytecodeVm.EvaluateEndsWith(left.AsString(), right.AsString()))),
            BuiltinFunctionId.Replace => BuiltinExecution.Ternary(static (vm, text, oldValue, newValue, location) => RuntimeValue.FromString(
                vm.EvaluateReplace(text.AsString(), oldValue.AsString(), newValue.AsString(), location))),
            BuiltinFunctionId.InStr => BuiltinExecution.Binary(static (vm, text, value, _) => RuntimeValue.FromInteger(BytecodeVm.EvaluateInStr(1, text.AsString(), value.AsString()))),
            BuiltinFunctionId.InStrFrom => BuiltinExecution.Ternary(static (vm, start, text, value, location) => RuntimeValue.FromInteger(
                BytecodeVm.EvaluateInStr(vm.RequirePositiveStart("INSTR", start.AsInteger(), location), text.AsString(), value.AsString()))),
            BuiltinFunctionId.InStrRev => BuiltinExecution.Binary(static (vm, text, value, _) => RuntimeValue.FromInteger(
                BytecodeVm.EvaluateInStrRev(text.AsString().Length, text.AsString(), value.AsString()))),
            BuiltinFunctionId.InStrRevFrom => BuiltinExecution.Ternary(static (vm, start, text, value, location) => RuntimeValue.FromInteger(
                BytecodeVm.EvaluateInStrRev(vm.RequirePositiveStart("INSTRREV", start.AsInteger(), location), text.AsString(), value.AsString()))),
            BuiltinFunctionId.SubStrCount => BuiltinExecution.Binary(static (vm, text, value, location) => RuntimeValue.FromInteger(vm.EvaluateSubStrCount(text.AsString(), value.AsString(), location))),
            BuiltinFunctionId.Repeat => BuiltinExecution.Binary(static (vm, value, count, location) => RuntimeValue.FromString(
                BytecodeVm.EvaluateRepeat(value.AsString(), vm.RequireNonNegativeCount("REPEAT", "count", count.AsInteger(), location)))),
            BuiltinFunctionId.Space => BuiltinExecution.Unary(static (vm, count, location) => RuntimeValue.FromString(
                BytecodeVm.EvaluateSpace(vm.RequireNonNegativeCount("SPACE", "count", count.AsInteger(), location)))),
            BuiltinFunctionId.StringFill => BuiltinExecution.Binary(static (vm, count, fill, location) => RuntimeValue.FromString(
                vm.EvaluateStringFill(
                    vm.RequireNonNegativeCount("STRING", "count", count.AsInteger(), location),
                    fill.AsString(),
                    location))),
            BuiltinFunctionId.PadLeft => BuiltinExecution.Binary(static (vm, value, width, location) => RuntimeValue.FromString(
                value.AsString().PadLeft(vm.RequireNonNegativeCount("PADLEFT", "width", width.AsInteger(), location)))),
            BuiltinFunctionId.PadLeftFill => BuiltinExecution.Ternary(static (vm, value, width, fill, location) => RuntimeValue.FromString(
                vm.EvaluatePadLeft(value.AsString(), vm.RequireNonNegativeCount("PADLEFT", "width", width.AsInteger(), location), fill.AsString(), location))),
            BuiltinFunctionId.PadRight => BuiltinExecution.Binary(static (vm, value, width, location) => RuntimeValue.FromString(
                value.AsString().PadRight(vm.RequireNonNegativeCount("PADRIGHT", "width", width.AsInteger(), location)))),
            BuiltinFunctionId.PadRightFill => BuiltinExecution.Ternary(static (vm, value, width, fill, location) => RuntimeValue.FromString(
                vm.EvaluatePadRight(value.AsString(), vm.RequireNonNegativeCount("PADRIGHT", "width", width.AsInteger(), location), fill.AsString(), location))),
            BuiltinFunctionId.Center => BuiltinExecution.Binary(static (vm, value, width, location) => RuntimeValue.FromString(
                vm.EvaluateCenter(value.AsString(), vm.RequireNonNegativeCount("CENTER", "width", width.AsInteger(), location), " ", location))),
            BuiltinFunctionId.CenterFill => BuiltinExecution.Ternary(static (vm, value, width, fill, location) => RuntimeValue.FromString(
                vm.EvaluateCenter(value.AsString(), vm.RequireNonNegativeCount("CENTER", "width", width.AsInteger(), location), fill.AsString(), location))),
            BuiltinFunctionId.Insert => BuiltinExecution.Ternary(static (vm, value, start, insertText, location) => RuntimeValue.FromString(
                BytecodeVm.EvaluateInsert(value.AsString(), vm.RequirePositiveStart("INSERT", start.AsInteger(), location), insertText.AsString()))),
            BuiltinFunctionId.Remove => BuiltinExecution.Ternary(static (vm, value, start, count, location) => RuntimeValue.FromString(
                BytecodeVm.EvaluateRemove(
                    value.AsString(),
                    vm.RequirePositiveStart("REMOVE", start.AsInteger(), location),
                    vm.RequireNonNegativeCount("REMOVE", "count", count.AsInteger(), location)))),
            BuiltinFunctionId.Asc => BuiltinExecution.Unary(static (vm, value, location) => RuntimeValue.FromInteger(vm.EvaluateAsc(value.AsString(), location))),
            BuiltinFunctionId.Chr => BuiltinExecution.Unary(static (vm, value, location) => RuntimeValue.FromString(
                BytecodeVm.EvaluateChr(vm.RequireValidUnicodeCodePoint("CHR", "code", value.AsInteger(), location)))),
            BuiltinFunctionId.Split => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateSplitBuiltin(value.AsString(), ",", location)),
            BuiltinFunctionId.SplitSeparator => BuiltinExecution.Binary(static (vm, value, separator, location) => vm.EvaluateSplitBuiltin(value.AsString(), separator.AsString(), location)),
            BuiltinFunctionId.Join => BuiltinExecution.Unary(static (vm, value, _) => BytecodeVm.EvaluateJoinBuiltin(value.AsArray(), ",")),
            BuiltinFunctionId.JoinSeparator => BuiltinExecution.Binary(static (vm, value, separator, _) => BytecodeVm.EvaluateJoinBuiltin(value.AsArray(), separator.AsString())),
            BuiltinFunctionId.RegexIsMatch => BuiltinExecution.Binary(static (vm, text, pattern, location) => vm.EvaluateRegexIsMatchBuiltin(text.AsString(), pattern.AsString(), "", location)),
            BuiltinFunctionId.RegexMatch => BuiltinExecution.Binary(static (vm, text, pattern, location) => vm.EvaluateRegexMatchBuiltin(text.AsString(), pattern.AsString(), "", location)),
            BuiltinFunctionId.RegexMatches => BuiltinExecution.Binary(static (vm, text, pattern, location) => vm.EvaluateRegexMatchesBuiltin(text.AsString(), pattern.AsString(), "", location)),
            BuiltinFunctionId.RegexSplit => BuiltinExecution.Binary(static (vm, text, pattern, location) => vm.EvaluateRegexSplitBuiltin(text.AsString(), pattern.AsString(), "", location)),
            BuiltinFunctionId.RegexCount => BuiltinExecution.Binary(static (vm, text, pattern, location) => vm.EvaluateRegexCountBuiltin(text.AsString(), pattern.AsString(), "", location)),
            BuiltinFunctionId.RegexExtract => BuiltinExecution.Ternary(static (vm, text, pattern, groupIndex, location) => vm.EvaluateRegexExtractBuiltin(text.AsString(), pattern.AsString(), groupIndex.AsInteger(), "", location)),
            BuiltinFunctionId.RegexExtractAll => BuiltinExecution.Ternary(static (vm, text, pattern, groupIndex, location) => vm.EvaluateRegexExtractAllBuiltin(text.AsString(), pattern.AsString(), groupIndex.AsInteger(), "", location)),
            BuiltinFunctionId.RegexReplace => BuiltinExecution.Ternary(static (vm, text, pattern, replacement, location) => vm.EvaluateRegexReplaceBuiltin(text.AsString(), pattern.AsString(), replacement.AsString(), "", count: null, location)),
            BuiltinFunctionId.Int => BuiltinExecution.Unary(static (vm, value, location) => RuntimeValue.FromInteger(vm.EvaluateIntBuiltin(value, location))),
            BuiltinFunctionId.Str => BuiltinExecution.Unary(static (vm, value, _) => RuntimeValue.FromString(value.ToDisplayString())),
            BuiltinFunctionId.Val => BuiltinExecution.Unary(static (vm, value, _) => RuntimeValue.FromDouble(ParseVal(value.AsString()))),
            BuiltinFunctionId.LCase => BuiltinExecution.Unary(static (vm, value, _) => RuntimeValue.FromString(value.AsString().ToLowerInvariant())),
            BuiltinFunctionId.UCase => BuiltinExecution.Unary(static (vm, value, _) => RuntimeValue.FromString(value.AsString().ToUpperInvariant())),
            BuiltinFunctionId.Rnd => BuiltinExecution.Nullary(static (vm, _) => vm.EvaluateRndBuiltin()),
            BuiltinFunctionId.Abs => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateAbsBuiltin(value, location)),
            BuiltinFunctionId.Sgn => BuiltinExecution.Unary(static (vm, value, location) => RuntimeValue.FromInteger(vm.EvaluateSgnBuiltin(value, location))),
            BuiltinFunctionId.Min => BuiltinExecution.Binary(static (vm, left, right, location) => vm.EvaluateMinBuiltin(left, right, location)),
            BuiltinFunctionId.Max => BuiltinExecution.Binary(static (vm, left, right, location) => vm.EvaluateMaxBuiltin(left, right, location)),
            BuiltinFunctionId.Clamp => BuiltinExecution.Ternary(static (vm, value, minValue, maxValue, location) => vm.EvaluateClampBuiltin(value, minValue, maxValue, location)),
            BuiltinFunctionId.Sin => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateDoubleMathBuiltin("SIN", "value", value, Math.Sin, location)),
            BuiltinFunctionId.Cos => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateDoubleMathBuiltin("COS", "value", value, Math.Cos, location)),
            BuiltinFunctionId.Tan => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateDoubleMathBuiltin("TAN", "value", value, Math.Tan, location)),
            BuiltinFunctionId.Atn => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateDoubleMathBuiltin("ATN", "value", value, Math.Atan, location)),
            BuiltinFunctionId.Atn2 => BuiltinExecution.Binary(static (vm, left, right, location) => vm.EvaluateAtn2Builtin(left, right, location)),
            BuiltinFunctionId.Sqr => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateSqrBuiltin(value, location)),
            BuiltinFunctionId.Cbrt => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateDoubleMathBuiltin("CBRT", "value", value, Math.Cbrt, location)),
            BuiltinFunctionId.Exp => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateDoubleMathBuiltin("EXP", "value", value, Math.Exp, location)),
            BuiltinFunctionId.Log => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateLogBuiltin("LOG", value, Math.Log, location)),
            BuiltinFunctionId.Log10 => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateLogBuiltin("LOG10", value, Math.Log10, location)),
            BuiltinFunctionId.Log2 => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateLogBuiltin("LOG2", value, Math.Log2, location)),
            BuiltinFunctionId.Pow => BuiltinExecution.Binary(static (vm, left, right, location) => vm.EvaluatePowBuiltin(left, right, location)),
            BuiltinFunctionId.Floor => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateFloorBuiltin(value, location)),
            BuiltinFunctionId.Ceil => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateCeilBuiltin(value, location)),
            BuiltinFunctionId.Round => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateRoundBuiltin(value, location)),
            BuiltinFunctionId.Fix => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateFixBuiltin(value, location)),
            BuiltinFunctionId.Asin => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateInverseTrigBuiltin("ASIN", value, Math.Asin, location)),
            BuiltinFunctionId.Acos => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateInverseTrigBuiltin("ACOS", value, Math.Acos, location)),
            BuiltinFunctionId.Asinh => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateDoubleMathBuiltin("ASINH", "value", value, Math.Asinh, location)),
            BuiltinFunctionId.Acosh => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateAcoshBuiltin(value, location)),
            BuiltinFunctionId.Atanh => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateAtanhBuiltin(value, location)),
            BuiltinFunctionId.Sinh => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateDoubleMathBuiltin("SINH", "value", value, Math.Sinh, location)),
            BuiltinFunctionId.Cosh => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateDoubleMathBuiltin("COSH", "value", value, Math.Cosh, location)),
            BuiltinFunctionId.Tanh => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateDoubleMathBuiltin("TANH", "value", value, Math.Tanh, location)),
            BuiltinFunctionId.SetSign => BuiltinExecution.Binary(static (vm, left, right, location) => vm.EvaluateSetSignBuiltin(left, right, location)),
            BuiltinFunctionId.ArrayLength => BuiltinExecution.Unary(static (vm, value, _) => RuntimeValue.FromInteger(BytecodeVm.EvaluateArrayLengthBuiltin(value))),
            BuiltinFunctionId.ArrayIsAllocated => BuiltinExecution.Unary(static (vm, value, _) => RuntimeValue.FromBoolean(value.AsArray().IsAllocated)),
            BuiltinFunctionId.ArrayRank => BuiltinExecution.Unary(static (vm, value, _) => RuntimeValue.FromInteger(BytecodeVm.EvaluateArrayRankBuiltin(value))),
            BuiltinFunctionId.ArrayBounds => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateArrayBoundsBuiltin(value, location)),
            BuiltinFunctionId.ArrayClone => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateArrayCloneBuiltin(value, location)),
            BuiltinFunctionId.ArrayFlatten => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateArrayFlattenBuiltin(value, location)),
            BuiltinFunctionId.ArrayReshape => BuiltinExecution.Binary(static (vm, left, right, location) => vm.EvaluateArrayReshapeBuiltin(left, right, location)),
            BuiltinFunctionId.ArrayDistinct => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateArrayDistinctBuiltin(value, location)),
            BuiltinFunctionId.ArrayFill => BuiltinExecution.Binary(static (vm, left, right, location) => vm.EvaluateArrayFillBuiltin(left, right, location)),
            BuiltinFunctionId.ArrayContains => BuiltinExecution.Binary(static (vm, left, right, location) => RuntimeValue.FromBoolean(vm.EvaluateArrayContainsBuiltin(left, right, location))),
            BuiltinFunctionId.ArrayIndexOf => BuiltinExecution.Binary(static (vm, left, right, location) => vm.EvaluateArrayIndexOfBuiltin(left, right, searchFromEnd: false, location)),
            BuiltinFunctionId.ArrayLastIndexOf => BuiltinExecution.Binary(static (vm, left, right, location) => vm.EvaluateArrayIndexOfBuiltin(left, right, searchFromEnd: true, location)),
            BuiltinFunctionId.ArrayCount => BuiltinExecution.Binary(static (vm, left, right, location) => RuntimeValue.FromInteger(vm.EvaluateArrayCountBuiltin(left, right, location))),
            BuiltinFunctionId.ArrayFindAll => BuiltinExecution.Binary(static (vm, left, right, location) => vm.EvaluateArrayFindAllBuiltin(left, right, location)),
            BuiltinFunctionId.ArrayEvery => BuiltinExecution.Binary(static (vm, left, right, location) => RuntimeValue.FromBoolean(vm.EvaluateArrayEveryBuiltin(left, right, location))),
            BuiltinFunctionId.ArraySome => BuiltinExecution.Binary(static (vm, left, right, location) => RuntimeValue.FromBoolean(vm.EvaluateArraySomeBuiltin(left, right, location))),
            BuiltinFunctionId.ArrayFilter => BuiltinExecution.Binary(static (vm, left, right, location) => vm.EvaluateArrayFilterBuiltin(left, right, location)),
            BuiltinFunctionId.ArrayMap => BuiltinExecution.Binary(static (vm, left, right, location) => vm.EvaluateArrayMapBuiltin(left, right, location)),
            BuiltinFunctionId.ArrayReduce => BuiltinExecution.Ternary(static (vm, array, reducer, seed, location) => vm.EvaluateArrayReduceBuiltin(array, reducer, seed, location)),
            BuiltinFunctionId.Date => BuiltinExecution.Nullary(static (vm, _) => vm.EvaluateDateBuiltin()),
            BuiltinFunctionId.DateInZone => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateDateBuiltin(value.AsString(), location)),
            BuiltinFunctionId.Time => BuiltinExecution.Nullary(static (vm, _) => vm.EvaluateTimeBuiltin()),
            BuiltinFunctionId.TimeInZone => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateTimeBuiltin(value.AsString(), location)),
            BuiltinFunctionId.Now => BuiltinExecution.Nullary(static (vm, _) => vm.EvaluateNowBuiltin()),
            BuiltinFunctionId.NowInZone => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateNowBuiltin(value.AsString(), location)),
            BuiltinFunctionId.UtcDate => BuiltinExecution.Nullary(static (vm, _) => vm.EvaluateUtcDateBuiltin()),
            BuiltinFunctionId.UtcTime => BuiltinExecution.Nullary(static (vm, _) => vm.EvaluateUtcTimeBuiltin()),
            BuiltinFunctionId.UtcNow => BuiltinExecution.Nullary(static (vm, _) => vm.EvaluateUtcNowBuiltin()),
            BuiltinFunctionId.Timer => BuiltinExecution.Nullary(static (vm, _) => vm.EvaluateTimerBuiltin()),
            BuiltinFunctionId.DateSerial => BuiltinExecution.Ternary(static (vm, year, month, day, location) => vm.EvaluateDateSerialBuiltin(year.AsInteger(), month.AsInteger(), day.AsInteger(), location)),
            BuiltinFunctionId.TimeSerial => BuiltinExecution.Ternary(static (vm, hour, minute, second, location) => vm.EvaluateTimeSerialBuiltin(hour.AsInteger(), minute.AsInteger(), second.AsInteger(), location)),
            BuiltinFunctionId.Year => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateYearBuiltin(value.AsString(), location)),
            BuiltinFunctionId.Month => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateMonthBuiltin(value.AsString(), location)),
            BuiltinFunctionId.MonthName => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateMonthNameBuiltin(value.AsInteger(), location)),
            BuiltinFunctionId.Day => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateDayBuiltin(value.AsString(), location)),
            BuiltinFunctionId.Hour => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateHourBuiltin(value.AsString(), location)),
            BuiltinFunctionId.Minute => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateMinuteBuiltin(value.AsString(), location)),
            BuiltinFunctionId.Second => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateSecondBuiltin(value.AsString(), location)),
            BuiltinFunctionId.Weekday => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateWeekdayBuiltin(value.AsString(), location)),
            BuiltinFunctionId.WeekdayName => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateWeekdayNameBuiltin(value.AsInteger(), location)),
            BuiltinFunctionId.DateAdd => BuiltinExecution.Ternary(static (vm, interval, value, dateText, location) => vm.EvaluateDateAddBuiltin(interval.AsString(), value.AsInteger(), dateText.AsString(), location)),
            BuiltinFunctionId.DateDiff => BuiltinExecution.Ternary(static (vm, interval, left, right, location) => vm.EvaluateDateDiffBuiltin(interval.AsString(), left.AsString(), right.AsString(), location)),
            BuiltinFunctionId.DatePart => BuiltinExecution.Binary(static (vm, interval, dateText, location) => vm.EvaluateDatePartBuiltin(interval.AsString(), dateText.AsString(), location)),
            BuiltinFunctionId.DateValue => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateDateValueBuiltin(value.AsString(), location)),
            BuiltinFunctionId.TimeValue => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateTimeValueBuiltin(value.AsString(), location)),
            BuiltinFunctionId.IsDate => BuiltinExecution.Unary(static (vm, value, _) => vm.EvaluateIsDateBuiltin(value.AsString())),
            BuiltinFunctionId.UtcOffset => BuiltinExecution.Nullary(static (vm, _) => vm.EvaluateUtcOffsetBuiltin()),
            BuiltinFunctionId.UtcOffsetValue => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateUtcOffsetBuiltin(value.AsString(), location)),
            BuiltinFunctionId.UtcOffsetValueZone => BuiltinExecution.Binary(static (vm, value, zone, location) => vm.EvaluateUtcOffsetBuiltin(value.AsString(), zone.AsString(), location)),
            BuiltinFunctionId.ToUtc => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateToUtcBuiltin(value.AsString(), location)),
            BuiltinFunctionId.ToUtcZone => BuiltinExecution.Binary(static (vm, value, zone, location) => vm.EvaluateToUtcBuiltin(value.AsString(), zone.AsString(), location)),
            BuiltinFunctionId.FromUtc => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateFromUtcBuiltin(value.AsString(), location)),
            BuiltinFunctionId.FromUtcZone => BuiltinExecution.Binary(static (vm, value, zone, location) => vm.EvaluateFromUtcBuiltin(value.AsString(), zone.AsString(), location)),
            BuiltinFunctionId.IsLeapYear => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateIsLeapYearBuiltin(value.AsInteger(), location)),
            BuiltinFunctionId.DaysInMonth => BuiltinExecution.Binary(static (vm, year, month, location) => vm.EvaluateDaysInMonthBuiltin(year.AsInteger(), month.AsInteger(), location)),
            BuiltinFunctionId.StartOfMonth => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateStartOfMonthBuiltin(value.AsString(), location)),
            BuiltinFunctionId.EndOfMonth => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateEndOfMonthBuiltin(value.AsString(), location)),
            BuiltinFunctionId.StartOfYear => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateStartOfYearBuiltin(value.AsString(), location)),
            BuiltinFunctionId.EndOfYear => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateEndOfYearBuiltin(value.AsString(), location)),
            BuiltinFunctionId.IsOpen => BuiltinExecution.Unary(static (vm, value, _) => vm.EvaluateIsOpenBuiltin(value)),
            BuiltinFunctionId.FilePath => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateFilePathBuiltin(value, location)),
            BuiltinFunctionId.FileMode => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateFileModeBuiltin(value, location)),
            BuiltinFunctionId.FilePos => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateFilePosBuiltin(value, location)),
            BuiltinFunctionId.FileLength => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateFileLengthBuiltin(value, location)),
            BuiltinFunctionId.LineNumber => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateLineNumberBuiltin(value, location)),
            BuiltinFunctionId.Eof => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateEofBuiltin(value, location)),
            BuiltinFunctionId.ReadToEnd => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateReadToEndBuiltin(value, location)),
            BuiltinFunctionId.ReadText => BuiltinExecution.Binary(static (vm, value, count, location) => vm.EvaluateReadTextBuiltin(
                value,
                vm.RequireNonNegativeCount("READTEXT", "count", count.AsInteger(), location),
                location)),
            BuiltinFunctionId.ReadLine => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateReadLineBuiltin(value, location)),
            BuiltinFunctionId.ReadLines => BuiltinExecution.Binary(static (vm, value, count, location) => vm.EvaluateReadLinesBuiltin(
                value,
                vm.RequireNonNegativeCount("READLINES", "count", count.AsInteger(), location),
                location)),
            BuiltinFunctionId.SkipLine => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateSkipLineBuiltin(value, location)),
            BuiltinFunctionId.Write => BuiltinExecution.Binary(static (vm, handle, text, location) => vm.EvaluateWriteBuiltin(handle, text.AsString(), appendLineBreak: false, location)),
            BuiltinFunctionId.WriteLine => BuiltinExecution.Binary(static (vm, handle, text, location) => vm.EvaluateWriteBuiltin(handle, text.AsString(), appendLineBreak: true, location)),
            BuiltinFunctionId.NewLine => BuiltinExecution.Unary(static (vm, handle, location) => vm.EvaluateNewLineBuiltin(handle, location)),
            BuiltinFunctionId.Flush => BuiltinExecution.Unary(static (vm, handle, location) => vm.EvaluateFlushBuiltin(handle, location)),
            BuiltinFunctionId.SeekFile => BuiltinExecution.Binary(static (vm, handle, position, location) => vm.EvaluateSeekFileBuiltin(handle, position, location)),
            BuiltinFunctionId.Rewind => BuiltinExecution.Unary(static (vm, handle, location) => vm.EvaluateRewindBuiltin(handle, location)),
            BuiltinFunctionId.FileExists => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateFileExistsBuiltin(value.AsString(), location)),
            BuiltinFunctionId.DirExists => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateDirExistsBuiltin(value.AsString(), location)),
            BuiltinFunctionId.PathExists => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluatePathExistsBuiltin(value.AsString(), location)),
            BuiltinFunctionId.ReadFile => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateReadFileBuiltin(value.AsString(), location)),
            BuiltinFunctionId.ReadAllLines => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateReadAllLinesBuiltin(value.AsString(), location)),
            BuiltinFunctionId.WriteFile => BuiltinExecution.Binary(static (vm, path, text, location) => vm.EvaluateWriteFileBuiltin(path.AsString(), text.AsString(), location)),
            BuiltinFunctionId.AppendFile => BuiltinExecution.Binary(static (vm, path, text, location) => vm.EvaluateAppendFileBuiltin(path.AsString(), text.AsString(), location)),
            BuiltinFunctionId.FileSize => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateFileSizeBuiltin(value.AsString(), location)),
            BuiltinFunctionId.FileDate => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateFileDateBuiltin(value.AsString(), location)),
            BuiltinFunctionId.FileCreateDate => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateFileCreateDateBuiltin(value.AsString(), location)),
            BuiltinFunctionId.FileAccessDate => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateFileAccessDateBuiltin(value.AsString(), location)),
            BuiltinFunctionId.DirDate => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateDirDateBuiltin(value.AsString(), location)),
            BuiltinFunctionId.GetFiles => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateGetFilesBuiltin(value.AsString(), "*", -1, location)),
            BuiltinFunctionId.GetFilesPattern => BuiltinExecution.Binary(static (vm, path, pattern, location) => vm.EvaluateGetFilesBuiltin(path.AsString(), pattern.AsString(), 1, location)),
            BuiltinFunctionId.GetDirs => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateGetDirsBuiltin(value.AsString(), "*", -1, location)),
            BuiltinFunctionId.GetDirsPattern => BuiltinExecution.Binary(static (vm, path, pattern, location) => vm.EvaluateGetDirsBuiltin(path.AsString(), pattern.AsString(), 1, location)),
            BuiltinFunctionId.CombinePath => BuiltinExecution.Binary(static (vm, left, right, location) => vm.EvaluateCombinePathBuiltin(left.AsString(), right.AsString(), location)),
            BuiltinFunctionId.FileName => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateFileNameBuiltin(value.AsString(), location)),
            BuiltinFunctionId.DirName => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateDirNameBuiltin(value.AsString(), location)),
            BuiltinFunctionId.ExtName => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateExtNameBuiltin(value.AsString(), location)),
            BuiltinFunctionId.FullPath => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateFullPathBuiltin(value.AsString(), location)),
            BuiltinFunctionId.PathRoot => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluatePathRootBuiltin(value.AsString(), location)),
            BuiltinFunctionId.IsAbsolutePath => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateIsAbsolutePathBuiltin(value.AsString(), location)),
            BuiltinFunctionId.MakeDir => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateMakeDirBuiltin(value.AsString(), location)),
            BuiltinFunctionId.DeleteFile => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateDeleteFileBuiltin(value.AsString(), location)),
            BuiltinFunctionId.CopyFile => BuiltinExecution.Binary(static (vm, source, target, location) => vm.EvaluateCopyFileBuiltin(source.AsString(), target.AsString(), overwrite: false, location)),
            BuiltinFunctionId.CopyFileOverwrite => BuiltinExecution.Ternary(static (vm, source, target, overwrite, location) => vm.EvaluateCopyFileBuiltin(source.AsString(), target.AsString(), overwrite.AsBoolean(), location)),
            BuiltinFunctionId.MoveFile => BuiltinExecution.Binary(static (vm, source, target, location) => vm.EvaluateMoveFileBuiltin(source.AsString(), target.AsString(), overwrite: false, location)),
            BuiltinFunctionId.MoveFileOverwrite => BuiltinExecution.Ternary(static (vm, source, target, overwrite, location) => vm.EvaluateMoveFileBuiltin(source.AsString(), target.AsString(), overwrite.AsBoolean(), location)),
            BuiltinFunctionId.RenameFile => BuiltinExecution.Binary(static (vm, source, target, location) => vm.EvaluateRenameFileBuiltin(source.AsString(), target.AsString(), location)),
            BuiltinFunctionId.DeleteDir => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateDeleteDirBuiltin(value.AsString(), location)),
            BuiltinFunctionId.DeleteDirRecursive => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateDeleteDirRecursiveBuiltin(value.AsString(), location)),
            BuiltinFunctionId.CopyDir => BuiltinExecution.Binary(static (vm, source, target, location) => vm.EvaluateCopyDirBuiltin(source.AsString(), target.AsString(), overwrite: false, location)),
            BuiltinFunctionId.CopyDirOverwrite => BuiltinExecution.Ternary(static (vm, source, target, overwrite, location) => vm.EvaluateCopyDirBuiltin(source.AsString(), target.AsString(), overwrite.AsBoolean(), location)),
            BuiltinFunctionId.MoveDir => BuiltinExecution.Binary(static (vm, source, target, location) => vm.EvaluateMoveDirBuiltin(source.AsString(), target.AsString(), location)),
            BuiltinFunctionId.RenameDir => BuiltinExecution.Binary(static (vm, source, target, location) => vm.EvaluateRenameDirBuiltin(source.AsString(), target.AsString(), location)),
            BuiltinFunctionId.CurrentDir => BuiltinExecution.Nullary(static (vm, location) => vm.EvaluateCurrentDirBuiltin(location)),
            BuiltinFunctionId.ChangeDir => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateChangeDirBuiltin(value.AsString(), location)),
            BuiltinFunctionId.DirSize => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateDirSizeBuiltin(value.AsString(), location)),
            BuiltinFunctionId.FreeSpace => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateFreeSpaceBuiltin(value.AsString(), location)),
            BuiltinFunctionId.TotalFreeSpace => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateTotalFreeSpaceBuiltin(value.AsString(), location)),
            BuiltinFunctionId.TotalSpace => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateTotalSpaceBuiltin(value.AsString(), location)),
            BuiltinFunctionId.IsReadOnly => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateIsReadOnlyBuiltin(value.AsString(), location)),
            BuiltinFunctionId.IsHidden => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateIsHiddenBuiltin(value.AsString(), location)),
            BuiltinFunctionId.DriveName => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateDriveNameBuiltin(value.AsString(), location)),
            BuiltinFunctionId.DriveFormat => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateDriveFormatBuiltin(value.AsString(), location)),
            BuiltinFunctionId.DriveType => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateDriveTypeBuiltin(value.AsString(), location)),
            BuiltinFunctionId.MakeTempFile => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateMakeTempFileBuiltin(value.AsString(), location)),
            BuiltinFunctionId.MakeTempDir => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateMakeTempDirBuiltin(value.AsString(), location)),

            BuiltinFunctionId.RegexIsMatchModes
            or BuiltinFunctionId.RegexMatchModes
            or BuiltinFunctionId.RegexExtractModes
            or BuiltinFunctionId.RegexMatchesModes
            or BuiltinFunctionId.RegexExtractAllModes
            or BuiltinFunctionId.RegexSplitModes
            or BuiltinFunctionId.RegexReplaceModes
            or BuiltinFunctionId.RegexReplaceCount
            or BuiltinFunctionId.RegexReplaceModesCount
            or BuiltinFunctionId.RegexCountModes
            or BuiltinFunctionId.LBound
            or BuiltinFunctionId.UBound
            or BuiltinFunctionId.ArraySlice
            or BuiltinFunctionId.ArrayToSorted
            or BuiltinFunctionId.ArrayToReversed
            or BuiltinFunctionId.ArrayTake
            or BuiltinFunctionId.ArraySkip
            or BuiltinFunctionId.ArrayConcat
            or BuiltinFunctionId.ArrayReverse
            or BuiltinFunctionId.ArraySort
            or BuiltinFunctionId.ArrayPush
            or BuiltinFunctionId.ArrayPop
            or BuiltinFunctionId.ArrayInsert
            or BuiltinFunctionId.ArrayRemove
            or BuiltinFunctionId.ArrayBinarySearch
                => BuiltinExecution.Variadic((vm, arguments, location) => vm.ExecuteBuiltin(builtin, arguments.ToArray(), location)),

            _ => null
        };
    }

    private enum BuiltinExecutionArity
    {
        Nullary,
        Unary,
        Binary,
        Ternary,
        Variadic
    }

    private delegate RuntimeValue BuiltinExecution0(BytecodeVm vm, TextLocation location);
    private delegate RuntimeValue BuiltinExecution1(BytecodeVm vm, RuntimeValue first, TextLocation location);
    private delegate RuntimeValue BuiltinExecution2(BytecodeVm vm, RuntimeValue first, RuntimeValue second, TextLocation location);
    private delegate RuntimeValue BuiltinExecution3(BytecodeVm vm, RuntimeValue first, RuntimeValue second, RuntimeValue third, TextLocation location);
    private delegate RuntimeValue BuiltinExecutionMany(BytecodeVm vm, ReadOnlySpan<RuntimeValue> arguments, TextLocation location);

    private sealed class BuiltinExecution
    {
        private BuiltinExecution(
            BuiltinExecutionArity arity,
            BuiltinExecution0? zero,
            BuiltinExecution1? one,
            BuiltinExecution2? two,
            BuiltinExecution3? three,
            BuiltinExecutionMany? many)
        {
            Arity = arity;
            Zero = zero;
            One = one;
            Two = two;
            Three = three;
            Many = many;
        }

        public BuiltinExecutionArity Arity { get; }

        public BuiltinExecution0? Zero { get; }

        public BuiltinExecution1? One { get; }

        public BuiltinExecution2? Two { get; }

        public BuiltinExecution3? Three { get; }

        public BuiltinExecutionMany? Many { get; }

        public static BuiltinExecution Nullary(BuiltinExecution0 execution) => new(BuiltinExecutionArity.Nullary, execution, null, null, null, null);

        public static BuiltinExecution Unary(BuiltinExecution1 execution) => new(BuiltinExecutionArity.Unary, null, execution, null, null, null);

        public static BuiltinExecution Binary(BuiltinExecution2 execution) => new(BuiltinExecutionArity.Binary, null, null, execution, null, null);

        public static BuiltinExecution Ternary(BuiltinExecution3 execution) => new(BuiltinExecutionArity.Ternary, null, null, null, execution, null);

        public static BuiltinExecution Variadic(BuiltinExecutionMany execution) => new(BuiltinExecutionArity.Variadic, null, null, null, null, execution);
    }
}
