using Axiom.Core;
using Axiom.Semantics;

namespace Axiom.Bytecode;

public sealed partial class BytecodeModuleValidator
{
    private const string SupportedCompatibilityToken = "axiom-bytecode-v1";
    private const BytecodeModuleFlags KnownModuleFlags = BytecodeModuleFlags.HasDebugInfo | BytecodeModuleFlags.UsesBuiltinTable;
    private static readonly TextLocation UnknownLocation = new(new TextSpan(0, 0), 1, 1);
    private readonly List<BytecodeDiagnostic> _diagnostics = [];

    public BytecodeValidationResult Validate(BytecodeModule module)
    {
        ArgumentNullException.ThrowIfNull(module);

        _diagnostics.Clear();

        ValidateHeader(module);
        ValidateModuleFlags(module);
        ValidateConstantPool(module);

        var routinesById = ValidateRoutineTable(module);
        var globalsBySlot = ValidateGlobalTable(module);
        var builtinsById = ValidateBuiltinTable(module);

        ValidateEntryPoint(module, routinesById);

        ValidateDebugInfo(module, routinesById);

        foreach (var routine in module.Routines)
        {
            ValidateRoutine(module, routine, routinesById, globalsBySlot, builtinsById);
        }

        return new BytecodeValidationResult(_diagnostics.ToArray());
    }

    private void ValidateHeader(BytecodeModule module)
    {
        if (string.IsNullOrWhiteSpace(module.Header.LanguageVersion))
        {
            Report(
                "BC4100",
                "Заголовок модуля содержит пустую версию языка.",
                module);
        }

        if (module.Header.FormatVersion != BytecodeFormatVersion.V1)
        {
            Report(
                "BC4068",
                $"Заголовок модуля содержит неподдерживаемую версию формата '{module.Header.FormatVersion}'.",
                module);
        }

        if (!string.Equals(module.Header.CompatibilityToken, SupportedCompatibilityToken, StringComparison.Ordinal))
        {
            Report(
                "BC4069",
                $"Заголовок модуля содержит несовместимый токен '{module.Header.CompatibilityToken}'.",
                module);
        }
    }

    private void ValidateModuleFlags(BytecodeModule module)
    {
        var unknownFlags = module.Header.Flags & ~KnownModuleFlags;
        if (unknownFlags != BytecodeModuleFlags.None)
        {
            Report(
                "BC4070",
                $"Заголовок модуля содержит неизвестные флаги формата {(int)unknownFlags}.",
                module);
        }

        var hasDebugInfoFlag = module.Header.Flags.HasFlag(BytecodeModuleFlags.HasDebugInfo);
        var hasDebugInfo = module.DebugInfo is not null;
        if (hasDebugInfoFlag != hasDebugInfo)
        {
            Report(
                "BC4071",
                $"Флаг HasDebugInfo равен {hasDebugInfoFlag}, но наличие отладочных данных равно {hasDebugInfo}.",
                module);
        }

        var usesBuiltinTableFlag = module.Header.Flags.HasFlag(BytecodeModuleFlags.UsesBuiltinTable);
        var hasBuiltinTable = module.Builtins.Count > 0;
        if (usesBuiltinTableFlag != hasBuiltinTable)
        {
            Report(
                "BC4072",
                $"Флаг UsesBuiltinTable равен {usesBuiltinTableFlag}, но наличие таблицы built-in равно {hasBuiltinTable}.",
                module);
        }
    }

    private void ValidateEntryPoint(BytecodeModule module, IReadOnlyDictionary<int, BytecodeRoutine> routinesById)
    {
        if (module.EntryPointRoutineId < 0)
        {
            Report(
                "BC4093",
                $"Точка входа содержит отрицательный routine-id {module.EntryPointRoutineId}.",
                module);
            return;
        }

        if (!routinesById.TryGetValue(module.EntryPointRoutineId, out var entryPoint))
        {
            Report(
                "BC4001",
                $"Точка входа с routine-id {module.EntryPointRoutineId} отсутствует в таблице процедур.",
                module,
                routineId: module.EntryPointRoutineId);
            return;
        }

        var markedEntryPoints = module.Routines.Where(static routine => routine.IsEntryPoint).ToArray();
        if (markedEntryPoints.Length == 0)
        {
            Report(
                "BC4073",
                "Таблица процедур не содержит процедуры, помеченной как точка входа.",
                module,
                entryPoint.Id);
        }
        else
        {
            if (markedEntryPoints.Length > 1)
            {
                Report(
                    "BC4074",
                    $"Таблица процедур содержит несколько точек входа: {markedEntryPoints.Length}.",
                    module,
                    entryPoint.Id);
            }

            if (!markedEntryPoints.Contains(entryPoint))
            {
                Report(
                    "BC4075",
                    $"Выбранная точка входа '{entryPoint.Name}' не помечена как точка входа.",
                    module,
                    entryPoint.Id);
            }
        }

        if (entryPoint.ParameterCount != 0)
        {
            Report(
                "BC4076",
                $"Точка входа '{entryPoint.Name}' объявляет {entryPoint.ParameterCount} параметр(а/ов), но VM запускает модуль без аргументов.",
                module,
                entryPoint.Id);
        }

        if (entryPoint.ResultType != TypeSymbol.Void)
        {
            Report(
                "BC4077",
                $"Точка входа '{entryPoint.Name}' имеет тип результата '{entryPoint.ResultType}', но корневая процедура модуля должна быть процедурой без результата.",
                module,
                entryPoint.Id);
        }
    }

    private void ValidateConstantPool(BytecodeModule module)
    {
        for (var index = 0; index < module.ConstantPool.Count; index++)
        {
            var constant = module.ConstantPool[index];
            if (constant is null)
            {
                Report(
                    "BC4101",
                    $"Пул констант содержит пустую запись с индексом {index}.",
                    module);
                continue;
            }

            if (!Enum.IsDefined(typeof(BytecodeConstantKind), constant.Kind))
            {
                Report(
                    "BC4102",
                    $"Пул констант содержит неизвестный вид константы '{constant.Kind}' с индексом {index}.",
                    module);
                continue;
            }

            if (!IsConstantValueShapeValid(constant))
            {
                var actualType = constant.Value?.GetType().Name ?? "<null>";
                Report(
                    "BC4103",
                    $"Пул констант содержит значение типа '{actualType}', несовместимое с видом константы '{constant.Kind}', с индексом {index}.",
                    module);
            }
        }
    }

    private static bool IsConstantValueShapeValid(BytecodeConstant constant)
    {
        return constant.Kind switch
        {
            BytecodeConstantKind.Integer => constant.Value is int,
            BytecodeConstantKind.Long => constant.Value is long,
            BytecodeConstantKind.Double => constant.Value is double,
            BytecodeConstantKind.Decimal => constant.Value is decimal,
            BytecodeConstantKind.String => constant.Value is string,
            BytecodeConstantKind.Boolean => constant.Value is bool,
            BytecodeConstantKind.RoutineName => constant.Value is string,
            BytecodeConstantKind.BuiltinName => constant.Value is string,
            BytecodeConstantKind.Null => constant.Value is null,
            _ => false
        };
    }

    private Dictionary<int, BytecodeRoutine> ValidateRoutineTable(BytecodeModule module)
    {
        var routinesById = new Dictionary<int, BytecodeRoutine>();

        foreach (var routine in module.Routines)
        {
            if (!routinesById.TryAdd(routine.Id, routine))
            {
                Report(
                    "BC4002",
                    $"Повторяющийся routine-id {routine.Id} в таблице процедур.",
                    module,
                    routine.Id);
            }
        }

        return routinesById;
    }

    private Dictionary<int, BytecodeGlobal> ValidateGlobalTable(BytecodeModule module)
    {
        var globalsBySlot = new Dictionary<int, BytecodeGlobal>();

        foreach (var global in module.Globals)
        {
            ValidateGlobalMetadata(module, global);

            if (!globalsBySlot.TryAdd(global.Slot, global))
            {
                Report(
                    "BC4003",
                    $"Повторяющийся слот глобальной переменной {global.Slot}.",
                    module);
            }
        }

        return globalsBySlot;
    }

    private void ValidateGlobalMetadata(BytecodeModule module, BytecodeGlobal global)
    {
        if (global.Slot < 0)
        {
            Report(
                "BC4097",
                $"Глобальная переменная '{global.Name}' содержит отрицательный слот {global.Slot}.",
                module);
            return;
        }

        if (!Enum.IsDefined(typeof(GlobalInitializationStrategy), global.InitializationStrategy))
        {
            Report(
                "BC4078",
                $"Глобальная переменная '{global.Name}' содержит неизвестную стратегию инициализации {global.InitializationStrategy}.",
                module);
        }

        if (global.Type == TypeSymbol.Void)
        {
            Report(
                "BC4079",
                $"Глобальная переменная '{global.Name}' имеет недопустимый тип VOID.",
                module);
        }

        if (global.FixedStringLength is { } fixedStringLength
            && (global.Type != TypeSymbol.String || fixedStringLength <= 0))
        {
            Report(
                "BC4080",
                $"Глобальная переменная '{global.Name}' содержит некорректную fixed-string длину {fixedStringLength}.",
                module);
        }
    }

    private Dictionary<int, BuiltinBinding> ValidateBuiltinTable(BytecodeModule module)
    {
        var builtinsById = new Dictionary<int, BuiltinBinding>();

        foreach (var builtin in module.Builtins)
        {
            ValidateBuiltinMetadata(module, builtin);

            if (!builtinsById.TryAdd(builtin.Id, builtin))
            {
                Report(
                    "BC4004",
                    $"Повторяющийся built-in id {builtin.Id} в таблице built-in.",
                    module);
            }
        }

        return builtinsById;
    }

    private void ValidateBuiltinMetadata(BytecodeModule module, BuiltinBinding builtin)
    {
        var hasInvalidShape = false;

        if (builtin.Id < 0)
        {
            Report(
                "BC4098",
                $"Таблица built-in содержит запись '{builtin.Name}' с отрицательным id {builtin.Id}.",
                module);
            hasInvalidShape = true;
        }

        if (builtin.ParameterCount < 0)
        {
            Report(
                "BC4099",
                $"Запись built-in '{builtin.Name}' содержит отрицательное число параметров {builtin.ParameterCount}.",
                module);
            hasInvalidShape = true;
        }

        if (hasInvalidShape)
        {
            return;
        }

        if (!Enum.IsDefined(typeof(BuiltinFunctionId), builtin.CanonicalId))
        {
            Report(
                "BC4062",
                $"Таблица built-in содержит неизвестный канонический идентификатор {builtin.CanonicalId}.",
                module);
        }

        if (builtin.Function is null)
        {
            return;
        }

        var function = builtin.Function;
        if (!function.IsBuiltIn)
        {
            Report(
                "BC4063",
                $"Запись built-in '{builtin.Name}' ссылается на пользовательский символ функции '{function.Name}'.",
                module);
            return;
        }

        if (function.BuiltinId != builtin.CanonicalId)
        {
            Report(
                "BC4064",
                $"Запись built-in '{builtin.Name}' имеет идентификатор {builtin.CanonicalId}, но символ функции '{function.Name}' указывает на {function.BuiltinId?.ToString() ?? "<none>"}.",
                module);
        }

        if (!string.Equals(function.Name, builtin.Name, StringComparison.OrdinalIgnoreCase))
        {
            Report(
                "BC4065",
                $"Запись built-in '{builtin.Name}' ссылается на символ функции с другим именем '{function.Name}'.",
                module);
        }

        if (function.Parameters.Count != builtin.ParameterCount)
        {
            Report(
                "BC4066",
                $"Запись built-in '{builtin.Name}' объявляет {builtin.ParameterCount} параметр(а/ов), но символ функции содержит {function.Parameters.Count}.",
                module);
        }

        var functionReturnsValue = function.ReturnType != TypeSymbol.Void;
        if (functionReturnsValue != builtin.ReturnsValue)
        {
            Report(
                "BC4067",
                $"Запись built-in '{builtin.Name}' имеет признак возвращаемого значения {builtin.ReturnsValue}, но символ функции требует {functionReturnsValue}.",
                module);
        }
    }

    private void ValidateDebugInfo(BytecodeModule module, IReadOnlyDictionary<int, BytecodeRoutine> routinesById)
    {
        if (module.DebugInfo is null)
        {
            return;
        }

        foreach (var map in module.DebugInfo.InstructionMap)
        {
            if (map.RoutineId < 0)
            {
                Report(
                    "BC4088",
                    $"Отладочная карта содержит отрицательный идентификатор процедуры {map.RoutineId}.",
                    module,
                    map.RoutineId);
                continue;
            }

            if (!routinesById.TryGetValue(map.RoutineId, out var routine))
            {
                Report(
                    "BC4005",
                    $"Отладочная карта ссылается на неизвестную процедуру {map.RoutineId}.",
                    module,
                    map.RoutineId);
                continue;
            }

            if (map.InstructionIndex < 0)
            {
                Report(
                    "BC4089",
                    $"Отладочная карта процедуры '{routine.Name}' содержит отрицательный индекс инструкции {map.InstructionIndex}.",
                    module,
                    routine.Id);
                continue;
            }

            if (map.InstructionIndex >= routine.Instructions.Count)
            {
                Report(
                    "BC4006",
                    $"Отладочная карта ссылается на несуществующую инструкцию {map.InstructionIndex} процедуры '{routine.Name}'.",
                    module,
                    routine.Id);
            }
        }

        foreach (var routineDebug in module.DebugInfo.Routines)
        {
            if (routineDebug.RoutineId < 0)
            {
                Report(
                    "BC4090",
                    $"Отладочная информация имён содержит отрицательный идентификатор процедуры {routineDebug.RoutineId}.",
                    module,
                    routineDebug.RoutineId);
                continue;
            }

            if (!routinesById.TryGetValue(routineDebug.RoutineId, out var routine))
            {
                Report(
                    "BC4007",
                    $"Отладочная информация имён ссылается на неизвестную процедуру {routineDebug.RoutineId}.",
                    module,
                    routineDebug.RoutineId);
                continue;
            }

            foreach (var parameterSlot in routineDebug.ParameterNames.Keys)
            {
                if (parameterSlot < 0 || parameterSlot >= routine.ParameterCount)
                {
                    Report(
                        "BC4008",
                        $"Отладочная информация параметров процедуры '{routine.Name}' содержит невалидный слот {parameterSlot}.",
                        module,
                        routine.Id);
                }
            }

            foreach (var localSlot in routineDebug.LocalNames.Keys)
            {
                if (localSlot < 0 || localSlot >= routine.LocalSlotCount)
                {
                    Report(
                        "BC4009",
                        $"Отладочная информация локальных переменных процедуры '{routine.Name}' содержит невалидный слот {localSlot}.",
                        module,
                        routine.Id);
                }
            }

            foreach (var blockRange in routineDebug.BlockRanges)
            {
                if (blockRange.StartInstructionIndex < 0)
                {
                    Report(
                        "BC4091",
                        $"Отладочный диапазон блока '{blockRange.Name}' процедуры '{routine.Name}' содержит отрицательное начало {blockRange.StartInstructionIndex}.",
                        module,
                        routine.Id);
                    continue;
                }

                if (blockRange.EndInstructionIndexExclusive < blockRange.StartInstructionIndex)
                {
                    Report(
                        "BC4092",
                        $"Отладочный диапазон блока '{blockRange.Name}' процедуры '{routine.Name}' завершается раньше начала: {blockRange.StartInstructionIndex}..{blockRange.EndInstructionIndexExclusive}.",
                        module,
                        routine.Id);
                    continue;
                }

                if (blockRange.EndInstructionIndexExclusive > routine.Instructions.Count)
                {
                    Report(
                        "BC4010",
                        $"Отладочный диапазон блока '{blockRange.Name}' процедуры '{routine.Name}' выходит за пределы инструкции.",
                        module,
                        routine.Id);
                }
            }
        }
    }

