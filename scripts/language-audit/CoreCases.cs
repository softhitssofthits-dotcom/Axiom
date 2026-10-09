static class CoreCases
{
    public static IEnumerable<AuditCase> Create()
    {
        var cases = new List<AuditCase>
        {
            new("core-precedence","PRINT 2+3*4, 2^3^2, 8 \\ 3, 8 MOD 3\nPRINT NOT FALSE AND TRUE\nPRINT 1 << 3, 8 >> 2","14 512 2 2\nTRUE\n8 2"),
            new("core-byref","SUB Inc(BYREF x AS INTEGER)\nx=x+1\nEND SUB\nDIM a AS INTEGER=4\nInc(a)\nPRINT a","5"),
            new("core-recursion","FUNCTION Fact(n AS INTEGER) AS INTEGER\nIF n <= 1 THEN\nRETURN 1\nEND IF\nRETURN n*Fact(n-1)\nEND FUNCTION\nPRINT Fact(6)","720"),
            new("core-type-copy","TYPE Box\nValue AS INTEGER\nEND TYPE\nDIM a AS Box\na.Value=4\nDIM b AS Box\nb=a\nb.Value=7\nPRINT a.Value,b.Value\nPRINT a=b","4 7\nFALSE"),
            new("core-for-step","DIM total AS INTEGER=0\nFOR i=5 TO 1 STEP -2\ntotal=total+i\nNEXT i\nPRINT total","9"),
            new("core-loop-finally","FOR i=1 TO 2\nTRY\nCONTINUE FOR\nFINALLY\nPRINT i\nEND TRY\nNEXT","1\n2"),
            new("core-return-finally","FUNCTION F() AS INTEGER\nTRY\nRETURN 9\nFINALLY\nPRINT 7\nEND TRY\nEND FUNCTION\nPRINT F()","7\n9"),
            new("core-select","SELECT CASE 4\nCASE 1 TO 3\nPRINT 1\nCASE 4,5\nPRINT 2\nCASE ELSE\nPRINT 3\nEND SELECT","2"),
            new("core-if","IF FALSE THEN\nPRINT 1\nELSEIF TRUE THEN\nPRINT 2\nELSE\nPRINT 3\nEND IF","2"),
            new("core-while-do","DIM n AS INTEGER=0\nWHILE n<2\nn=n+1\nEND WHILE\nDO\nn=n-1\nLOOP UNTIL n=0\nPRINT n","0"),
            new("core-print","PRINT\nPRINT \"Русский 😀\"\nPRINT 1;2\nPRINT USING \"##.##\"; 1.25","\nРусский 😀\n12\n 1.25"),
            new("core-expression-no-effect","DIM a AS INTEGER\na", ErrorCode:"MB2040"),
            new("core-byref-invalid","SUB Inc(BYREF x AS INTEGER)\nx=x+1\nEND SUB\nInc(1+2)",ErrorCode:"MB2028"),
            new("core-call-depth","FUNCTION F(n AS INTEGER) AS INTEGER\nRETURN F(n+1)\nEND FUNCTION\nPRINT F(0)",ErrorCode:"MB3011")
        };
        return cases;
    }
}
