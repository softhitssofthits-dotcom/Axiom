using System.Text;

static class FileCases
{
    public static IEnumerable<AuditCase> Create(string output)
    {
        var cases = new List<AuditCase>();
        void Add(string id, Func<string,string> source, string? expected = null, string? error = null)
        {
            // Новый изолированный каталог для каждого сценария и каждой пары движков.
            var folder = Path.GetFullPath(Path.Combine(output,"fs-cases",id));
            var allowed = Path.GetFullPath(Path.Combine(output,"fs-cases")) + Path.DirectorySeparatorChar;
            if (!folder.StartsWith(allowed,StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Каталог вне аудита");
            void Prepare()
            {
                if(Directory.Exists(folder)) Directory.Delete(folder,true);
                Directory.CreateDirectory(folder);
                File.WriteAllText(Path.Combine(folder,"данные.txt"),"abc",new UTF8Encoding(false));
                Directory.CreateDirectory(Path.Combine(folder,"вложенный"));
                File.WriteAllText(Path.Combine(folder,"вложенный/ещё.txt"),"xy",new UTF8Encoding(false));
            }
            cases.Add(new("fs-"+id,source(folder),expected,error,Prepare,
                ()=>string.Join(";",Directory.Exists(folder)? Directory.GetFiles(folder,"*",SearchOption.AllDirectories).Select(p=>Path.GetRelativePath(folder,p)+":"+Convert.ToHexString(File.ReadAllBytes(p))).Order():[])));
        }
        string Q(string p)=>"\""+p.Replace("\"","\"\"")+"\"";
        string F(string p)=>Q(Path.Combine(p,"данные.txt"));
        Add("exists",p=>$"PRINT FILEEXISTS({F(p)}), DIREXISTS({Q(p)}), PATHEXISTS({F(p)})\nPRINT FILEEXISTS({Q(Path.Combine(p,"нет"))})","TRUE TRUE TRUE\nFALSE");
        Add("readlines",p=>$"PRINT READFILE({F(p)})\nPRINT ARRAYLENGTH(READALLLINES({F(p)}))","abc\n1");
        Add("names",p=>$"PRINT FILENAME({F(p)}), EXTNAME({F(p)})\nPRINT ISABSOLUTEPATH({F(p)})\nPRINT DIRNAME({F(p)}) = {Q(p)}\nPRINT COMBINEPATH({Q(p)},\"данные.txt\") = {F(p)}","данные.txt .txt\nTRUE\nTRUE\nTRUE");
        Add("fullpath",p=>$"PRINT FULLPATH({F(p)}) = {F(p)}\nPRINT PATHROOT({F(p)}) = {Q(Path.GetPathRoot(p)!)}","TRUE\nTRUE");
        Add("lists",p=>$"PRINT ARRAYLENGTH(GETFILES({Q(p)})), ARRAYLENGTH(GETFILES({Q(p)},\"*.txt\"))\nPRINT ARRAYLENGTH(GETDIRS({Q(p)})), ARRAYLENGTH(GETDIRS({Q(p)},\"в*\"))","1 1\n1 1");
        Add("size",p=>$"PRINT FILESIZE({F(p)}), DIRSIZE({Q(p)})","3 5");
        Add("dates",p=>$"PRINT LEN(FILEDATE({F(p)})) > 0\nPRINT LEN(FILECREATEDATE({F(p)})) > 0\nPRINT LEN(FILEACCESSEDDATE({F(p)})) > 0\nPRINT LEN(DIRDATE({Q(p)})) > 0","TRUE\nTRUE\nTRUE\nTRUE");
        Add("drive",p=>$"PRINT FREESPACE({Q(p)}) >= 0\nPRINT TOTALFREESPACE({Q(p)}) >= 0\nPRINT TOTALSPACE({Q(p)}) > 0\nPRINT LEN(DRIVENAME({Q(p)})) > 0\nPRINT LEN(DRIVEFORMAT({Q(p)})) > 0\nPRINT LEN(DRIVETYPE({Q(p)})) > 0","TRUE\nTRUE\nTRUE\nTRUE\nTRUE\nTRUE");
        Add("attributes",p=>$"PRINT ISREADONLY({F(p)}), ISHIDDEN({F(p)})","FALSE FALSE");
        Add("write-append",p=>$"WRITEFILE({F(p)},\"Я\")\nAPPENDFILE({F(p)},\"😀\")\nPRINT READFILE({F(p)})","Я😀");
        Add("copy",p=>$"COPYFILE({F(p)},{Q(Path.Combine(p,"копия"))})\nPRINT READFILE({Q(Path.Combine(p,"копия"))})","abc");
        Add("copy-overwrite",p=>$"COPYFILE({F(p)},{Q(Path.Combine(p,"копия"))},TRUE)\nCOPYFILE({F(p)},{Q(Path.Combine(p,"копия"))},TRUE)\nPRINT READFILE({Q(Path.Combine(p,"копия"))})","abc");
        Add("copy-existing",p=>$"COPYFILE({F(p)},{F(p)},FALSE)",error:"MB3007");
        Add("move",p=>$"MOVEFILE({F(p)},{Q(Path.Combine(p,"новый"))})\nPRINT FILEEXISTS({F(p)}), FILEEXISTS({Q(Path.Combine(p,"новый"))})","FALSE TRUE");
        Add("move-overwrite",p=>$"MOVEFILE({F(p)},{Q(Path.Combine(p,"новый"))},TRUE)\nPRINT FILEEXISTS({Q(Path.Combine(p,"новый"))})","TRUE");
        Add("rename",p=>$"RENAMEFILE({F(p)},\"новый.txt\")\nPRINT FILEEXISTS({Q(Path.Combine(p,"новый.txt"))})","TRUE");
        Add("deletefile",p=>$"DELETEFILE({F(p)})\nPRINT FILEEXISTS({F(p)})","FALSE");
        Add("directory",p=>$"MAKEDIR({Q(Path.Combine(p,"новый/глубже"))})\nPRINT DIREXISTS({Q(Path.Combine(p,"новый/глубже"))})\nDELETEDIR({Q(Path.Combine(p,"новый/глубже"))})\nPRINT DIREXISTS({Q(Path.Combine(p,"новый/глубже"))})","TRUE\nFALSE");
        Add("recursive-delete",p=>$"DELETEDIRRECURSIVE({Q(Path.Combine(p,"вложенный"))})\nPRINT DIREXISTS({Q(Path.Combine(p,"вложенный"))})","FALSE");
        Add("copydir",p=>$"COPYDIR({Q(Path.Combine(p,"вложенный"))},{Q(Path.Combine(p,"копия"))})\nPRINT DIRSIZE({Q(Path.Combine(p,"копия"))})","2");
        Add("copydir-overwrite",p=>$"COPYDIR({Q(Path.Combine(p,"вложенный"))},{Q(Path.Combine(p,"копия"))},TRUE)\nPRINT DIRSIZE({Q(Path.Combine(p,"копия"))})","2");
        Add("movedir",p=>$"MOVEDIR({Q(Path.Combine(p,"вложенный"))},{Q(Path.Combine(p,"новый"))})\nPRINT DIREXISTS({Q(Path.Combine(p,"вложенный"))}), DIREXISTS({Q(Path.Combine(p,"новый"))})","FALSE TRUE");
        Add("renamedir",p=>$"RENAMEDIR({Q(Path.Combine(p,"вложенный"))},\"новый\")\nPRINT DIREXISTS({Q(Path.Combine(p,"новый"))})","TRUE");
        Add("missing-read",p=>$"PRINT READFILE({Q(Path.Combine(p,"нет"))})",error:"MB3007");
        Add("missing-parent",p=>$"WRITEFILE({Q(Path.Combine(p,"нет/данные.txt"))},\"x\")",error:"MB3007");
        Add("handle-lifecycle",p=>$"DIM f AS FILE\nOPEN {F(p)} FOR INPUT AS f\nPRINT ISOPEN(f), FILEMODE(f), FILEPOS(f), FILELENGTH(f), LINENUMBER(f), EOF(f)\nPRINT FILEPATH(f) = {F(p)}\nPRINT READTEXT(f,1), FILEPOS(f)\nSEEKFILE(f,3)\nPRINT EOF(f)\nREWIND(f)\nPRINT READLINE(f)\nCLOSE f\nPRINT ISOPEN(f)","TRUE INPUT 0 3 1 FALSE\nTRUE\na 1\nTRUE\nabc\nFALSE");
        Add("handle-readlines",p=>$"DIM f AS FILE\nOPEN {F(p)} FOR INPUT AS f\nPRINT ARRAYLENGTH(READLINES(f,2))\nREWIND(f)\nSKIPLINE(f)\nPRINT EOF(f)\nCLOSE f","1\nTRUE");
        Add("handle-write",p=>$"DIM f AS FILE\nOPEN {F(p)} FOR OUTPUT AS f\nWRITE(f,\"ab\")\nFLUSH(f)\nPRINT FILEPOS(f), FILELENGTH(f)\nCLOSE f\nOPEN {F(p)} FOR APPEND AS f\nWRITE(f,\"c\")\nCLOSE f\nPRINT READFILE({F(p)})","2 2\nabc");
        Add("handle-closed",p=>$"DIM f AS FILE\nOPEN {F(p)} FOR INPUT AS f\nCLOSE f\nPRINT READTOEND(f)",error:"MB3007");
        Add("handle-seek-negative",p=>$"DIM f AS FILE\nOPEN {F(p)} FOR INPUT AS f\nSEEKFILE(f,-1)",error:"MB3007");
        Add("handle-mode",p=>$"DIM f AS FILE\nOPEN {F(p)} FOR INPUT AS f\nWRITE(f,\"x\")",error:"MB3007");
        return cases;
    }
}
