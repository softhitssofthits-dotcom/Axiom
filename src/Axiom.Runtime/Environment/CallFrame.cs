using Axiom.Core;
using Axiom.Semantics;

namespace Axiom.Runtime;

public sealed class CallFrame
{
    public CallFrame(FunctionSymbol function, RuntimeScope scope, TextLocation callSiteLocation)
    {
        ArgumentNullException.ThrowIfNull(function);
        ArgumentNullException.ThrowIfNull(scope);

        Function = function;
        Scope = scope;
        CallSiteLocation = callSiteLocation;
    }

    public FunctionSymbol Function { get; }

    public RuntimeScope Scope { get; }

    public TextLocation CallSiteLocation { get; }
}
