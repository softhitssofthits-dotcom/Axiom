namespace Axiom.Semantics;

public static partial class BuiltinFunctions
{
    private static IReadOnlyList<BuiltinFunctionDescriptor> GetFileSystemDescriptors()
    {
        return
        [
            CreateDefault(BuiltinFunctionId.FileExists, "FILEEXISTS", TypeSymbol.Boolean, [Parameter("path", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.DirExists, "DIREXISTS", TypeSymbol.Boolean, [Parameter("path", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.PathExists, "PATHEXISTS", TypeSymbol.Boolean, [Parameter("path", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.ReadFile, "READFILE", TypeSymbol.String, [Parameter("path", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.ReadAllLines, "READALLLINES", ArrayTypeSymbol.Create(TypeSymbol.String), [Parameter("path", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.WriteFile, "WRITEFILE", TypeSymbol.Boolean, [Parameter("path", TypeSymbol.String), Parameter("text", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.AppendFile, "APPENDFILE", TypeSymbol.Boolean, [Parameter("path", TypeSymbol.String), Parameter("text", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.FileSize, "FILESIZE", TypeSymbol.Long, [Parameter("path", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.FileDate, "FILEDATE", TypeSymbol.String, [Parameter("path", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.FileCreateDate, "FILECREATEDATE", TypeSymbol.String, [Parameter("path", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.FileAccessDate, "FILEACCESSEDDATE", TypeSymbol.String, [Parameter("path", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.DirDate, "DIRDATE", TypeSymbol.String, [Parameter("path", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.GetFiles, "GETFILES", ArrayTypeSymbol.Create(TypeSymbol.String), [Parameter("path", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.GetFilesPattern, "GETFILES", ArrayTypeSymbol.Create(TypeSymbol.String), [Parameter("path", TypeSymbol.String), Parameter("pattern", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.GetDirs, "GETDIRS", ArrayTypeSymbol.Create(TypeSymbol.String), [Parameter("path", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.GetDirsPattern, "GETDIRS", ArrayTypeSymbol.Create(TypeSymbol.String), [Parameter("path", TypeSymbol.String), Parameter("pattern", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.CombinePath, "COMBINEPATH", TypeSymbol.String, [Parameter("left", TypeSymbol.String), Parameter("right", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.FileName, "FILENAME", TypeSymbol.String, [Parameter("path", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.DirName, "DIRNAME", TypeSymbol.String, [Parameter("path", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.ExtName, "EXTNAME", TypeSymbol.String, [Parameter("path", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.FullPath, "FULLPATH", TypeSymbol.String, [Parameter("path", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.PathRoot, "PATHROOT", TypeSymbol.String, [Parameter("path", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.IsAbsolutePath, "ISABSOLUTEPATH", TypeSymbol.Boolean, [Parameter("path", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.MakeDir, "MAKEDIR", TypeSymbol.Boolean, [Parameter("path", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.DeleteFile, "DELETEFILE", TypeSymbol.Boolean, [Parameter("path", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.CopyFile, "COPYFILE", TypeSymbol.Boolean, [Parameter("source", TypeSymbol.String), Parameter("destination", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.CopyFileOverwrite, "COPYFILE", TypeSymbol.Boolean, [Parameter("source", TypeSymbol.String), Parameter("destination", TypeSymbol.String), Parameter("overwrite", TypeSymbol.Boolean)]),
            CreateDefault(BuiltinFunctionId.MoveFile, "MOVEFILE", TypeSymbol.Boolean, [Parameter("source", TypeSymbol.String), Parameter("destination", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.MoveFileOverwrite, "MOVEFILE", TypeSymbol.Boolean, [Parameter("source", TypeSymbol.String), Parameter("destination", TypeSymbol.String), Parameter("overwrite", TypeSymbol.Boolean)]),
            CreateDefault(BuiltinFunctionId.RenameFile, "RENAMEFILE", TypeSymbol.Boolean, [Parameter("path", TypeSymbol.String), Parameter("newName", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.DeleteDir, "DELETEDIR", TypeSymbol.Boolean, [Parameter("path", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.DeleteDirRecursive, "DELETEDIRRECURSIVE", TypeSymbol.Boolean, [Parameter("path", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.CopyDir, "COPYDIR", TypeSymbol.Boolean, [Parameter("source", TypeSymbol.String), Parameter("destination", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.CopyDirOverwrite, "COPYDIR", TypeSymbol.Boolean, [Parameter("source", TypeSymbol.String), Parameter("destination", TypeSymbol.String), Parameter("overwrite", TypeSymbol.Boolean)]),
            CreateDefault(BuiltinFunctionId.MoveDir, "MOVEDIR", TypeSymbol.Boolean, [Parameter("source", TypeSymbol.String), Parameter("destination", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.RenameDir, "RENAMEDIR", TypeSymbol.Boolean, [Parameter("path", TypeSymbol.String), Parameter("newName", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.CurrentDir, "CURRENTDIR", TypeSymbol.String, []),
            CreateDefault(BuiltinFunctionId.ChangeDir, "CHANGEDIR", TypeSymbol.Boolean, [Parameter("path", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.DirSize, "DIRSIZE", TypeSymbol.Long, [Parameter("path", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.FreeSpace, "FREESPACE", TypeSymbol.Long, [Parameter("path", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.TotalFreeSpace, "TOTALFREESPACE", TypeSymbol.Long, [Parameter("path", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.TotalSpace, "TOTALSPACE", TypeSymbol.Long, [Parameter("path", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.IsReadOnly, "ISREADONLY", TypeSymbol.Boolean, [Parameter("path", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.IsHidden, "ISHIDDEN", TypeSymbol.Boolean, [Parameter("path", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.DriveName, "DRIVENAME", TypeSymbol.String, [Parameter("path", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.DriveFormat, "DRIVEFORMAT", TypeSymbol.String, [Parameter("path", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.DriveType, "DRIVETYPE", TypeSymbol.String, [Parameter("path", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.MakeTempFile, "MAKETEMPFILE", TypeSymbol.String, [Parameter("prefix", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.MakeTempDir, "MAKETEMPDIR", TypeSymbol.String, [Parameter("prefix", TypeSymbol.String)])
        ];
    }
}
