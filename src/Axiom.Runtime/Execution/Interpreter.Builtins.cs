using Axiom.Core;
using Axiom.Semantics;

namespace Axiom.Runtime;

public sealed partial class Interpreter
{
    private RuntimeValue InvokeBuiltIn(BoundCallExpression expression, FunctionSymbol function, IReadOnlyList<RuntimeValue> arguments)
    {
        if (function.BuiltinId is null)
        {
            return ReportInvalidRuntimeState<RuntimeValue>(
                $"У встроенной функции '{function.Name}' отсутствует канонический идентификатор.",
                expression.Location);
        }

        return function.BuiltinId.Value switch
        {
            BuiltinFunctionId.LBound => EvaluateLBound(expression, arguments),
            BuiltinFunctionId.UBound => EvaluateUBound(expression, arguments),
            BuiltinFunctionId.ArrayLength => EvaluateArrayLength(arguments[0]),
            BuiltinFunctionId.ArrayIsAllocated => EvaluateArrayIsAllocated(arguments[0]),
            BuiltinFunctionId.ArrayClone => EvaluateArrayClone(arguments[0]),
            BuiltinFunctionId.ArraySlice => EvaluateArraySlice(
                expression,
                arguments[0],
                arguments[1].AsInteger(),
                RequireNonNegativeCount(expression, "ARRAYSLICE", "count", 2, arguments[2].AsInteger())),
            BuiltinFunctionId.ArrayToSorted => EvaluateArrayToSorted(
                expression,
                arguments[0]),
            BuiltinFunctionId.ArrayToReversed => EvaluateArrayToReversed(
                expression,
                arguments[0]),
            BuiltinFunctionId.ArrayTake => EvaluateArrayTake(
                expression,
                arguments[0],
                RequireNonNegativeCount(expression, "ARRAYTAKE", "count", 1, arguments[1].AsInteger())),
            BuiltinFunctionId.ArraySkip => EvaluateArraySkip(
                expression,
                arguments[0],
                RequireNonNegativeCount(expression, "ARRAYSKIP", "count", 1, arguments[1].AsInteger())),
            BuiltinFunctionId.ArrayDistinct => EvaluateArrayDistinct(
                expression,
                arguments[0]),
            BuiltinFunctionId.ArrayContains => EvaluateArrayContains(
                expression,
                arguments[0],
                arguments[1]),
            BuiltinFunctionId.ArrayIndexOf => EvaluateArrayIndexOf(
                expression,
                arguments[0],
                arguments[1]),
            BuiltinFunctionId.ArrayLastIndexOf => EvaluateArrayLastIndexOf(
                expression,
                arguments[0],
                arguments[1]),
            BuiltinFunctionId.ArrayCount => EvaluateArrayCount(
                expression,
                arguments[0],
                arguments[1]),
            BuiltinFunctionId.ArrayFindAll => EvaluateArrayFindAll(
                expression,
                arguments[0],
                arguments[1]),
            BuiltinFunctionId.ArrayBinarySearch => EvaluateArrayBinarySearch(
                expression,
                arguments[0],
                arguments[1]),
            BuiltinFunctionId.ArrayFill => EvaluateArrayFill(
                expression,
                arguments[0],
                arguments[1]),
            BuiltinFunctionId.ArrayReverse => EvaluateArrayReverse(
                expression,
                arguments[0]),
            BuiltinFunctionId.ArraySort => EvaluateArraySort(
                expression,
                arguments[0]),
            BuiltinFunctionId.ArrayPush => EvaluateArrayPush(
                expression,
                arguments[0],
                arguments[1]),
            BuiltinFunctionId.ArrayPop => EvaluateArrayPop(
                expression,
                arguments[0]),
            BuiltinFunctionId.ArrayInsert => EvaluateArrayInsert(
                expression,
                arguments[0],
                arguments[1].AsInteger(),
                arguments[2]),
            BuiltinFunctionId.ArrayRemove => EvaluateArrayRemove(
                expression,
                arguments[0],
                arguments[1].AsInteger()),
            BuiltinFunctionId.ArrayEvery => EvaluateArrayEvery(
                expression,
                arguments[0],
                arguments[1]),
            BuiltinFunctionId.ArraySome => EvaluateArraySome(
                expression,
                arguments[0],
                arguments[1]),
            BuiltinFunctionId.ArrayFilter => EvaluateArrayFilter(
                expression,
                arguments[0],
                arguments[1]),
            BuiltinFunctionId.ArrayMap => EvaluateArrayMap(
                expression,
                arguments[0],
                arguments[1]),
            BuiltinFunctionId.ArrayReduce => EvaluateArrayReduce(
                expression,
                arguments[0],
                arguments[1],
                arguments[2]),
            BuiltinFunctionId.ArrayConcat => EvaluateArrayConcat(
                expression,
                arguments[0],
                arguments[1]),
            BuiltinFunctionId.Len => RuntimeValue.FromInteger(arguments[0].AsString().Length),
            BuiltinFunctionId.Left => RuntimeValue.FromString(
                EvaluateLeft(
                    arguments[0].AsString(),
                    RequirePositiveCount(expression, "LEFT", "count", 1, arguments[1].AsInteger()))),
            BuiltinFunctionId.Right => RuntimeValue.FromString(
                EvaluateRight(
                    arguments[0].AsString(),
                    RequirePositiveCount(expression, "RIGHT", "count", 1, arguments[1].AsInteger()))),
            BuiltinFunctionId.Mid => RuntimeValue.FromString(
                EvaluateMid(
                    arguments[0].AsString(),
                    RequirePositiveStart(expression, "MID", 1, arguments[1].AsInteger()),
                    RequirePositiveCount(expression, "MID", "count", 2, arguments[2].AsInteger()))),
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
            BuiltinFunctionId.Replace => RuntimeValue.FromString(EvaluateReplace(expression, arguments[0].AsString(), arguments[1].AsString(), arguments[2].AsString())),
            BuiltinFunctionId.InStr => RuntimeValue.FromInteger(EvaluateInStr(1, arguments[0].AsString(), arguments[1].AsString())),
            BuiltinFunctionId.InStrFrom => RuntimeValue.FromInteger(
                EvaluateInStr(
                    RequirePositiveStart(expression, "INSTR", 0, arguments[0].AsInteger()),
                    arguments[1].AsString(),
                    arguments[2].AsString())),
            BuiltinFunctionId.InStrRev => RuntimeValue.FromInteger(EvaluateInStrRev(arguments[0].AsString().Length, arguments[0].AsString(), arguments[1].AsString())),
            BuiltinFunctionId.InStrRevFrom => RuntimeValue.FromInteger(
                EvaluateInStrRev(
                    RequirePositiveStart(expression, "INSTRREV", 0, arguments[0].AsInteger()),
                    arguments[1].AsString(),
                    arguments[2].AsString())),
            BuiltinFunctionId.SubStrCount => RuntimeValue.FromInteger(EvaluateSubStrCount(expression, arguments[0].AsString(), arguments[1].AsString())),
            BuiltinFunctionId.Repeat => RuntimeValue.FromString(
                EvaluateRepeat(
                    arguments[0].AsString(),
                    RequireNonNegativeCount(expression, "REPEAT", "count", 1, arguments[1].AsInteger()))),
            BuiltinFunctionId.Space => RuntimeValue.FromString(
                EvaluateSpace(
                    RequireNonNegativeCount(expression, "SPACE", "count", 0, arguments[0].AsInteger()))),
            BuiltinFunctionId.StringFill => RuntimeValue.FromString(
                EvaluateStringFill(
                    expression,
                    RequireNonNegativeCount(expression, "STRING", "count", 0, arguments[0].AsInteger()),
                    arguments[1].AsString())),
            BuiltinFunctionId.PadLeft => RuntimeValue.FromString(
                arguments[0].AsString().PadLeft(
                    RequireNonNegativeCount(expression, "PADLEFT", "width", 1, arguments[1].AsInteger()))),
            BuiltinFunctionId.PadLeftFill => RuntimeValue.FromString(
                EvaluatePadLeft(
                    expression,
                    arguments[0].AsString(),
                    RequireNonNegativeCount(expression, "PADLEFT", "width", 1, arguments[1].AsInteger()),
                    arguments[2].AsString())),
            BuiltinFunctionId.PadRight => RuntimeValue.FromString(
                arguments[0].AsString().PadRight(
                    RequireNonNegativeCount(expression, "PADRIGHT", "width", 1, arguments[1].AsInteger()))),
            BuiltinFunctionId.PadRightFill => RuntimeValue.FromString(
                EvaluatePadRight(
                    expression,
                    arguments[0].AsString(),
                    RequireNonNegativeCount(expression, "PADRIGHT", "width", 1, arguments[1].AsInteger()),
                    arguments[2].AsString())),
            BuiltinFunctionId.Center => RuntimeValue.FromString(
                arguments[0].AsString().Length >= RequireNonNegativeCount(expression, "CENTER", "width", 1, arguments[1].AsInteger())
                    ? arguments[0].AsString()
                    : EvaluateCenter(
                        expression,
                        arguments[0].AsString(),
                        arguments[1].AsInteger(),
                        " ")),
            BuiltinFunctionId.CenterFill => RuntimeValue.FromString(
                EvaluateCenter(
                    expression,
                    arguments[0].AsString(),
                    RequireNonNegativeCount(expression, "CENTER", "width", 1, arguments[1].AsInteger()),
                    arguments[2].AsString())),
            BuiltinFunctionId.Insert => RuntimeValue.FromString(
                EvaluateInsert(
                    arguments[0].AsString(),
                    RequirePositiveStart(expression, "INSERT", 1, arguments[1].AsInteger()),
                    arguments[2].AsString())),
            BuiltinFunctionId.Remove => RuntimeValue.FromString(
                EvaluateRemove(
                    arguments[0].AsString(),
                    RequirePositiveStart(expression, "REMOVE", 1, arguments[1].AsInteger()),
                    RequireNonNegativeCount(expression, "REMOVE", "count", 2, arguments[2].AsInteger()))),
            BuiltinFunctionId.Asc => RuntimeValue.FromInteger(EvaluateAsc(expression, arguments[0].AsString())),
            BuiltinFunctionId.Chr => RuntimeValue.FromString(
                EvaluateChr(
                    expression,
                    RequireValidUnicodeCodePoint(expression, "CHR", "code", 0, arguments[0].AsInteger()))),
            BuiltinFunctionId.Split => EvaluateSplit(expression, arguments[0].AsString(), separator: ",", separatorArgumentIndex: -1),
            BuiltinFunctionId.SplitSeparator => EvaluateSplit(expression, arguments[0].AsString(), arguments[1].AsString(), separatorArgumentIndex: 1),
            BuiltinFunctionId.Join => EvaluateJoin(arguments[0].AsArray(), separator: ","),
            BuiltinFunctionId.JoinSeparator => EvaluateJoin(arguments[0].AsArray(), arguments[1].AsString()),
            BuiltinFunctionId.RegexIsMatch => EvaluateRegexIsMatch(expression, arguments[0].AsString(), arguments[1].AsString(), modes: "", modesArgumentIndex: -1),
            BuiltinFunctionId.RegexIsMatchModes => EvaluateRegexIsMatch(expression, arguments[0].AsString(), arguments[1].AsString(), arguments[2].AsString(), modesArgumentIndex: 2),
            BuiltinFunctionId.RegexMatch => EvaluateRegexMatch(expression, arguments[0].AsString(), arguments[1].AsString(), modes: "", modesArgumentIndex: -1),
            BuiltinFunctionId.RegexMatchModes => EvaluateRegexMatch(expression, arguments[0].AsString(), arguments[1].AsString(), arguments[2].AsString(), modesArgumentIndex: 2),
            BuiltinFunctionId.RegexExtract => EvaluateRegexExtract(expression, arguments[0].AsString(), arguments[1].AsString(), arguments[2].AsInteger(), modes: "", modesArgumentIndex: -1),
            BuiltinFunctionId.RegexExtractModes => EvaluateRegexExtract(expression, arguments[0].AsString(), arguments[1].AsString(), arguments[2].AsInteger(), arguments[3].AsString(), modesArgumentIndex: 3),
            BuiltinFunctionId.RegexMatches => EvaluateRegexMatches(expression, arguments[0].AsString(), arguments[1].AsString(), modes: "", modesArgumentIndex: -1),
            BuiltinFunctionId.RegexMatchesModes => EvaluateRegexMatches(expression, arguments[0].AsString(), arguments[1].AsString(), arguments[2].AsString(), modesArgumentIndex: 2),
            BuiltinFunctionId.RegexExtractAll => EvaluateRegexExtractAll(expression, arguments[0].AsString(), arguments[1].AsString(), arguments[2].AsInteger(), modes: "", modesArgumentIndex: -1),
            BuiltinFunctionId.RegexExtractAllModes => EvaluateRegexExtractAll(expression, arguments[0].AsString(), arguments[1].AsString(), arguments[2].AsInteger(), arguments[3].AsString(), modesArgumentIndex: 3),
            BuiltinFunctionId.RegexSplit => EvaluateRegexSplit(expression, arguments[0].AsString(), arguments[1].AsString(), modes: "", modesArgumentIndex: -1),
            BuiltinFunctionId.RegexSplitModes => EvaluateRegexSplit(expression, arguments[0].AsString(), arguments[1].AsString(), arguments[2].AsString(), modesArgumentIndex: 2),
            BuiltinFunctionId.RegexReplace => EvaluateRegexReplace(expression, arguments[0].AsString(), arguments[1].AsString(), arguments[2].AsString(), modes: "", count: null, modesArgumentIndex: -1, countArgumentIndex: -1),
            BuiltinFunctionId.RegexReplaceModes => EvaluateRegexReplace(expression, arguments[0].AsString(), arguments[1].AsString(), arguments[2].AsString(), arguments[3].AsString(), count: null, modesArgumentIndex: 3, countArgumentIndex: -1),
            BuiltinFunctionId.RegexReplaceCount => EvaluateRegexReplace(expression, arguments[0].AsString(), arguments[1].AsString(), arguments[2].AsString(), modes: "", arguments[3].AsInteger(), modesArgumentIndex: -1, countArgumentIndex: 3),
            BuiltinFunctionId.RegexReplaceModesCount => EvaluateRegexReplace(expression, arguments[0].AsString(), arguments[1].AsString(), arguments[2].AsString(), arguments[3].AsString(), arguments[4].AsInteger(), modesArgumentIndex: 3, countArgumentIndex: 4),
            BuiltinFunctionId.RegexCount => EvaluateRegexCount(expression, arguments[0].AsString(), arguments[1].AsString(), modes: "", modesArgumentIndex: -1),
            BuiltinFunctionId.RegexCountModes => EvaluateRegexCount(expression, arguments[0].AsString(), arguments[1].AsString(), arguments[2].AsString(), modesArgumentIndex: 2),
            BuiltinFunctionId.Date => EvaluateDateBuiltin(),
            BuiltinFunctionId.DateInZone => EvaluateDateBuiltin(expression, arguments[0].AsString()),
            BuiltinFunctionId.Time => EvaluateTimeBuiltin(),
            BuiltinFunctionId.TimeInZone => EvaluateTimeBuiltin(expression, arguments[0].AsString()),
            BuiltinFunctionId.Now => EvaluateNowBuiltin(),
            BuiltinFunctionId.NowInZone => EvaluateNowBuiltin(expression, arguments[0].AsString()),
            BuiltinFunctionId.UtcDate => EvaluateUtcDateBuiltin(),
            BuiltinFunctionId.UtcTime => EvaluateUtcTimeBuiltin(),
            BuiltinFunctionId.UtcNow => EvaluateUtcNowBuiltin(),
            BuiltinFunctionId.Timer => EvaluateTimerBuiltin(),
            BuiltinFunctionId.DateSerial => EvaluateDateSerialBuiltin(expression, arguments[0].AsInteger(), arguments[1].AsInteger(), arguments[2].AsInteger()),
            BuiltinFunctionId.TimeSerial => EvaluateTimeSerialBuiltin(expression, arguments[0].AsInteger(), arguments[1].AsInteger(), arguments[2].AsInteger()),
            BuiltinFunctionId.Year => EvaluateYearBuiltin(expression, arguments[0].AsString()),
            BuiltinFunctionId.Month => EvaluateMonthBuiltin(expression, arguments[0].AsString()),
            BuiltinFunctionId.MonthName => EvaluateMonthNameBuiltin(expression, arguments[0].AsInteger()),
            BuiltinFunctionId.Day => EvaluateDayBuiltin(expression, arguments[0].AsString()),
            BuiltinFunctionId.Hour => EvaluateHourBuiltin(expression, arguments[0].AsString()),
            BuiltinFunctionId.Minute => EvaluateMinuteBuiltin(expression, arguments[0].AsString()),
            BuiltinFunctionId.Second => EvaluateSecondBuiltin(expression, arguments[0].AsString()),
            BuiltinFunctionId.Weekday => EvaluateWeekdayBuiltin(expression, arguments[0].AsString()),
            BuiltinFunctionId.WeekdayName => EvaluateWeekdayNameBuiltin(expression, arguments[0].AsInteger()),
            BuiltinFunctionId.DateAdd => EvaluateDateAddBuiltin(expression, arguments[0].AsString(), arguments[1].AsInteger(), arguments[2].AsString()),
            BuiltinFunctionId.DateDiff => EvaluateDateDiffBuiltin(expression, arguments[0].AsString(), arguments[1].AsString(), arguments[2].AsString()),
            BuiltinFunctionId.DatePart => EvaluateDatePartBuiltin(expression, arguments[0].AsString(), arguments[1].AsString()),
            BuiltinFunctionId.DateValue => EvaluateDateValueBuiltin(expression, arguments[0].AsString()),
            BuiltinFunctionId.TimeValue => EvaluateTimeValueBuiltin(expression, arguments[0].AsString()),
            BuiltinFunctionId.IsDate => EvaluateIsDateBuiltin(arguments[0].AsString()),
            BuiltinFunctionId.UtcOffset => EvaluateUtcOffsetBuiltin(),
            BuiltinFunctionId.UtcOffsetValue => EvaluateUtcOffsetBuiltin(expression, arguments[0].AsString()),
            BuiltinFunctionId.UtcOffsetValueZone => EvaluateUtcOffsetBuiltin(expression, arguments[0].AsString(), arguments[1].AsString()),
            BuiltinFunctionId.ToUtc => EvaluateToUtcBuiltin(expression, arguments[0].AsString()),
            BuiltinFunctionId.ToUtcZone => EvaluateToUtcBuiltin(expression, arguments[0].AsString(), arguments[1].AsString()),
            BuiltinFunctionId.FromUtc => EvaluateFromUtcBuiltin(expression, arguments[0].AsString()),
            BuiltinFunctionId.FromUtcZone => EvaluateFromUtcBuiltin(expression, arguments[0].AsString(), arguments[1].AsString()),
            BuiltinFunctionId.IsLeapYear => EvaluateIsLeapYearBuiltin(expression, arguments[0].AsInteger()),
            BuiltinFunctionId.DaysInMonth => EvaluateDaysInMonthBuiltin(expression, arguments[0].AsInteger(), arguments[1].AsInteger()),
            BuiltinFunctionId.StartOfMonth => EvaluateStartOfMonthBuiltin(expression, arguments[0].AsString()),
            BuiltinFunctionId.EndOfMonth => EvaluateEndOfMonthBuiltin(expression, arguments[0].AsString()),
            BuiltinFunctionId.StartOfYear => EvaluateStartOfYearBuiltin(expression, arguments[0].AsString()),
            BuiltinFunctionId.EndOfYear => EvaluateEndOfYearBuiltin(expression, arguments[0].AsString()),
            BuiltinFunctionId.FileExists => EvaluateFileExists(expression, arguments[0].AsString()),
            BuiltinFunctionId.DirExists => EvaluateDirExists(expression, arguments[0].AsString()),
            BuiltinFunctionId.PathExists => EvaluatePathExists(expression, arguments[0].AsString()),
            BuiltinFunctionId.ReadFile => EvaluateReadFile(expression, arguments[0].AsString()),
            BuiltinFunctionId.ReadAllLines => EvaluateReadAllLines(expression, arguments[0].AsString()),
            BuiltinFunctionId.WriteFile => EvaluateWriteFile(expression, arguments[0].AsString(), arguments[1].AsString()),
            BuiltinFunctionId.AppendFile => EvaluateAppendFile(expression, arguments[0].AsString(), arguments[1].AsString()),
            BuiltinFunctionId.FileSize => EvaluateFileSize(expression, arguments[0].AsString()),
            BuiltinFunctionId.FileDate => EvaluateFileDate(expression, arguments[0].AsString()),
            BuiltinFunctionId.FileCreateDate => EvaluateFileCreateDate(expression, arguments[0].AsString()),
            BuiltinFunctionId.FileAccessDate => EvaluateFileAccessDate(expression, arguments[0].AsString()),
            BuiltinFunctionId.DirDate => EvaluateDirDate(expression, arguments[0].AsString()),
            BuiltinFunctionId.GetFiles => EvaluateGetFiles(expression, arguments[0].AsString(), pattern: "*", patternArgumentIndex: -1),
            BuiltinFunctionId.GetFilesPattern => EvaluateGetFiles(expression, arguments[0].AsString(), arguments[1].AsString(), patternArgumentIndex: 1),
            BuiltinFunctionId.GetDirs => EvaluateGetDirs(expression, arguments[0].AsString(), pattern: "*", patternArgumentIndex: -1),
            BuiltinFunctionId.GetDirsPattern => EvaluateGetDirs(expression, arguments[0].AsString(), arguments[1].AsString(), patternArgumentIndex: 1),
            BuiltinFunctionId.CombinePath => EvaluateCombinePath(expression, arguments[0].AsString(), arguments[1].AsString()),
            BuiltinFunctionId.FileName => EvaluateFileName(expression, arguments[0].AsString()),
            BuiltinFunctionId.DirName => EvaluateDirName(expression, arguments[0].AsString()),
            BuiltinFunctionId.ExtName => EvaluateExtName(expression, arguments[0].AsString()),
            BuiltinFunctionId.FullPath => EvaluateFullPath(expression, arguments[0].AsString()),
            BuiltinFunctionId.PathRoot => EvaluatePathRoot(expression, arguments[0].AsString()),
            BuiltinFunctionId.IsAbsolutePath => EvaluateIsAbsolutePath(expression, arguments[0].AsString()),
            BuiltinFunctionId.MakeDir => EvaluateMakeDir(expression, arguments[0].AsString()),
            BuiltinFunctionId.DeleteFile => EvaluateDeleteFile(expression, arguments[0].AsString()),
            BuiltinFunctionId.CopyFile => EvaluateCopyFile(expression, arguments[0].AsString(), arguments[1].AsString(), overwrite: false),
            BuiltinFunctionId.CopyFileOverwrite => EvaluateCopyFile(expression, arguments[0].AsString(), arguments[1].AsString(), arguments[2].AsBoolean()),
            BuiltinFunctionId.MoveFile => EvaluateMoveFile(expression, arguments[0].AsString(), arguments[1].AsString(), overwrite: false),
            BuiltinFunctionId.MoveFileOverwrite => EvaluateMoveFile(expression, arguments[0].AsString(), arguments[1].AsString(), arguments[2].AsBoolean()),
            BuiltinFunctionId.RenameFile => EvaluateRenameFile(expression, arguments[0].AsString(), arguments[1].AsString()),
            BuiltinFunctionId.DeleteDir => EvaluateDeleteDir(expression, arguments[0].AsString()),
            BuiltinFunctionId.DeleteDirRecursive => EvaluateDeleteDirRecursive(expression, arguments[0].AsString()),
            BuiltinFunctionId.CopyDir => EvaluateCopyDir(expression, arguments[0].AsString(), arguments[1].AsString(), overwrite: false),
            BuiltinFunctionId.CopyDirOverwrite => EvaluateCopyDir(expression, arguments[0].AsString(), arguments[1].AsString(), arguments[2].AsBoolean()),
            BuiltinFunctionId.MoveDir => EvaluateMoveDir(expression, arguments[0].AsString(), arguments[1].AsString()),
            BuiltinFunctionId.RenameDir => EvaluateRenameDir(expression, arguments[0].AsString(), arguments[1].AsString()),
            BuiltinFunctionId.CurrentDir => EvaluateCurrentDir(expression),
            BuiltinFunctionId.ChangeDir => EvaluateChangeDir(expression, arguments[0].AsString()),
            BuiltinFunctionId.DirSize => EvaluateDirSize(expression, arguments[0].AsString()),
            BuiltinFunctionId.FreeSpace => EvaluateFreeSpace(expression, arguments[0].AsString()),
            BuiltinFunctionId.TotalFreeSpace => EvaluateTotalFreeSpace(expression, arguments[0].AsString()),
            BuiltinFunctionId.TotalSpace => EvaluateTotalSpace(expression, arguments[0].AsString()),
            BuiltinFunctionId.IsReadOnly => EvaluateIsReadOnly(expression, arguments[0].AsString()),
            BuiltinFunctionId.IsHidden => EvaluateIsHidden(expression, arguments[0].AsString()),
            BuiltinFunctionId.DriveName => EvaluateDriveName(expression, arguments[0].AsString()),
            BuiltinFunctionId.DriveFormat => EvaluateDriveFormat(expression, arguments[0].AsString()),
            BuiltinFunctionId.DriveType => EvaluateDriveType(expression, arguments[0].AsString()),
            BuiltinFunctionId.MakeTempFile => EvaluateMakeTempFile(expression, arguments[0].AsString()),
            BuiltinFunctionId.MakeTempDir => EvaluateMakeTempDir(expression, arguments[0].AsString()),
            BuiltinFunctionId.IsOpen => EvaluateIsOpen(arguments[0]),
            BuiltinFunctionId.FilePath => EvaluateFilePath(expression, arguments[0]),
            BuiltinFunctionId.FileMode => EvaluateFileMode(expression, arguments[0]),
            BuiltinFunctionId.FilePos => EvaluateFilePos(expression, arguments[0]),
            BuiltinFunctionId.FileLength => EvaluateFileLength(expression, arguments[0]),
            BuiltinFunctionId.LineNumber => EvaluateLineNumber(expression, arguments[0]),
            BuiltinFunctionId.Eof => EvaluateEof(expression, arguments[0]),
            BuiltinFunctionId.ReadToEnd => EvaluateReadToEnd(expression, arguments[0]),
            BuiltinFunctionId.ReadText => EvaluateReadText(expression, arguments[0], RequireNonNegativeCount(expression, "READTEXT", "count", 1, arguments[1].AsInteger())),
            BuiltinFunctionId.ReadLine => EvaluateReadLine(expression, arguments[0]),
            BuiltinFunctionId.ReadLines => EvaluateReadLines(expression, arguments[0], RequireNonNegativeCount(expression, "READLINES", "count", 1, arguments[1].AsInteger())),
            BuiltinFunctionId.SkipLine => EvaluateSkipLine(expression, arguments[0]),
            BuiltinFunctionId.Write => EvaluateWrite(expression, arguments[0], arguments[1].AsString(), appendLineBreak: false),
            BuiltinFunctionId.WriteLine => EvaluateWrite(expression, arguments[0], arguments[1].AsString(), appendLineBreak: true),
            BuiltinFunctionId.NewLine => EvaluateNewLine(expression, arguments[0]),
            BuiltinFunctionId.Flush => EvaluateFlush(expression, arguments[0]),
            BuiltinFunctionId.SeekFile => EvaluateSeekFile(expression, arguments[0], arguments[1]),
            BuiltinFunctionId.Rewind => EvaluateRewind(expression, arguments[0]),
            BuiltinFunctionId.Int => RuntimeValue.FromInteger(EvaluateIntBuiltin(expression, arguments[0])),
            BuiltinFunctionId.Str => RuntimeValue.FromString(arguments[0].ToDisplayString()),
            BuiltinFunctionId.Val => RuntimeValue.FromDouble(ParseVal(arguments[0].AsString())),
            BuiltinFunctionId.LCase => RuntimeValue.FromString(arguments[0].AsString().ToLowerInvariant()),
            BuiltinFunctionId.UCase => RuntimeValue.FromString(arguments[0].AsString().ToUpperInvariant()),
            BuiltinFunctionId.Rnd => EvaluateRndBuiltin(),
            BuiltinFunctionId.Abs => EvaluateAbsBuiltin(expression, arguments[0]),
            BuiltinFunctionId.Sgn => EvaluateSgnBuiltin(arguments[0], expression.Location),
            BuiltinFunctionId.Min => EvaluateMinBuiltin(arguments[0], arguments[1], expression.Location),
            BuiltinFunctionId.Max => EvaluateMaxBuiltin(arguments[0], arguments[1], expression.Location),
            BuiltinFunctionId.Clamp => EvaluateClampBuiltin(expression, arguments[0], arguments[1], arguments[2]),
            BuiltinFunctionId.Sin => EvaluateDoubleMathBuiltin(expression, "SIN", "value", 0, arguments[0], Math.Sin),
            BuiltinFunctionId.Cos => EvaluateDoubleMathBuiltin(expression, "COS", "value", 0, arguments[0], Math.Cos),
            BuiltinFunctionId.Tan => EvaluateDoubleMathBuiltin(expression, "TAN", "value", 0, arguments[0], Math.Tan),
            BuiltinFunctionId.Atn => EvaluateDoubleMathBuiltin(expression, "ATN", "value", 0, arguments[0], Math.Atan),
            BuiltinFunctionId.Atn2 => EvaluateAtn2Builtin(expression, arguments[0], arguments[1]),
            BuiltinFunctionId.Sqr => EvaluateSqrBuiltin(expression, arguments[0]),
            BuiltinFunctionId.Cbrt => EvaluateDoubleMathBuiltin(expression, "CBRT", "value", 0, arguments[0], Math.Cbrt),
            BuiltinFunctionId.Exp => EvaluateDoubleMathBuiltin(expression, "EXP", "value", 0, arguments[0], Math.Exp),
            BuiltinFunctionId.Log => EvaluateLogBuiltin(expression, "LOG", arguments[0], Math.Log),
            BuiltinFunctionId.Log10 => EvaluateLogBuiltin(expression, "LOG10", arguments[0], Math.Log10),
            BuiltinFunctionId.Log2 => EvaluateLogBuiltin(expression, "LOG2", arguments[0], Math.Log2),
            BuiltinFunctionId.Pow => EvaluatePowBuiltin(expression, arguments[0], arguments[1]),
            BuiltinFunctionId.Floor => EvaluateFloorBuiltin(expression, arguments[0]),
            BuiltinFunctionId.Ceil => EvaluateCeilBuiltin(expression, arguments[0]),
            BuiltinFunctionId.Round => EvaluateRoundBuiltin(expression, arguments[0]),
            BuiltinFunctionId.Fix => EvaluateFixBuiltin(expression, arguments[0]),
            BuiltinFunctionId.Asin => EvaluateInverseTrigBuiltin(expression, "ASIN", arguments[0], Math.Asin),
            BuiltinFunctionId.Acos => EvaluateInverseTrigBuiltin(expression, "ACOS", arguments[0], Math.Acos),
            BuiltinFunctionId.Asinh => EvaluateDoubleMathBuiltin(expression, "ASINH", "value", 0, arguments[0], Math.Asinh),
            BuiltinFunctionId.Acosh => EvaluateAcoshBuiltin(expression, arguments[0]),
            BuiltinFunctionId.Atanh => EvaluateAtanhBuiltin(expression, arguments[0]),
            BuiltinFunctionId.Sinh => EvaluateDoubleMathBuiltin(expression, "SINH", "value", 0, arguments[0], Math.Sinh),
            BuiltinFunctionId.Cosh => EvaluateDoubleMathBuiltin(expression, "COSH", "value", 0, arguments[0], Math.Cosh),
            BuiltinFunctionId.Tanh => EvaluateDoubleMathBuiltin(expression, "TANH", "value", 0, arguments[0], Math.Tanh),
            BuiltinFunctionId.SetSign => EvaluateSetSignBuiltin(expression, arguments[0], arguments[1]),
            _ => ReportInvalidRuntimeState<RuntimeValue>(
                $"Неизвестная встроенная функция '{function.Name}'.",
                expression.Location)
        };
    }

