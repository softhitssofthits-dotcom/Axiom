using System.Text.RegularExpressions;
using Axiom.Runtime.Builtins;
using Axiom.Semantics;

namespace Axiom.Runtime;

public sealed partial class Interpreter
{
    private RuntimeValue EvaluateRegexIsMatch(
        BoundCallExpression expression,
        string text,
        string pattern,
        string modes,
        int modesArgumentIndex)
    {
        var regex = CreateRegex(expression, "REGEXISMATCH", pattern, modes, modesArgumentIndex);
        return BuiltinRegexHelpers.IsMatch(regex, text, expression.Location, ReportBuiltInArgumentError, "REGEXISMATCH");
    }

    private RuntimeValue EvaluateRegexMatch(
        BoundCallExpression expression,
        string text,
        string pattern,
        string modes,
        int modesArgumentIndex)
    {
        var regex = CreateRegex(expression, "REGEXMATCH", pattern, modes, modesArgumentIndex);
        return BuiltinRegexHelpers.Match(regex, text, expression.Location, ReportBuiltInArgumentError, "REGEXMATCH");
    }

    private RuntimeValue EvaluateRegexExtract(
        BoundCallExpression expression,
        string text,
        string pattern,
        int group,
        string modes,
        int modesArgumentIndex)
    {
        if (group < 0)
        {
            ReportBuiltInArgumentError(
                "REGEXEXTRACT",
                "group",
                "не может быть отрицательным.",
                GetBuiltInArgumentLocation(expression, 2));
            throw new RuntimeFailureException();
        }

        var regex = CreateRegex(expression, "REGEXEXTRACT", pattern, modes, modesArgumentIndex);
        return BuiltinRegexHelpers.Extract(regex, text, group, expression.Location, ReportBuiltInArgumentError, "REGEXEXTRACT", CreateStringArrayValue);
    }

    private RuntimeValue EvaluateRegexMatches(
        BoundCallExpression expression,
        string text,
        string pattern,
        string modes,
        int modesArgumentIndex)
    {
        var regex = CreateRegex(expression, "REGEXMATCHES", pattern, modes, modesArgumentIndex);
        return BuiltinRegexHelpers.Matches(regex, text, expression.Location, ReportBuiltInArgumentError, "REGEXMATCHES", CreateStringArrayValue);
    }

    private RuntimeValue EvaluateRegexExtractAll(
        BoundCallExpression expression,
        string text,
        string pattern,
        int group,
        string modes,
        int modesArgumentIndex)
    {
        if (group < 0)
        {
            ReportBuiltInArgumentError(
                "REGEXEXTRACTALL",
                "group",
                "не может быть отрицательным.",
                GetBuiltInArgumentLocation(expression, 2));
            throw new RuntimeFailureException();
        }

        var regex = CreateRegex(expression, "REGEXEXTRACTALL", pattern, modes, modesArgumentIndex);
        return BuiltinRegexHelpers.ExtractAll(regex, text, group, expression.Location, ReportBuiltInArgumentError, "REGEXEXTRACTALL", CreateStringArrayValue);
    }

    private RuntimeValue EvaluateRegexSplit(
        BoundCallExpression expression,
        string text,
        string pattern,
        string modes,
        int modesArgumentIndex)
    {
        var regex = CreateRegex(expression, "REGEXSPLIT", pattern, modes, modesArgumentIndex);
        return BuiltinRegexHelpers.Split(regex, text, expression.Location, ReportBuiltInArgumentError, "REGEXSPLIT", CreateStringArrayValue);
    }

    private RuntimeValue EvaluateRegexReplace(
        BoundCallExpression expression,
        string text,
        string pattern,
        string replacement,
        string modes,
        int? count,
        int modesArgumentIndex,
        int countArgumentIndex)
    {
        if (count is < 0)
        {
            ReportBuiltInArgumentError(
                "REGEXREPLACE",
                "count",
                "не может быть отрицательным.",
                GetBuiltInArgumentLocation(expression, countArgumentIndex));
            throw new RuntimeFailureException();
        }

        if (count == 0)
        {
            return RuntimeValue.FromString(text);
        }

        var regex = CreateRegex(expression, "REGEXREPLACE", pattern, modes, modesArgumentIndex);
        var result = BuiltinRegexHelpers.Replace(regex, text, replacement, count, expression.Location, ReportBuiltInArgumentError, "REGEXREPLACE");
        return RuntimeValue.FromString(result);
    }

    private RuntimeValue EvaluateRegexCount(
        BoundCallExpression expression,
        string text,
        string pattern,
        string modes,
        int modesArgumentIndex)
    {
        var regex = CreateRegex(expression, "REGEXCOUNT", pattern, modes, modesArgumentIndex);
        return RuntimeValue.FromInteger(BuiltinRegexHelpers.Count(regex, text, expression.Location, ReportBuiltInArgumentError, "REGEXCOUNT"));
    }

    private Regex CreateRegex(
        BoundCallExpression expression,
        string functionName,
        string pattern,
        string modes,
        int modesArgumentIndex)
    {
        var options = BuiltinRegexHelpers.ParseRegexModes(modes, GetBuiltInArgumentLocation(expression, modesArgumentIndex), functionName, ReportBuiltInArgumentError);
        return BuiltinRegexHelpers.CreateRegex(pattern, options, GetBuiltInArgumentLocation(expression, 1), functionName, ReportBuiltInArgumentError);
    }
}
