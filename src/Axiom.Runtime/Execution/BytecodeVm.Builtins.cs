using System.Globalization;
using Axiom.Bytecode;
using Axiom.Core;
using Axiom.Semantics;

namespace Axiom.Runtime;

public sealed partial class BytecodeVm
{
    private RuntimeValue ExecuteBuiltin(BuiltinBinding builtin, ReadOnlySpan<RuntimeValue> arguments, TextLocation location)
    {
        return ExecuteBuiltin(builtin, (IReadOnlyList<RuntimeValue>)arguments.ToArray(), location);
    }

    private RuntimeValue ExecuteBuiltin(BuiltinBinding builtin, IReadOnlyList<RuntimeValue> arguments, TextLocation location)
    {
        return builtin.CanonicalId switch
        {
            BuiltinFunctionId.Len => RuntimeValue.FromInteger(arguments[0].AsString().Length),
            BuiltinFunctionId.Left => RuntimeValue.FromString(
                EvaluateLeft(
                    arguments[0].AsString(),
                    RequirePositiveCount("LEFT", "count", arguments[1].AsInteger(), location))),
            BuiltinFunctionId.Right => RuntimeValue.FromString(
                EvaluateRight(
                    arguments[0].AsString(),
                    RequirePositiveCount("RIGHT", "count", arguments[1].AsInteger(), location))),
            BuiltinFunctionId.Mid => RuntimeValue.FromString(
                EvaluateMid(
                    arguments[0].AsString(),
                    RequirePositiveStart("MID", arguments[1].AsInteger(), location),
                    RequirePositiveCount("MID", "count", arguments[2].AsInteger(), location))),
            BuiltinFunctionId.TrimWhitespace => RuntimeValue.FromString(EvaluateTrimWhitespace(arguments[0].AsString())),
            BuiltinFunctionId.TrimCharacters => RuntimeValue.FromString(EvaluateTrimCharacters(arguments[0].AsString(), arguments[1].AsString())),
            BuiltinFunctionId.LTrimWhitespace => RuntimeValue.FromString(EvaluateLeftTrimWhitespace(arguments[0].AsString())),
            BuiltinFunctionId.LTrimCharacters => RuntimeValue.FromString(EvaluateLeftTrimCharacters(arguments[0].AsString(), arguments[1].AsString())),
            BuiltinFunctionId.RTrimWhitespace => RuntimeValue.FromString(EvaluateRightTrimWhitespace(arguments[0].AsString())),
            BuiltinFunctionId.RTrimCharacters => RuntimeValue.FromString(EvaluateRightTrimCharacters(arguments[0].AsString(), arguments[1].AsString())),
            BuiltinFunctionId.Reverse => RuntimeValue.FromString(EvaluateReverse(arguments[0].AsString())),
            BuiltinFunctionId.Contains => RuntimeValue.FromBoolean(EvaluateContains(arguments[0].AsString(), arguments[1].AsString())),
            BuiltinFunctionId.StartsWith => RuntimeValue.FromBoolean(EvaluateStartsWith(arguments[0].AsString(), arguments[1].AsString())),
            BuiltinFunctionId.EndsWith => RuntimeValue.FromBoolean(EvaluateEndsWith(arguments[0].AsString(), arguments[1].AsString())),
            BuiltinFunctionId.Replace => RuntimeValue.FromString(
                EvaluateReplace(
                    arguments[0].AsString(),
                    arguments[1].AsString(),
                    arguments[2].AsString(),
                    location)),
            BuiltinFunctionId.InStr => RuntimeValue.FromInteger(EvaluateInStr(1, arguments[0].AsString(), arguments[1].AsString())),
            BuiltinFunctionId.InStrFrom => RuntimeValue.FromInteger(
                EvaluateInStr(
                    RequirePositiveStart("INSTR", arguments[0].AsInteger(), location),
                    arguments[1].AsString(),
                    arguments[2].AsString())),
            BuiltinFunctionId.InStrRev => RuntimeValue.FromInteger(EvaluateInStrRev(arguments[0].AsString().Length, arguments[0].AsString(), arguments[1].AsString())),
            BuiltinFunctionId.InStrRevFrom => RuntimeValue.FromInteger(
                EvaluateInStrRev(
                    RequirePositiveStart("INSTRREV", arguments[0].AsInteger(), location),
                    arguments[1].AsString(),
                    arguments[2].AsString())),
            BuiltinFunctionId.SubStrCount => RuntimeValue.FromInteger(EvaluateSubStrCount(arguments[0].AsString(), arguments[1].AsString(), location)),
            BuiltinFunctionId.Repeat => RuntimeValue.FromString(
                EvaluateRepeat(
                    arguments[0].AsString(),
                    RequireNonNegativeCount("REPEAT", "count", arguments[1].AsInteger(), location))),
            BuiltinFunctionId.Space => RuntimeValue.FromString(
                EvaluateSpace(
                    RequireNonNegativeCount("SPACE", "count", arguments[0].AsInteger(), location))),
            BuiltinFunctionId.StringFill => RuntimeValue.FromString(
                EvaluateStringFill(
                    RequireNonNegativeCount("STRING", "count", arguments[0].AsInteger(), location),
                    arguments[1].AsString(),
                    location)),
            BuiltinFunctionId.PadLeft => RuntimeValue.FromString(
                arguments[0].AsString().PadLeft(
                    RequireNonNegativeCount("PADLEFT", "width", arguments[1].AsInteger(), location))),
            BuiltinFunctionId.PadLeftFill => RuntimeValue.FromString(
                EvaluatePadLeft(
                    arguments[0].AsString(),
                    RequireNonNegativeCount("PADLEFT", "width", arguments[1].AsInteger(), location),
                    arguments[2].AsString(),
                    location)),
            BuiltinFunctionId.PadRight => RuntimeValue.FromString(
                arguments[0].AsString().PadRight(
                    RequireNonNegativeCount("PADRIGHT", "width", arguments[1].AsInteger(), location))),
            BuiltinFunctionId.PadRightFill => RuntimeValue.FromString(
                EvaluatePadRight(
                    arguments[0].AsString(),
                    RequireNonNegativeCount("PADRIGHT", "width", arguments[1].AsInteger(), location),
                    arguments[2].AsString(),
                    location)),
            BuiltinFunctionId.Center => RuntimeValue.FromString(
                EvaluateCenter(
                    arguments[0].AsString(),
                    RequireNonNegativeCount("CENTER", "width", arguments[1].AsInteger(), location),
                    " ",
                    location)),
            BuiltinFunctionId.CenterFill => RuntimeValue.FromString(
                EvaluateCenter(
                    arguments[0].AsString(),
                    RequireNonNegativeCount("CENTER", "width", arguments[1].AsInteger(), location),
                    arguments[2].AsString(),
                    location)),
            BuiltinFunctionId.Insert => RuntimeValue.FromString(
                EvaluateInsert(
                    arguments[0].AsString(),
                    RequirePositiveStart("INSERT", arguments[1].AsInteger(), location),
                    arguments[2].AsString())),
            BuiltinFunctionId.Remove => RuntimeValue.FromString(
                EvaluateRemove(
                    arguments[0].AsString(),
                    RequirePositiveStart("REMOVE", arguments[1].AsInteger(), location),
                    RequireNonNegativeCount("REMOVE", "count", arguments[2].AsInteger(), location))),
            BuiltinFunctionId.Asc => RuntimeValue.FromInteger(EvaluateAsc(arguments[0].AsString(), location)),
            BuiltinFunctionId.Chr => RuntimeValue.FromString(
                EvaluateChr(
                    RequireValidUnicodeCodePoint("CHR", "code", arguments[0].AsInteger(), location))),
            BuiltinFunctionId.Split => EvaluateSplitBuiltin(arguments[0].AsString(), separator: ",", location),
            BuiltinFunctionId.SplitSeparator => EvaluateSplitBuiltin(arguments[0].AsString(), arguments[1].AsString(), location),
            BuiltinFunctionId.Join => EvaluateJoinBuiltin(arguments[0].AsArray(), separator: ","),
            BuiltinFunctionId.JoinSeparator => EvaluateJoinBuiltin(arguments[0].AsArray(), arguments[1].AsString()),
            BuiltinFunctionId.RegexIsMatch => EvaluateRegexIsMatchBuiltin(arguments[0].AsString(), arguments[1].AsString(), modes: "", location),
            BuiltinFunctionId.RegexIsMatchModes => EvaluateRegexIsMatchBuiltin(arguments[0].AsString(), arguments[1].AsString(), arguments[2].AsString(), location),
            BuiltinFunctionId.RegexMatch => EvaluateRegexMatchBuiltin(arguments[0].AsString(), arguments[1].AsString(), modes: "", location),
            BuiltinFunctionId.RegexMatchModes => EvaluateRegexMatchBuiltin(arguments[0].AsString(), arguments[1].AsString(), arguments[2].AsString(), location),
            BuiltinFunctionId.RegexExtract => EvaluateRegexExtractBuiltin(arguments[0].AsString(), arguments[1].AsString(), arguments[2].AsInteger(), modes: "", location),
            BuiltinFunctionId.RegexExtractModes => EvaluateRegexExtractBuiltin(arguments[0].AsString(), arguments[1].AsString(), arguments[2].AsInteger(), arguments[3].AsString(), location),
            BuiltinFunctionId.RegexMatches => EvaluateRegexMatchesBuiltin(arguments[0].AsString(), arguments[1].AsString(), modes: "", location),
            BuiltinFunctionId.RegexMatchesModes => EvaluateRegexMatchesBuiltin(arguments[0].AsString(), arguments[1].AsString(), arguments[2].AsString(), location),
            BuiltinFunctionId.RegexExtractAll => EvaluateRegexExtractAllBuiltin(arguments[0].AsString(), arguments[1].AsString(), arguments[2].AsInteger(), modes: "", location),
            BuiltinFunctionId.RegexExtractAllModes => EvaluateRegexExtractAllBuiltin(arguments[0].AsString(), arguments[1].AsString(), arguments[2].AsInteger(), arguments[3].AsString(), location),
            BuiltinFunctionId.RegexSplit => EvaluateRegexSplitBuiltin(arguments[0].AsString(), arguments[1].AsString(), modes: "", location),
            BuiltinFunctionId.RegexSplitModes => EvaluateRegexSplitBuiltin(arguments[0].AsString(), arguments[1].AsString(), arguments[2].AsString(), location),
            BuiltinFunctionId.RegexReplace => EvaluateRegexReplaceBuiltin(arguments[0], arguments[1], arguments[2], modes: "", count: null, location),
            BuiltinFunctionId.RegexReplaceModes => EvaluateRegexReplaceBuiltin(arguments[0], arguments[1], arguments[2], GetStringUnchecked(arguments[3]), count: null, location),
            BuiltinFunctionId.RegexReplaceCount => EvaluateRegexReplaceBuiltin(arguments[0], arguments[1], arguments[2], modes: "", GetIntegerUnchecked(arguments[3]), location),
            BuiltinFunctionId.RegexReplaceModesCount => EvaluateRegexReplaceBuiltin(arguments[0], arguments[1], arguments[2], GetStringUnchecked(arguments[3]), GetIntegerUnchecked(arguments[4]), location),
            BuiltinFunctionId.RegexCount => EvaluateRegexCountBuiltin(arguments[0].AsString(), arguments[1].AsString(), modes: "", location),
            BuiltinFunctionId.RegexCountModes => EvaluateRegexCountBuiltin(arguments[0].AsString(), arguments[1].AsString(), arguments[2].AsString(), location),
            BuiltinFunctionId.InKey => RuntimeValue.FromString(ReadKeyboardInput()),
            BuiltinFunctionId.KeyDown => RuntimeValue.FromBoolean(IsKeyboardKeyDown(arguments[0].AsString())),
            BuiltinFunctionId.KeyPress => RuntimeValue.FromBoolean(ConsumeKeyboardKeyPress(arguments[0].AsString())),
            BuiltinFunctionId.MouseX => RuntimeValue.FromLong(ReadMouseX()),
            BuiltinFunctionId.MouseY => RuntimeValue.FromLong(ReadMouseY()),
            BuiltinFunctionId.MouseButton => RuntimeValue.FromBoolean(IsMouseButtonDown(arguments[0].AsInteger())),
            BuiltinFunctionId.MousePress => RuntimeValue.FromBoolean(ConsumeMouseButtonPress(arguments[0].AsInteger())),
            BuiltinFunctionId.Int => RuntimeValue.FromInteger(EvaluateIntBuiltin(arguments[0], location)),
            BuiltinFunctionId.Str => RuntimeValue.FromString(arguments[0].ToDisplayString()),
            BuiltinFunctionId.Val => RuntimeValue.FromDouble(ParseVal(arguments[0].AsString())),
            BuiltinFunctionId.LCase => RuntimeValue.FromString(arguments[0].AsString().ToLowerInvariant()),
            BuiltinFunctionId.UCase => RuntimeValue.FromString(arguments[0].AsString().ToUpperInvariant()),
            BuiltinFunctionId.Rnd => EvaluateRndBuiltin(),
            BuiltinFunctionId.Abs => EvaluateAbsBuiltin(arguments[0], location),
            BuiltinFunctionId.Sgn => RuntimeValue.FromInteger(EvaluateSgnBuiltin(arguments[0], location)),
            BuiltinFunctionId.Min => EvaluateMinBuiltin(arguments[0], arguments[1], location),
            BuiltinFunctionId.Max => EvaluateMaxBuiltin(arguments[0], arguments[1], location),
            BuiltinFunctionId.Clamp => EvaluateClampBuiltin(arguments[0], arguments[1], arguments[2], location),
            BuiltinFunctionId.Sin => EvaluateDoubleMathBuiltin("SIN", "value", arguments[0], Math.Sin, location),
            BuiltinFunctionId.Cos => EvaluateDoubleMathBuiltin("COS", "value", arguments[0], Math.Cos, location),
            BuiltinFunctionId.Tan => EvaluateDoubleMathBuiltin("TAN", "value", arguments[0], Math.Tan, location),
            BuiltinFunctionId.Atn => EvaluateDoubleMathBuiltin("ATN", "value", arguments[0], Math.Atan, location),
            BuiltinFunctionId.Atn2 => EvaluateAtn2Builtin(arguments[0], arguments[1], location),
            BuiltinFunctionId.Sqr => EvaluateSqrBuiltin(arguments[0], location),
            BuiltinFunctionId.Cbrt => EvaluateDoubleMathBuiltin("CBRT", "value", arguments[0], Math.Cbrt, location),
            BuiltinFunctionId.Exp => EvaluateDoubleMathBuiltin("EXP", "value", arguments[0], Math.Exp, location),
            BuiltinFunctionId.Log => EvaluateLogBuiltin("LOG", arguments[0], Math.Log, location),
            BuiltinFunctionId.Log10 => EvaluateLogBuiltin("LOG10", arguments[0], Math.Log10, location),
            BuiltinFunctionId.Log2 => EvaluateLogBuiltin("LOG2", arguments[0], Math.Log2, location),
            BuiltinFunctionId.Pow => EvaluatePowBuiltin(arguments[0], arguments[1], location),
            BuiltinFunctionId.Floor => EvaluateFloorBuiltin(arguments[0], location),
            BuiltinFunctionId.Ceil => EvaluateCeilBuiltin(arguments[0], location),
            BuiltinFunctionId.Round => EvaluateRoundBuiltin(arguments[0], location),
            BuiltinFunctionId.Fix => EvaluateFixBuiltin(arguments[0], location),
            BuiltinFunctionId.Asin => EvaluateInverseTrigBuiltin("ASIN", arguments[0], Math.Asin, location),
            BuiltinFunctionId.Acos => EvaluateInverseTrigBuiltin("ACOS", arguments[0], Math.Acos, location),
            BuiltinFunctionId.Asinh => EvaluateDoubleMathBuiltin("ASINH", "value", arguments[0], Math.Asinh, location),
            BuiltinFunctionId.Acosh => EvaluateAcoshBuiltin(arguments[0], location),
            BuiltinFunctionId.Atanh => EvaluateAtanhBuiltin(arguments[0], location),
            BuiltinFunctionId.Sinh => EvaluateDoubleMathBuiltin("SINH", "value", arguments[0], Math.Sinh, location),
            BuiltinFunctionId.Cosh => EvaluateDoubleMathBuiltin("COSH", "value", arguments[0], Math.Cosh, location),
            BuiltinFunctionId.Tanh => EvaluateDoubleMathBuiltin("TANH", "value", arguments[0], Math.Tanh, location),
            BuiltinFunctionId.SetSign => EvaluateSetSignBuiltin(arguments[0], arguments[1], location),
            BuiltinFunctionId.LBound => RuntimeValue.FromInteger(EvaluateLBoundBuiltin(arguments, location)),
            BuiltinFunctionId.UBound => RuntimeValue.FromInteger(EvaluateUBoundBuiltin(arguments, location)),
            BuiltinFunctionId.ArrayLength => RuntimeValue.FromInteger(EvaluateArrayLengthBuiltin(arguments[0])),
            BuiltinFunctionId.ArrayIsAllocated => RuntimeValue.FromBoolean(arguments[0].AsArray().IsAllocated),
            BuiltinFunctionId.ArrayRank => RuntimeValue.FromInteger(EvaluateArrayRankBuiltin(arguments[0])),
            BuiltinFunctionId.ArrayBounds => EvaluateArrayBoundsBuiltin(arguments[0], location),
            BuiltinFunctionId.ArrayClone => EvaluateArrayCloneBuiltin(arguments[0], location),
            BuiltinFunctionId.ArraySlice => EvaluateArraySliceBuiltin(arguments, location),
            BuiltinFunctionId.ArrayFlatten => EvaluateArrayFlattenBuiltin(arguments[0], location),
            BuiltinFunctionId.ArrayReshape => EvaluateArrayReshapeBuiltin(arguments[0], arguments[1], location),
            BuiltinFunctionId.ArrayToSorted => EvaluateArrayToSortedBuiltin(arguments, location),
            BuiltinFunctionId.ArrayToReversed => EvaluateArrayToReversedBuiltin(arguments, location),
            BuiltinFunctionId.ArrayTake => EvaluateArrayTakeBuiltin(arguments, location),
            BuiltinFunctionId.ArraySkip => EvaluateArraySkipBuiltin(arguments, location),
            BuiltinFunctionId.ArrayDistinct => EvaluateArrayDistinctBuiltin(arguments[0], location),
            BuiltinFunctionId.ArrayConcat => EvaluateArrayConcatBuiltin(arguments, location),
            BuiltinFunctionId.ArrayFill => EvaluateArrayFillBuiltin(arguments[0], arguments[1], location),
            BuiltinFunctionId.ArrayReverse => EvaluateArrayReverseBuiltin(arguments, location),
            BuiltinFunctionId.ArraySort => EvaluateArraySortBuiltin(arguments, location),
            BuiltinFunctionId.ArrayPush => EvaluateArrayPushBuiltin(arguments, location),
            BuiltinFunctionId.ArrayPop => EvaluateArrayPopBuiltin(arguments, location),
            BuiltinFunctionId.ArrayInsert => EvaluateArrayInsertBuiltin(arguments, location),
            BuiltinFunctionId.ArrayRemove => EvaluateArrayRemoveBuiltin(arguments, location),
            BuiltinFunctionId.ArrayContains => RuntimeValue.FromBoolean(EvaluateArrayContainsBuiltin(arguments[0], arguments[1], location)),
            BuiltinFunctionId.ArrayIndexOf => EvaluateArrayIndexOfBuiltin(arguments[0], arguments[1], searchFromEnd: false, location),
            BuiltinFunctionId.ArrayLastIndexOf => EvaluateArrayIndexOfBuiltin(arguments[0], arguments[1], searchFromEnd: true, location),
            BuiltinFunctionId.ArrayCount => RuntimeValue.FromInteger(EvaluateArrayCountBuiltin(arguments[0], arguments[1], location)),
            BuiltinFunctionId.ArrayFindAll => EvaluateArrayFindAllBuiltin(arguments[0], arguments[1], location),
            BuiltinFunctionId.ArrayBinarySearch => EvaluateArrayBinarySearchBuiltin(arguments, location),
            BuiltinFunctionId.ArrayEvery => RuntimeValue.FromBoolean(EvaluateArrayEveryBuiltin(arguments[0], arguments[1], location)),
            BuiltinFunctionId.ArraySome => RuntimeValue.FromBoolean(EvaluateArraySomeBuiltin(arguments[0], arguments[1], location)),
            BuiltinFunctionId.ArrayFilter => EvaluateArrayFilterBuiltin(arguments[0], arguments[1], location),
            BuiltinFunctionId.ArrayMap => EvaluateArrayMapBuiltin(arguments[0], arguments[1], location),
            BuiltinFunctionId.ArrayReduce => EvaluateArrayReduceBuiltin(arguments[0], arguments[1], arguments[2], location),
            BuiltinFunctionId.Date => EvaluateDateBuiltin(),
            BuiltinFunctionId.DateInZone => EvaluateDateBuiltin(arguments[0].AsString(), location),
            BuiltinFunctionId.Time => EvaluateTimeBuiltin(),
            BuiltinFunctionId.TimeInZone => EvaluateTimeBuiltin(arguments[0].AsString(), location),
            BuiltinFunctionId.Now => EvaluateNowBuiltin(),
            BuiltinFunctionId.NowInZone => EvaluateNowBuiltin(arguments[0].AsString(), location),
            BuiltinFunctionId.UtcDate => EvaluateUtcDateBuiltin(),
            BuiltinFunctionId.UtcTime => EvaluateUtcTimeBuiltin(),
            BuiltinFunctionId.UtcNow => EvaluateUtcNowBuiltin(),
            BuiltinFunctionId.Timer => EvaluateTimerBuiltin(),
            BuiltinFunctionId.DateSerial => EvaluateDateSerialBuiltin(arguments[0].AsInteger(), arguments[1].AsInteger(), arguments[2].AsInteger(), location),
            BuiltinFunctionId.TimeSerial => EvaluateTimeSerialBuiltin(arguments[0].AsInteger(), arguments[1].AsInteger(), arguments[2].AsInteger(), location),
            BuiltinFunctionId.Year => EvaluateYearBuiltin(arguments[0].AsString(), location),
            BuiltinFunctionId.Month => EvaluateMonthBuiltin(arguments[0].AsString(), location),
            BuiltinFunctionId.MonthName => EvaluateMonthNameBuiltin(arguments[0].AsInteger(), location),
            BuiltinFunctionId.Day => EvaluateDayBuiltin(arguments[0].AsString(), location),
            BuiltinFunctionId.Hour => EvaluateHourBuiltin(arguments[0].AsString(), location),
            BuiltinFunctionId.Minute => EvaluateMinuteBuiltin(arguments[0].AsString(), location),
            BuiltinFunctionId.Second => EvaluateSecondBuiltin(arguments[0].AsString(), location),
            BuiltinFunctionId.Weekday => EvaluateWeekdayBuiltin(arguments[0].AsString(), location),
            BuiltinFunctionId.WeekdayName => EvaluateWeekdayNameBuiltin(arguments[0].AsInteger(), location),
            BuiltinFunctionId.DateAdd => EvaluateDateAddBuiltin(arguments[0].AsString(), arguments[1].AsInteger(), arguments[2].AsString(), location),
            BuiltinFunctionId.DateDiff => EvaluateDateDiffBuiltin(arguments[0].AsString(), arguments[1].AsString(), arguments[2].AsString(), location),
            BuiltinFunctionId.DatePart => EvaluateDatePartBuiltin(arguments[0].AsString(), arguments[1].AsString(), location),
            BuiltinFunctionId.DateValue => EvaluateDateValueBuiltin(arguments[0].AsString(), location),
            BuiltinFunctionId.TimeValue => EvaluateTimeValueBuiltin(arguments[0].AsString(), location),
            BuiltinFunctionId.IsDate => EvaluateIsDateBuiltin(arguments[0].AsString()),
            BuiltinFunctionId.UtcOffset => EvaluateUtcOffsetBuiltin(),
            BuiltinFunctionId.UtcOffsetValue => EvaluateUtcOffsetBuiltin(arguments[0].AsString(), location),
            BuiltinFunctionId.UtcOffsetValueZone => EvaluateUtcOffsetBuiltin(arguments[0].AsString(), arguments[1].AsString(), location),
            BuiltinFunctionId.ToUtc => EvaluateToUtcBuiltin(arguments[0].AsString(), location),
            BuiltinFunctionId.ToUtcZone => EvaluateToUtcBuiltin(arguments[0].AsString(), arguments[1].AsString(), location),
            BuiltinFunctionId.FromUtc => EvaluateFromUtcBuiltin(arguments[0].AsString(), location),
            BuiltinFunctionId.FromUtcZone => EvaluateFromUtcBuiltin(arguments[0].AsString(), arguments[1].AsString(), location),
            BuiltinFunctionId.IsLeapYear => EvaluateIsLeapYearBuiltin(arguments[0].AsInteger(), location),
            BuiltinFunctionId.DaysInMonth => EvaluateDaysInMonthBuiltin(arguments[0].AsInteger(), arguments[1].AsInteger(), location),
            BuiltinFunctionId.StartOfMonth => EvaluateStartOfMonthBuiltin(arguments[0].AsString(), location),
            BuiltinFunctionId.EndOfMonth => EvaluateEndOfMonthBuiltin(arguments[0].AsString(), location),
            BuiltinFunctionId.StartOfYear => EvaluateStartOfYearBuiltin(arguments[0].AsString(), location),
            BuiltinFunctionId.EndOfYear => EvaluateEndOfYearBuiltin(arguments[0].AsString(), location),
            BuiltinFunctionId.IsOpen => EvaluateIsOpenBuiltin(arguments[0]),
            BuiltinFunctionId.FilePath => EvaluateFilePathBuiltin(arguments[0], location),
            BuiltinFunctionId.FileMode => EvaluateFileModeBuiltin(arguments[0], location),
            BuiltinFunctionId.FilePos => EvaluateFilePosBuiltin(arguments[0], location),
            BuiltinFunctionId.FileLength => EvaluateFileLengthBuiltin(arguments[0], location),
            BuiltinFunctionId.LineNumber => EvaluateLineNumberBuiltin(arguments[0], location),
            BuiltinFunctionId.Eof => EvaluateEofBuiltin(arguments[0], location),
            BuiltinFunctionId.ReadToEnd => EvaluateReadToEndBuiltin(arguments[0], location),
            BuiltinFunctionId.ReadText => EvaluateReadTextBuiltin(
                arguments[0],
                RequireNonNegativeCount("READTEXT", "count", arguments[1].AsInteger(), location),
                location),
            BuiltinFunctionId.ReadLine => EvaluateReadLineBuiltin(arguments[0], location),
            BuiltinFunctionId.ReadLines => EvaluateReadLinesBuiltin(
                arguments[0],
                RequireNonNegativeCount("READLINES", "count", arguments[1].AsInteger(), location),
                location),
            BuiltinFunctionId.SkipLine => EvaluateSkipLineBuiltin(arguments[0], location),
            BuiltinFunctionId.Write => EvaluateWriteBuiltin(arguments[0], arguments[1].AsString(), appendLineBreak: false, location),
            BuiltinFunctionId.WriteLine => EvaluateWriteBuiltin(arguments[0], arguments[1].AsString(), appendLineBreak: true, location),
            BuiltinFunctionId.NewLine => EvaluateNewLineBuiltin(arguments[0], location),
            BuiltinFunctionId.Flush => EvaluateFlushBuiltin(arguments[0], location),
            BuiltinFunctionId.SeekFile => EvaluateSeekFileBuiltin(arguments[0], arguments[1], location),
            BuiltinFunctionId.Rewind => EvaluateRewindBuiltin(arguments[0], location),
            BuiltinFunctionId.FileExists => EvaluateFileExistsBuiltin(arguments[0].AsString(), location),
            BuiltinFunctionId.DirExists => EvaluateDirExistsBuiltin(arguments[0].AsString(), location),
            BuiltinFunctionId.PathExists => EvaluatePathExistsBuiltin(arguments[0].AsString(), location),
            BuiltinFunctionId.ReadFile => EvaluateReadFileBuiltin(arguments[0].AsString(), location),
            BuiltinFunctionId.ReadAllLines => EvaluateReadAllLinesBuiltin(arguments[0].AsString(), location),
            BuiltinFunctionId.WriteFile => EvaluateWriteFileBuiltin(arguments[0].AsString(), arguments[1].AsString(), location),
            BuiltinFunctionId.AppendFile => EvaluateAppendFileBuiltin(arguments[0].AsString(), arguments[1].AsString(), location),
            BuiltinFunctionId.FileSize => EvaluateFileSizeBuiltin(arguments[0].AsString(), location),
            BuiltinFunctionId.FileDate => EvaluateFileDateBuiltin(arguments[0].AsString(), location),
            BuiltinFunctionId.FileCreateDate => EvaluateFileCreateDateBuiltin(arguments[0].AsString(), location),
            BuiltinFunctionId.FileAccessDate => EvaluateFileAccessDateBuiltin(arguments[0].AsString(), location),
            BuiltinFunctionId.DirDate => EvaluateDirDateBuiltin(arguments[0].AsString(), location),
            BuiltinFunctionId.GetFiles => EvaluateGetFilesBuiltin(arguments[0].AsString(), pattern: "*", patternArgumentIndex: -1, location),
            BuiltinFunctionId.GetFilesPattern => EvaluateGetFilesBuiltin(arguments[0].AsString(), arguments[1].AsString(), patternArgumentIndex: 1, location),
            BuiltinFunctionId.GetDirs => EvaluateGetDirsBuiltin(arguments[0].AsString(), pattern: "*", patternArgumentIndex: -1, location),
            BuiltinFunctionId.GetDirsPattern => EvaluateGetDirsBuiltin(arguments[0].AsString(), arguments[1].AsString(), patternArgumentIndex: 1, location),
            BuiltinFunctionId.CombinePath => EvaluateCombinePathBuiltin(arguments[0].AsString(), arguments[1].AsString(), location),
            BuiltinFunctionId.FileName => EvaluateFileNameBuiltin(arguments[0].AsString(), location),
            BuiltinFunctionId.DirName => EvaluateDirNameBuiltin(arguments[0].AsString(), location),
            BuiltinFunctionId.ExtName => EvaluateExtNameBuiltin(arguments[0].AsString(), location),
            BuiltinFunctionId.FullPath => EvaluateFullPathBuiltin(arguments[0].AsString(), location),
            BuiltinFunctionId.PathRoot => EvaluatePathRootBuiltin(arguments[0].AsString(), location),
            BuiltinFunctionId.IsAbsolutePath => EvaluateIsAbsolutePathBuiltin(arguments[0].AsString(), location),
            BuiltinFunctionId.MakeDir => EvaluateMakeDirBuiltin(arguments[0].AsString(), location),
            BuiltinFunctionId.DeleteFile => EvaluateDeleteFileBuiltin(arguments[0].AsString(), location),
            BuiltinFunctionId.CopyFile => EvaluateCopyFileBuiltin(arguments[0].AsString(), arguments[1].AsString(), overwrite: false, location),
            BuiltinFunctionId.CopyFileOverwrite => EvaluateCopyFileBuiltin(arguments[0].AsString(), arguments[1].AsString(), arguments[2].AsBoolean(), location),
            BuiltinFunctionId.MoveFile => EvaluateMoveFileBuiltin(arguments[0].AsString(), arguments[1].AsString(), overwrite: false, location),
            BuiltinFunctionId.MoveFileOverwrite => EvaluateMoveFileBuiltin(arguments[0].AsString(), arguments[1].AsString(), arguments[2].AsBoolean(), location),
            BuiltinFunctionId.RenameFile => EvaluateRenameFileBuiltin(arguments[0].AsString(), arguments[1].AsString(), location),
            BuiltinFunctionId.DeleteDir => EvaluateDeleteDirBuiltin(arguments[0].AsString(), location),
            BuiltinFunctionId.DeleteDirRecursive => EvaluateDeleteDirRecursiveBuiltin(arguments[0].AsString(), location),
            BuiltinFunctionId.CopyDir => EvaluateCopyDirBuiltin(arguments[0].AsString(), arguments[1].AsString(), overwrite: false, location),
            BuiltinFunctionId.CopyDirOverwrite => EvaluateCopyDirBuiltin(arguments[0].AsString(), arguments[1].AsString(), arguments[2].AsBoolean(), location),
            BuiltinFunctionId.MoveDir => EvaluateMoveDirBuiltin(arguments[0].AsString(), arguments[1].AsString(), location),
            BuiltinFunctionId.RenameDir => EvaluateRenameDirBuiltin(arguments[0].AsString(), arguments[1].AsString(), location),
            BuiltinFunctionId.CurrentDir => EvaluateCurrentDirBuiltin(location),
            BuiltinFunctionId.ChangeDir => EvaluateChangeDirBuiltin(arguments[0].AsString(), location),
            BuiltinFunctionId.DirSize => EvaluateDirSizeBuiltin(arguments[0].AsString(), location),
            BuiltinFunctionId.FreeSpace => EvaluateFreeSpaceBuiltin(arguments[0].AsString(), location),
            BuiltinFunctionId.TotalFreeSpace => EvaluateTotalFreeSpaceBuiltin(arguments[0].AsString(), location),
            BuiltinFunctionId.TotalSpace => EvaluateTotalSpaceBuiltin(arguments[0].AsString(), location),
            BuiltinFunctionId.IsReadOnly => EvaluateIsReadOnlyBuiltin(arguments[0].AsString(), location),
            BuiltinFunctionId.IsHidden => EvaluateIsHiddenBuiltin(arguments[0].AsString(), location),
            BuiltinFunctionId.DriveName => EvaluateDriveNameBuiltin(arguments[0].AsString(), location),
            BuiltinFunctionId.DriveFormat => EvaluateDriveFormatBuiltin(arguments[0].AsString(), location),
            BuiltinFunctionId.DriveType => EvaluateDriveTypeBuiltin(arguments[0].AsString(), location),
            BuiltinFunctionId.MakeTempFile => EvaluateMakeTempFileBuiltin(arguments[0].AsString(), location),
            BuiltinFunctionId.MakeTempDir => EvaluateMakeTempDirBuiltin(arguments[0].AsString(), location),
            _ => ReportInvalidRuntimeState<RuntimeValue>(
                $"Bytecode VM пока не поддерживает built-in '{builtin.Name}' ({builtin.CanonicalId}).",
                location)
        };
    }

