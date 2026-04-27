namespace Axiom.Semantics;

public static partial class BuiltinFunctions
{
    private static IReadOnlyList<BuiltinFunctionDescriptor> GetArrayDescriptors()
    {
        return
        [
            CreateArrayInfo(BuiltinFunctionId.LBound, "LBOUND", TypeSymbol.Integer),
            CreateArrayInfo(BuiltinFunctionId.UBound, "UBOUND", TypeSymbol.Integer),
            CreateArrayInfo(BuiltinFunctionId.ArrayLength, "ARRAYLENGTH", TypeSymbol.Integer),
            CreateArrayInfo(BuiltinFunctionId.ArrayIsAllocated, "ARRAYISALLOCATED", TypeSymbol.Boolean),
            CreateArraySameType(BuiltinFunctionId.ArrayClone, "ARRAYCLONE"),
            CreateArraySameType(
                BuiltinFunctionId.ArraySlice,
                "ARRAYSLICE",
                [
                    Parameter("start", TypeSymbol.Integer),
                    Parameter("count", TypeSymbol.Integer)
                ]),
            CreateArraySameType(BuiltinFunctionId.ArrayToSorted, "ARRAYTOSORTED"),
            CreateArraySameType(BuiltinFunctionId.ArrayToReversed, "ARRAYTOREVERSED"),
            CreateArraySameType(
                BuiltinFunctionId.ArrayTake,
                "ARRAYTAKE",
                [
                    Parameter("count", TypeSymbol.Integer)
                ]),
            CreateArraySameType(
                BuiltinFunctionId.ArraySkip,
                "ARRAYSKIP",
                [
                    Parameter("count", TypeSymbol.Integer)
                ]),
            CreateArraySameType(BuiltinFunctionId.ArrayDistinct, "ARRAYDISTINCT"),
            CreateArrayBoolean(
                BuiltinFunctionId.ArrayContains,
                "ARRAYCONTAINS",
                [
                    Parameter("value", TypeSymbol.Integer)
                ]),
            CreateArrayInteger(
                BuiltinFunctionId.ArrayIndexOf,
                "ARRAYINDEXOF",
                [
                    Parameter("value", TypeSymbol.Integer)
                ]),
            CreateArrayInteger(
                BuiltinFunctionId.ArrayLastIndexOf,
                "ARRAYLASTINDEXOF",
                [
                    Parameter("value", TypeSymbol.Integer)
                ]),
            CreateArrayInteger(
                BuiltinFunctionId.ArrayCount,
                "ARRAYCOUNT",
                [
                    Parameter("value", TypeSymbol.Integer)
                ]),
            CreateArrayIntegerArray(
                BuiltinFunctionId.ArrayFindAll,
                "ARRAYFINDALL",
                [
                    Parameter("value", TypeSymbol.Integer)
                ]),
            CreateArrayInteger(
                BuiltinFunctionId.ArrayBinarySearch,
                "ARRAYBINARYSEARCH",
                [
                    Parameter("value", TypeSymbol.Integer)
                ]),
            CreateArrayVoid(
                BuiltinFunctionId.ArrayFill,
                "ARRAYFILL",
                [
                    Parameter("value", TypeSymbol.Integer)
                ]),
            CreateArrayVoid(BuiltinFunctionId.ArrayReverse, "ARRAYREVERSE"),
            CreateArrayVoid(BuiltinFunctionId.ArraySort, "ARRAYSORT"),
            CreateArrayVoid(
                BuiltinFunctionId.ArrayPush,
                "ARRAYPUSH",
                [
                    Parameter("value", TypeSymbol.Integer)
                ]),
            CreateArrayElementType(BuiltinFunctionId.ArrayPop, "ARRAYPOP"),
            CreateArrayVoid(
                BuiltinFunctionId.ArrayInsert,
                "ARRAYINSERT",
                [
                    Parameter("index", TypeSymbol.Integer),
                    Parameter("value", TypeSymbol.Integer)
                ]),
            CreateArrayElementType(
                BuiltinFunctionId.ArrayRemove,
                "ARRAYREMOVE",
                [
                    Parameter("index", TypeSymbol.Integer)
                ]),
            CreateArrayBoolean(
                BuiltinFunctionId.ArrayEvery,
                "ARRAYEVERY",
                [
                    Parameter("predicate", FunctionTypeSymbol.Create([TypeSymbol.Integer], TypeSymbol.Boolean))
                ]),
            CreateArrayBoolean(
                BuiltinFunctionId.ArraySome,
                "ARRAYSOME",
                [
                    Parameter("predicate", FunctionTypeSymbol.Create([TypeSymbol.Integer], TypeSymbol.Boolean))
                ]),
            CreateArraySameType(
                BuiltinFunctionId.ArrayFilter,
                "ARRAYFILTER",
                [
                    Parameter("predicate", FunctionTypeSymbol.Create([TypeSymbol.Integer], TypeSymbol.Boolean))
                ]),
            new BuiltinFunctionDescriptor(
                BuiltinFunctionId.ArrayMap,
                "ARRAYMAP",
                [
                    Parameter("array", ArrayTypeSymbol.Create(TypeSymbol.Integer)),
                    Parameter("selector", FunctionTypeSymbol.Create([TypeSymbol.Integer], TypeSymbol.Integer))
                ],
                ArrayTypeSymbol.Create(TypeSymbol.Integer),
                bindingKind: BuiltinBindingKind.Array,
                resultTypePolicy: BuiltinResultTypePolicy.ArrayMapSelectorReturnArray),
            new BuiltinFunctionDescriptor(
                BuiltinFunctionId.ArrayReduce,
                "ARRAYREDUCE",
                [
                    Parameter("array", ArrayTypeSymbol.Create(TypeSymbol.Integer)),
                    Parameter("seed", TypeSymbol.Integer),
                    Parameter("reducer", FunctionTypeSymbol.Create([TypeSymbol.Integer, TypeSymbol.Integer], TypeSymbol.Integer))
                ],
                TypeSymbol.Integer,
                bindingKind: BuiltinBindingKind.Array,
                resultTypePolicy: BuiltinResultTypePolicy.ArrayReduceAccumulator),
            new BuiltinFunctionDescriptor(
                BuiltinFunctionId.ArrayConcat,
                "ARRAYCONCAT",
                [
                    Parameter("left", ArrayTypeSymbol.Create(TypeSymbol.Integer)),
                    Parameter("right", ArrayTypeSymbol.Create(TypeSymbol.Integer))
                ],
                ArrayTypeSymbol.Create(TypeSymbol.Integer),
                bindingKind: BuiltinBindingKind.Array,
                resultTypePolicy: BuiltinResultTypePolicy.SameAsFirstArgument)
        ];
    }
}
