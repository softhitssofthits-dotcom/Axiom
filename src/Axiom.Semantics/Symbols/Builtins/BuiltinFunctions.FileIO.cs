namespace Axiom.Semantics;

public static partial class BuiltinFunctions
{
    private static IReadOnlyList<BuiltinFunctionDescriptor> GetFileIoDescriptors()
    {
        return
        [
            CreateDefault(BuiltinFunctionId.IsOpen, "ISOPEN", TypeSymbol.Boolean, [Parameter("file", TypeSymbol.File)]),
            CreateDefault(BuiltinFunctionId.FilePath, "FILEPATH", TypeSymbol.String, [Parameter("file", TypeSymbol.File)]),
            CreateDefault(BuiltinFunctionId.FileMode, "FILEMODE", TypeSymbol.String, [Parameter("file", TypeSymbol.File)]),
            CreateDefault(BuiltinFunctionId.FilePos, "FILEPOS", TypeSymbol.Long, [Parameter("file", TypeSymbol.File)]),
            CreateDefault(BuiltinFunctionId.FileLength, "FILELENGTH", TypeSymbol.Long, [Parameter("file", TypeSymbol.File)]),
            CreateDefault(BuiltinFunctionId.LineNumber, "LINENUMBER", TypeSymbol.Long, [Parameter("file", TypeSymbol.File)]),
            CreateDefault(BuiltinFunctionId.Eof, "EOF", TypeSymbol.Boolean, [Parameter("file", TypeSymbol.File)]),
            CreateDefault(BuiltinFunctionId.ReadToEnd, "READTOEND", TypeSymbol.String, [Parameter("file", TypeSymbol.File)]),
            CreateDefault(BuiltinFunctionId.ReadText, "READTEXT", TypeSymbol.String, [Parameter("file", TypeSymbol.File), Parameter("count", TypeSymbol.Integer)]),
            CreateDefault(BuiltinFunctionId.ReadLine, "READLINE", TypeSymbol.String, [Parameter("file", TypeSymbol.File)]),
            CreateDefault(BuiltinFunctionId.ReadLines, "READLINES", ArrayTypeSymbol.Create(TypeSymbol.String), [Parameter("file", TypeSymbol.File), Parameter("count", TypeSymbol.Integer)]),
            CreateDefault(BuiltinFunctionId.SkipLine, "SKIPLINE", TypeSymbol.Boolean, [Parameter("file", TypeSymbol.File)]),
            CreateDefault(BuiltinFunctionId.Write, "WRITE", TypeSymbol.Boolean, [Parameter("file", TypeSymbol.File), Parameter("text", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.WriteLine, "WRITELINE", TypeSymbol.Boolean, [Parameter("file", TypeSymbol.File), Parameter("text", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.NewLine, "NEWLINE", TypeSymbol.Boolean, [Parameter("file", TypeSymbol.File)]),
            CreateDefault(BuiltinFunctionId.Flush, "FLUSH", TypeSymbol.Boolean, [Parameter("file", TypeSymbol.File)]),
            CreateDefault(BuiltinFunctionId.SeekFile, "SEEKFILE", TypeSymbol.Boolean, [Parameter("file", TypeSymbol.File), Parameter("position", TypeSymbol.Long)]),
            CreateDefault(BuiltinFunctionId.Rewind, "REWIND", TypeSymbol.Boolean, [Parameter("file", TypeSymbol.File)])
        ];
    }
}