    private void ValidateRoutine(
        BytecodeModule module,
        BytecodeRoutine routine,
        IReadOnlyDictionary<int, BytecodeRoutine> routinesById,
        IReadOnlyDictionary<int, BytecodeGlobal> globalsBySlot,
        IReadOnlyDictionary<int, BuiltinBinding> builtinsById)
    {
        var diagnosticCountBeforeRoutine = _diagnostics.Count;

        ValidateRoutineMetadata(module, routine);

        if (_diagnostics.Count > diagnosticCountBeforeRoutine)
        {
            return;
        }

        for (var instructionIndex = 0; instructionIndex < routine.Instructions.Count; instructionIndex++)
        {
            var instruction = routine.Instructions[instructionIndex];

            switch (instruction.Opcode)
            {
                case Opcode.PushConst:
                    ValidateOperandCount(module, routine, instructionIndex, instruction, 1);
                    ValidatePushConstant(module, routine, instructionIndex, instruction);
                    break;

                case Opcode.PushIntegerZero:
                case Opcode.PushIntegerOne:
                    ValidateOperandCount(module, routine, instructionIndex, instruction, 0);
                    break;

                case Opcode.LoadFunction:
                    ValidateOperandCount(module, routine, instructionIndex, instruction, 1);
                    ValidateRoutineReference(module, routine, instructionIndex, instruction, routinesById);
                    break;

                case Opcode.LoadBuiltinFunction:
                    ValidateOperandCount(module, routine, instructionIndex, instruction, 1);
                    ValidateBuiltinReference(module, routine, instructionIndex, instruction, builtinsById);
                    break;

                case Opcode.LoadGlobal:
                case Opcode.StoreGlobal:
                    ValidateOperandCount(module, routine, instructionIndex, instruction, 1);
                    ValidateGlobalSlot(module, routine, instructionIndex, instruction, globalsBySlot);
                    break;

                case Opcode.AppendStringGlobal:
                    ValidateOperandCount(module, routine, instructionIndex, instruction, 2);
                    ValidateGlobalSlot(module, routine, instructionIndex, instruction, globalsBySlot);
                    break;

                case Opcode.LoadLocal:
                case Opcode.StoreLocal:
                    ValidateOperandCount(module, routine, instructionIndex, instruction, 1);
                    ValidateLocalSlot(module, routine, instructionIndex, instruction);
                    break;

                case Opcode.AppendStringLocal:
                    ValidateOperandCount(module, routine, instructionIndex, instruction, 2);
                    ValidateLocalSlot(module, routine, instructionIndex, instruction);
                    break;

                case Opcode.LoadArgument:
                case Opcode.StoreArgument:
                    ValidateOperandCount(module, routine, instructionIndex, instruction, 1);
                    ValidateArgumentSlot(module, routine, instructionIndex, instruction);
                    break;

                case Opcode.AppendStringArgument:
                    ValidateOperandCount(module, routine, instructionIndex, instruction, 2);
                    ValidateArgumentSlot(module, routine, instructionIndex, instruction);
                    break;

                case Opcode.LoadCapture:
                case Opcode.StoreCapture:
                    ValidateOperandCount(module, routine, instructionIndex, instruction, 1);
                    ValidateCaptureSlot(module, routine, instructionIndex, instruction);
                    break;

                case Opcode.AppendStringCapture:
                    ValidateOperandCount(module, routine, instructionIndex, instruction, 2);
                    ValidateCaptureSlot(module, routine, instructionIndex, instruction);
                    break;

                case Opcode.Jump:
                case Opcode.JumpIfEqual:
                case Opcode.JumpIfNotEqual:
                case Opcode.JumpIfLess:
                case Opcode.JumpIfLessOrEqual:
                case Opcode.JumpIfGreater:
                case Opcode.JumpIfGreaterOrEqual:
                case Opcode.JumpIfTrue:
                case Opcode.JumpIfFalse:
                    ValidateOperandCount(module, routine, instructionIndex, instruction, 1);
                    ValidateJumpTarget(module, routine, instructionIndex, instruction);
                    break;

                case Opcode.JumpIfForDoneLocal:
                    ValidateOperandCount(module, routine, instructionIndex, instruction, 4);
                    ValidateLocalSlot(module, routine, instructionIndex, instruction, operandIndex: 0);
                    ValidateLocalSlot(module, routine, instructionIndex, instruction, operandIndex: 1);
                    ValidateLocalSlot(module, routine, instructionIndex, instruction, operandIndex: 2);
                    ValidateJumpTarget(module, routine, instructionIndex, instruction, operandIndex: 3);
                    break;

                case Opcode.Call:
                    ValidateOperandCount(module, routine, instructionIndex, instruction, 2);
                    ValidateRoutineCall(module, routine, instructionIndex, instruction, routinesById);
                    break;

                case Opcode.CallByRef:
                    ValidateRoutineCall(module, routine, instructionIndex, instruction, routinesById);
                    break;

                case Opcode.CallValue:
                    ValidateOperandCount(module, routine, instructionIndex, instruction, 2);
                    ValidateFunctionValueCall(module, routine, instructionIndex, instruction);
                    break;

                case Opcode.CallBuiltin:
                    ValidateOperandCount(module, routine, instructionIndex, instruction, 2);
                    ValidateBuiltinCall(module, routine, instructionIndex, instruction, builtinsById);
                    break;

                case Opcode.Convert:
                    ValidateOperandCount(module, routine, instructionIndex, instruction, 1);
                    ValidateConversionInstruction(module, routine, instructionIndex, instruction);
                    break;

                case Opcode.AddLongInteger:
                    ValidateOperandCount(module, routine, instructionIndex, instruction, 0);
                    break;

                case Opcode.CreateRecord:
                case Opcode.ValidateTypeInvariant:
                    if (instruction.OperandCount != 1 && instruction.OperandCount != 2)
                    {
                        Report(
                            "BC4012",
                            $"Инструкция '{instruction.Opcode}' в процедуре '{routine.Name}' ожидает 1 или 2 операнда, но получила {instruction.OperandCount}.",
                            module,
                            routine.Id,
                            instructionIndex);
                    }

                    ValidateStringConstantIndex(module, routine, instructionIndex, instruction, operandIndex: 0, "имя TYPE");
                    break;

                case Opcode.RedimArray:
                    ValidateOperandCount(module, routine, instructionIndex, instruction, 3);
                    ValidateRedimArrayInstruction(module, routine, instructionIndex, instruction);
                    break;

                case Opcode.NewArray:
                    ValidateOperandCount(module, routine, instructionIndex, instruction, 2);
                    ValidateStringConstantIndex(module, routine, instructionIndex, instruction, operandIndex: 0, "имя массива");
                    ValidatePositiveCountOperand(module, routine, instructionIndex, instruction, operandIndex: 1, code: "BC4109", label: "число измерений");
                    break;

                case Opcode.LoadElement:
                    ValidateOperandCount(module, routine, instructionIndex, instruction, 3);
                    ValidateElementInstruction(module, routine, instructionIndex, instruction);
                    ValidateStringConstantIndex(module, routine, instructionIndex, instruction, operandIndex: 0, "имя массива");
                    break;

                case Opcode.StoreElement:
                    ValidateOperandCount(module, routine, instructionIndex, instruction, 2);
                    ValidateElementInstruction(module, routine, instructionIndex, instruction);
                    ValidateStringConstantIndex(module, routine, instructionIndex, instruction, operandIndex: 0, "имя массива");
                    break;

                case Opcode.LoadField:
                case Opcode.StoreField:
                    ValidateOperandCount(module, routine, instructionIndex, instruction, 1);
                    ValidateStringConstantIndex(module, routine, instructionIndex, instruction, operandIndex: 0, "имя поля");
                    break;

                case Opcode.PrintSeparator:
                    ValidateOperandCount(module, routine, instructionIndex, instruction, 1);
                    ValidatePrintSeparatorInstruction(module, routine, instructionIndex, instruction);
                    break;

                case Opcode.PrintUsing:
                    ValidateOperandCount(module, routine, instructionIndex, instruction, 2);
                    ValidatePrintUsingInstruction(module, routine, instructionIndex, instruction);
                    break;

                case Opcode.Input:
                    ValidateInputInstruction(module, routine, instructionIndex, instruction, globalsBySlot);
                    break;

                case Opcode.Sleep:
                    ValidateOperandCount(module, routine, instructionIndex, instruction, 1);
                    ValidateSleepInstruction(module, routine, instructionIndex, instruction);
                    break;

                case Opcode.ReseedRandom:
                    ValidateOperandCount(module, routine, instructionIndex, instruction, 1);
                    if (instruction.OperandCount == 1 && instruction.GetOperand(0) is not 0 and not 1)
                    {
                        Report("BC4133", $"Инструкция ReseedRandom в процедуре '{routine.Name}' содержит неизвестный флаг наличия зерна {instruction.GetOperand(0)}.", module, routine.Id, instructionIndex);
                    }
                    break;

                case Opcode.Color:
                    ValidateOperandCount(module, routine, instructionIndex, instruction, 2);
                    break;

                case Opcode.View:
                case Opcode.Window:
                    ValidateOperandCount(module, routine, instructionIndex, instruction, 2);
                    break;

                case Opcode.ViewPrint:
                    ValidateOperandCount(module, routine, instructionIndex, instruction, 1);
                    break;

                case Opcode.Palette:
                    ValidateOperandCount(module, routine, instructionIndex, instruction, 1);
                    ValidatePaletteInstruction(module, routine, instructionIndex, instruction);
                    break;

                case Opcode.PCopy:
                case Opcode.Canvas:
                    ValidateOperandCount(module, routine, instructionIndex, instruction, 0);
                    break;

                case Opcode.Smooth:
                    var smoothOperandCountValid = instruction.OperandCount == 1;
                    ValidateOperandCount(module, routine, instructionIndex, instruction, 1);
                    if (smoothOperandCountValid && instruction.GetOperand(0) is not 0 and not 1)
                    {
                        Report("BC4125", $"Инструкция Smooth в процедуре '{routine.Name}' ожидает значение 0 или 1.", module, routine.Id, instructionIndex);
                    }
                    break;

                case Opcode.BeginFrame:
                case Opcode.Present:
                case Opcode.Camera:
                case Opcode.ResetCamera:
                case Opcode.DrawLine:
                case Opcode.DrawCircle:
                case Opcode.DrawRect:
                case Opcode.FillCircle:
                case Opcode.FillRect:
                case Opcode.DrawText:
                    ValidateOperandCount(module, routine, instructionIndex, instruction, 0);
                    break;

                case Opcode.Viewport:
                    ValidateOperandCount(module, routine, instructionIndex, instruction, 1);
                    break;

                case Opcode.Cls:
                    ValidateOperandCount(module, routine, instructionIndex, instruction, 1);
                    if (instruction.GetOperand(0) is not 0 and not 1)
                    {
                        Report(
                            "BC4132",
                            $"Инструкция Cls в процедуре '{routine.Name}' ожидает флаг цвета 0 или 1, получено {instruction.GetOperand(0)}.",
                            module,
                            routine.Id,
                            instructionIndex);
                    }

                    break;

                case Opcode.PSet:
                case Opcode.Get:
                case Opcode.Put:
                case Opcode.Line:
                    ValidateOperandCount(module, routine, instructionIndex, instruction, 2);
                    break;

                case Opcode.Circle:
                case Opcode.Paint:
                    ValidateOperandCount(module, routine, instructionIndex, instruction, 2);
                    break;
                case Opcode.Draw:
                    ValidateOperandCount(module, routine, instructionIndex, instruction, 1);
                    if (instruction.GetOperand(0) != 1)
                    {
                        Report(
                            "BC4121",
                            $"Инструкция Draw в процедуре '{routine.Name}' должна потреблять ровно одно значение, получено {instruction.GetOperand(0)}.",
                            module,
                            routine.Id,
                            instructionIndex);
                    }
                    break;

                case Opcode.DrawImage:
                    ValidateOperandCount(module, routine, instructionIndex, instruction, 1);
                    if (instruction.GetOperand(0) is not 0 and not 1)
                    {
                        Report(
                            "BC4130",
                            $"Инструкция DrawImage в процедуре '{routine.Name}' ожидает флаг масштабирования 0 или 1, получено {instruction.GetOperand(0)}.",
                            module,
                            routine.Id,
                            instructionIndex);
                    }
                    break;

                case Opcode.DrawSprite:
                    ValidateOperandCount(module, routine, instructionIndex, instruction, 1);
                    if (instruction.GetOperand(0) is not 0 and not 1)
                    {
                        Report(
                            "BC4131",
                            $"Инструкция DrawSprite в процедуре '{routine.Name}' ожидает флаг масштабирования 0 или 1, получено {instruction.GetOperand(0)}.",
                            module,
                            routine.Id,
                            instructionIndex);
                    }
                    break;

                case Opcode.DrawTile:
                case Opcode.DrawTilemap:
                case Opcode.DrawTileLayer:
                case Opcode.SetMapTile:
                    ValidateOperandCount(module, routine, instructionIndex, instruction, 0);
                    break;

                case Opcode.StatementBoundary:
                    ValidateOperandCount(module, routine, instructionIndex, instruction, 0);
                    break;

                case Opcode.RegisterKeyHandler:
                    ValidateOperandCount(module, routine, instructionIndex, instruction, 1);
                    ValidateRegisterKeyHandlerInstruction(module, routine, instructionIndex, instruction);
                    break;

                case Opcode.RegisterMouseHandler:
                    ValidateOperandCount(module, routine, instructionIndex, instruction, 2);
                    ValidateRegisterMouseHandlerInstruction(module, routine, instructionIndex, instruction);
                    break;

                case Opcode.UnregisterKeyHandler:
                case Opcode.PauseKeyHandler:
                case Opcode.ResumeKeyHandler:
                    ValidateOperandCount(module, routine, instructionIndex, instruction, 0);
                    break;

                case Opcode.UnregisterMouseHandler:
                case Opcode.PauseMouseHandler:
                case Opcode.ResumeMouseHandler:
                    ValidateOperandCount(module, routine, instructionIndex, instruction, 1);
                    ValidateMouseEventKind(module, routine, instructionIndex, instruction, operandIndex: 0);
                    break;

                case Opcode.PushErrorHandler:
                    ValidateOperandCount(module, routine, instructionIndex, instruction, 5);
                    ValidateOptionalJumpTarget(module, routine, instructionIndex, instruction, operandIndex: 0);
                    ValidateOptionalJumpTarget(module, routine, instructionIndex, instruction, operandIndex: 1);
                    ValidateJumpTarget(module, routine, instructionIndex, instruction, operandIndex: 2);
                    ValidateErrorHandlerTarget(module, routine, instructionIndex, instruction, globalsBySlot);
                    break;

                case Opcode.RaiseError:
                    ValidateOperandCount(module, routine, instructionIndex, instruction, 1);
                    if (instruction.GetOperand(0) is not (0 or 1 or 2))
                    {
                        Report(
                            "BC4120",
                            $"Инструкция RaiseError в процедуре '{routine.Name}' содержит некорректный флаг сообщения {instruction.GetOperand(0)}.",
                            module,
                            routine.Id,
                            instructionIndex);
                    }
                    break;

                case Opcode.OpenFile:
                    ValidateOperandCount(module, routine, instructionIndex, instruction, instruction.OperandCount == 3 ? 3 : 2);
                    ValidateOpenFileInstruction(module, routine, instructionIndex, instruction);
                    break;

                case Opcode.PopErrorHandler:
                case Opcode.EndFinally:
                case Opcode.Pop:
                case Opcode.Negate:
                case Opcode.Concat:
                case Opcode.Add:
                case Opcode.Subtract:
                case Opcode.Multiply:
                case Opcode.Power:
                case Opcode.Divide:
                case Opcode.IntegralDivide:
                case Opcode.Modulo:
                case Opcode.Not:
                case Opcode.And:
                case Opcode.Or:
                case Opcode.Xor:
                case Opcode.Eqv:
                case Opcode.Imp:
                case Opcode.ShiftLeft:
                case Opcode.ShiftRight:
                case Opcode.CompareEqual:
                case Opcode.CompareNotEqual:
                case Opcode.CompareLess:
                case Opcode.CompareLessOrEqual:
                case Opcode.CompareGreater:
                case Opcode.CompareGreaterOrEqual:
                case Opcode.ValidateForStep:
                case Opcode.Return:
                case Opcode.EraseArray:
                case Opcode.PrintValue:
                case Opcode.PrintLine:
                case Opcode.Locate:
                case Opcode.CloseFile:
                case Opcode.FormatInterpolatedPart:
                case Opcode.SnapshotValue:
                    ValidateOperandCount(module, routine, instructionIndex, instruction, 0);
                    break;

                case Opcode.Screen:
                    ValidateOperandCount(module, routine, instructionIndex, instruction, 0);
                    break;

                case Opcode.AdvanceForLoopLocal:
                    ValidateOperandCount(module, routine, instructionIndex, instruction, 2);
                    ValidateLocalSlot(module, routine, instructionIndex, instruction, operandIndex: 0);
                    ValidateLocalSlot(module, routine, instructionIndex, instruction, operandIndex: 1);
                    break;

                default:
                    Report(
                        "BC4011",
                        $"Инструкция '{instruction.Opcode}' ещё не описана валидатором модуля.",
                        module,
                        routine.Id,
                        instructionIndex);
                    break;
            }
        }

        if (_diagnostics.Count == diagnosticCountBeforeRoutine)
        {
            var diagnosticCountBeforeStack = _diagnostics.Count;
            ValidateRoutineStack(module, routine, routinesById, builtinsById);

            if (_diagnostics.Count == diagnosticCountBeforeStack)
            {
                ValidateRoutineStackTypes(module, routine, routinesById, globalsBySlot, builtinsById);
            }
        }
    }

    private void ValidateRoutineMetadata(BytecodeModule module, BytecodeRoutine routine)
    {
        var hasInvalidShape = false;

        if (routine.Id < 0)
        {
            Report(
                "BC4094",
                $"Процедура '{routine.Name}' содержит отрицательный routine-id {routine.Id}.",
                module,
                routine.Id);
            hasInvalidShape = true;
        }

        if (routine.ParameterCount < 0)
        {
            Report(
                "BC4095",
                $"Процедура '{routine.Name}' содержит отрицательное число параметров {routine.ParameterCount}.",
                module,
                routine.Id);
            hasInvalidShape = true;
        }

        if (routine.LocalSlotCount < 0)
        {
            Report(
                "BC4096",
                $"Процедура '{routine.Name}' содержит отрицательное число локальных слотов {routine.LocalSlotCount}.",
                module,
                routine.Id);
            hasInvalidShape = true;
        }

        if (hasInvalidShape)
        {
            return;
        }

        if (routine.ParameterVariables.Count != routine.ParameterCount)
        {
            Report(
                "BC4056",
                $"Процедура '{routine.Name}' объявляет {routine.ParameterCount} параметр(а/ов), но таблица параметров содержит {routine.ParameterVariables.Count} записей.",
                module,
                routine.Id);
        }

        if (routine.LocalVariables.Count != routine.LocalSlotCount)
        {
            Report(
                "BC4057",
                $"Процедура '{routine.Name}' объявляет {routine.LocalSlotCount} локальный слот(а/ов), но таблица локальных переменных содержит {routine.LocalVariables.Count} записей.",
                module,
                routine.Id);
        }

        var seenVariables = new HashSet<VariableSymbol>();

        for (var index = 0; index < routine.ParameterVariables.Count; index++)
        {
            var variable = routine.ParameterVariables[index];
            if (variable is not ParameterSymbol parameter || parameter.Ordinal != index)
            {
                Report(
                    "BC4058",
                    $"Процедура '{routine.Name}' содержит некорректную запись параметра в слоте {index}.",
                    module,
                    routine.Id);
                continue;
            }

            ValidateUniqueRoutineVariable(module, routine, seenVariables, variable, "параметров", index);
        }

        for (var index = 0; index < routine.LocalVariables.Count; index++)
        {
            var variable = routine.LocalVariables[index];
            if (variable is null || variable.IsGlobal || variable is ParameterSymbol)
            {
                Report(
                    "BC4059",
                    $"Процедура '{routine.Name}' содержит некорректную запись локальной переменной в слоте {index}.",
                    module,
                    routine.Id);
                continue;
            }

            ValidateUniqueRoutineVariable(module, routine, seenVariables, variable, "локальных переменных", index);
        }

        for (var index = 0; index < routine.CapturedVariables.Count; index++)
        {
            var variable = routine.CapturedVariables[index];
            if (variable is null || variable.IsGlobal)
            {
                Report(
                    "BC4060",
                    $"Процедура '{routine.Name}' содержит некорректную запись захваченной переменной в слоте {index}.",
                    module,
                    routine.Id);
                continue;
            }

            ValidateUniqueRoutineVariable(module, routine, seenVariables, variable, "захваченных переменных", index);
        }
    }

    private void ValidateUniqueRoutineVariable(
        BytecodeModule module,
        BytecodeRoutine routine,
        ISet<VariableSymbol> seenVariables,
        VariableSymbol variable,
        string tableName,
        int slot)
    {
        if (!seenVariables.Add(variable))
        {
            Report(
                "BC4061",
                $"Процедура '{routine.Name}' содержит повторяющуюся переменную '{variable.Name}' в таблице {tableName}, слот {slot}.",
                module,
                routine.Id);
        }
    }

    private void ValidateRoutineStack(
        BytecodeModule module,
        BytecodeRoutine routine,
        IReadOnlyDictionary<int, BytecodeRoutine> routinesById,
        IReadOnlyDictionary<int, BuiltinBinding> builtinsById)
    {
        if (routine.Instructions.Count == 0)
        {
            return;
        }

        var incomingHeights = new Dictionary<int, int> { [0] = 0 };
        var pending = new Queue<int>();
        pending.Enqueue(0);

        while (pending.Count > 0)
        {
            var instructionIndex = pending.Dequeue();
            var stackHeight = incomingHeights[instructionIndex];
            var instruction = routine.Instructions[instructionIndex];

            if (instruction.Opcode == Opcode.Return)
            {
                ValidateReturnStack(module, routine, instructionIndex, stackHeight);
                continue;
            }

            if (!BytecodeStackEffect.TryGet(instruction, routinesById, builtinsById, out var popCount, out var pushCount))
            {
                continue;
            }

            if (stackHeight < popCount)
            {
                Report(
                    "BC4053",
                    $"Инструкция '{instruction.Opcode}' в процедуре '{routine.Name}' снимает со стека {popCount} значений, но доступно только {stackHeight}.",
                    module,
                    routine.Id,
                    instructionIndex);
                continue;
            }

            var nextStackHeight = stackHeight - popCount + pushCount;
            foreach (var successor in GetSuccessorInstructionIndices(routine, instructionIndex, instruction))
            {
                EnqueueStackSuccessor(module, routine, instructionIndex, successor, nextStackHeight, incomingHeights, pending);
            }
        }
    }

