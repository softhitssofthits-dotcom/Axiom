using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;
using Axiom.Parsing;
using Axiom.Syntax;

namespace Axiom.Execution;

public sealed class IdentifierNormalizationService
{
    private string? _cachedSourceText;
    private int _cachedSourceHash;
    private ProgramSyntax? _cachedProgram;

    public string NormalizeDocument(string sourceText)
    {
        ArgumentNullException.ThrowIfNull(sourceText);
        return NormalizeRange(sourceText, 0, sourceText.Length);
    }

    public string NormalizeLine(string sourceText, int lineStartOffset, int lineLength)
    {
        ArgumentNullException.ThrowIfNull(sourceText);

        if (lineStartOffset < 0 || lineLength < 0 || lineStartOffset + lineLength > sourceText.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(lineStartOffset));
        }

        return NormalizeRange(sourceText, lineStartOffset, lineLength);
    }

    private string NormalizeRange(string sourceText, int rangeStart, int rangeLength)
    {
        if (sourceText.Length == 0 || rangeLength == 0)
        {
            return rangeLength == sourceText.Length
                ? sourceText
                : sourceText.Substring(rangeStart, rangeLength);
        }

        var program = GetOrParseProgram(sourceText);
        var canonicalNames = BuildCanonicalNames(program);
        if (canonicalNames.Count == 0)
        {
            return rangeLength == sourceText.Length
                ? sourceText
                : sourceText.Substring(rangeStart, rangeLength);
        }

        var replacements = CollectReplacements(program, canonicalNames, rangeStart, rangeLength);
        if (replacements.Count == 0)
        {
            return rangeLength == sourceText.Length
                ? sourceText
                : sourceText.Substring(rangeStart, rangeLength);
        }

        if (rangeStart == 0 && rangeLength == sourceText.Length)
        {
            var normalizedText = sourceText;
            for (var index = replacements.Count - 1; index >= 0; index--)
            {
                var replacement = replacements[index];
                normalizedText = normalizedText.Remove(replacement.Start, replacement.Length)
                    .Insert(replacement.Start, replacement.Text);
            }

            return normalizedText;
        }

        var rangeText = sourceText.Substring(rangeStart, rangeLength);
        for (var index = replacements.Count - 1; index >= 0; index--)
        {
            var replacement = replacements[index];
            var relativeStart = replacement.Start - rangeStart;
            rangeText = rangeText.Remove(relativeStart, replacement.Length)
                .Insert(relativeStart, replacement.Text);
        }

        return rangeText;
    }

    private ProgramSyntax GetOrParseProgram(string sourceText)
    {
        var hash = sourceText.GetHashCode();

        if (_cachedProgram is not null &&
            _cachedSourceHash == hash &&
            string.Equals(_cachedSourceText, sourceText, StringComparison.Ordinal))
        {
            return _cachedProgram;
        }

        var program = Parser.Parse(sourceText, out _);
        _cachedSourceText = sourceText;
        _cachedSourceHash = hash;
        _cachedProgram = program;
        return program;
    }

    private static Dictionary<string, string> BuildCanonicalNames(ProgramSyntax program)
    {
        var canonicalNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var node in EnumerateNodes(program))
        {
            switch (node)
            {
                case VariableDeclaratorSyntax declarator:
                    RegisterCanonicalName(canonicalNames, declarator.Identifier);
                    break;
                case ConstantDeclarationSyntax constantDeclaration:
                    RegisterCanonicalName(canonicalNames, constantDeclaration.Identifier);
                    break;
                case ParameterSyntax parameter:
                    RegisterCanonicalName(canonicalNames, parameter.Identifier);
                    break;
                case SubDeclarationSyntax subDeclaration:
                    RegisterCanonicalName(canonicalNames, subDeclaration.Identifier);
                    break;
                case FunctionDeclarationSyntax functionDeclaration:
                    RegisterCanonicalName(canonicalNames, functionDeclaration.Identifier);
                    break;
                case ForStatementSyntax forStatement:
                    RegisterCanonicalName(canonicalNames, forStatement.Identifier);
                    break;
            }
        }

        return canonicalNames;
    }

    private static List<IdentifierReplacement> CollectReplacements(
        ProgramSyntax program,
        IReadOnlyDictionary<string, string> canonicalNames,
        int rangeStart,
        int rangeLength)
    {
        var replacements = new List<IdentifierReplacement>();
        var rangeEnd = rangeStart + rangeLength;

        foreach (var node in EnumerateNodes(program, skipTypeNames: true))
        {
            if (node is not SyntaxToken token ||
                token.Kind != SyntaxKind.IdentifierToken ||
                string.IsNullOrWhiteSpace(token.Text) ||
                token.Span.Length != token.Text.Length ||
                token.Span.Start < rangeStart ||
                token.Span.End > rangeEnd ||
                !canonicalNames.TryGetValue(token.Text, out var canonicalText) ||
                string.Equals(token.Text, canonicalText, StringComparison.Ordinal))
            {
                continue;
            }

            replacements.Add(new IdentifierReplacement(token.Span.Start, token.Span.Length, canonicalText));
        }

        return replacements;
    }

    private static void RegisterCanonicalName(IDictionary<string, string> canonicalNames, SyntaxToken identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier.Text))
        {
            return;
        }

        canonicalNames.TryAdd(identifier.Text, identifier.Text);
    }

    private static IEnumerable<SyntaxNode> EnumerateNodes(SyntaxNode root, bool skipTypeNames = false)
    {
        var stack = new Stack<SyntaxNode>();
        stack.Push(root);

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            yield return current;

            // Имена типов не принадлежат таблице переменных и параметров.
            // В BYREF walker AS Walker нельзя заменять тип написанием параметра.
            if (skipTypeNames && current is NamedTypeSyntax) continue;

            var children = GetChildren(current);
            for (var index = children.Count - 1; index >= 0; index--)
            {
                if (skipTypeNames && current is TypeDeclarationSyntax type &&
                    ReferenceEquals(children[index], type.Identifier)) continue;
                stack.Push(children[index]);
            }
        }
    }

    private static IReadOnlyList<SyntaxNode> GetChildren(SyntaxNode node)
    {
        var accessor = ChildAccessorCache.GetOrAdd(node.GetType(), CreateChildAccessor);
        return accessor(node);
    }

    private static Func<SyntaxNode, IReadOnlyList<SyntaxNode>> CreateChildAccessor(Type nodeType)
    {
        var properties = nodeType.GetProperties(BindingFlags.Instance | BindingFlags.Public);

        return node =>
        {
            var children = new List<SyntaxNode>();

            foreach (var property in properties)
            {
                var value = property.GetValue(node);
                if (value is null)
                {
                    continue;
                }

                if (value is SyntaxNode childNode)
                {
                    children.Add(childNode);
                    continue;
                }

                if (value is string || value is not IEnumerable sequence)
                {
                    continue;
                }

                foreach (var item in sequence)
                {
                    if (item is SyntaxNode child)
                    {
                        children.Add(child);
                    }
                }
            }

            return children;
        };
    }

    private static readonly ConcurrentDictionary<Type, Func<SyntaxNode, IReadOnlyList<SyntaxNode>>> ChildAccessorCache = new();

    private sealed record IdentifierReplacement(int Start, int Length, string Text);
}
