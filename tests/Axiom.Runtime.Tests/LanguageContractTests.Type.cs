using Axiom.Parsing;
using Axiom.Semantics;
using System.Text;

namespace Axiom.Runtime.Tests;

public sealed partial class LanguageContractTests
{
    [Fact]
    public void Execute_TypeFieldsAndNestedRecords()
    {
        const string text = """
TYPE Address
    City AS STRING
END TYPE

TYPE Person
    Name AS STRING
    Age AS INTEGER
    Home AS Address
END TYPE

DIM user AS Person
PRINT user.Name
PRINT user.Age
user.Name = "Ada"
user.Age = 36
user.Home.City = "London"
PRINT user.Name
PRINT user.Age
PRINT user.Home.City
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["", "0", "Ada", "36", "London"], host.Output);
    }

    [Fact]
    public void Execute_TypeAssignmentCopiesRecordValue()
    {
        const string text = """
TYPE Person
    Name AS STRING
END TYPE

DIM first AS Person
DIM second AS Person
first.Name = "Ada"
second = first
first.Name = "Grace"
PRINT second.Name
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["Ada"], host.Output);
    }

    [Fact]
    public void Execute_TypeArrayElementsAndFields()
    {
        const string text = "TYPE Address\r\n    City AS STRING\r\nEND TYPE\r\n\r\nTYPE Person\r\n    Name AS STRING\r\n    Age AS INTEGER\r\n    Home AS Address\r\nEND TYPE\r\n\r\nDIM people[1 TO 2] AS Person\r\nPRINT people[1].Name\r\nPRINT people[1].Age\r\npeople[1].Name = \"Ada\"\r\npeople[1].Age = 36\r\npeople[1].Home.City = \"London\"\r\npeople[2].Name = \"Grace\"\r\nPRINT people[1].Name\r\nPRINT people[1].Age\r\nPRINT people[1].Home.City\r\nPRINT people[2].Name\r\nPRINT ARRAYLENGTH(people[])";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["", "0", "Ada", "36", "London", "Grace", "2"], host.Output);
    }

    [Fact]
    public void Execute_TypeArrayElementAssignmentCopiesRecordValue()
    {
        const string text = "TYPE Person\r\n    Name AS STRING\r\nEND TYPE\r\n\r\nDIM first AS Person\r\nDIM copy AS Person\r\nDIM people[1 TO 1] AS Person\r\nfirst.Name = \"Ada\"\r\npeople[1] = first\r\nfirst.Name = \"Grace\"\r\ncopy = people[1]\r\npeople[1].Name = \"Alan\"\r\nPRINT people[1].Name\r\nPRINT copy.Name\r\nPRINT first.Name";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["Alan", "Ada", "Grace"], host.Output);
    }

    [Fact]
    public void Execute_TypeDynamicArrayField()
    {
        const string text = "TYPE Person\r\n    Scores[] AS INTEGER\r\nEND TYPE\r\n\r\nDIM user AS Person\r\nREDIM user.Scores[1 TO 2]\r\nuser.Scores[1] = 10\r\nuser.Scores[2] = 20\r\nPRINT user.Scores[1]\r\nPRINT user.Scores[2]\r\nERASE user.Scores[]\r\nPRINT ARRAYISALLOCATED(user.Scores[])\r\nREDIM user.Scores[5 TO 5]\r\nPRINT user.Scores[5]";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["10", "20", "FALSE", "0"], host.Output);
    }

    [Fact]
    public void Execute_TypeDynamicArrayFieldOnArrayElement()
    {
        const string text = "TYPE Person\r\n    Scores[] AS INTEGER\r\nEND TYPE\r\n\r\nDIM people[1 TO 1] AS Person\r\nREDIM people[1].Scores[2 TO 3]\r\npeople[1].Scores[2] = 7\r\npeople[1].Scores[3] = 9\r\nPRINT people[1].Scores[2]\r\nPRINT people[1].Scores[3]\r\nREDIM PRESERVE people[1].Scores[3 TO 4]\r\nPRINT people[1].Scores[3]\r\nPRINT people[1].Scores[4]";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["7", "9", "9", "0"], host.Output);
    }

    [Fact]
    public void Execute_TypeDynamicMultidimensionalArrayField()
    {
        const string text = "TYPE Person\r\n    Scores[,] AS INTEGER\r\nEND TYPE\r\n\r\nDIM user AS Person\r\nREDIM user.Scores[1 TO 2, 3 TO 4]\r\nuser.Scores[1, 3] = 10\r\nuser.Scores[2, 4] = 20\r\nPRINT user.Scores[1, 3]\r\nPRINT user.Scores[2, 4]\r\nPRINT ARRAYLENGTH(user.Scores[])\r\nERASE user.Scores[]\r\nPRINT ARRAYISALLOCATED(user.Scores[])\r\n\r\nDIM people[1 TO 1] AS Person\r\nREDIM people[1].Scores[2 TO 3, 5 TO 6]\r\npeople[1].Scores[2, 5] = 7\r\npeople[1].Scores[3, 6] = 9\r\nREDIM PRESERVE people[1].Scores[3 TO 4, 6 TO 7]\r\nPRINT people[1].Scores[3, 6]\r\nPRINT people[1].Scores[4, 7]";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["10", "20", "4", "FALSE", "9", "0"], host.Output);
    }

    [Fact]
    public void Execute_TypeArrayFieldViews()
    {
        const string text = "TYPE Board\r\n    Cells[,] AS INTEGER\r\nEND TYPE\r\n\r\nDIM board AS Board\r\nDIM lineView[] AS INTEGER\r\nDIM region[,] AS INTEGER\r\n\r\nREDIM board.Cells[1 TO 3, 1 TO 4]\r\nboard.Cells[2, 3] = 23\r\n\r\nlineView[] = board.Cells[2]\r\nPRINT lineView[3]\r\nlineView[4] = 24\r\nPRINT board.Cells[2, 4]\r\n\r\nregion[] = board.Cells[2 TO 3, 3 TO 4]\r\nPRINT LBOUND(region[], 1)\r\nPRINT UBOUND(region[], 2)\r\nPRINT ARRAYLENGTH(region[])\r\nregion[3, 3] = 99\r\nPRINT board.Cells[3, 3]";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["23", "24", "2", "4", "4", "99"], host.Output);
    }

    [Fact]
    public void Execute_TypeNamedLiteral()
    {
        const string text = """
TYPE Address
    City AS STRING
END TYPE

TYPE Person
    Name AS STRING
    Age AS INTEGER
    Home AS Address
END TYPE

DIM user AS Person
DIM blank AS Person
user = Person(Name := "Ada", Age := 36, Home := Address(City := "Paris"))
blank = Person()
PRINT user.Name
PRINT user.Age
PRINT user.Home.City
PRINT LEN(blank.Name)
PRINT blank.Age
PRINT blank.Home.City = ""
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["Ada", "36", "Paris", "0", "0", "TRUE"], host.Output);
    }

    [Fact]
    public void Execute_TypePositionalLiteral()
    {
        const string text = """
TYPE Address
    City AS STRING
END TYPE

TYPE Person
    Name AS STRING
    Age AS INTEGER
    Home AS Address
END TYPE

DIM user AS Person
user = Person("Ada", 36, Address("Paris"))
PRINT user.Name
PRINT user.Age
PRINT user.Home.City
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["Ada", "36", "Paris"], host.Output);
    }

    [Fact]
    public void Execute_TypeFieldDefaultValues()
    {
        const string text = """
TYPE Person
    Name AS STRING = "Ada"
    Age AS INTEGER = 36
    Active AS BOOLEAN = TRUE
END TYPE

DIM user AS Person
DIM partial AS Person
user = Person()
partial = Person(Name := "Grace")
PRINT user.Name
PRINT user.Age
PRINT user.Active
PRINT partial.Name
PRINT partial.Age
PRINT partial.Active
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["Ada", "36", "TRUE", "Grace", "36", "TRUE"], host.Output);
    }

    [Fact]
    public void Execute_TypeStructuralEquality()
    {
        const string text = """
TYPE Address
    City AS STRING
END TYPE

TYPE Person
    Name AS STRING = "Ada"
    Age AS INTEGER = 36
    Home AS Address
END TYPE

DIM left AS Person
DIM right AS Person
DIM changed AS Person
left.Home.City = "Paris"
right.Home.City = "Paris"
changed.Home.City = "Rome"
PRINT left = right
PRINT left <> right
PRINT left = changed
PRINT left <> changed
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "FALSE", "FALSE", "TRUE"], host.Output);
    }

    [Fact]
    public void Execute_TypeFixedArrayField()
    {
        const string text = "TYPE Person\r\n    Scores[1 TO 3] AS INTEGER\r\nEND TYPE\r\n\r\nDIM first AS Person\r\nDIM second AS Person\r\nDIM people[1 TO 1] AS Person\r\nPRINT first.Scores[1]\r\nfirst.Scores[1] = 10\r\nfirst.Scores[3] = 30\r\nsecond = first\r\nfirst.Scores[1] = 99\r\npeople[1] = second\r\npeople[1].Scores[2] = 20\r\nPRINT second.Scores[1]\r\nPRINT second.Scores[3]\r\nPRINT people[1].Scores[1]\r\nPRINT people[1].Scores[2]\r\nPRINT ARRAYLENGTH(people[1].Scores[])";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["0", "10", "30", "10", "20", "3"], host.Output);
    }

    [Fact]
    public void Execute_TypeFixedMultidimensionalArrayField()
    {
        const string text = "TYPE Person\r\n    Grid[1 TO 2, 4 TO 5] AS INTEGER\r\nEND TYPE\r\n\r\nDIM first AS Person\r\nDIM second AS Person\r\nDIM people[1 TO 1] AS Person\r\n\r\nPRINT first.Grid[1, 4]\r\nfirst.Grid[1, 4] = 10\r\nfirst.Grid[2, 5] = 30\r\nsecond = first\r\nfirst.Grid[1, 4] = 99\r\npeople[1] = second\r\npeople[1].Grid[1, 5] = 20\r\n\r\nPRINT second.Grid[1, 4]\r\nPRINT second.Grid[2, 5]\r\nPRINT ARRAYLENGTH(people[1].Grid[])\r\nPRINT people[1].Grid[1, 4]\r\nPRINT people[1].Grid[1, 5]";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["0", "10", "30", "4", "10", "20"], host.Output);
    }

    [Fact]
    public void Execute_TypeFixedArrayFieldWithConstBounds()
    {
        const string text = "CONST MinScore AS INTEGER = 1\r\nCONST MaxScore AS LONG = MinScore + 2\r\n\r\nTYPE Person\r\n    Scores[MinScore TO MaxScore] AS INTEGER\r\nEND TYPE\r\n\r\nDIM first AS Person\r\nDIM second AS Person\r\nPRINT first.Scores[1]\r\nPRINT ARRAYLENGTH(first.Scores[])\r\nfirst.Scores[3] = 30\r\nsecond = first\r\nfirst.Scores[3] = 99\r\nPRINT second.Scores[3]";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["0", "3", "30"], host.Output);
    }

    [Fact]
    public void Execute_TypeFixedArrayFieldWithConstBoundsDeclaredLater()
    {
        const string text = "TYPE Person\r\n    Scores[MinScore TO MaxScore] AS INTEGER\r\nEND TYPE\r\n\r\nCONST MinScore AS INTEGER = 1\r\nCONST MaxScore AS LONG = MinScore + 2\r\n\r\nDIM first AS Person\r\nDIM second AS Person\r\nPRINT first.Scores[1]\r\nPRINT ARRAYLENGTH(first.Scores[])\r\nfirst.Scores[3] = 30\r\nsecond = first\r\nfirst.Scores[3] = 99\r\nPRINT second.Scores[3]";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["0", "3", "30"], host.Output);
    }

    [Fact]
    public void Execute_DimFixedLengthStringPadsAndTruncates()
    {
        const string text = """
DIM code AS STRING * 4
PRINT "[" + code + "]"
code = "AB"
PRINT "[" + code + "]"
code = "Alphabet"
PRINT "[" + code + "]"
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["[    ]", "[AB  ]", "[Alph]"], host.Output);
    }

    [Fact]
    public void Execute_TypeFixedLengthStringFieldPadsAndTruncates()
    {
        const string text = """
TYPE Employee
    Name AS STRING * 5
    Role AS STRING * 8
END TYPE

DIM user AS Employee
PRINT "[" + user.Name + "]"
user.Name = "Ada"
user.Role = "Analyst"
PRINT "[" + user.Name + "]"
PRINT "[" + user.Role + "]"
user.Role = "Architect"
PRINT "[" + user.Role + "]"
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["[     ]", "[Ada  ]", "[Analyst ]", "[Architec]"], host.Output);
    }

    [Fact]
    public void Execute_TypeGroupedFields()
    {
        const string text = "TYPE Person\r\n    Name, Alias AS STRING\r\n    Scores[], Flags[1 TO 2] AS INTEGER\r\nEND TYPE\r\n\r\nDIM first AS Person\r\nDIM second AS Person\r\nPRINT first.Name\r\nPRINT first.Alias\r\nPRINT first.Flags[1]\r\nREDIM first.Scores[1 TO 2]\r\nfirst.Name = \"Ada\"\r\nfirst.Alias = \"Analyst\"\r\nfirst.Scores[1] = 10\r\nfirst.Scores[2] = 20\r\nfirst.Flags[2] = 7\r\nsecond = first\r\nfirst.Alias = \"Changed\"\r\nfirst.Scores[1] = 99\r\nfirst.Flags[2] = 42\r\nPRINT second.Name\r\nPRINT second.Alias\r\nPRINT second.Scores[1]\r\nPRINT second.Scores[2]\r\nPRINT second.Flags[2]";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["", "", "0", "Ada", "Analyst", "99", "20", "7"], host.Output);
    }

    [Fact]
    public void Execute_ByRefRecordParameterMutatesCallerVariable()
    {
        const string text = """
TYPE Person
    Name AS STRING
    Age AS INTEGER
END TYPE

SUB Birthday(BYREF person AS Person)
    person.Age = person.Age + 1
END SUB

DIM user AS Person
user.Name = "Ada"
user.Age = 36
Birthday(user)
PRINT user.Age
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["37"], host.Output);
    }

    [Fact]
    public void Execute_ByRefScalarParameterMutatesCallerVariable()
    {
        const string text = """
SUB AddOne(BYREF value AS INTEGER)
    value = value + 1
END SUB

DIM count AS INTEGER
count = 10
AddOne(count)
PRINT count
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["11"], host.Output);
    }

    [Fact]
    public void Execute_ByValRecordParameterKeepsCallerVariableIndependent()
    {
        const string text = """
TYPE Person
    Name AS STRING
    Age AS INTEGER
END TYPE

SUB Rewrite(person AS Person)
    person.Name = "Changed"
    person.Age = 99
END SUB

DIM user AS Person
user.Name = "Ada"
user.Age = 36
Rewrite(user)
PRINT user.Name
PRINT user.Age
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["Ada", "36"], host.Output);
    }

    [Fact]
    public void Execute_TypeMethodsMutateSelfAndReturnValues()
    {
        const string text = """
TYPE Counter
    Value AS INTEGER

    SUB Increment(stepValue AS INTEGER)
        SELF.Value = SELF.Value + stepValue
    END SUB

    FUNCTION Current() AS INTEGER
        RETURN SELF.Value
    END FUNCTION
END TYPE

DIM counter AS Counter
counter.Increment(2)
counter.Increment(3)
PRINT counter.Current()
PRINT counter.Value
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["5", "5"], host.Output);
    }

    [Fact]
    public void Execute_TypeMethodsAcceptFieldAndArrayElementReceivers()
    {
        const string text = "TYPE Counter\r\n    Value AS INTEGER\r\n\r\n    SUB Increment(stepValue AS INTEGER)\r\n        SELF.Value = SELF.Value + stepValue\r\n    END SUB\r\n\r\n    FUNCTION Current() AS INTEGER\r\n        RETURN SELF.Value\r\n    END FUNCTION\r\nEND TYPE\r\n\r\nTYPE Holder\r\n    Item AS Counter\r\nEND TYPE\r\n\r\nDIM holder AS Holder\r\nDIM counters[1 TO 2] AS Counter\r\nholder.Item.Increment(5)\r\nPRINT holder.Item.Current()\r\ncounters[1].Increment(7)\r\nPRINT counters[1].Current()\r\nholder.Item.Value = holder.Item.Current() + 3\r\nPRINT holder.Item.Current()\r\ncounters[1].Increment(2)\r\nPRINT counters[1].Value";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["5", "7", "8", "9"], host.Output);
    }

    [Fact]
    public void Execute_TypeMethodsAcceptTemporaryReceivers()
    {
        const string text = """
TYPE Counter
    Value AS INTEGER

    SUB Increment(stepValue AS INTEGER)
        SELF.Value = SELF.Value + stepValue
    END SUB

    FUNCTION Current() AS INTEGER
        RETURN SELF.Value
    END FUNCTION
END TYPE

FUNCTION MakeCounter(value AS INTEGER) AS Counter
    RETURN Counter(value)
END FUNCTION

PRINT Counter(1).Current()
PRINT MakeCounter(2).Current()
PRINT Counter(3).Current()
Counter(4).Increment(5)
PRINT Counter(4).Current()
PRINT MakeCounter(6).Current()
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["1", "2", "3", "4", "6"], host.Output);
    }

    [Fact]
    public void Execute_TypeMethodsSupportOverloadsBySignature()
    {
        const string text = """
TYPE Counter
    Value AS INTEGER
    Label AS STRING

    SUB Set(value AS INTEGER)
        SELF.Value = value
    END SUB

    SUB Set(label AS STRING)
        SELF.Label = label
    END SUB

    SUB Set(value AS INTEGER, label AS STRING)
        SELF.Value = value
        SELF.Label = label
    END SUB
END TYPE

DIM counter AS Counter
counter.Set(2)
PRINT counter.Value
counter.Set("Alpha")
PRINT counter.Label
counter.Set(7, "Beta")
PRINT counter.Value
PRINT counter.Label
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["2", "Alpha", "7", "Beta"], host.Output);
    }

    [Fact]
    public void Execute_TypeConstructorSupportsOverloadsAndZeroFormWhileNamedLiteralStaysLiteral()
    {
        const string text = """
TYPE Person
    Name AS STRING = "Unknown"
    Age AS INTEGER = 18

    SUB New()
        SELF.Name = "Zero"
    END SUB

    SUB New(name AS STRING)
        SELF.Name = name
        SELF.Age = SELF.Age + 1
    END SUB

    SUB New(name AS STRING, age AS INTEGER)
        SELF.Name = name
        SELF.Age = age
    END SUB
END TYPE

FUNCTION BuildPerson(name AS STRING) AS Person
    RETURN Person(name)
END FUNCTION

DIM zeroUser AS Person = Person()
DIM user AS Person = Person("Ada")
PRINT zeroUser.Name
PRINT zeroUser.Age
PRINT user.Name
PRINT user.Age
PRINT Person("Lin", 20).Age
PRINT Person(Name := "Grace").Name
PRINT Person(Name := "Grace").Age
PRINT BuildPerson("Lin").Age
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["Zero", "18", "Ada", "19", "20", "Grace", "18", "19"], host.Output);
    }

    [Fact]
    public void Execute_TypeConstructorSupportsSameArityWithDifferentTypes()
    {
        const string text = """
TYPE Person
    Name AS STRING = "Unknown"
    Age AS INTEGER = -1

    SUB New(name AS STRING)
        SELF.Name = name
    END SUB

    SUB New(age AS INTEGER)
        SELF.Age = age
    END SUB
END TYPE

PRINT Person("Ada").Name
PRINT Person(20).Age
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["Ada", "20"], host.Output);
    }

    [Fact]
    public void Execute_TypeConstructorRunsInvariantAfterConstruction()
    {
        const string text = """
TYPE Person
    Name AS STRING = "Unknown"
    Age AS INTEGER = 0

    SUB New(name AS STRING, age AS INTEGER)
        SELF.Name = name
        SELF.Age = age
    END SUB

    FUNCTION IsValid() AS BOOLEAN
        RETURN SELF.Name <> "" AND SELF.Age >= 0
    END FUNCTION
END TYPE

PRINT Person("Ada", 20).Name
PRINT Person("Ada", 20).Age
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["Ada", "20"], host.Output);
    }

    [Fact]
    public void Execute_TypeConstructorReportsRuntimeFailureWhenInvariantReturnsFalse()
    {
        const string text = """
TYPE Person
    Age AS INTEGER

    SUB New(age AS INTEGER)
        SELF.Age = age
    END SUB

    FUNCTION IsValid() AS BOOLEAN
        RETURN SELF.Age >= 0
    END FUNCTION
END TYPE

PRINT Person(-1).Age
""";

        var (_, _, result) = ExecuteProgram(text);

        Assert.False(result.Succeeded);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("MB3005", diagnostic.Code);
        Assert.Contains("инвариант", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

}