    private void ValidateReturnStack(BytecodeModule module, BytecodeRoutine routine, int instructionIndex, int stackHeight)
    {
        var expectedStackHeight = routine.ResultType == TypeSymbol.Void ? 0 : 1;
        if (stackHeight == expectedStackHeight)
        {
            return;
        }

        // Пустой стек у Return в FUNCTION — легальный сценарий: ветвь завершилась
        // без RETURN со значением, ошибку MB3006 фиксирует рантайм (AXD-020a).
        if (expectedStackHeight == 1 && stackHeight == 0)
        {
            return;
        }

        Report(
            "BC4054",
            $"Инструкция Return в процедуре '{routine.Name}' ожидает высоту стека {expectedStackHeight}, но получила {stackHeight}.",
            module,
            routine.Id,
            instructionIndex);
    }

    private void ValidateRoutineStackTypes(
        BytecodeModule module,
        BytecodeRoutine routine,
        IReadOnlyDictionary<int, BytecodeRoutine> routinesById,
        IReadOnlyDictionary<int, BytecodeGlobal> globalsBySlot,
        IReadOnlyDictionary<int, BuiltinBinding> builtinsById)
    {
        if (routine.Instructions.Count == 0)
        {
            return;
        }

        var incomingStacks = new Dictionary<int, TypeSymbol?[]> { [0] = [] };
        var pending = new Queue<int>();
        pending.Enqueue(0);

        while (pending.Count > 0)
        {
            var instructionIndex = pending.Dequeue();
            var stack = incomingStacks[instructionIndex];
            var instruction = routine.Instructions[instructionIndex];

            if (instruction.Opcode == Opcode.Return)
            {
                ValidateReturnStackType(module, routine, instructionIndex, stack);
                continue;
            }

            if (!TryApplyStackTypeEffect(
                    module,
                    routine,
                    instructionIndex,
                    instruction,
                    stack,
                    routinesById,
                    globalsBySlot,
                    builtinsById,
                    out var nextStack))
            {
                continue;
            }

            foreach (var successor in GetSuccessorInstructionIndices(routine, instructionIndex, instruction))
            {
                EnqueueTypeStackSuccessor(routine, successor, nextStack, incomingStacks, pending);
            }
        }
    }

