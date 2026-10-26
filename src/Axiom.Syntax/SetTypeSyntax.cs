namespace Axiom.Syntax;

// HASHSET(<тип элемента>) — самостоятельный тип множества уникальных значений.
// Аргумент один и задаёт тип элемента; тип элемента входит в идентичность типа.
public sealed class SetTypeSyntax : TypeSyntax
{
    public SetTypeSyntax(
        SyntaxToken setKeyword,
        SyntaxToken openParenToken,
        TypeSyntax elementType,
        SyntaxToken closeParenToken)
    {
        ArgumentNullException.ThrowIfNull(setKeyword);
        ArgumentNullException.ThrowIfNull(openParenToken);
        ArgumentNullException.ThrowIfNull(elementType);
        ArgumentNullException.ThrowIfNull(closeParenToken);

        SetKeyword = setKeyword;
        OpenParenToken = openParenToken;
        ElementType = elementType;
        CloseParenToken = closeParenToken;
    }

    public override SyntaxKind Kind => SyntaxKind.SetType;

    public SyntaxToken SetKeyword { get; }

    public SyntaxToken OpenParenToken { get; }

    public TypeSyntax ElementType { get; }

    public SyntaxToken CloseParenToken { get; }
}
