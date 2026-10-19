namespace Axiom.Core;

public static class DiagnosticCodes
{
    // Лексический анализ (MB0xxx)
    public const string InvalidCharacter = "MB0001";
    public const string UnterminatedString = "MB0002";
    public const string InvalidNumericLiteral = "MB0003";
    public const string NumericLiteralOutOfRange = "MB0004";

    // Синтаксический анализ (MB1xxx)
    public const string ExpectedToken = "MB1001";
    public const string ExpectedIdentifier = "MB1002";
    public const string ExpectedExpression = "MB1003";
    public const string UnexpectedToken = "MB1004";
    public const string DuplicateElse = "MB1005";
    public const string DuplicateCaseElse = "MB1006";

    // Семантический анализ (MB2xxx)
    public const string UnknownName = "MB2001";
    public const string DuplicateDeclaration = "MB2002";
    public const string TypeMismatch = "MB2003";
    public const string WrongArgumentCount = "MB2004";
    public const string UnknownCallable = "MB2005";
    public const string ReturnValueInSub = "MB2006";
    public const string MissingReturnValue = "MB2007";
    public const string InvalidCondition = "MB2008";
    public const string InvalidForControl = "MB2009";
    public const string AssignToConst = "MB2010";
    public const string InvalidArrayAccess = "MB2011";
    public const string InvalidReturnContext = "MB2012";
    public const string InvalidExitForContext = "MB2013";
    public const string InvalidExitDoContext = "MB2014";
    public const string InvalidSelectCaseRange = "MB2015";
    public const string ShadowedDeclaration = "MB2016";
    public const string MissingVariableType = "MB2017";
    public const string ConstantOutOfRange = "MB2018";
    public const string ProcedureValue = "MB2019";
    public const string ValueNotCallable = "MB2020";
    public const string UnknownType = "MB2021";
    public const string InvalidTypeMember = "MB2022";
    public const string UnknownField = "MB2023";
    public const string InvalidFieldAccess = "MB2024";
    public const string FixedTypeArrayFieldOperation = "MB2025";
    public const string InvalidFixedStringDeclaration = "MB2026";
    public const string InvalidTypeFieldDefaultValue = "MB2027";
    public const string InvalidByRefArgument = "MB2028";
    public const string UnsupportedByRefCallableValue = "MB2029";
    public const string MixedTypeLiteralInitialization = "MB2031";
    public const string TypeLiteralPositionalArity = "MB2032";
    public const string AmbiguousTypeConstructor = "MB2033";
    public const string InvalidOnKeyHandler = "MB2034";
    public const string InvalidOnMouseHandler = "MB2035";
    public const string InvalidOnMouseEvent = "MB2036";
    public const string InvalidContinueForContext = "MB2037";
    public const string InvalidContinueWhileContext = "MB2038";
    public const string InvalidContinueDoContext = "MB2039";
    public const string ExpressionStatementHasNoEffect = "MB2040";
    public const string FieldRequiresSelf = "MB2044";
    public const string NameClassCollision = "MB2045";
    public const string InvalidGraphicsContext = "MB2046";
    public const string UnusedVariable = "MB2047";
    public const string InvalidNextCounter = "MB2048";
    public const string UnknownModule = "MB2049";
    public const string DuplicateModule = "MB2050";
    public const string MissingEntryModule = "MB2051";
    public const string CyclicModuleImport = "MB2052";
    public const string PrivateModuleMember = "MB2053";
    public const string AmbiguousModuleMember = "MB2054";
    public const string ImportedNameConflict = "MB2055";
    public const string InvalidVisibilityContext = "MB2056";

    // Время выполнения (MB3xxx)
    public const string DivisionByZero = "MB3001";
    public const string ArrayBounds = "MB3002";
    public const string InputConversion = "MB3003";
    public const string StepZero = "MB3004";
    public const string InvalidRuntimeState = "MB3005";
    public const string RuntimeMissingReturnValue = "MB3006";
    public const string BuiltinArgument = "MB3007";
    public const string NumericOverflow = "MB3008";
    public const string EmptyFunctionValue = "MB3009";
    public const string MissingGraphicsMode = "MB3010";
    public const string CallStackOverflow = "MB3011";
    public const string NumericDomain = "MB3012";
    public const string ArrayAllocation = "MB3013";
    public const string ExpiredArrayView = "MB3014";

    // Конвейер выполнения (MB4xxx)
    public const string BytecodeCoverage = "MB4001";
    public const string BytecodeCapture = "MB4002";
    public const string BytecodeJsonSnapshot = "MB4003";
}