    private bool TryApplyStackTypeEffect(
        BytecodeModule module,
        BytecodeRoutine routine,
        int instructionIndex,
        BytecodeInstruction instruction,
        TypeSymbol?[] stack,
        IReadOnlyDictionary<int, BytecodeRoutine> routinesById,
        IReadOnlyDictionary<int, BytecodeGlobal> globalsBySlot,
        IReadOnlyDictionary<int, BuiltinBinding> builtinsById,
        out TypeSymbol?[] nextStack)
    {
        nextStack = stack;

        if (!BytecodeStackEffect.TryGet(instruction, routinesById, builtinsById, out var popCount, out var pushCount)
            || stack.Length < popCount)
        {
            return false;
        }

        var workingStack = new List<TypeSymbol?>(stack);

        TypeSymbol? Pop()
        {
            return PopStackType(workingStack);
        }

        void Push(TypeSymbol? type)
        {
            workingStack.Add(type);
        }

        switch (instruction.Opcode)
        {
            case Opcode.PushConst:
                Push(GetConstantType(module.ConstantPool[instruction.GetOperand(0)]));
                break;

            case Opcode.PushIntegerZero:
            case Opcode.PushIntegerOne:
                Push(TypeSymbol.Integer);
                break;

            case Opcode.LoadFunction:
                Push(routinesById.TryGetValue(instruction.GetOperand(0), out var functionRoutine) && functionRoutine.Symbol is not null
                    ? FunctionTypeSymbol.FromFunction(functionRoutine.Symbol)
                    : null);
                break;

            case Opcode.LoadBuiltinFunction:
                Push(builtinsById.TryGetValue(instruction.GetOperand(0), out var functionBinding) && functionBinding.Function is not null
                    ? FunctionTypeSymbol.FromFunction(functionBinding.Function)
                    : null);
                break;

            case Opcode.LoadGlobal:
                Push(globalsBySlot.TryGetValue(instruction.GetOperand(0), out var global) ? global.Type : null);
                break;

            case Opcode.LoadLocal:
                Push(GetRoutineLocalType(routine, instruction.GetOperand(0)));
                break;

            case Opcode.LoadArgument:
                Push(GetRoutineParameterType(routine, instruction.GetOperand(0)));
                break;

            case Opcode.LoadCapture:
                Push(GetRoutineCaptureType(routine, instruction.GetOperand(0)));
                break;

            case Opcode.JumpIfForDoneLocal:
            {
                var currentType = GetRoutineLocalType(routine, instruction.GetOperand(0));
                var endType = GetRoutineLocalType(routine, instruction.GetOperand(1));
                var stepType = GetRoutineLocalType(routine, instruction.GetOperand(2));
                ValidateBinaryOperationStackTypes(module, routine, instructionIndex, Opcode.CompareGreater, currentType, endType);
                ValidateNumericStackType(module, routine, instructionIndex, instruction.Opcode, stepType, "шаг локального цикла FOR");

                if (currentType is not null && stepType is not null && currentType != stepType)
                {
                    Report(
                        "BC3069",
                        $"Инструкция '{instruction.Opcode}' ожидает одинаковые типы счётчика и шага, но получила {currentType.Name} и {stepType.Name}.",
                        module,
                        routine.Id,
                        instructionIndex);
                }

                break;
            }

            case Opcode.StoreGlobal:
            {
                var valueType = Pop();
                if (globalsBySlot.TryGetValue(instruction.GetOperand(0), out var targetGlobal))
                {
                    ValidateAssignableStackType(module, routine, instructionIndex, instruction.Opcode, valueType, targetGlobal.Type, "значение глобального слота");
                }
                break;
            }

            case Opcode.StoreLocal:
            {
                var valueType = Pop();
                if (GetRoutineLocalType(routine, instruction.GetOperand(0)) is { } expectedType)
                {
                    ValidateAssignableStackType(module, routine, instructionIndex, instruction.Opcode, valueType, expectedType, "значение локального слота");
                }
                break;
            }

            case Opcode.StoreArgument:
            {
                var valueType = Pop();
                if (GetRoutineParameterType(routine, instruction.GetOperand(0)) is { } expectedType)
                {
                    ValidateAssignableStackType(module, routine, instructionIndex, instruction.Opcode, valueType, expectedType, "значение параметрического слота");
                }
                break;
            }

            case Opcode.StoreCapture:
            {
                var valueType = Pop();
                if (GetRoutineCaptureType(routine, instruction.GetOperand(0)) is { } expectedType)
                {
                    ValidateAssignableStackType(module, routine, instructionIndex, instruction.Opcode, valueType, expectedType, "значение захваченного слота");
                }
                break;
            }

            case Opcode.Pop:
            case Opcode.PrintValue:
                _ = Pop();
                break;

            case Opcode.SnapshotValue:
            {
                var valueType = Pop();
                Push(valueType);
                break;
            }

            case Opcode.Screen:
            {
                var valueCountType = Pop();
                ValidateExpectedStackType(module, routine, instructionIndex, instruction.Opcode, valueCountType, TypeSymbol.Integer, "число аргументов SCREEN");
                var visualPageType = Pop();
                ValidateExpectedStackType(module, routine, instructionIndex, instruction.Opcode, visualPageType, TypeSymbol.Integer, "видимая страница SCREEN");
                var activePageType = Pop();
                ValidateExpectedStackType(module, routine, instructionIndex, instruction.Opcode, activePageType, TypeSymbol.Integer, "активная страница SCREEN");
                var modeType = Pop();
                ValidateExpectedStackType(module, routine, instructionIndex, instruction.Opcode, modeType, TypeSymbol.Integer, "режим SCREEN");
                break;
            }

            case Opcode.Canvas:
                ApplyCanvasStackTypeEffect(module, routine, instructionIndex, instruction, workingStack);
                break;

            case Opcode.Camera:
                ApplyCameraStackTypeEffect(module, routine, instructionIndex, instruction, workingStack);
                break;

            case Opcode.Viewport:
                ApplyViewportStackTypeEffect(module, routine, instructionIndex, instruction, workingStack);
                break;

            case Opcode.ResetCamera:
                break;

            case Opcode.PCopy:
            {
                var destinationPageType = Pop();
                ValidateExpectedStackType(module, routine, instructionIndex, instruction.Opcode, destinationPageType, TypeSymbol.Integer, "страница назначения PCOPY");
                var sourcePageType = Pop();
                ValidateExpectedStackType(module, routine, instructionIndex, instruction.Opcode, sourcePageType, TypeSymbol.Integer, "исходная страница PCOPY");
                break;
            }

            case Opcode.View:
            {
                var flags = instruction.GetOperand(0);
                var valueCount = instruction.GetOperand(1);
                var hasCoordinates = (flags & 2) != 0;
                var hasFillColor = (flags & 4) != 0;
                var hasBorderColor = (flags & 8) != 0;
                var expectedValueCount = hasCoordinates
                    ? 4 + (hasFillColor ? 1 : 0) + (hasBorderColor ? 1 : 0)
                    : 0;
                if (valueCount != expectedValueCount)
                {
                    Report(
                        "BC4124",
                        $"Инструкция View в процедуре '{routine.Name}' ожидает {expectedValueCount} значений, но содержит {valueCount}.",
                        module,
                        routine.Id,
                        instructionIndex);
                    break;
                }

                if (hasBorderColor)
                {
                    var borderType = Pop();
                    ValidateExpectedStackType(module, routine, instructionIndex, instruction.Opcode, borderType, TypeSymbol.Integer, "цвет рамки VIEW");
                }

                if (hasFillColor)
                {
                    var fillType = Pop();
                    ValidateExpectedStackType(module, routine, instructionIndex, instruction.Opcode, fillType, TypeSymbol.Integer, "цвет заливки VIEW");
                }

                if (hasCoordinates)
                {
                    var bottomType = Pop();
                    var rightType = Pop();
                    var topType = Pop();
                    var leftType = Pop();
                    ValidateExpectedStackType(module, routine, instructionIndex, instruction.Opcode, leftType, TypeSymbol.Long, "левая координата VIEW");
                    ValidateExpectedStackType(module, routine, instructionIndex, instruction.Opcode, topType, TypeSymbol.Long, "верхняя координата VIEW");
                    ValidateExpectedStackType(module, routine, instructionIndex, instruction.Opcode, rightType, TypeSymbol.Long, "правая координата VIEW");
                    ValidateExpectedStackType(module, routine, instructionIndex, instruction.Opcode, bottomType, TypeSymbol.Long, "нижняя координата VIEW");
                }

                break;
            }

            case Opcode.ViewPrint:
            {
                var valueCount = instruction.GetOperand(0);
                if (valueCount != 0 && valueCount != 2)
                {
                    Report(
                        "BC4126",
                        $"Инструкция ViewPrint в процедуре '{routine.Name}' ожидает 0 или 2 значения, но содержит {valueCount}.",
                        module,
                        routine.Id,
                        instructionIndex);
                    break;
                }

                if (valueCount == 2)
                {
                    var bottomType = Pop();
                    var topType = Pop();
                    ValidateExpectedStackType(module, routine, instructionIndex, instruction.Opcode, topType, TypeSymbol.Integer, "верхняя строка VIEW PRINT");
                    ValidateExpectedStackType(module, routine, instructionIndex, instruction.Opcode, bottomType, TypeSymbol.Integer, "нижняя строка VIEW PRINT");
                }

                break;
            }

            case Opcode.Window:
            {
                var flags = instruction.GetOperand(0);
                var valueCount = instruction.GetOperand(1);
                var hasCoordinates = (flags & 2) != 0;
                var expectedValueCount = hasCoordinates ? 4 : 0;
                if (valueCount != expectedValueCount)
                {
                    Report(
                        "BC4125",
                        $"Инструкция Window в процедуре '{routine.Name}' ожидает {expectedValueCount} значений, но содержит {valueCount}.",
                        module,
                        routine.Id,
                        instructionIndex);
                    break;
                }

                if (hasCoordinates)
                {
                    var bottomType = Pop();
                    var rightType = Pop();
                    var topType = Pop();
                    var leftType = Pop();
                    ValidateExpectedStackType(module, routine, instructionIndex, instruction.Opcode, leftType, TypeSymbol.Double, "левая координата WINDOW");
                    ValidateExpectedStackType(module, routine, instructionIndex, instruction.Opcode, topType, TypeSymbol.Double, "верхняя координата WINDOW");
                    ValidateExpectedStackType(module, routine, instructionIndex, instruction.Opcode, rightType, TypeSymbol.Double, "правая координата WINDOW");
                    ValidateExpectedStackType(module, routine, instructionIndex, instruction.Opcode, bottomType, TypeSymbol.Double, "нижняя координата WINDOW");
                }

                break;
            }

            case Opcode.Sleep:
            {
                if (instruction.GetOperand(0) == 1)
                {
                    var durationType = Pop();
                    ValidateExpectedStackType(module, routine, instructionIndex, instruction.Opcode, durationType, TypeSymbol.Long, "длительность SLEEP");
                }

                break;
            }

            case Opcode.ReseedRandom:
            {
                if (instruction.GetOperand(0) == 1)
                {
                    var seedType = Pop();
                    ValidateExpectedStackType(module, routine, instructionIndex, instruction.Opcode, seedType, TypeSymbol.Long, "seed оператора RANDOMIZE");
                }

                break;
            }

            case Opcode.Cls:
                if (instruction.GetOperand(0) == 1)
                {
                    var colorType = Pop();
                    ValidateExpectedStackType(module, routine, instructionIndex, instruction.Opcode, colorType, TypeSymbol.Integer, "цвет CLS");
                }

                break;

            case Opcode.Color:
            {
                if (instruction.GetOperand(1) == 2)
                {
                    var backgroundType = Pop();
                    ValidateExpectedStackType(module, routine, instructionIndex, instruction.Opcode, backgroundType, TypeSymbol.Integer, "цвет фона");
                }

                var foregroundType = Pop();
                ValidateExpectedStackType(module, routine, instructionIndex, instruction.Opcode, foregroundType, TypeSymbol.Integer, "цвет текста");
                break;
            }

            case Opcode.Locate:
            {
                var columnType = Pop();
                var rowType = Pop();
                ValidateExpectedStackType(module, routine, instructionIndex, instruction.Opcode, rowType, TypeSymbol.Integer, "строка LOCATE");
                ValidateExpectedStackType(module, routine, instructionIndex, instruction.Opcode, columnType, TypeSymbol.Integer, "столбец LOCATE");
                break;
            }

            case Opcode.Palette:
            {
                var valueCount = instruction.GetOperand(0);
                if (valueCount == 2)
                {
                    var colorType = Pop();
                    var indexType = Pop();
                    ValidateExpectedStackType(module, routine, instructionIndex, instruction.Opcode, indexType, TypeSymbol.Integer, "индекс палитры");
                    ValidateExpectedStackType(module, routine, instructionIndex, instruction.Opcode, colorType, TypeSymbol.Integer, "цвет палитры");
                }
                else if (valueCount == 1)
                {
                    var valuesType = Pop();
                    ValidateArrayStackType(module, routine, instructionIndex, instruction.Opcode, valuesType, "массив PALETTE USING");
                }

                break;
            }

            case Opcode.PSet:
            {
                if (instruction.GetOperand(1) == 3)
                {
                    var colorType = Pop();
                    ValidateExpectedStackType(module, routine, instructionIndex, instruction.Opcode, colorType, TypeSymbol.Integer, "цвет пикселя");
                }

                var yType = Pop();
                var xType = Pop();
                ValidateExpectedStackType(module, routine, instructionIndex, instruction.Opcode, xType, TypeSymbol.Long, "координата X");
                ValidateExpectedStackType(module, routine, instructionIndex, instruction.Opcode, yType, TypeSymbol.Long, "координата Y");
                break;
            }

            case Opcode.Get:
            {
                var flags = instruction.GetOperand(0);
                var valueCount = instruction.GetOperand(1);
                if (valueCount != 5)
                {
                    Report(
                        "BC4126",
                        $"Инструкция Get в процедуре '{routine.Name}' ожидает 5 значений, но содержит {valueCount}.",
                        module,
                        routine.Id,
                        instructionIndex);
                }

                if ((flags & ~3) != 0)
                {
                    Report(
                        "BC4127",
                        $"Инструкция Get в процедуре '{routine.Name}' содержит неизвестные флаги {flags}.",
                        module,
                        routine.Id,
                        instructionIndex);
                }

                var bufferType = Pop();
                var y2Type = Pop();
                var x2Type = Pop();
                var y1Type = Pop();
                var x1Type = Pop();
                ValidateExpectedStackType(module, routine, instructionIndex, instruction.Opcode, x1Type, TypeSymbol.Long, "координата X1");
                ValidateExpectedStackType(module, routine, instructionIndex, instruction.Opcode, y1Type, TypeSymbol.Long, "координата Y1");
                ValidateExpectedStackType(module, routine, instructionIndex, instruction.Opcode, x2Type, TypeSymbol.Long, "координата X2");
                ValidateExpectedStackType(module, routine, instructionIndex, instruction.Opcode, y2Type, TypeSymbol.Long, "координата Y2");
                ValidateRankTwoNumericArrayStackType(module, routine, instructionIndex, instruction.Opcode, bufferType, "буфер GET");
                break;
            }

            case Opcode.Put:
            {
                var flags = instruction.GetOperand(0);
                var valueCount = instruction.GetOperand(1);
                if (valueCount != 3)
                {
                    Report(
                        "BC4128",
                        $"Инструкция Put в процедуре '{routine.Name}' ожидает 3 значения, но содержит {valueCount}.",
                        module,
                        routine.Id,
                        instructionIndex);
                }

                var action = flags >> 1;
                if ((flags & 1) != flags && action is < 0 or > 4)
                {
                    Report(
                        "BC4129",
                        $"Инструкция Put в процедуре '{routine.Name}' содержит неизвестные флаги {flags}.",
                        module,
                        routine.Id,
                        instructionIndex);
                }

                var bufferType = Pop();
                var yType = Pop();
                var xType = Pop();
                ValidateExpectedStackType(module, routine, instructionIndex, instruction.Opcode, xType, TypeSymbol.Long, "координата X");
                ValidateExpectedStackType(module, routine, instructionIndex, instruction.Opcode, yType, TypeSymbol.Long, "координата Y");
                ValidateRankTwoNumericArrayStackType(module, routine, instructionIndex, instruction.Opcode, bufferType, "буфер PUT");
                break;
            }

            case Opcode.Line:
            {
                var flags = instruction.GetOperand(0);
                var count = instruction.GetOperand(1);
                var hasExplicitStartPoint = (flags & 4) != 0;
                var hasColor = hasExplicitStartPoint ? count == 5 : count == 3;

                if (hasColor)
                {
                    var colorType = Pop();
                    ValidateExpectedStackType(module, routine, instructionIndex, instruction.Opcode, colorType, TypeSymbol.Integer, "цвет линии");
                }

                var y2Type = Pop();
                var x2Type = Pop();
                if (hasExplicitStartPoint)
                {
                    var y1Type = Pop();
                    var x1Type = Pop();
                    ValidateExpectedStackType(module, routine, instructionIndex, instruction.Opcode, x1Type, TypeSymbol.Long, "координата X1");
                    ValidateExpectedStackType(module, routine, instructionIndex, instruction.Opcode, y1Type, TypeSymbol.Long, "координата Y1");
                }

                ValidateExpectedStackType(module, routine, instructionIndex, instruction.Opcode, x2Type, TypeSymbol.Long, "координата X2");
                ValidateExpectedStackType(module, routine, instructionIndex, instruction.Opcode, y2Type, TypeSymbol.Long, "координата Y2");
                break;
            }

            case Opcode.Circle:
            {
                var flags = instruction.GetOperand(0);
                var valueCount = instruction.GetOperand(1);
                var expectedValueCount = 3
                    + ((flags & 16) != 0 ? 1 : 0)
                    + ((flags & 2) != 0 ? 1 : 0)
                    + ((flags & 4) != 0 ? 1 : 0)
                    + ((flags & 8) != 0 ? 1 : 0);
                if (valueCount != expectedValueCount)
                {
                    Report(
                        "BC4122",
                        $"Инструкция 'Circle' в процедуре '{routine.Name}' содержит несовместимые флаги {flags} и число значений {valueCount}.",
                        module,
                        routine.Id,
                        instructionIndex);
                }

                if ((flags & 8) != 0)
                {
                    var aspectType = Pop();
                    ValidateExpectedStackType(module, routine, instructionIndex, instruction.Opcode, aspectType, TypeSymbol.Double, "aspect");
                }

                if ((flags & 4) != 0)
                {
                    var endType = Pop();
                    ValidateExpectedStackType(module, routine, instructionIndex, instruction.Opcode, endType, TypeSymbol.Double, "конечный угол");
                }

                if ((flags & 2) != 0)
                {
                    var startType = Pop();
                    ValidateExpectedStackType(module, routine, instructionIndex, instruction.Opcode, startType, TypeSymbol.Double, "начальный угол");
                }

                if ((flags & 16) != 0)
                {
                    var colorType = Pop();
                    ValidateExpectedStackType(module, routine, instructionIndex, instruction.Opcode, colorType, TypeSymbol.Integer, "цвет окружности");
                }

                var radiusType = Pop();
                var yType = Pop();
                var xType = Pop();
                ValidateExpectedStackType(module, routine, instructionIndex, instruction.Opcode, xType, TypeSymbol.Long, "координата X");
                ValidateExpectedStackType(module, routine, instructionIndex, instruction.Opcode, yType, TypeSymbol.Long, "координата Y");
                ValidateExpectedStackType(module, routine, instructionIndex, instruction.Opcode, radiusType, TypeSymbol.Double, "радиус");
                break;
            }

            case Opcode.Paint:
            {
                var flags = instruction.GetOperand(0);
                var valueCount = instruction.GetOperand(1);
                var hasFillColor = (flags & 2) != 0;
                var hasBorderColor = (flags & 4) != 0;
                var hasBackground = (flags & 8) != 0;
                var expectedValueCount = 2
                    + (hasFillColor ? 1 : 0)
                    + (hasBorderColor ? 1 : 0)
                    + (hasBackground ? 1 : 0);
                if (valueCount != expectedValueCount)
                {
                    Report(
                        "BC4123",
                        $"Инструкция {instruction.Opcode} ожидает {expectedValueCount} значений на стеке, но содержит {valueCount}.",
                        module,
                        routine.Id,
                        instructionIndex);
                    break;
                }

                if (hasBackground)
                {
                    var backgroundType = Pop();
                    ValidateExpectedStackType(module, routine, instructionIndex, instruction.Opcode, backgroundType, TypeSymbol.String, "фоновый шаблон");
                }

                if (hasBorderColor)
                {
                    var borderType = Pop();
                    ValidateNumericStackType(module, routine, instructionIndex, instruction.Opcode, borderType, "цвет границы");
                }

                if (hasFillColor)
                {
                    var fillType = Pop();
                    ValidatePaintFillStackType(module, routine, instructionIndex, instruction.Opcode, fillType);
                    if (hasBackground && fillType is not null && fillType != TypeSymbol.String)
                    {
                        Report(
                            "BC4128",
                            $"Инструкция '{instruction.Opcode}' в процедуре '{routine.Name}' использует аргумент 'фон' без строковой формы аргумента 'краска'.",
                            module,
                            routine.Id,
                            instructionIndex);
                    }
                }
                else if (hasBackground)
                {
                    Report(
                        "BC4129",
                        $"Инструкция '{instruction.Opcode}' в процедуре '{routine.Name}' использует аргумент 'фон' без аргумента 'краска'.",
                        module,
                        routine.Id,
                        instructionIndex);
                }

                var yType = Pop();
                var xType = Pop();
                ValidateExpectedStackType(module, routine, instructionIndex, instruction.Opcode, xType, TypeSymbol.Long, "координата X");
                ValidateExpectedStackType(module, routine, instructionIndex, instruction.Opcode, yType, TypeSymbol.Long, "координата Y");
                break;
            }

            case Opcode.Draw:
            {
                var commandType = Pop();
                ValidateExpectedStackType(module, routine, instructionIndex, instruction.Opcode, commandType, TypeSymbol.String, "команды DRAW");
                break;
            }

            case Opcode.DrawRect:
            case Opcode.FillRect:
                ApplyDrawRectStackTypeEffect(module, routine, instructionIndex, instruction, workingStack);
                break;

            case Opcode.DrawLine:
                ApplyDrawLineStackTypeEffect(module, routine, instructionIndex, instruction, workingStack);
                break;

            case Opcode.DrawCircle:
            case Opcode.FillCircle:
                ApplyDrawCircleStackTypeEffect(module, routine, instructionIndex, instruction, workingStack);
                break;

            case Opcode.DrawText:
                ApplyDrawTextStackTypeEffect(module, routine, instructionIndex, instruction, workingStack);
                break;

            case Opcode.DrawImage:
                ApplyDrawImageStackTypeEffect(module, routine, instructionIndex, instruction, workingStack);
                break;

            case Opcode.DrawSprite:
                ApplyDrawSpriteStackTypeEffect(module, routine, instructionIndex, instruction, workingStack);
                break;

            case Opcode.DrawTile:
                ApplyDrawTileStackTypeEffect(module, routine, instructionIndex, instruction, workingStack);
                break;

            case Opcode.DrawTilemap:
                ApplyDrawTilemapStackTypeEffect(module, routine, instructionIndex, instruction, workingStack);
                break;

            case Opcode.DrawTileLayer:
                ApplyDrawTileLayerStackTypeEffect(module, routine, instructionIndex, instruction, workingStack);
                break;

            case Opcode.SetMapTile:
                ApplySetMapTileStackTypeEffect(module, routine, instructionIndex, instruction, workingStack);
                break;

            case Opcode.Negate:
            case Opcode.Not:
            {
                var operandType = Pop();
                ValidateUnaryOperationStackType(module, routine, instructionIndex, instruction.Opcode, operandType);
                Push(GetUnaryOperationResultType(instruction.Opcode, operandType));
                break;
            }

            case Opcode.Convert:
            {
                var valueType = Pop();
                var targetType = GetScalarType(instruction.GetOperand(0));
                if (targetType is not null)
                {
                    ValidateConversionStackType(module, routine, instructionIndex, valueType, targetType);
                }

                Push(targetType);
                break;
            }

            case Opcode.AddLongInteger:
            {
                var rightType = Pop();
                var leftType = Pop();
                if (leftType is not null && rightType is not null
                    && (leftType != TypeSymbol.Long || rightType != TypeSymbol.Integer))
                {
                    Report(
                        "BC3068",
                        $"Инструкция '{instruction.Opcode}' ожидает на стеке пару Long + Integer, но получила {leftType.Name} + {rightType.Name}.",
                        module,
                        routine.Id,
                        instructionIndex);
                }

                Push(TypeSymbol.Long);
                break;
            }

            case Opcode.ValidateForStep:
            {
                var stepType = Pop();
                ValidateNumericStackType(module, routine, instructionIndex, instruction.Opcode, stepType, "шаг цикла FOR");
                break;
            }

            case Opcode.ValidateTypeInvariant:
                ApplyValidateTypeInvariantStackTypeEffect(module, routine, instructionIndex, instruction, workingStack);
                break;

            case Opcode.AdvanceForLoopLocal:
            {
                var currentType = GetRoutineLocalType(routine, instruction.GetOperand(0));
                var stepType = GetRoutineLocalType(routine, instruction.GetOperand(1));
                ValidateBinaryOperationStackTypes(module, routine, instructionIndex, Opcode.Add, currentType, stepType);

                if (currentType is not null && stepType is not null && currentType != stepType)
                {
                    Report(
                        "BC3070",
                        $"Инструкция '{instruction.Opcode}' ожидает одинаковые типы счётчика и шага, но получила {currentType.Name} и {stepType.Name}.",
                        module,
                        routine.Id,
                        instructionIndex);
                }

                break;
            }

            case Opcode.Concat:
                // Интерполяция использует Concat для любых отображаемых значений.
                _ = Pop();
                _ = Pop();
                Push(TypeSymbol.String);
                break;

            case Opcode.Add:
            case Opcode.Subtract:
            case Opcode.Multiply:
            case Opcode.Power:
            case Opcode.Divide:
            case Opcode.IntegralDivide:
            case Opcode.Modulo:
            case Opcode.And:
            case Opcode.Or:
            case Opcode.Xor:
            case Opcode.Eqv:
            case Opcode.Imp:
            case Opcode.ShiftLeft:
            case Opcode.ShiftRight:
            case Opcode.CompareEqual:
            case Opcode.CompareNotEqual:
            case Opcode.CompareLess:
            case Opcode.CompareLessOrEqual:
            case Opcode.CompareGreater:
            case Opcode.CompareGreaterOrEqual:
            {
                var rightType = Pop();
                var leftType = Pop();
                ValidateBinaryOperationStackTypes(module, routine, instructionIndex, instruction.Opcode, leftType, rightType);
                Push(GetBinaryOperationResultType(instruction.Opcode, leftType, rightType));
                break;
            }

            case Opcode.CloseFile:
            {
                var fileType = Pop();
                ValidateExpectedStackType(module, routine, instructionIndex, instruction.Opcode, fileType, TypeSymbol.File, "открытый FILE");
                Push(TypeSymbol.File);
                break;
            }

            case Opcode.FormatInterpolatedPart:
            {
                var formatType = Pop();
                var alignmentType = Pop();
                _ = Pop();
                ValidateExpectedStackType(module, routine, instructionIndex, instruction.Opcode, formatType, TypeSymbol.String, "строку формата");
                ValidateExpectedStackType(module, routine, instructionIndex, instruction.Opcode, alignmentType, TypeSymbol.Integer, "выравнивание");
                Push(TypeSymbol.String);
                break;
            }

            case Opcode.EraseArray:
                ApplyEraseArrayStackTypeEffect(module, routine, instructionIndex, instruction, workingStack);
                break;

            case Opcode.OpenFile:
            {
                var fileType = Pop();
                if (instruction.OperandCount == 3)
                {
                    ValidateExpectedStackType(module, routine, instructionIndex, instruction.Opcode, Pop(), TypeSymbol.String, "кодировка файла");
                }
                var pathType = Pop();
                ValidateExpectedStackType(module, routine, instructionIndex, instruction.Opcode, pathType, TypeSymbol.String, "путь файла");
                ValidateExpectedStackType(module, routine, instructionIndex, instruction.Opcode, fileType, TypeSymbol.File, "файловую переменную");
                Push(TypeSymbol.File);
                break;
            }

            case Opcode.RegisterKeyHandler:
            {
                var handlerType = Pop();
                var keyType = Pop();
                ValidateExpectedStackType(module, routine, instructionIndex, instruction.Opcode, keyType, TypeSymbol.String, "имя клавиши");
                if (handlerType is FunctionTypeSymbol functionType)
                {
                    var passKeyText = instruction.OperandCount > 0 && instruction.GetOperand(0) != 0;
                    var expectedParameterCount = passKeyText ? 1 : 0;
                    var hasExpectedParameters = passKeyText
                        ? functionType.ParameterTypes.Count == 1 && functionType.ParameterTypes[0] == TypeSymbol.String
                        : functionType.ParameterTypes.Count == 0;

                    if (functionType.ReturnType != TypeSymbol.Void || !hasExpectedParameters)
                    {
                        Report(
                            "BC4122",
                            $"Инструкция '{instruction.Opcode}' ожидает обработчик-процедуру с количеством параметров {expectedParameterCount}, но получила несовместимую сигнатуру.",
                            module,
                            routine.Id,
                            instructionIndex);
                    }
                }
                else if (handlerType is not null)
                {
                    ReportIncompatibleOperationStackType(module, routine, instructionIndex, instruction.Opcode, handlerType, "обработчик клавиши");
                }
                break;
            }

            case Opcode.UnregisterKeyHandler:
            case Opcode.PauseKeyHandler:
            case Opcode.ResumeKeyHandler:
            {
                var keyType = Pop();
                ValidateExpectedStackType(module, routine, instructionIndex, instruction.Opcode, keyType, TypeSymbol.String, "имя клавиши");
                break;
            }

            case Opcode.RegisterMouseHandler:
            {
                var handlerType = Pop();
                var buttonType = Pop();
                ValidateExpectedStackType(module, routine, instructionIndex, instruction.Opcode, buttonType, TypeSymbol.Integer, "номер кнопки мыши");
                if (handlerType is FunctionTypeSymbol functionType)
                {
                    var passEvent = instruction.OperandCount > 0 && instruction.GetOperand(0) != 0;
                    var expectedParameterCount = passEvent ? 3 : 0;
                    var hasExpectedParameters = passEvent
                        ? functionType.ParameterTypes.Count == 3
                          && functionType.ParameterTypes[0] == TypeSymbol.Integer
                          && functionType.ParameterTypes[1] == TypeSymbol.Long
                          && functionType.ParameterTypes[2] == TypeSymbol.Long
                        : functionType.ParameterTypes.Count == 0;

                    if (functionType.ReturnType != TypeSymbol.Void || !hasExpectedParameters)
                    {
                        Report(
                            "BC4124",
                            $"Инструкция '{instruction.Opcode}' ожидает обработчик-процедуру с количеством параметров {expectedParameterCount}, но получила несовместимую сигнатуру.",
                            module,
                            routine.Id,
                            instructionIndex);
                    }
                }
                else if (handlerType is not null)
                {
                    ReportIncompatibleOperationStackType(module, routine, instructionIndex, instruction.Opcode, handlerType, "обработчик мыши");
                }
                break;
            }

            case Opcode.UnregisterMouseHandler:
            case Opcode.PauseMouseHandler:
            case Opcode.ResumeMouseHandler:
            {
                var buttonType = Pop();
                ValidateExpectedStackType(module, routine, instructionIndex, instruction.Opcode, buttonType, TypeSymbol.Integer, "номер кнопки мыши");
                break;
            }

            case Opcode.RedimArray:
                ApplyRedimArrayStackTypeEffect(module, routine, instructionIndex, instruction, workingStack);
                break;

            case Opcode.NewArray:
                ApplyNewArrayStackTypeEffect(module, routine, instructionIndex, instruction, workingStack);
                break;

            case Opcode.LoadElement:
                ApplyLoadElementStackTypeEffect(module, routine, instructionIndex, instruction, workingStack);
                break;

            case Opcode.StoreElement:
                ApplyStoreElementStackTypeEffect(module, routine, instructionIndex, instruction, workingStack);
                break;

            case Opcode.CreateRecord:
                ApplyCreateRecordStackTypeEffect(module, routine, instructionIndex, instruction, workingStack);
                break;

            case Opcode.LoadField:
                ApplyLoadFieldStackTypeEffect(module, routine, instructionIndex, instruction, workingStack);
                break;

            case Opcode.StoreField:
                ApplyStoreFieldStackTypeEffect(module, routine, instructionIndex, instruction, workingStack);
                break;

            case Opcode.PrintUsing:
            {
                var valueCount = instruction.GetOperand(0);
                for (var index = 0; index < valueCount; index++)
                {
                    _ = Pop();
                }

                var formatType = Pop();
                ValidateExpectedStackType(module, routine, instructionIndex, instruction.Opcode, formatType, TypeSymbol.String, "строку формата");
                break;
            }

            case Opcode.JumpIfTrue:
            case Opcode.JumpIfFalse:
            {
                var conditionType = Pop();
                ValidateExpectedStackType(module, routine, instructionIndex, instruction.Opcode, conditionType, TypeSymbol.Boolean, "логическое условие");
                break;
            }

            case Opcode.JumpIfEqual:
            case Opcode.JumpIfNotEqual:
            {
                var rightType = Pop();
                var leftType = Pop();
                ValidateBinaryOperationStackTypes(module, routine, instructionIndex, MapBranchComparisonOpcode(instruction.Opcode), leftType, rightType);
                break;
            }

            case Opcode.JumpIfLess:
            case Opcode.JumpIfLessOrEqual:
            case Opcode.JumpIfGreater:
            case Opcode.JumpIfGreaterOrEqual:
            {
                var rightType = Pop();
                var leftType = Pop();
                ValidateBinaryOperationStackTypes(module, routine, instructionIndex, MapBranchComparisonOpcode(instruction.Opcode), leftType, rightType);
                break;
            }

            case Opcode.Call:
            case Opcode.CallByRef:
            {
                var argumentCount = instruction.Opcode == Opcode.CallByRef ? instruction.GetOperand(2) : instruction.GetOperand(1);
                var argumentTypes = new TypeSymbol?[argumentCount];
                for (var index = argumentCount - 1; index >= 0; index--)
                {
                    argumentTypes[index] = Pop();
                }

                routinesById.TryGetValue(instruction.GetOperand(0), out var targetRoutine);

                if (targetRoutine is not null)
                {
                    var stackParameters = instruction.Opcode == Opcode.CallByRef
                        ? targetRoutine.ParameterVariables.Where(static parameter => parameter is not ParameterSymbol { IsByRef: true }).ToArray()
                        : targetRoutine.ParameterVariables;

                    if (stackParameters.Count == argumentCount)
                    {
                        ValidateCallArgumentStackTypes(
                            module,
                            routine,
                            instructionIndex,
                            instruction.Opcode,
                            argumentTypes,
                            stackParameters,
                            targetRoutine.Name);
                    }
                }

                if (targetRoutine is not null && targetRoutine.ResultType != TypeSymbol.Void)
                {
                    Push(targetRoutine.ResultType);
                }
                break;
            }

            case Opcode.CallValue:
            {
                var argumentCount = instruction.GetOperand(0);
                var returnsValue = instruction.GetOperand(1) != 0;
                var argumentTypes = new TypeSymbol?[argumentCount];
                for (var index = argumentCount - 1; index >= 0; index--)
                {
                    argumentTypes[index] = Pop();
                }

                var targetType = Pop();
                if (targetType is FunctionTypeSymbol functionType)
                {
                    ValidateFunctionValueReturnFlag(module, routine, instructionIndex, functionType, returnsValue);
                    ValidateFunctionValueCallStackTypes(
                        module,
                        routine,
                        instructionIndex,
                        instruction.Opcode,
                        argumentTypes,
                        functionType);

                    if (returnsValue && functionType.ReturnType != TypeSymbol.Void)
                    {
                        Push(functionType.ReturnType);
                    }
                }
                else if (targetType is not null)
                {
                    ReportIncompatibleOperationStackType(module, routine, instructionIndex, instruction.Opcode, targetType, "цель косвенного вызова");
                }
                else if (returnsValue)
                {
                    Push(null);
                }

                break;
            }

            case Opcode.CallBuiltin:
            {
                var argumentCount = instruction.GetOperand(1);
                var argumentTypes = new TypeSymbol?[argumentCount];
                for (var index = argumentCount - 1; index >= 0; index--)
                {
                    argumentTypes[index] = Pop();
                }

                if (builtinsById.TryGetValue(instruction.GetOperand(0), out var builtin)
                    && builtin.Function is { } function
                    && function.Parameters.Count == argumentCount)
                {
                    ValidateBuiltinCallArgumentStackTypes(
                        module,
                        routine,
                        instructionIndex,
                        instruction.Opcode,
                        argumentTypes,
                        function.Parameters,
                        builtin.CanonicalId,
                        builtin.Name);
                }
                else if (builtin is not null
                    && BuiltinFunctions.TryGetDescriptor(builtin.CanonicalId, out var descriptor)
                    && descriptor.Parameters.Count == argumentCount)
                {
                    ValidateBuiltinCallArgumentStackTypes(
                        module,
                        routine,
                        instructionIndex,
                        instruction.Opcode,
                        argumentTypes,
                        descriptor,
                        builtin.Name);
                }

                if (builtin is not null && builtin.ReturnsValue)
                {
                    Push(GetBuiltinReturnType(builtin, argumentTypes));
                }
                break;
            }

            default:
                for (var index = 0; index < popCount; index++)
                {
                    _ = Pop();
                }

                for (var index = 0; index < pushCount; index++)
                {
                    Push(null);
                }
                break;
        }

        nextStack = workingStack.ToArray();
        return true;
    }