    private static double ParseVal(string text)
    {
        return double.TryParse(text, NumberStyles.Float | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var value)
            ? value
            : 0d;
    }

    private RuntimeValue EvaluateAbsBuiltin(RuntimeValue value, TextLocation location)
    {
        return value.Type switch
        {
            var type when type == TypeSymbol.Integer => EvaluateCheckedIntegerOperation(
                () => checked(Math.Abs(value.AsInteger())),
                "Результат ABS",
                location),
            var type when type == TypeSymbol.Long => EvaluateCheckedLongOperation(
                () => checked(Math.Abs(value.AsLong())),
                "Результат ABS",
                location),
            var type when type == TypeSymbol.Double => EnsureFiniteDoubleResult(
                Math.Abs(value.AsDouble()),
                "Результат ABS",
                location),
            var type when type == TypeSymbol.Decimal => EvaluateCheckedDecimalOperation(
                () => decimal.Abs(value.AsDecimal()),
                "Результат ABS",
                location),
            _ => ReportInvalidRuntimeState<RuntimeValue>($"ABS не поддерживает тип '{value.Type.Name}'.", location)
        };
    }

    private int EvaluateSgnBuiltin(RuntimeValue value, TextLocation location)
    {
        return value.Type switch
        {
            var type when type == TypeSymbol.Integer => Math.Sign(value.AsInteger()),
            var type when type == TypeSymbol.Long => Math.Sign(value.AsLong()),
            var type when type == TypeSymbol.Double => Math.Sign(value.AsDouble()),
            var type when type == TypeSymbol.Decimal => Math.Sign(value.AsDecimal()),
            _ => ReportInvalidRuntimeState<int>($"SGN не поддерживает тип '{value.Type.Name}'.", location)
        };
    }

    private RuntimeValue EvaluateMinBuiltin(RuntimeValue left, RuntimeValue right, TextLocation location)
    {
        if (Compare(left, right, location) <= 0)
        {
            return left.CopyForStorage();
        }

        return right.CopyForStorage();
    }

    private RuntimeValue EvaluateMaxBuiltin(RuntimeValue left, RuntimeValue right, TextLocation location)
    {
        if (Compare(left, right, location) >= 0)
        {
            return left.CopyForStorage();
        }

        return right.CopyForStorage();
    }

    private RuntimeValue EvaluateClampBuiltin(RuntimeValue value, RuntimeValue minValue, RuntimeValue maxValue, TextLocation location)
    {
        if (Compare(minValue, maxValue, location) > 0)
        {
            ReportBuiltInArgumentError("CLAMP", "min", "не должен быть больше аргумента 'max'.", location);
            throw new RuntimeFailureException();
        }

        if (Compare(value, minValue, location) < 0)
        {
            return minValue.CopyForStorage();
        }

        if (Compare(value, maxValue, location) > 0)
        {
            return maxValue.CopyForStorage();
        }

        return value.CopyForStorage();
    }
}