    private RuntimeValue InvokeUserRoutine(
        RuntimeFunctionValue routineValue,
        IReadOnlyList<RuntimeValue> arguments,
        TextLocation callSiteLocation)
    {
        var function = routineValue.Function;

        if (!_routines.TryGetValue(function, out var routine))
        {
            return ReportInvalidRuntimeState<RuntimeValue>(
                $"Для вызова '{function.Name}' отсутствует связанная процедура или функция.",
                callSiteLocation);
        }

        var scope = new RuntimeScope(routineValue.CapturedScope, function);

        for (var index = 0; index < function.Parameters.Count; index++)
        {
            var parameter = function.Parameters[index];
            var argument = arguments[index];
            IRuntimeStoredValue storedArgument = parameter.IsArray ? argument.AsArray() : argument.CopyForStorage();
            scope.Define(parameter, storedArgument);
        }

        _callStack.Push(new CallFrame(function, scope, callSiteLocation));
        _scopeStack.Push(scope);

        try
        {
            var signal = ExecuteStatements(routine.Statements);

            if (signal.HasExitFor)
            {
                ReportInvalidRuntimeState(
                    "EXIT FOR вышел за пределы процедуры или функции без окружающего цикла FOR.",
                    routine.Location);
                throw new RuntimeFailureException();
            }

            if (function.IsSub)
            {
                return RuntimeValue.Void;
            }

            if (!signal.HasReturn || signal.Value is null || signal.Value.Type == TypeSymbol.Void)
            {
                ReportMissingReturnValue(function.Name, routine.Location);
                throw new RuntimeFailureException();
            }

            return signal.Value.CopyForStorage();
        }
        finally
        {
            _scopeStack.Pop();
            _callStack.Pop();
        }
    }

    private int RequirePositiveCount(
        BoundCallExpression expression,
        string functionName,
        string argumentName,
        int argumentIndex,
        int value)
    {
        if (value == 0)
        {
            ReportBuiltInArgumentError(
                functionName,
                argumentName,
                "должен быть больше 0.",
                GetBuiltInArgumentLocation(expression, argumentIndex));
            throw new RuntimeFailureException();
        }

        if (value < 0)
        {
            ReportBuiltInArgumentError(
                functionName,
                argumentName,
                "не может быть отрицательным.",
                GetBuiltInArgumentLocation(expression, argumentIndex));
            throw new RuntimeFailureException();
        }

        return value;
    }

    private int RequirePositiveStart(BoundCallExpression expression, string functionName, int argumentIndex, int value)
    {
        if (value < 1)
        {
            ReportBuiltInArgumentError(
                functionName,
                "start",
                "должен быть больше или равен 1.",
                GetBuiltInArgumentLocation(expression, argumentIndex));
            throw new RuntimeFailureException();
        }

        return value;
    }
}