    private void EnqueueTypeStackSuccessor(
        BytecodeRoutine routine,
        int successor,
        TypeSymbol?[] stack,
        Dictionary<int, TypeSymbol?[]> incomingStacks,
        Queue<int> pending)
    {
        if (successor < 0 || successor >= routine.Instructions.Count)
        {
            return;
        }

        if (incomingStacks.TryGetValue(successor, out var existingStack))
        {
            incomingStacks[successor] = MergeStackTypes(existingStack, stack);
            return;
        }

        incomingStacks[successor] = stack.ToArray();
        pending.Enqueue(successor);
    }

    private static TypeSymbol?[] MergeStackTypes(TypeSymbol?[] left, TypeSymbol?[] right)
    {
        if (left.Length != right.Length)
        {
            return left;
        }

        var merged = new TypeSymbol?[left.Length];
        for (var index = 0; index < left.Length; index++)
        {
            merged[index] = left[index] is not null && left[index] == right[index]
                ? left[index]
                : null;
        }

        return merged;
    }

    private static TypeSymbol? GetRoutineLocalType(BytecodeRoutine routine, int slot)
    {
        return slot >= 0 && slot < routine.LocalVariables.Count
            ? routine.LocalVariables[slot].Type
            : null;
    }

    private static TypeSymbol? GetRoutineParameterType(BytecodeRoutine routine, int slot)
    {
        return slot >= 0 && slot < routine.ParameterVariables.Count
            ? routine.ParameterVariables[slot].Type
            : null;
    }

    private static TypeSymbol? GetRoutineCaptureType(BytecodeRoutine routine, int slot)
    {
        return slot >= 0 && slot < routine.CapturedVariables.Count
            ? routine.CapturedVariables[slot].Type
            : null;
    }

    private static TypeSymbol? GetConstantType(BytecodeConstant constant)
    {
        return constant.Kind switch
        {
            BytecodeConstantKind.Integer => TypeSymbol.Integer,
            BytecodeConstantKind.Long => TypeSymbol.Long,
            BytecodeConstantKind.Double => TypeSymbol.Double,
            BytecodeConstantKind.Decimal => TypeSymbol.Decimal,
            BytecodeConstantKind.String => TypeSymbol.String,
            BytecodeConstantKind.Boolean => TypeSymbol.Boolean,
            _ => null
        };
    }

    private static TypeSymbol? GetScalarType(int typeCode)
    {
        return (BytecodeScalarTypeCode)typeCode switch
        {
            BytecodeScalarTypeCode.Integer => TypeSymbol.Integer,
            BytecodeScalarTypeCode.Long => TypeSymbol.Long,
            BytecodeScalarTypeCode.Double => TypeSymbol.Double,
            BytecodeScalarTypeCode.Decimal => TypeSymbol.Decimal,
            BytecodeScalarTypeCode.String => TypeSymbol.String,
            BytecodeScalarTypeCode.Date => TypeSymbol.Date,
            BytecodeScalarTypeCode.Time => TypeSymbol.Time,
            BytecodeScalarTypeCode.DateTime => TypeSymbol.DateTime,
            BytecodeScalarTypeCode.UtcDateTime => TypeSymbol.UtcDateTime,
            _ => null
        };
    }

    private TypeSymbol? ResolveCreatedRecordType(
        BytecodeModule module,
        BytecodeRoutine routine,
        int instructionIndex,
        BytecodeInstruction instruction)
    {
        var typeIdentity = TryGetStringConstantValue(module, instruction.GetOperand(0));
        if (string.IsNullOrWhiteSpace(typeIdentity))
        {
            return null;
        }

        if (TryResolveKnownUserDefinedType(module, typeIdentity) is { } recordType)
        {
            return recordType;
        }

        Report(
            "BC4117",
            $"Инструкция '{instruction.Opcode}' в процедуре '{routine.Name}' ссылается на неизвестный TYPE '{typeIdentity}'.",
            module,
            routine.Id,
            instructionIndex);
        return null;
    }

    private void ValidateExpectedStackType(
        BytecodeModule module,
        BytecodeRoutine routine,
        int instructionIndex,
        Opcode opcode,
        TypeSymbol? actualType,
        TypeSymbol expectedType,
        string role)
    {
        if (actualType is null || actualType == expectedType)
        {
            return;
        }

        Report(
            "BC4111",
            $"Инструкция '{opcode}' в процедуре '{routine.Name}' ожидает {role} типа {expectedType.Name}, но получила {actualType.Name}.",
            module,
            routine.Id,
            instructionIndex);
    }

    private void ValidateExpectedIntegralStackType(
        BytecodeModule module,
        BytecodeRoutine routine,
        int instructionIndex,
        Opcode opcode,
        TypeSymbol? actualType,
        string role)
    {
        if (actualType is null || actualType == TypeSymbol.Integer || actualType == TypeSymbol.Long)
        {
            return;
        }

        Report(
            "BC4111",
            $"Инструкция '{opcode}' в процедуре '{routine.Name}' ожидает {role} типа INTEGER или LONG, но получила {actualType.Name}.",
            module,
            routine.Id,
            instructionIndex);
    }

    private ArrayTypeSymbol? ValidateArrayStackType(
        BytecodeModule module,
        BytecodeRoutine routine,
        int instructionIndex,
        Opcode opcode,
        TypeSymbol? actualType,
        string role)
    {
        if (actualType is null)
        {
            return null;
        }

        if (actualType is ArrayTypeSymbol arrayType)
        {
            return arrayType;
        }

        Report(
            "BC4112",
            $"Инструкция '{opcode}' в процедуре '{routine.Name}' ожидает {role}, но получила {actualType.Name}.",
            module,
            routine.Id,
            instructionIndex);
        return null;
    }

    private void ValidateNumericStackType(
        BytecodeModule module,
        BytecodeRoutine routine,
        int instructionIndex,
        Opcode opcode,
        TypeSymbol? actualType,
        string role)
    {
        if (actualType is null || IsNumericType(actualType))
        {
            return;
        }

        Report(
            "BC4113",
            $"Инструкция '{opcode}' в процедуре '{routine.Name}' ожидает числовое значение для роли '{role}', но получила {actualType.Name}.",
            module,
            routine.Id,
            instructionIndex);
    }

    private void ValidateRankTwoNumericArrayStackType(
        BytecodeModule module,
        BytecodeRoutine routine,
        int instructionIndex,
        Opcode opcode,
        TypeSymbol? actualType,
        string role)
    {
        var arrayType = ValidateArrayStackType(module, routine, instructionIndex, opcode, actualType, role);
        if (arrayType is null)
        {
            return;
        }

        if (arrayType.Rank != 2)
        {
            Report(
                "BC4130",
                $"Инструкция '{opcode}' в процедуре '{routine.Name}' ожидает двумерный массив для роли '{role}', но получила массив ранга {arrayType.Rank}.",
                module,
                routine.Id,
                instructionIndex);
        }

        if (!IsNumericType(arrayType.ElementType))
        {
            Report(
                "BC4131",
                $"Инструкция '{opcode}' в процедуре '{routine.Name}' ожидает числовой массив для роли '{role}', но получила элементы типа {arrayType.ElementType.Name}.",
                module,
                routine.Id,
                instructionIndex);
        }
    }

    private void ValidatePaintFillStackType(
        BytecodeModule module,
        BytecodeRoutine routine,
        int instructionIndex,
        Opcode opcode,
        TypeSymbol? actualType)
    {
        if (actualType is null
            || actualType == TypeSymbol.String
            || IsNumericType(actualType))
        {
            return;
        }

        Report(
            "BC4130",
            $"Инструкция '{opcode}' в процедуре '{routine.Name}' ожидает для аргумента 'краска' числовой или строковый тип, но получила {actualType.Name}.",
            module,
            routine.Id,
            instructionIndex);
    }

    private TypeSymbol? ValidateRecordFieldStackType(
        BytecodeModule module,
        BytecodeRoutine routine,
        int instructionIndex,
        BytecodeInstruction instruction,
        TypeSymbol? actualType)
    {
        if (actualType is null)
        {
            return null;
        }

        var fieldName = TryGetStringConstantValue(module, instruction.GetOperand(0));
        if (actualType == TypeSymbol.RuntimeError)
        {
            if (fieldName is not null
                && TypeSymbol.RuntimeError.TryGetField(fieldName, out var errorField)
                && errorField is not null)
            {
                return errorField.Type;
            }

            Report(
                "BC4115",
                $"Инструкция '{instruction.Opcode}' в процедуре '{routine.Name}' ссылается на отсутствующее поле '{fieldName ?? "<field>"}' типа ERROR.",
                module,
                routine.Id,
                instructionIndex);
            return null;
        }

        if (actualType is not UserDefinedTypeSymbol recordType)
        {
            Report(
                "BC4114",
                $"Инструкция '{instruction.Opcode}' в процедуре '{routine.Name}' ожидает значение TYPE для доступа к полю '{fieldName ?? "<field>"}', но получила {actualType.Name}.",
                module,
                routine.Id,
                instructionIndex);
            return null;
        }

        if (fieldName is null || !recordType.TryGetField(fieldName, out var field) || field is null)
        {
            Report(
                "BC4115",
                $"Инструкция '{instruction.Opcode}' в процедуре '{routine.Name}' ссылается на отсутствующее поле '{fieldName ?? "<field>"}' типа {recordType.Name}.",
                module,
                routine.Id,
                instructionIndex);
            return null;
        }

        return field.Type;
    }

    private void ValidateAssignableStackType(
        BytecodeModule module,
        BytecodeRoutine routine,
        int instructionIndex,
        Opcode opcode,
        TypeSymbol? actualType,
        TypeSymbol expectedType,
        string role)
    {
        if (actualType is null || actualType == expectedType)
        {
            return;
        }

        Report(
            "BC4116",
            $"Инструкция '{opcode}' в процедуре '{routine.Name}' ожидает {role} типа {expectedType.Name}, но получила {actualType.Name}.",
            module,
            routine.Id,
                instructionIndex);
    }

    private void ValidateUnaryOperationStackType(
        BytecodeModule module,
        BytecodeRoutine routine,
        int instructionIndex,
        Opcode opcode,
        TypeSymbol? operandType)
    {
        if (operandType is null || IsUnaryOperationCompatible(opcode, operandType))
        {
            return;
        }

        if (opcode == Opcode.Negate)
        {
            ValidateNumericStackType(module, routine, instructionIndex, opcode, operandType, "операнд унарного '-'");
            return;
        }

        ReportIncompatibleOperationStackType(module, routine, instructionIndex, opcode, operandType, "операнд NOT");
    }

    private void ValidateBinaryOperationStackTypes(
        BytecodeModule module,
        BytecodeRoutine routine,
        int instructionIndex,
        Opcode opcode,
        TypeSymbol? leftType,
        TypeSymbol? rightType)
    {
        if (leftType is null || rightType is null || IsBinaryOperationCompatible(opcode, leftType, rightType))
        {
            return;
        }

        Report(
            "BC4116",
            $"Инструкция '{opcode}' в процедуре '{routine.Name}' получила несовместимые типы операндов {leftType.Name} и {rightType.Name}.",
            module,
            routine.Id,
            instructionIndex);
    }

    private void ValidateConversionStackType(
        BytecodeModule module,
        BytecodeRoutine routine,
        int instructionIndex,
        TypeSymbol? sourceType,
        TypeSymbol targetType)
    {
        if (sourceType is null || IsConversionCompatible(sourceType, targetType))
        {
            return;
        }

        Report(
            "BC4116",
            $"Инструкция Convert в процедуре '{routine.Name}' не поддерживает преобразование {sourceType.Name} -> {targetType.Name}.",
            module,
            routine.Id,
            instructionIndex);
    }

    private void ReportIncompatibleOperationStackType(
        BytecodeModule module,
        BytecodeRoutine routine,
        int instructionIndex,
        Opcode opcode,
        TypeSymbol actualType,
        string role)
    {
        Report(
            "BC4116",
            $"Инструкция '{opcode}' в процедуре '{routine.Name}' получила несовместимый тип {actualType.Name} для роли '{role}'.",
            module,
            routine.Id,
            instructionIndex);
    }

    private void ValidateCallArgumentStackTypes(
        BytecodeModule module,
        BytecodeRoutine routine,
        int instructionIndex,
        Opcode opcode,
        IReadOnlyList<TypeSymbol?> actualTypes,
        IReadOnlyList<VariableSymbol> parameters,
        string targetName)
    {
        if (actualTypes.Count != parameters.Count)
        {
            return;
        }

        for (var index = 0; index < actualTypes.Count; index++)
        {
            ValidateAssignableStackType(
                module,
                routine,
                instructionIndex,
                opcode,
                actualTypes[index],
                parameters[index].Type,
                $"аргумент {index + 1} вызова '{targetName}'");
        }
    }

    private void ValidateFunctionValueCallStackTypes(
        BytecodeModule module,
        BytecodeRoutine routine,
        int instructionIndex,
        Opcode opcode,
        IReadOnlyList<TypeSymbol?> actualTypes,
        FunctionTypeSymbol functionType)
    {
        if (actualTypes.Count != functionType.ParameterTypes.Count)
        {
            Report(
                "BC4116",
                $"Инструкция '{opcode}' в процедуре '{routine.Name}' передаёт {actualTypes.Count} аргумент(а/ов) в функциональное значение типа {functionType.Name}.",
                module,
                routine.Id,
                instructionIndex);
            return;
        }

        for (var index = 0; index < actualTypes.Count; index++)
        {
            ValidateAssignableStackType(
                module,
                routine,
                instructionIndex,
                opcode,
                actualTypes[index],
                functionType.ParameterTypes[index],
                $"аргумент {index + 1} косвенного вызова");
        }
    }

    private void ValidateFunctionValueReturnFlag(
        BytecodeModule module,
        BytecodeRoutine routine,
        int instructionIndex,
        FunctionTypeSymbol functionType,
        bool returnsValue)
    {
        var expectedReturnsValue = functionType.ReturnType != TypeSymbol.Void;
        if (returnsValue == expectedReturnsValue)
        {
            return;
        }

        Report(
            "BC4118",
            $"Инструкция CallValue в процедуре '{routine.Name}' содержит признак возвращаемого значения, несовместимый с типом {functionType.Name}.",
            module,
            routine.Id,
            instructionIndex);
    }

    private void ValidateBuiltinCallArgumentStackTypes(
        BytecodeModule module,
        BytecodeRoutine routine,
        int instructionIndex,
        Opcode opcode,
        IReadOnlyList<TypeSymbol?> actualTypes,
        IReadOnlyList<ParameterSymbol> parameters,
        BuiltinFunctionId builtinId,
        string targetName)
    {
        if (actualTypes.Count != parameters.Count)
        {
            return;
        }

        for (var index = 0; index < actualTypes.Count; index++)
        {
            var actualType = actualTypes[index];
            var expectedType = parameters[index].Type;
            if (IsBuiltinArgumentCompatible(actualType, expectedType)
                || IsPartExtractionArgument(builtinId, actualType, expectedType))
            {
                continue;
            }

            Report(
                "BC4116",
                $"Инструкция '{opcode}' в процедуре '{routine.Name}' ожидает аргумент {index + 1} вызова '{targetName}' типа {expectedType.Name}, но получила {actualType!.Name}.",
                module,
                routine.Id,
                instructionIndex);
        }
    }

    private void ValidateBuiltinCallArgumentStackTypes(
        BytecodeModule module,
        BytecodeRoutine routine,
        int instructionIndex,
        Opcode opcode,
        IReadOnlyList<TypeSymbol?> actualTypes,
        BuiltinFunctionDescriptor descriptor,
        string targetName)
    {
        if (actualTypes.Count != descriptor.Parameters.Count)
        {
            return;
        }

        for (var index = 0; index < actualTypes.Count; index++)
        {
            var actualType = actualTypes[index];
            var expectedType = descriptor.Parameters[index].Type;
            if (IsBuiltinDescriptorArgumentCompatible(actualType, expectedType, descriptor, index, actualTypes)
                || IsPartExtractionArgument(descriptor.Id, actualType, expectedType))
            {
                continue;
            }

            Report(
                "BC4116",
                $"Инструкция '{opcode}' в процедуре '{routine.Name}' ожидает аргумент {index + 1} вызова '{targetName}' типа {expectedType.Name}, но получила {actualType!.Name}.",
                module,
                routine.Id,
                instructionIndex);
        }
    }

    private static bool IsBuiltinArgumentCompatible(TypeSymbol? actualType, TypeSymbol expectedType)
    {
        if (actualType is null || actualType == expectedType)
        {
            return true;
        }

        return expectedType == TypeSymbol.Double && IsNumericType(actualType);
    }

