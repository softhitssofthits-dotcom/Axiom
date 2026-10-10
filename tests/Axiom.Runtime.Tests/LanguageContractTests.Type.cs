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
        const string text = """
TYPE Address
    City AS STRING
END TYPE

TYPE Person
    Name AS STRING
    Age AS INTEGER
    Home AS Address
END TYPE

DIM people(1 TO 2) AS Person
PRINT people(1).Name
PRINT people(1).Age
people(1).Name = "Ada"
people(1).Age = 36
people(1).Home.City = "London"
people(2).Name = "Grace"
PRINT people(1).Name
PRINT people(1).Age
PRINT people(1).Home.City
PRINT people(2).Name
PRINT ARRAYLENGTH(people)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["", "0", "Ada", "36", "London", "Grace", "2"], host.Output);
    }

    [Fact]
    public void Execute_TypeArrayElementAssignmentCopiesRecordValue()
    {
        const string text = """
TYPE Person
    Name AS STRING
END TYPE

DIM first AS Person
DIM copy AS Person
DIM people(1 TO 1) AS Person
first.Name = "Ada"
people(1) = first
first.Name = "Grace"
copy = people(1)
people(1).Name = "Alan"
PRINT people(1).Name
PRINT copy.Name
PRINT first.Name
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["Alan", "Ada", "Grace"], host.Output);
    }

    [Fact]
    public void Execute_TypeDynamicArrayField()
    {
        const string text = """
TYPE Person
    Scores() AS INTEGER
END TYPE

DIM user AS Person
REDIM user.Scores(1 TO 2)
user.Scores(1) = 10
user.Scores(2) = 20
PRINT user.Scores(1)
PRINT user.Scores(2)
ERASE user.Scores
PRINT ARRAYISALLOCATED(user.Scores)
REDIM user.Scores(5 TO 5)
PRINT user.Scores(5)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["10", "20", "FALSE", "0"], host.Output);
    }

    [Fact]
    public void Execute_TypeDynamicArrayFieldOnArrayElement()
    {
        const string text = """
TYPE Person
    Scores() AS INTEGER
END TYPE

DIM people(1 TO 1) AS Person
REDIM people(1).Scores(2 TO 3)
people(1).Scores(2) = 7
people(1).Scores(3) = 9
PRINT people(1).Scores(2)
PRINT people(1).Scores(3)
REDIM PRESERVE people(1).Scores(3 TO 4)
PRINT people(1).Scores(3)
PRINT people(1).Scores(4)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["7", "9", "9", "0"], host.Output);
    }

    [Fact]
    public void Execute_TypeDynamicMultidimensionalArrayField()
    {
        const string text = """
TYPE Person
    Scores(,) AS INTEGER
END TYPE

DIM user AS Person
REDIM user.Scores(1 TO 2, 3 TO 4)
user.Scores(1, 3) = 10
user.Scores(2, 4) = 20
PRINT user.Scores(1, 3)
PRINT user.Scores(2, 4)
PRINT ARRAYLENGTH(user.Scores)
ERASE user.Scores
PRINT ARRAYISALLOCATED(user.Scores)

DIM people(1 TO 1) AS Person
REDIM people(1).Scores(2 TO 3, 5 TO 6)
people(1).Scores(2, 5) = 7
people(1).Scores(3, 6) = 9
REDIM PRESERVE people(1).Scores(3 TO 4, 6 TO 7)
PRINT people(1).Scores(3, 6)
PRINT people(1).Scores(4, 7)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["10", "20", "4", "FALSE", "9", "0"], host.Output);
    }

    [Fact]
    public void Execute_TypeArrayFieldViews()
    {
        const string text = """
TYPE Board
    Cells(,) AS INTEGER
END TYPE

DIM board AS Board
DIM lineView() AS INTEGER
DIM region(,) AS INTEGER

REDIM board.Cells(1 TO 3, 1 TO 4)
board.Cells(2, 3) = 23

lineView = board.Cells(2)
PRINT lineView(3)
lineView(4) = 24
PRINT board.Cells(2, 4)

region = board.Cells(2 TO 3, 3 TO 4)
PRINT LBOUND(region, 1)
PRINT UBOUND(region, 2)
PRINT ARRAYLENGTH(region)
region(3, 3) = 99
PRINT board.Cells(3, 3)
""";

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
        const string text = """
TYPE Person
    Scores(1 TO 3) AS INTEGER
END TYPE

DIM first AS Person
DIM second AS Person
DIM people(1 TO 1) AS Person
PRINT first.Scores(1)
first.Scores(1) = 10
first.Scores(3) = 30
second = first
first.Scores(1) = 99
people(1) = second
people(1).Scores(2) = 20
PRINT second.Scores(1)
PRINT second.Scores(3)
PRINT people(1).Scores(1)
PRINT people(1).Scores(2)
PRINT ARRAYLENGTH(people(1).Scores)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["0", "10", "30", "10", "20", "3"], host.Output);
    }

    [Fact]
    public void Execute_TypeFixedMultidimensionalArrayField()
    {
        const string text = """
TYPE Person
    Grid(1 TO 2, 4 TO 5) AS INTEGER
END TYPE

DIM first AS Person
DIM second AS Person
DIM people(1 TO 1) AS Person

PRINT first.Grid(1, 4)
first.Grid(1, 4) = 10
first.Grid(2, 5) = 30
second = first
first.Grid(1, 4) = 99
people(1) = second
people(1).Grid(1, 5) = 20

PRINT second.Grid(1, 4)
PRINT second.Grid(2, 5)
PRINT ARRAYLENGTH(people(1).Grid)
PRINT people(1).Grid(1, 4)
PRINT people(1).Grid(1, 5)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["0", "10", "30", "4", "10", "20"], host.Output);
    }

    [Fact]
    public void Execute_TypeFixedArrayFieldWithConstBounds()
    {
        const string text = """
CONST MinScore AS INTEGER = 1
CONST MaxScore AS LONG = MinScore + 2

TYPE Person
    Scores(MinScore TO MaxScore) AS INTEGER
END TYPE

DIM first AS Person
DIM second AS Person
PRINT first.Scores(1)
PRINT ARRAYLENGTH(first.Scores)
first.Scores(3) = 30
second = first
first.Scores(3) = 99
PRINT second.Scores(3)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["0", "3", "30"], host.Output);
    }

    [Fact]
    public void Execute_TypeFixedArrayFieldWithConstBoundsDeclaredLater()
    {
        const string text = """
TYPE Person
    Scores(MinScore TO MaxScore) AS INTEGER
END TYPE

CONST MinScore AS INTEGER = 1
CONST MaxScore AS LONG = MinScore + 2

DIM first AS Person
DIM second AS Person
PRINT first.Scores(1)
PRINT ARRAYLENGTH(first.Scores)
first.Scores(3) = 30
second = first
first.Scores(3) = 99
PRINT second.Scores(3)
""";

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
        const string text = """
TYPE Person
    Name, Alias AS STRING
    Scores(), Flags(1 TO 2) AS INTEGER
END TYPE

DIM first AS Person
DIM second AS Person
PRINT first.Name
PRINT first.Alias
PRINT first.Flags(1)
REDIM first.Scores(1 TO 2)
first.Name = "Ada"
first.Alias = "Analyst"
first.Scores(1) = 10
first.Scores(2) = 20
first.Flags(2) = 7
second = first
first.Alias = "Changed"
first.Scores(1) = 99
first.Flags(2) = 42
PRINT second.Name
PRINT second.Alias
PRINT second.Scores(1)
PRINT second.Scores(2)
PRINT second.Flags(2)
""";

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

TYPE Holder
    Item AS Counter
END TYPE

DIM holder AS Holder
DIM counters(1 TO 2) AS Counter
holder.Item.Increment(5)
PRINT holder.Item.Current()
counters(1).Increment(7)
PRINT counters(1).Current()
holder.Item.Value = holder.Item.Current() + 3
PRINT holder.Item.Current()
counters(1).Increment(2)
PRINT counters(1).Value
""";

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
