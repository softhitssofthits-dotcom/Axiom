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
            CreateArraySameType(
                BuiltinFunctionId.ArraySlice,
                "ARRAYSLICE",
                [
                    Parameter("start", TypeSymbol.Integer),
                    Parameter("count", TypeSymbol.Integer),
                    Parameter("dimension", TypeSymbol.Integer)
                ]),
            CreateArraySameType(BuiltinFunctionId.ArrayToSorted, "ARRAYTOSORTED"),
            CreateArraySameType(
                BuiltinFunctionId.ArrayToSorted,
                "ARRAYTOSORTED",
                [
                    Parameter("dimension", TypeSymbol.Integer)
                ]),
            CreateArraySameType(BuiltinFunctionId.ArrayToReversed, "ARRAYTOREVERSED"),
            CreateArraySameType(
                BuiltinFunctionId.ArrayToReversed,
                "ARRAYTOREVERSED",
                [
                    Parameter("dimension", TypeSymbol.Integer)
                ]),
            CreateArraySameType(
                BuiltinFunctionId.ArrayTake,
                "ARRAYTAKE",
                [
                    Parameter("count", TypeSymbol.Integer)
                ]),
            CreateArraySameType(
                BuiltinFunctionId.ArrayTake,
                "ARRAYTAKE",
                [
                    Parameter("count", TypeSymbol.Integer),
                    Parameter("dimension", TypeSymbol.Integer)
                ]),
            CreateArraySameType(
                BuiltinFunctionId.ArraySkip,
                "ARRAYSKIP",
                [
                    Parameter("count", TypeSymbol.Integer)
                ]),
            CreateArraySameType(
                BuiltinFunctionId.ArraySkip,
                "ARRAYSKIP",
                [
                    Parameter("count", TypeSymbol.Integer),
                    Parameter("dimension", TypeSymbol.Integer)
                ]),
            new BuiltinFunctionDescriptor(
                BuiltinFunctionId.ArrayFlatten,
                "ARRAYFLATTEN",
                [
                    Parameter("array", ArrayTypeSymbol.Create(TypeSymbol.Integer))
                ],
                ArrayTypeSymbol.Create(TypeSymbol.Integer),
                bindingKind: BuiltinBindingKind.Array,
                resultTypePolicy: BuiltinResultTypePolicy.ArrayOfFirstArrayElementType),
            new BuiltinFunctionDescriptor(
                BuiltinFunctionId.ArrayReshape,
                "ARRAYRESHAPE",
                [
                    Parameter("source", ArrayTypeSymbol.Create(TypeSymbol.Integer)),
                    Parameter("template", ArrayTypeSymbol.Create(TypeSymbol.Integer))
                ],
                ArrayTypeSymbol.Create(TypeSymbol.Integer),
                bindingKind: BuiltinBindingKind.Array,
                resultTypePolicy: BuiltinResultTypePolicy.SameAsSecondArgument),
            new BuiltinFunctionDescriptor(
                BuiltinFunctionId.ArrayDistinct,
                "ARRAYDISTINCT",
                [
                    Parameter("array", ArrayTypeSymbol.Create(TypeSymbol.Integer))
                ],
                ArrayTypeSymbol.Create(TypeSymbol.Integer),
                bindingKind: BuiltinBindingKind.Array,
                resultTypePolicy: BuiltinResultTypePolicy.ArrayOfFirstArrayElementType),
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
                ],
                resultTypePolicy: BuiltinResultTypePolicy.ArrayIndexCoordinateResult),
            CreateArrayInteger(
                BuiltinFunctionId.ArrayLastIndexOf,
                "ARRAYLASTINDEXOF",
                [
                    Parameter("value", TypeSymbol.Integer)
                ],
                resultTypePolicy: BuiltinResultTypePolicy.ArrayIndexCoordinateResult),
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
                ],
                resultTypePolicy: BuiltinResultTypePolicy.ArrayFindAllCoordinateResult),
            CreateArrayInteger(
                BuiltinFunctionId.ArrayBinarySearch,
                "ARRAYBINARYSEARCH",
                [
                    Parameter("value", TypeSymbol.Integer)
                ],
                resultTypePolicy: BuiltinResultTypePolicy.ArrayIndexCoordinateResult),
            CreateArrayInteger(
                BuiltinFunctionId.ArrayBinarySearch,
                "ARRAYBINARYSEARCH",
                [
                    Parameter("value", TypeSymbol.Integer),
                    Parameter("dimension", TypeSymbol.Integer)
                ],
                resultTypePolicy: BuiltinResultTypePolicy.ArrayIndexCoordinateResult),
            CreateArrayVoid(
                BuiltinFunctionId.ArrayFill,
                "ARRAYFILL",
                [
                    Parameter("value", TypeSymbol.Integer)
                ]),
            CreateArrayVoid(BuiltinFunctionId.ArrayReverse, "ARRAYREVERSE"),
            CreateArrayVoid(
                BuiltinFunctionId.ArrayReverse,
                "ARRAYREVERSE",
                [
                    Parameter("dimension", TypeSymbol.Integer)
                ]),
            CreateArrayVoid(BuiltinFunctionId.ArraySort, "ARRAYSORT"),
            CreateArrayVoid(
                BuiltinFunctionId.ArraySort,
                "ARRAYSORT",
                [
                    Parameter("dimension", TypeSymbol.Integer)
                ]),
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
            new BuiltinFunctionDescriptor(
                BuiltinFunctionId.ArrayFilter,
                "ARRAYFILTER",
                [
                    Parameter("array", ArrayTypeSymbol.Create(TypeSymbol.Integer)),
                    Parameter("predicate", FunctionTypeSymbol.Create([TypeSymbol.Integer], TypeSymbol.Boolean))
                ],
                ArrayTypeSymbol.Create(TypeSymbol.Integer),
                bindingKind: BuiltinBindingKind.Array,
                resultTypePolicy: BuiltinResultTypePolicy.ArrayOfFirstArrayElementType),
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
                resultTypePolicy: BuiltinResultTypePolicy.SameAsFirstArgument),
            new BuiltinFunctionDescriptor(
                BuiltinFunctionId.ArrayConcat,
                "ARRAYCONCAT",
                [
                    Parameter("left", ArrayTypeSymbol.Create(TypeSymbol.Integer)),
                    Parameter("right", ArrayTypeSymbol.Create(TypeSymbol.Integer)),
                    Parameter("dimension", TypeSymbol.Integer)
                ],
                ArrayTypeSymbol.Create(TypeSymbol.Integer),
                bindingKind: BuiltinBindingKind.Array,
                resultTypePolicy: BuiltinResultTypePolicy.SameAsFirstArgument)
        ];
    }
}