    /// <summary>
    /// Валидатор повторяет договор связывания: функция извлечения части принимает
    /// DATETIME и канонический текст напрямую, остальные аргументы подчиняются
    /// общему запрету потери компонента.
    /// </summary>
    private static bool IsPartExtractionArgument(
        BuiltinFunctionId builtinId,
        TypeSymbol? actualType,
        TypeSymbol expectedType)
    {
        return actualType is not null
            && TemporalConversionRules.IsPartExtractionBuiltin(builtinId)
            && (expectedType == TypeSymbol.Date || expectedType == TypeSymbol.Time)
            && TemporalConversionRules.AcceptsPartExtractionArgument(actualType, expectedType);
    }

    private static bool IsBuiltinDescriptorArgumentCompatible(
        TypeSymbol? actualType,
        TypeSymbol expectedType,
        BuiltinFunctionDescriptor descriptor,
        int parameterIndex,
        IReadOnlyList<TypeSymbol?> actualTypes)
    {
        if (actualType is null || actualType == expectedType)
        {
            return true;
        }

        if (descriptor.BindingKind == BuiltinBindingKind.Numeric
            && expectedType == TypeSymbol.Double
            && IsNumericType(actualType))
        {
            return true;
        }

        if (descriptor.BindingKind == BuiltinBindingKind.Array
            && IsArrayDescriptorArgumentCompatible(actualType, expectedType, descriptor, parameterIndex, actualTypes))
        {
            return true;
        }

        return false;
    }

    private static bool IsArrayDescriptorArgumentCompatible(
        TypeSymbol actualType,
        TypeSymbol expectedType,
        BuiltinFunctionDescriptor descriptor,
        int parameterIndex,
        IReadOnlyList<TypeSymbol?> actualTypes)
    {
        var parameterName = descriptor.Parameters[parameterIndex].Name;
        var elementType = actualTypes.Count > 0 && actualTypes[0] is ArrayTypeSymbol arrayType
            ? arrayType.ElementType
            : null;

        if (expectedType is ArrayTypeSymbol)
        {
            return actualType is ArrayTypeSymbol actualArrayType
                && (parameterIndex == 0 || elementType is null || actualArrayType.ElementType == elementType);
        }

        if (string.Equals(parameterName, "start", StringComparison.OrdinalIgnoreCase)
            || string.Equals(parameterName, "count", StringComparison.OrdinalIgnoreCase)
            || string.Equals(parameterName, "index", StringComparison.OrdinalIgnoreCase))
        {
            return actualType == TypeSymbol.Integer;
        }

        if (string.Equals(parameterName, "predicate", StringComparison.OrdinalIgnoreCase)
            && actualType is FunctionTypeSymbol predicateType)
        {
            return elementType is null
                || predicateType.Equals(FunctionTypeSymbol.Create([elementType], TypeSymbol.Boolean));
        }

        if (string.Equals(parameterName, "selector", StringComparison.OrdinalIgnoreCase)
            && actualType is FunctionTypeSymbol selectorType)
        {
            return elementType is null
                || (selectorType.ParameterTypes.Count == 1
                    && selectorType.ParameterTypes[0] == elementType
                    && selectorType.ReturnType != TypeSymbol.Void
                    && selectorType.ReturnType is not ArrayTypeSymbol);
        }

        if (string.Equals(parameterName, "reducer", StringComparison.OrdinalIgnoreCase)
            && actualType is FunctionTypeSymbol reducerType)
        {
            var accumulatorType = actualTypes.Count > 1 ? actualTypes[1] : null;
            return elementType is null
                || accumulatorType is null
                || reducerType.Equals(FunctionTypeSymbol.Create([accumulatorType, elementType], accumulatorType));
        }

        if (string.Equals(parameterName, "value", StringComparison.OrdinalIgnoreCase)
            && elementType is not null)
        {
            return actualType == elementType;
        }

        if (string.Equals(parameterName, "seed", StringComparison.OrdinalIgnoreCase))
        {
            return actualType != TypeSymbol.Void;
        }

        return actualType == expectedType;
    }

    private static TypeSymbol? GetBuiltinReturnType(BuiltinBinding builtin, IReadOnlyList<TypeSymbol?> argumentTypes)
    {
        if (BuiltinFunctions.TryGetDescriptor(builtin.CanonicalId, out var descriptor))
        {
            // Тип-сохраняющие политики должны выводить тип из фактического аргумента,
            // а не из объявленного (обычно Double) возвращаемого типа.
            switch (descriptor.ResultTypePolicy)
            {
                case BuiltinResultTypePolicy.SameAsFirstArgument:
                case BuiltinResultTypePolicy.PromotedNumeric:
                    return argumentTypes.Count > 0 ? argumentTypes[0] : descriptor.DeclaredReturnType;
            }
        }

        if (builtin.Function is not null)
        {
            return builtin.Function.ReturnType;
        }

        if (descriptor is not null)
        {
            return descriptor.ResultTypePolicy switch
            {
                BuiltinResultTypePolicy.ElementTypeOfFirstArrayArgument => argumentTypes.Count > 0 && argumentTypes[0] is ArrayTypeSymbol arrayType
                    ? arrayType.ElementType
                    : descriptor.DeclaredReturnType,
                BuiltinResultTypePolicy.ArrayMapSelectorReturnArray => argumentTypes.Count > 1
                    && argumentTypes[1] is FunctionTypeSymbol selectorType
                    && selectorType.ReturnType != TypeSymbol.Void
                    && selectorType.ReturnType is not ArrayTypeSymbol
                        ? ArrayTypeSymbol.Create(selectorType.ReturnType)
                        : descriptor.DeclaredReturnType,
                BuiltinResultTypePolicy.ArrayReduceAccumulator => argumentTypes.Count > 1 && argumentTypes[1] is not null
                    ? argumentTypes[1]
                    : descriptor.DeclaredReturnType,
                _ => descriptor.DeclaredReturnType
            };
        }

        return null;
    }

    private static bool IsUnaryOperationCompatible(Opcode opcode, TypeSymbol operandType)
    {
        return opcode switch
        {
            Opcode.Negate => IsNumericType(operandType),
            Opcode.Not => operandType == TypeSymbol.Boolean
                || operandType == TypeSymbol.Integer
                || operandType == TypeSymbol.Long,
            _ => true
        };
    }

    private static TypeSymbol? GetUnaryOperationResultType(Opcode opcode, TypeSymbol? operandType)
    {
        return operandType is not null && IsUnaryOperationCompatible(opcode, operandType)
            ? operandType
            : null;
    }

    private static bool IsBinaryOperationCompatible(Opcode opcode, TypeSymbol leftType, TypeSymbol rightType)
    {
        return opcode switch
        {
            Opcode.Add => (leftType == TypeSymbol.String && rightType == TypeSymbol.String)
                || IsArithmeticOperationCompatible(leftType, rightType),
            Opcode.Subtract or Opcode.Multiply => IsArithmeticOperationCompatible(leftType, rightType),
            Opcode.Power => leftType == TypeSymbol.Double && rightType == TypeSymbol.Double,
            Opcode.Divide => IsDivisionOperationCompatible(leftType, rightType),
            Opcode.IntegralDivide => IsIntegralOperationCompatible(leftType, rightType),
            Opcode.Modulo => leftType == rightType && (leftType == TypeSymbol.Integer || leftType == TypeSymbol.Long),
            Opcode.And or Opcode.Or or Opcode.Xor or Opcode.Eqv or Opcode.Imp => leftType == rightType
                && (leftType == TypeSymbol.Boolean || leftType == TypeSymbol.Integer || leftType == TypeSymbol.Long),
            Opcode.ShiftLeft or Opcode.ShiftRight => IsShiftOperationCompatible(leftType, rightType),
            Opcode.CompareEqual or Opcode.CompareNotEqual => IsEqualityOperationCompatible(leftType, rightType),
            Opcode.CompareLess or Opcode.CompareLessOrEqual or Opcode.CompareGreater or Opcode.CompareGreaterOrEqual =>
                IsComparisonOperationCompatible(leftType, rightType),
            _ => true
        };
    }

    private static TypeSymbol? GetBinaryOperationResultType(Opcode opcode, TypeSymbol? leftType, TypeSymbol? rightType)
    {
        if (opcode is Opcode.CompareEqual
            or Opcode.CompareNotEqual
            or Opcode.CompareLess
            or Opcode.CompareLessOrEqual
            or Opcode.CompareGreater
            or Opcode.CompareGreaterOrEqual)
        {
            return TypeSymbol.Boolean;
        }

        if (leftType is null || rightType is null || !IsBinaryOperationCompatible(opcode, leftType, rightType))
        {
            return null;
        }

        if (opcode == Opcode.Divide)
        {
            return leftType == TypeSymbol.Decimal ? TypeSymbol.Decimal : TypeSymbol.Double;
        }

        if (opcode == Opcode.Power)
        {
            return TypeSymbol.Double;
        }

        return leftType;
    }

    private static Opcode MapBranchComparisonOpcode(Opcode opcode)
    {
        return opcode switch
        {
            Opcode.JumpIfEqual => Opcode.CompareEqual,
            Opcode.JumpIfNotEqual => Opcode.CompareNotEqual,
            Opcode.JumpIfLess => Opcode.CompareLess,
            Opcode.JumpIfLessOrEqual => Opcode.CompareLessOrEqual,
            Opcode.JumpIfGreater => Opcode.CompareGreater,
            Opcode.JumpIfGreaterOrEqual => Opcode.CompareGreaterOrEqual,
            _ => throw new InvalidOperationException($"Opcode '{opcode}' is not a comparison branch.")
        };
    }

    private static bool IsArithmeticOperationCompatible(TypeSymbol leftType, TypeSymbol rightType)
    {
        if (leftType == TypeSymbol.Integer || leftType == TypeSymbol.Long)
        {
            return rightType == leftType;
        }

        if (leftType == TypeSymbol.Double)
        {
            return rightType == TypeSymbol.Integer || rightType == TypeSymbol.Long || rightType == TypeSymbol.Double;
        }

        return leftType == TypeSymbol.Decimal
            && (rightType == TypeSymbol.Integer || rightType == TypeSymbol.Long || rightType == TypeSymbol.Decimal);
    }

    private static bool IsDivisionOperationCompatible(TypeSymbol leftType, TypeSymbol rightType)
    {
        if (leftType == TypeSymbol.Integer || leftType == TypeSymbol.Long || leftType == TypeSymbol.Double)
        {
            return rightType == TypeSymbol.Integer || rightType == TypeSymbol.Long || rightType == TypeSymbol.Double;
        }

        return leftType == TypeSymbol.Decimal
            && (rightType == TypeSymbol.Integer || rightType == TypeSymbol.Long || rightType == TypeSymbol.Decimal);
    }

    private static bool IsIntegralOperationCompatible(TypeSymbol leftType, TypeSymbol rightType)
    {
        return leftType == rightType && (leftType == TypeSymbol.Integer || leftType == TypeSymbol.Long);
    }

    private static bool IsShiftOperationCompatible(TypeSymbol leftType, TypeSymbol rightType)
    {
        return (leftType == TypeSymbol.Integer || leftType == TypeSymbol.Long)
            && (rightType == TypeSymbol.Integer || rightType == TypeSymbol.Long);
    }

    private static bool IsEqualityOperationCompatible(TypeSymbol leftType, TypeSymbol rightType)
    {
        if (leftType == rightType
            && leftType is UserDefinedTypeSymbol userDefinedType
            && SupportsStructuralEquality(userDefinedType))
        {
            return true;
        }

        if (leftType == TypeSymbol.String || leftType == TypeSymbol.Boolean)
        {
            return rightType == leftType;
        }

        return IsComparisonOperationCompatible(leftType, rightType);
    }

    private static bool IsComparisonOperationCompatible(TypeSymbol leftType, TypeSymbol rightType)
    {
        if (leftType == TypeSymbol.Integer || leftType == TypeSymbol.Long || leftType == TypeSymbol.String
            || leftType == TypeSymbol.Date || leftType == TypeSymbol.Time
            || leftType == TypeSymbol.DateTime || leftType == TypeSymbol.UtcDateTime)
        {
            return rightType == leftType;
        }

        if (leftType == TypeSymbol.Double)
        {
            return rightType == TypeSymbol.Integer || rightType == TypeSymbol.Long || rightType == TypeSymbol.Double;
        }

        return leftType == TypeSymbol.Decimal
            && (rightType == TypeSymbol.Integer || rightType == TypeSymbol.Long || rightType == TypeSymbol.Decimal);
    }

    private static bool SupportsStructuralEquality(TypeSymbol type)
    {
        return StructuralEqualitySupport.IsSupported(type);
    }

    private static bool IsConversionCompatible(TypeSymbol sourceType, TypeSymbol targetType)
    {
        if (targetType == TypeSymbol.Integer)
        {
            return sourceType == TypeSymbol.Integer;
        }

        if (targetType == TypeSymbol.Long)
        {
            return sourceType == TypeSymbol.Integer || sourceType == TypeSymbol.Long;
        }

        if (targetType == TypeSymbol.Double)
        {
            return sourceType == TypeSymbol.Integer
                || sourceType == TypeSymbol.Long
                || sourceType == TypeSymbol.Double
                || sourceType == TypeSymbol.Decimal;
        }

        if (targetType == TypeSymbol.String)
        {
            return sourceType == TypeSymbol.Date
                || sourceType == TypeSymbol.Time
                || sourceType == TypeSymbol.DateTime
                || sourceType == TypeSymbol.UtcDateTime;
        }

        // Потеря календарной даты или времени суток не является допустимым
        // преобразованием Convert: для неё существует явное извлечение части.
        if (targetType == TypeSymbol.Date)
        {
            return sourceType == TypeSymbol.String;
        }

        if (targetType == TypeSymbol.Time)
        {
            return sourceType == TypeSymbol.String;
        }

        if (targetType == TypeSymbol.DateTime)
        {
            return sourceType == TypeSymbol.Date
                || sourceType == TypeSymbol.Time
                || sourceType == TypeSymbol.String;
        }

        if (targetType == TypeSymbol.UtcDateTime)
        {
            return sourceType == TypeSymbol.String;
        }

        return targetType == TypeSymbol.Decimal
            && (sourceType == TypeSymbol.Integer || sourceType == TypeSymbol.Long || sourceType == TypeSymbol.Decimal);
    }

    private void ValidateReturnStackType(
        BytecodeModule module,
        BytecodeRoutine routine,
        int instructionIndex,
        IReadOnlyList<TypeSymbol?> stack)
    {
        if (routine.ResultType == TypeSymbol.Void || stack.Count == 0)
        {
            return;
        }

        ValidateAssignableStackType(
            module,
            routine,
            instructionIndex,
            Opcode.Return,
            stack[^1],
            routine.ResultType,
            "возвращаемое значение");
    }

    private static bool IsNumericType(TypeSymbol type)
    {
        return type == TypeSymbol.Integer
            || type == TypeSymbol.Long
            || type == TypeSymbol.Double
            || type == TypeSymbol.Decimal;
    }

    private static string? TryGetStringConstantValue(BytecodeModule module, int constantIndex)
    {
        if (constantIndex < 0 || constantIndex >= module.ConstantPool.Count)
        {
            return null;
        }

        var constant = module.ConstantPool[constantIndex];
        return constant.Kind == BytecodeConstantKind.String
            ? (string?)constant.Value
            : null;
    }

    private static UserDefinedTypeSymbol? TryResolveKnownUserDefinedType(BytecodeModule module, string typeIdentity)
    {
        ArgumentNullException.ThrowIfNull(module);
        ArgumentException.ThrowIfNullOrWhiteSpace(typeIdentity);

        var visited = new HashSet<TypeSymbol>();

        foreach (var global in module.Globals)
        {
            if (TryResolveFromType(global.Type, typeIdentity, visited) is { } globalType)
            {
                return globalType;
            }
        }

        foreach (var routine in module.Routines)
        {
            if (TryResolveFromType(routine.ResultType, typeIdentity, visited) is { } resultType)
            {
                return resultType;
            }

            foreach (var variable in routine.ParameterVariables)
            {
                if (TryResolveFromType(variable.Type, typeIdentity, visited) is { } parameterType)
                {
                    return parameterType;
                }
            }

            foreach (var variable in routine.LocalVariables)
            {
                if (TryResolveFromType(variable.Type, typeIdentity, visited) is { } localType)
                {
                    return localType;
                }
            }

            foreach (var variable in routine.CapturedVariables)
            {
                if (TryResolveFromType(variable.Type, typeIdentity, visited) is { } capturedType)
                {
                    return capturedType;
                }
            }
        }

        return null;
    }

    private static UserDefinedTypeSymbol? TryResolveFromType(TypeSymbol type, string typeIdentity, HashSet<TypeSymbol> visited)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentException.ThrowIfNullOrWhiteSpace(typeIdentity);
        ArgumentNullException.ThrowIfNull(visited);

        if (!visited.Add(type))
        {
            return null;
        }

        if (type is UserDefinedTypeSymbol userType)
        {
            if (MatchesTypeIdentity(userType, typeIdentity))
            {
                return userType;
            }

            foreach (var field in userType.Fields)
            {
                if (TryResolveFromType(field.Type, typeIdentity, visited) is { } nestedType)
                {
                    return nestedType;
                }
            }

            return null;
        }

        return type is ArrayTypeSymbol arrayType
            ? TryResolveFromType(arrayType.ElementType, typeIdentity, visited)
            : null;
    }

    /// <summary>
    /// Сравнивается полная идентичность. У типов из старых снимков без модуля
    /// она совпадает с коротким именем; квалифицированные типы им не подменяются.
    /// </summary>
    private static bool MatchesTypeIdentity(UserDefinedTypeSymbol type, string typeIdentity)
    {
        return TypeIdentityFormat.Equals(type.TypeIdentity, typeIdentity);
    }

    private static IEnumerable<int> GetSuccessorInstructionIndices(
        BytecodeRoutine routine,
        int instructionIndex,
        BytecodeInstruction instruction)
    {
        switch (instruction.Opcode)
        {
            case Opcode.Jump:
                if (instruction.OperandCount >= 1)
                {
                    yield return instruction.GetOperand(0);
                }
                break;

            case Opcode.JumpIfLess:
            case Opcode.JumpIfLessOrEqual:
            case Opcode.JumpIfGreater:
            case Opcode.JumpIfGreaterOrEqual:
            case Opcode.JumpIfEqual:
            case Opcode.JumpIfNotEqual:
            case Opcode.JumpIfTrue:
            case Opcode.JumpIfFalse:
                if (instruction.OperandCount >= 1)
                {
                    yield return instruction.GetOperand(0);
                }

                if (instructionIndex + 1 < routine.Instructions.Count)
                {
                    yield return instructionIndex + 1;
                }
                break;

            default:
                if (instructionIndex + 1 < routine.Instructions.Count)
                {
                    yield return instructionIndex + 1;
                }
                break;
        }
    }

    private void EnqueueStackSuccessor(
        BytecodeModule module,
        BytecodeRoutine routine,
        int instructionIndex,
        int successor,
        int stackHeight,
        Dictionary<int, int> incomingHeights,
        Queue<int> pending)
    {
        if (successor < 0 || successor >= routine.Instructions.Count)
        {
            return;
        }

        if (incomingHeights.TryGetValue(successor, out var existingHeight))
        {
            if (existingHeight != stackHeight)
            {
                Report(
                    "BC4055",
                    $"Инструкция {successor} процедуры '{routine.Name}' достижима с разной высотой стека: {existingHeight} и {stackHeight}.",
                    module,
                    routine.Id,
                    successor);
            }

            return;
        }

        incomingHeights[successor] = stackHeight;
        pending.Enqueue(successor);
    }

    private void ValidateOperandCount(
        BytecodeModule module,
        BytecodeRoutine routine,
        int instructionIndex,
        BytecodeInstruction instruction,
        int expectedCount)
    {
        if (instruction.OperandCount != expectedCount)
        {
            Report(
                "BC4012",
                $"Инструкция '{instruction.Opcode}' в процедуре '{routine.Name}' ожидает {expectedCount} операнд(а), но получила {instruction.OperandCount}.",
                module,
                routine.Id,
                instructionIndex);
        }
    }

    private void ValidateOptionalJumpTarget(
        BytecodeModule module,
        BytecodeRoutine routine,
        int instructionIndex,
        BytecodeInstruction instruction,
        int operandIndex)
    {
        if (instruction.GetOperand(operandIndex) < 0)
        {
            return;
        }

        ValidateJumpTarget(module, routine, instructionIndex, instruction, operandIndex);
    }

    private void ValidateErrorHandlerTarget(
        BytecodeModule module,
        BytecodeRoutine routine,
        int instructionIndex,
        BytecodeInstruction instruction,
        IReadOnlyDictionary<int, BytecodeGlobal> globalsBySlot)
    {
        var targetKind = instruction.GetOperand(3);
        var slot = instruction.GetOperand(4);
        if (targetKind == -1 && slot == -1)
        {
            return;
        }

        switch (targetKind)
        {
            case 0:
                ValidateGlobalSlot(module, routine, instructionIndex, instruction, globalsBySlot, operandIndex: 4);
                break;

            case 1:
                ValidateLocalSlot(module, routine, instructionIndex, instruction, operandIndex: 4);
                break;

            case 2:
                ValidateArgumentSlot(module, routine, instructionIndex, instruction, operandIndex: 4);
                break;

            case 3:
                ValidateCaptureSlot(module, routine, instructionIndex, instruction, operandIndex: 4);
                break;

            default:
                Report(
                    "BC4121",
                    $"Инструкция PushErrorHandler в процедуре '{routine.Name}' содержит некорректный вид цели CATCH {targetKind}.",
                    module,
                    routine.Id,
                    instructionIndex);
                break;
        }
    }

    private void ValidateOpenFileInstruction(
        BytecodeModule module,
        BytecodeRoutine routine,
        int instructionIndex,
        BytecodeInstruction instruction)
    {
        if (instruction.OperandCount < 2)
        {
            return;
        }

        if (instruction.OperandCount == 3 && instruction.GetOperand(2) != 1)
        {
            Report("BC4023", "Инструкция OpenFile содержит неизвестный признак явной кодировки.", module, routine.Id, instructionIndex);
        }

        var mode = instruction.GetOperand(0);
        if (mode is < 0 or > 2)
        {
            Report(
                "BC4023",
                $"Инструкция OpenFile в процедуре '{routine.Name}' содержит неизвестный режим открытия файла {mode}.",
                module,
                routine.Id,
                instructionIndex);
        }

        ValidateStringConstantIndex(module, routine, instructionIndex, instruction, operandIndex: 1, "имя файловой переменной");
    }

    private void ValidatePrintUsingInstruction(
        BytecodeModule module,
        BytecodeRoutine routine,
        int instructionIndex,
        BytecodeInstruction instruction)
    {
        if (instruction.OperandCount < 2)
        {
            return;
        }

        var valueCount = instruction.GetOperand(0);
        if (valueCount < 0)
        {
            Report(
                "BC4037",
                $"Инструкция PrintUsing в процедуре '{routine.Name}' содержит отрицательное количество значений {valueCount}.",
                module,
                routine.Id,
                instructionIndex);
        }

        var separator = instruction.GetOperand(1);
        if (separator is < 0 or > 2)
        {
            Report(
                "BC4038",
                $"Инструкция PrintUsing в процедуре '{routine.Name}' содержит неизвестный разделитель {separator}.",
                module,
                routine.Id,
                instructionIndex);
        }
    }

    private void ValidatePrintSeparatorInstruction(
        BytecodeModule module,
        BytecodeRoutine routine,
        int instructionIndex,
        BytecodeInstruction instruction)
    {
        if (instruction.OperandCount < 1)
        {
            return;
        }

        var separator = instruction.GetOperand(0);
        if (separator is < 0 or > 1)
        {
            Report(
                "BC4050",
                $"Инструкция PrintSeparator в процедуре '{routine.Name}' содержит неизвестный разделитель {separator}.",
                module,
                routine.Id,
                instructionIndex);
        }
    }

    private void ValidateRegisterKeyHandlerInstruction(
        BytecodeModule module,
        BytecodeRoutine routine,
        int instructionIndex,
        BytecodeInstruction instruction)
    {
        if (instruction.OperandCount < 1)
        {
            return;
        }

        var passKeyTextFlag = instruction.GetOperand(0);
        if (passKeyTextFlag is < 0 or > 1)
        {
            Report(
                "BC4123",
                $"Инструкция RegisterKeyHandler в процедуре '{routine.Name}' содержит неизвестный флаг передачи имени клавиши {passKeyTextFlag}.",
                module,
                routine.Id,
                instructionIndex);
        }
    }

    private void ValidateRegisterMouseHandlerInstruction(
        BytecodeModule module,
        BytecodeRoutine routine,
        int instructionIndex,
        BytecodeInstruction instruction)
    {
        if (instruction.OperandCount < 1)
        {
            return;
        }

        var passEventFlag = instruction.GetOperand(0);
        if (passEventFlag is < 0 or > 1)
        {
            Report(
                "BC4125",
                $"Инструкция RegisterMouseHandler в процедуре '{routine.Name}' содержит неизвестный флаг передачи события мыши {passEventFlag}.",
                module,
                routine.Id,
                instructionIndex);
        }

        if (instruction.OperandCount < 2)
        {
            return;
        }

        ValidateMouseEventKind(module, routine, instructionIndex, instruction, operandIndex: 1);
    }

    private void ValidateSleepInstruction(
        BytecodeModule module,
        BytecodeRoutine routine,
        int instructionIndex,
        BytecodeInstruction instruction)
    {
        if (instruction.OperandCount < 1)
        {
            return;
        }

        var durationFlag = instruction.GetOperand(0);
        if (durationFlag is < 0 or > 1)
        {
            Report(
                "BC4127",
                $"Инструкция Sleep в процедуре '{routine.Name}' содержит неизвестный флаг длительности {durationFlag}.",
                module,
                routine.Id,
                instructionIndex);
        }
    }

    private void ValidatePaletteInstruction(
        BytecodeModule module,
        BytecodeRoutine routine,
        int instructionIndex,
        BytecodeInstruction instruction)
    {
        if (instruction.OperandCount < 1)
        {
            return;
        }

        var valueCount = instruction.GetOperand(0);
        if (valueCount is 0 or 1 or 2)
        {
            return;
        }

        Report(
            "BC4128",
            $"Инструкция Palette в процедуре '{routine.Name}' содержит некорректное число аргументов {valueCount}.",
            module,
            routine.Id,
            instructionIndex);
    }

    private void ValidateMouseEventKind(
        BytecodeModule module,
        BytecodeRoutine routine,
        int instructionIndex,
        BytecodeInstruction instruction,
        int operandIndex)
    {
        var eventKind = instruction.GetOperand(operandIndex);
        if (eventKind is < 0 or > 3)
        {
            Report(
                "BC4126",
                $"Инструкция {instruction.Opcode} в процедуре '{routine.Name}' содержит неизвестный вид события мыши {eventKind}.",
                module,
                routine.Id,
                instructionIndex);
        }
    }

    private void ValidateRedimArrayInstruction(
        BytecodeModule module,
        BytecodeRoutine routine,
        int instructionIndex,
        BytecodeInstruction instruction)
    {
        if (instruction.OperandCount < 3)
        {
            return;
        }

        var preserveFlag = instruction.GetOperand(0);
        if (preserveFlag is < 0 or > 1)
        {
            Report(
                "BC4051",
                $"Инструкция RedimArray в процедуре '{routine.Name}' содержит неизвестный флаг PRESERVE {preserveFlag}.",
                module,
                routine.Id,
                instructionIndex);
        }

        ValidateStringConstantIndex(module, routine, instructionIndex, instruction, operandIndex: 1, "имя массива");
        ValidatePositiveCountOperand(module, routine, instructionIndex, instruction, operandIndex: 2, code: "BC4109", label: "число измерений");
    }

    private void ValidateElementInstruction(
        BytecodeModule module,
        BytecodeRoutine routine,
        int instructionIndex,
        BytecodeInstruction instruction)
    {
        ValidatePositiveCountOperand(module, routine, instructionIndex, instruction, operandIndex: 1, code: "BC4110", label: "число индексов");

        if (instruction.Opcode == Opcode.LoadElement && instruction.OperandCount >= 3)
        {
            var indexCount = instruction.GetOperand(1);
            var rangeMask = instruction.GetOperand(2);

            if (rangeMask < 0 || (indexCount < 31 && (rangeMask >> indexCount) != 0))
            {
                Report(
                    "BC4111",
                    $"Инструкция LoadElement в процедуре '{routine.Name}' содержит некорректную маску диапазонов {rangeMask} для {indexCount} индексов.",
                    module,
                    routine.Id,
                    instructionIndex);
            }
        }
    }

    private static bool IsRangeDimension(int rangeMask, int dimension)
    {
        return (rangeMask & (1 << dimension)) != 0;
    }

    private void ValidatePositiveCountOperand(
        BytecodeModule module,
        BytecodeRoutine routine,
        int instructionIndex,
        BytecodeInstruction instruction,
        int operandIndex,
        string code,
        string label)
    {
        if (instruction.OperandCount <= operandIndex)
        {
            return;
        }

        var value = instruction.GetOperand(operandIndex);
        if (value > 0)
        {
            return;
        }

        Report(
            code,
            $"Инструкция {instruction.Opcode} в процедуре '{routine.Name}' содержит некорректное {label} {value}.",
            module,
            routine.Id,
            instructionIndex);
    }

    private void ValidateInputInstruction(
        BytecodeModule module,
        BytecodeRoutine routine,
        int instructionIndex,
        BytecodeInstruction instruction,
        IReadOnlyDictionary<int, BytecodeGlobal> globalsBySlot)
    {
        if (instruction.OperandCount < 2)
        {
            Report(
                "BC4039",
                $"Инструкция Input в процедуре '{routine.Name}' ожидает минимум 2 операнда, но получила {instruction.OperandCount}.",
                module,
                routine.Id,
                instructionIndex);
            return;
        }

        var valueCount = instruction.GetOperand(0);
        if (valueCount < 0)
        {
            Report(
                "BC4087",
                $"Инструкция Input в процедуре '{routine.Name}' содержит отрицательное количество значений {valueCount}.",
                module,
                routine.Id,
                instructionIndex);
            return;
        }

        var expectedOperandCount = 2L + (long)valueCount * 2L;
        if (expectedOperandCount > int.MaxValue)
        {
            Report(
                "BC4052",
                $"Инструкция Input в процедуре '{routine.Name}' содержит слишком большое количество значений {valueCount}.",
                module,
                routine.Id,
                instructionIndex);
            return;
        }

        if (instruction.OperandCount != expectedOperandCount)
        {
            Report(
                "BC4040",
                $"Инструкция Input в процедуре '{routine.Name}' для {valueCount} значений ожидает {expectedOperandCount} операнда, но получила {instruction.OperandCount}.",
                module,
                routine.Id,
                instructionIndex);
            return;
        }

        ValidateInputPromptOperand(module, routine, instructionIndex, instruction);

        for (var targetIndex = 0; targetIndex < valueCount; targetIndex++)
        {
            var operandOffset = 2 + targetIndex * 2;
            ValidateInputTarget(module, routine, instructionIndex, instruction, operandOffset, globalsBySlot);
        }
    }

    private void ValidateInputPromptOperand(
        BytecodeModule module,
        BytecodeRoutine routine,
        int instructionIndex,
        BytecodeInstruction instruction)
    {
        var promptOperand = instruction.GetOperand(1);
        if (promptOperand == 0)
        {
            return;
        }

        var constantIndex = promptOperand - 1;
        if (!TryGetConstant(module, routine, instructionIndex, instruction.Opcode, constantIndex, out var constant))
        {
            return;
        }

        if (constant!.Kind != BytecodeConstantKind.String)
        {
            Report(
                "BC4048",
                $"Инструкция Input в процедуре '{routine.Name}' ожидает строковую константу подсказки, но получила '{constant.Kind}'.",
                module,
                routine.Id,
                instructionIndex);
        }
    }

    private void ValidateInputTarget(
        BytecodeModule module,
        BytecodeRoutine routine,
        int instructionIndex,
        BytecodeInstruction instruction,
        int operandOffset,
        IReadOnlyDictionary<int, BytecodeGlobal> globalsBySlot)
    {
        var targetKind = instruction.GetOperand(operandOffset);
        var slot = instruction.GetOperand(operandOffset + 1);

        switch (targetKind)
        {
            case 0:
                if (slot < 0)
                {
                    Report(
                        "BC4083",
                        $"Инструкция Input в процедуре '{routine.Name}' ссылается на отрицательный глобальный слот {slot}.",
                        module,
                        routine.Id,
                        instructionIndex);
                    break;
                }

                if (!globalsBySlot.TryGetValue(slot, out var global))
                {
                    Report(
                        "BC4042",
                        $"Инструкция Input в процедуре '{routine.Name}' ссылается на несуществующий глобальный слот {slot}.",
                        module,
                        routine.Id,
                        instructionIndex);
                    break;
                }

                ValidateInputTargetType(module, routine, instructionIndex, global.Name, global.Type);
                break;

            case 1:
                if (slot < 0)
                {
                    Report(
                        "BC4084",
                        $"Инструкция Input в процедуре '{routine.Name}' ссылается на отрицательный локальный слот {slot}.",
                        module,
                        routine.Id,
                        instructionIndex);
                    break;
                }

                if (slot >= routine.LocalSlotCount)
                {
                    Report(
                        "BC4043",
                        $"Инструкция Input в процедуре '{routine.Name}' ссылается на несуществующий локальный слот {slot}.",
                        module,
                        routine.Id,
                        instructionIndex);
                    break;
                }

                ValidateInputTargetType(module, routine, instructionIndex, routine.LocalVariables[slot].Name, routine.LocalVariables[slot].Type);
                break;

            case 2:
                if (slot < 0)
                {
                    Report(
                        "BC4085",
                        $"Инструкция Input в процедуре '{routine.Name}' ссылается на отрицательный слот аргумента {slot}.",
                        module,
                        routine.Id,
                        instructionIndex);
                    break;
                }

                if (slot >= routine.ParameterCount)
                {
                    Report(
                        "BC4044",
                        $"Инструкция Input в процедуре '{routine.Name}' ссылается на несуществующий слот аргумента {slot}.",
                        module,
                        routine.Id,
                        instructionIndex);
                    break;
                }

                ValidateInputTargetType(module, routine, instructionIndex, routine.ParameterVariables[slot].Name, routine.ParameterVariables[slot].Type);
                break;

            case 3:
                if (slot < 0)
                {
                    Report(
                        "BC4086",
                        $"Инструкция Input в процедуре '{routine.Name}' ссылается на отрицательный захваченный слот {slot}.",
                        module,
                        routine.Id,
                        instructionIndex);
                    break;
                }

                if (slot >= routine.CapturedVariables.Count)
                {
                    Report(
                        "BC4045",
                        $"Инструкция Input в процедуре '{routine.Name}' ссылается на несуществующий захваченный слот {slot}.",
                        module,
                        routine.Id,
                        instructionIndex);
                    break;
                }

                ValidateInputTargetType(module, routine, instructionIndex, routine.CapturedVariables[slot].Name, routine.CapturedVariables[slot].Type);
                break;

            default:
                Report(
                    "BC4046",
                    $"Инструкция Input в процедуре '{routine.Name}' содержит неизвестный вид цели {targetKind}.",
                    module,
                    routine.Id,
                    instructionIndex);
                break;
        }
    }

    private void ValidateInputTargetType(
        BytecodeModule module,
        BytecodeRoutine routine,
        int instructionIndex,
        string targetName,
        TypeSymbol targetType)
    {
        if (IsSupportedInputTargetType(targetType))
        {
            return;
        }

        Report(
            "BC4110",
            $"Инструкция Input в процедуре '{routine.Name}' ссылается на цель '{targetName}' неподдерживаемого типа {targetType.Name}.",
            module,
            routine.Id,
            instructionIndex);
    }

    private static bool IsSupportedInputTargetType(TypeSymbol type)
    {
        return type == TypeSymbol.Integer
            || type == TypeSymbol.Long
            || type == TypeSymbol.Double
            || type == TypeSymbol.Decimal
            || type == TypeSymbol.String
            || type == TypeSymbol.Boolean
            || type == TypeSymbol.Date
            || type == TypeSymbol.Time
            || type == TypeSymbol.DateTime
            || type == TypeSymbol.UtcDateTime;
    }

    private void ValidateConversionInstruction(
        BytecodeModule module,
        BytecodeRoutine routine,
        int instructionIndex,
        BytecodeInstruction instruction)
    {
        if (instruction.OperandCount == 0)
        {
            return;
        }

        var typeCode = instruction.GetOperand(0);
        if (!Enum.IsDefined(typeof(BytecodeScalarTypeCode), typeCode))
        {
            Report(
                "BC4047",
                $"Инструкция Convert в процедуре '{routine.Name}' содержит неизвестный код типа {typeCode}.",
                module,
                routine.Id,
                instructionIndex);
        }
    }

    private void ValidatePushConstant(
        BytecodeModule module,
        BytecodeRoutine routine,
        int instructionIndex,
        BytecodeInstruction instruction)
    {
        if (!TryGetConstant(module, routine, instructionIndex, instruction, operandIndex: 0, out var constant))
        {
            return;
        }

        if (constant!.Kind is BytecodeConstantKind.RoutineName or BytecodeConstantKind.BuiltinName)
        {
            Report(
                "BC4049",
                $"Инструкция PushConst в процедуре '{routine.Name}' ссылается на служебную константу вида '{constant.Kind}', а не на значение времени выполнения.",
                module,
                routine.Id,
                instructionIndex);
        }
    }

    private void ValidateStringConstantIndex(
        BytecodeModule module,
        BytecodeRoutine routine,
        int instructionIndex,
        BytecodeInstruction instruction,
        int operandIndex,
        string role)
    {
        if (!TryGetConstant(module, routine, instructionIndex, instruction, operandIndex, out var constant))
        {
            return;
        }

        if (constant!.Kind != BytecodeConstantKind.String)
        {
            Report(
                "BC4048",
                $"Инструкция '{instruction.Opcode}' в процедуре '{routine.Name}' ожидает строковую константу для роли '{role}', но получила '{constant.Kind}'.",
                module,
                routine.Id,
                instructionIndex);
        }
    }

    private bool TryGetConstant(
        BytecodeModule module,
        BytecodeRoutine routine,
        int instructionIndex,
        BytecodeInstruction instruction,
        int operandIndex,
        out BytecodeConstant? constant)
    {
        if (instruction.OperandCount <= operandIndex)
        {
            constant = null;
            return false;
        }

        var constantIndex = instruction.GetOperand(operandIndex);
        return TryGetConstant(module, routine, instructionIndex, instruction.Opcode, constantIndex, out constant);
    }

    private bool TryGetConstant(
        BytecodeModule module,
        BytecodeRoutine routine,
        int instructionIndex,
        Opcode opcode,
        int constantIndex,
        out BytecodeConstant? constant)
    {
        if (constantIndex < 0)
        {
            constant = null;
            Report(
                "BC4081",
                $"Инструкция '{opcode}' в процедуре '{routine.Name}' ссылается на отрицательный индекс константы {constantIndex}.",
                module,
                routine.Id,
                instructionIndex);
            return false;
        }

        if (constantIndex >= module.ConstantPool.Count)
        {
            constant = null;
            Report(
                "BC4013",
                $"Инструкция '{opcode}' в процедуре '{routine.Name}' ссылается на несуществующую константу {constantIndex}.",
                module,
                routine.Id,
                instructionIndex);
            return false;
        }

        constant = module.ConstantPool[constantIndex];
        return true;
    }

    private void ValidateGlobalSlot(
        BytecodeModule module,
        BytecodeRoutine routine,
        int instructionIndex,
        BytecodeInstruction instruction,
        IReadOnlyDictionary<int, BytecodeGlobal> globalsBySlot,
        int operandIndex = 0)
    {
        if (instruction.OperandCount <= operandIndex)
        {
            return;
        }

        var slot = instruction.GetOperand(operandIndex);
        if (slot < 0)
        {
            Report(
                "BC4083",
                $"Инструкция '{instruction.Opcode}' в процедуре '{routine.Name}' ссылается на отрицательный глобальный слот {slot}.",
                module,
                routine.Id,
                instructionIndex);
            return;
        }

        if (!globalsBySlot.ContainsKey(slot))
        {
            Report(
                "BC4014",
                $"Инструкция '{instruction.Opcode}' в процедуре '{routine.Name}' ссылается на несуществующий глобальный слот {slot}.",
                module,
                routine.Id,
                instructionIndex);
        }
    }

    private void ValidateLocalSlot(BytecodeModule module, BytecodeRoutine routine, int instructionIndex, BytecodeInstruction instruction, int operandIndex = 0)
    {
        if (instruction.OperandCount <= operandIndex)
        {
            return;
        }

        var slot = instruction.GetOperand(operandIndex);
        if (slot < 0)
        {
            Report(
                "BC4084",
                $"Инструкция '{instruction.Opcode}' в процедуре '{routine.Name}' ссылается на отрицательный локальный слот {slot}.",
                module,
                routine.Id,
                instructionIndex);
            return;
        }

        if (slot >= routine.LocalSlotCount)
        {
            Report(
                "BC4015",
                $"Инструкция '{instruction.Opcode}' в процедуре '{routine.Name}' ссылается на несуществующий локальный слот {slot}.",
                module,
                routine.Id,
                instructionIndex);
        }
    }

    private void ValidateArgumentSlot(BytecodeModule module, BytecodeRoutine routine, int instructionIndex, BytecodeInstruction instruction, int operandIndex = 0)
    {
        if (instruction.OperandCount <= operandIndex)
        {
            return;
        }

        var slot = instruction.GetOperand(operandIndex);
        if (slot < 0)
        {
            Report(
                "BC4085",
                $"Инструкция '{instruction.Opcode}' в процедуре '{routine.Name}' ссылается на отрицательный слот аргумента {slot}.",
                module,
                routine.Id,
                instructionIndex);
            return;
        }

        if (slot >= routine.ParameterCount)
        {
            Report(
                "BC4016",
                $"Инструкция '{instruction.Opcode}' в процедуре '{routine.Name}' ссылается на несуществующий аргумент {slot}.",
                module,
                routine.Id,
                instructionIndex);
        }
    }

    private void ValidateCaptureSlot(BytecodeModule module, BytecodeRoutine routine, int instructionIndex, BytecodeInstruction instruction, int operandIndex = 0)
    {
        if (instruction.OperandCount <= operandIndex)
        {
            return;
        }

        var slot = instruction.GetOperand(operandIndex);
        if (slot < 0)
        {
            Report(
                "BC4086",
                $"Инструкция '{instruction.Opcode}' в процедуре '{routine.Name}' ссылается на отрицательный захваченный слот {slot}.",
                module,
                routine.Id,
                instructionIndex);
            return;
        }

        if (slot >= routine.CapturedVariables.Count)
        {
            Report(
                "BC4022",
                $"Инструкция '{instruction.Opcode}' в процедуре '{routine.Name}' ссылается на несуществующий захваченный слот {slot}.",
                module,
                routine.Id,
                instructionIndex);
        }
    }

    private void ValidateJumpTarget(BytecodeModule module, BytecodeRoutine routine, int instructionIndex, BytecodeInstruction instruction, int operandIndex = 0)
    {
        if (instruction.OperandCount <= operandIndex)
        {
            return;
        }

        var target = instruction.GetOperand(operandIndex);
        if (target < 0)
        {
            Report(
                "BC4082",
                $"Инструкция '{instruction.Opcode}' в процедуре '{routine.Name}' ссылается на отрицательную цель перехода {target}.",
                module,
                routine.Id,
                instructionIndex);
            return;
        }

        if (target >= routine.Instructions.Count)
        {
            Report(
                "BC4017",
                $"Инструкция '{instruction.Opcode}' в процедуре '{routine.Name}' ссылается на несуществующую цель перехода {target}.",
                module,
                routine.Id,
                instructionIndex);
        }
    }

    private void ValidateRoutineCall(
        BytecodeModule module,
        BytecodeRoutine routine,
        int instructionIndex,
        BytecodeInstruction instruction,
        IReadOnlyDictionary<int, BytecodeRoutine> routinesById)
    {
        if (instruction.OperandCount < 2)
        {
            return;
        }

        var targetRoutineId = instruction.GetOperand(0);
        var argumentCount = instruction.GetOperand(1);
        var isByRefCall = instruction.Opcode == Opcode.CallByRef;
        var byValArgumentCount = isByRefCall && instruction.OperandCount >= 4 ? instruction.GetOperand(2) : argumentCount;
        var byRefCount = isByRefCall && instruction.OperandCount >= 4 ? instruction.GetOperand(3) : 0;

        if (targetRoutineId < 0)
        {
            Report("BC4104", $"Инструкция {instruction.Opcode} в процедуре '{routine.Name}' ссылается на отрицательный routine-id {targetRoutineId}.", module, routine.Id, instructionIndex);
            return;
        }

        if (argumentCount < 0)
        {
            Report("BC4105", $"Инструкция {instruction.Opcode} в процедуре '{routine.Name}' содержит отрицательное число аргументов {argumentCount}.", module, routine.Id, instructionIndex);
            return;
        }

        if (!routinesById.TryGetValue(targetRoutineId, out var targetRoutine))
        {
            Report("BC4018", $"Инструкция {instruction.Opcode} в процедуре '{routine.Name}' ссылается на неизвестную процедуру {targetRoutineId}.", module, routine.Id, instructionIndex);
            return;
        }

        if (argumentCount != targetRoutine.ParameterCount)
        {
            Report("BC4019", $"Инструкция {instruction.Opcode} в процедуре '{routine.Name}' передаёт {argumentCount} аргумент(а/ов) в '{targetRoutine.Name}', но сигнатура требует {targetRoutine.ParameterCount}.", module, routine.Id, instructionIndex);
        }

        if (isByRefCall)
        {
            var operandIndex = 4;
            for (var index = 0; index < byRefCount; index++)
            {
                if (!TryReadByRefArgumentLayout(instruction, ref operandIndex, argumentCount, out var error))
                {
                    Report("BC4119", $"Инструкция CallByRef в процедуре '{routine.Name}' имеет некорректную раскладку BYREF-аргумента {index + 1}: {error}", module, routine.Id, instructionIndex);
                    return;
                }
            }

            if (operandIndex != instruction.OperandCount)
            {
                Report("BC4119", $"Инструкция CallByRef в процедуре '{routine.Name}' содержит {instruction.OperandCount} операнд(а/ов), но по раскладке используется {operandIndex}.", module, routine.Id, instructionIndex);
                return;
            }

            if (byValArgumentCount < 0 || byValArgumentCount + byRefCount != argumentCount)
            {
                Report("BC4121", $"Инструкция CallByRef в процедуре '{routine.Name}' содержит несовместимую раскладку BYVAL/BYREF для арности {argumentCount}.", module, routine.Id, instructionIndex);
            }
        }

        ValidateTargetCapturesVisible(module, routine, instructionIndex, instruction.Opcode, targetRoutine);
    }

    private static bool TryReadByRefArgumentLayout(
        BytecodeInstruction instruction,
        ref int operandIndex,
        int argumentCount,
        out string error)
    {
        if (operandIndex + 2 > instruction.OperandCount)
        {
            error = "не хватает операндов для индекса и вида источника.";
            return false;
        }

        var argumentIndex = instruction.GetOperand(operandIndex++);
        var sourceKind = instruction.GetOperand(operandIndex++);

        if (argumentIndex < 0 || argumentIndex >= argumentCount)
        {
            error = $"индекс аргумента {argumentIndex} вне диапазона 0..{argumentCount - 1}.";
            return false;
        }

        if (sourceKind is < 0 or > 3 and not ByRefArgumentSourceKind.Address)
        {
            error = $"неизвестный вид источника {sourceKind}.";
            return false;
        }

        if (sourceKind != ByRefArgumentSourceKind.Address)
        {
            if (operandIndex + 1 > instruction.OperandCount)
            {
                error = "не указан слот источника.";
                return false;
            }

            _ = instruction.GetOperand(operandIndex++);
            error = string.Empty;
            return true;
        }

        if (operandIndex + 3 > instruction.OperandCount)
        {
            error = "не хватает операндов описания адреса.";
            return false;
        }

        var baseKind = instruction.GetOperand(operandIndex++);
        _ = instruction.GetOperand(operandIndex++);
        var stepCount = instruction.GetOperand(operandIndex++);

        if (baseKind is < 0 or > 3)
        {
            error = $"неизвестный вид базовой переменной адреса {baseKind}.";
            return false;
        }

        if (stepCount <= 0)
        {
            error = "адрес не содержит шагов.";
            return false;
        }

        for (var stepIndex = 0; stepIndex < stepCount; stepIndex++)
        {
            if (operandIndex + 2 > instruction.OperandCount)
            {
                error = "не хватает операндов шага адреса.";
                return false;
            }

            var stepKind = instruction.GetOperand(operandIndex++);

            if (stepKind == 0)
            {
                _ = instruction.GetOperand(operandIndex++);
                continue;
            }

            if (stepKind != 1)
            {
                error = $"неизвестный вид шага адреса {stepKind}.";
                return false;
            }

            var indexCount = instruction.GetOperand(operandIndex++);

            if (indexCount <= 0 || operandIndex + (indexCount * 2) > instruction.OperandCount)
            {
                error = "некорректное число индексов шага адреса.";
                return false;
            }

            for (var indexIndex = 0; indexIndex < indexCount; indexIndex++)
            {
                var indexKind = instruction.GetOperand(operandIndex++);
                _ = instruction.GetOperand(operandIndex++);

                if (indexKind is < 0 or > 3)
                {
                    error = $"неизвестный вид источника индекса {indexKind}.";
                    return false;
                }
            }
        }

        error = string.Empty;
        return true;
    }

    private void ValidateFunctionValueCall(
        BytecodeModule module,
        BytecodeRoutine routine,
        int instructionIndex,
        BytecodeInstruction instruction)
    {
        if (instruction.OperandCount < 2)
        {
            return;
        }

        var argumentCount = instruction.GetOperand(0);
        var returnsValue = instruction.GetOperand(1);

        if (argumentCount < 0)
        {
            Report(
                "BC4105",
                $"Инструкция CallValue в процедуре '{routine.Name}' содержит отрицательное число аргументов {argumentCount}.",
                module,
                routine.Id,
                instructionIndex);
        }

        if (returnsValue is not 0 and not 1)
        {
            Report(
                "BC4118",
                $"Инструкция CallValue в процедуре '{routine.Name}' содержит недопустимый признак возвращаемого значения {returnsValue}.",
                module,
                routine.Id,
                instructionIndex);
        }
    }

    private void ValidateRoutineReference(
        BytecodeModule module,
        BytecodeRoutine routine,
        int instructionIndex,
        BytecodeInstruction instruction,
        IReadOnlyDictionary<int, BytecodeRoutine> routinesById)
    {
        if (instruction.OperandCount == 0)
        {
            return;
        }

        var targetRoutineId = instruction.GetOperand(0);
        if (targetRoutineId < 0)
        {
            Report(
                "BC4104",
                $"Инструкция LoadFunction в процедуре '{routine.Name}' ссылается на отрицательный routine-id {targetRoutineId}.",
                module,
                routine.Id,
                instructionIndex);
            return;
        }

        if (!routinesById.ContainsKey(targetRoutineId))
        {
            Report(
                "BC4023",
                $"Инструкция LoadFunction в процедуре '{routine.Name}' ссылается на неизвестную процедуру {targetRoutineId}.",
                module,
                routine.Id,
                instructionIndex);
            return;
        }

        var targetRoutine = routinesById[targetRoutineId];
        if (targetRoutine.Symbol is null)
        {
            Report(
                "BC4106",
                $"Инструкция LoadFunction в процедуре '{routine.Name}' ссылается на процедуру '{targetRoutine.Name}' без сохранённого символа функции.",
                module,
                routine.Id,
                instructionIndex);
        }

        ValidateTargetCapturesVisible(module, routine, instructionIndex, instruction.Opcode, targetRoutine);
    }

    private void ValidateTargetCapturesVisible(
        BytecodeModule module,
        BytecodeRoutine routine,
        int instructionIndex,
        Opcode opcode,
        BytecodeRoutine targetRoutine)
    {
        if (targetRoutine.CapturedVariables.Count == 0)
        {
            return;
        }

        var visibleVariables = new HashSet<VariableSymbol>();
        AddVisibleVariables(routine.ParameterVariables);
        AddVisibleVariables(routine.LocalVariables);
        AddVisibleVariables(routine.CapturedVariables);

        foreach (var capturedVariable in targetRoutine.CapturedVariables)
        {
            if (visibleVariables.Contains(capturedVariable))
            {
                continue;
            }

            Report(
                "BC4117",
                $"Инструкция '{opcode}' в процедуре '{routine.Name}' ссылается на процедуру '{targetRoutine.Name}', но захваченная переменная '{capturedVariable.Name}' не видна из текущей процедуры.",
                module,
                routine.Id,
                instructionIndex);
        }

        void AddVisibleVariables(IReadOnlyList<VariableSymbol> variables)
        {
            foreach (var variable in variables)
            {
                visibleVariables.Add(variable);
            }
        }
    }

    private void ValidateBuiltinCall(
        BytecodeModule module,
        BytecodeRoutine routine,
        int instructionIndex,
        BytecodeInstruction instruction,
        IReadOnlyDictionary<int, BuiltinBinding> builtinsById)
    {
        if (instruction.OperandCount < 2)
        {
            return;
        }

        var builtinId = instruction.GetOperand(0);
        var argumentCount = instruction.GetOperand(1);

        if (builtinId < 0)
        {
            Report(
                "BC4107",
                $"Инструкция CallBuiltin в процедуре '{routine.Name}' ссылается на отрицательный built-in id {builtinId}.",
                module,
                routine.Id,
                instructionIndex);
            return;
        }

        if (argumentCount < 0)
        {
            Report(
                "BC4108",
                $"Инструкция CallBuiltin в процедуре '{routine.Name}' содержит отрицательное число аргументов {argumentCount}.",
                module,
                routine.Id,
                instructionIndex);
            return;
        }

        if (!builtinsById.TryGetValue(builtinId, out var builtin))
        {
            Report(
                "BC4020",
                $"Инструкция CallBuiltin в процедуре '{routine.Name}' ссылается на неизвестный built-in {builtinId}.",
                module,
                routine.Id,
                instructionIndex);
            return;
        }

        if (argumentCount != builtin.ParameterCount)
        {
            Report(
                "BC4021",
                $"Инструкция CallBuiltin в процедуре '{routine.Name}' передаёт {argumentCount} аргумент(а/ов) в '{builtin.Name}', но таблица built-in требует {builtin.ParameterCount}.",
                module,
                routine.Id,
                instructionIndex);
        }
    }

    private void ValidateBuiltinReference(
        BytecodeModule module,
        BytecodeRoutine routine,
        int instructionIndex,
        BytecodeInstruction instruction,
        IReadOnlyDictionary<int, BuiltinBinding> builtinsById)
    {
        if (instruction.OperandCount < 1)
        {
            return;
        }

        var builtinId = instruction.GetOperand(0);
        if (builtinId < 0)
        {
            Report(
                "BC4107",
                $"Инструкция LoadBuiltinFunction в процедуре '{routine.Name}' ссылается на отрицательный built-in id {builtinId}.",
                module,
                routine.Id,
                instructionIndex);
            return;
        }

        if (!builtinsById.TryGetValue(builtinId, out var builtin))
        {
            Report(
                "BC4024",
                $"Инструкция LoadBuiltinFunction в процедуре '{routine.Name}' ссылается на неизвестный built-in {builtinId}.",
                module,
                routine.Id,
                instructionIndex);
            return;
        }

        if (builtin.Function is null)
        {
            Report(
                "BC4109",
                $"Инструкция LoadBuiltinFunction в процедуре '{routine.Name}' ссылается на built-in '{builtin.Name}' без сохранённого символа функции.",
                module,
                routine.Id,
                instructionIndex);
        }
    }

    private void Report(string code, string message, BytecodeModule module, int? routineId = null, int? instructionIndex = null)
    {
        var location = ResolveLocation(module, routineId, instructionIndex);
        _diagnostics.Add(new BytecodeDiagnostic(code, message, location));
    }

    private static TextLocation ResolveLocation(BytecodeModule module, int? routineId, int? instructionIndex)
    {
        if (routineId is not null)
        {
            var routine = module.Routines.FirstOrDefault(candidate => candidate.Id == routineId.Value);
            if (routine is not null)
            {
                if (instructionIndex is not null
                    && module.DebugInfo is not null
                    && module.DebugInfo.TryGetLocation(routine.Id, instructionIndex.Value, out var instructionLocation))
                {
                    return instructionLocation;
                }

                return routine.Location;
            }
        }

        return UnknownLocation;
    }
}



