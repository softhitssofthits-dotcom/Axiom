using Axiom.Semantics;

namespace Axiom.Bytecode;

public sealed partial class BytecodeModuleValidator
{
    private void ApplyCanvasStackTypeEffect(
        BytecodeModule module,
        BytecodeRoutine routine,
        int instructionIndex,
        BytecodeInstruction instruction,
        List<TypeSymbol?> workingStack)
    {
        var heightType = PopStackType(workingStack);
        var widthType = PopStackType(workingStack);
        ValidateExpectedIntegralStackType(module, routine, instructionIndex, instruction.Opcode, widthType, "ширина CANVAS");
        ValidateExpectedIntegralStackType(module, routine, instructionIndex, instruction.Opcode, heightType, "высота CANVAS");
    }

    private void ApplyCameraStackTypeEffect(
        BytecodeModule module,
        BytecodeRoutine routine,
        int instructionIndex,
        BytecodeInstruction instruction,
        List<TypeSymbol?> workingStack)
    {
        var yType = PopStackType(workingStack);
        var xType = PopStackType(workingStack);
        ValidateExpectedIntegralStackType(module, routine, instructionIndex, instruction.Opcode, xType, "координата X CAMERA");
        ValidateExpectedIntegralStackType(module, routine, instructionIndex, instruction.Opcode, yType, "координата Y CAMERA");
    }

    private void ApplyViewportStackTypeEffect(
        BytecodeModule module,
        BytecodeRoutine routine,
        int instructionIndex,
        BytecodeInstruction instruction,
        List<TypeSymbol?> workingStack)
    {
        var valueCount = instruction.GetOperand(0);
        if (valueCount != 0 && valueCount != 4)
        {
            Report(
                "BC4124",
                $"Инструкция Viewport в процедуре '{routine.Name}' ожидает 0 или 4 значений, но содержит {valueCount}.",
                module,
                routine.Id,
                instructionIndex);
            return;
        }

        if (valueCount == 4)
        {
            var bottomType = PopStackType(workingStack);
            var rightType = PopStackType(workingStack);
            var topType = PopStackType(workingStack);
            var leftType = PopStackType(workingStack);
            ValidateExpectedIntegralStackType(module, routine, instructionIndex, instruction.Opcode, leftType, "левая граница VIEWPORT");
            ValidateExpectedIntegralStackType(module, routine, instructionIndex, instruction.Opcode, topType, "верхняя граница VIEWPORT");
            ValidateExpectedIntegralStackType(module, routine, instructionIndex, instruction.Opcode, rightType, "правая граница VIEWPORT");
            ValidateExpectedIntegralStackType(module, routine, instructionIndex, instruction.Opcode, bottomType, "нижняя граница VIEWPORT");
        }

    }

    private void ApplyDrawRectStackTypeEffect(
        BytecodeModule module,
        BytecodeRoutine routine,
        int instructionIndex,
        BytecodeInstruction instruction,
        List<TypeSymbol?> workingStack)
    {
        var colorType = PopStackType(workingStack);
        var heightType = PopStackType(workingStack);
        var widthType = PopStackType(workingStack);
        var yType = PopStackType(workingStack);
        var xType = PopStackType(workingStack);
        ValidateExpectedIntegralStackType(module, routine, instructionIndex, instruction.Opcode, xType, "координата X");
        ValidateExpectedIntegralStackType(module, routine, instructionIndex, instruction.Opcode, yType, "координата Y");
        ValidateExpectedIntegralStackType(module, routine, instructionIndex, instruction.Opcode, widthType, "ширина");
        ValidateExpectedIntegralStackType(module, routine, instructionIndex, instruction.Opcode, heightType, "высота");
        ValidateExpectedStackType(module, routine, instructionIndex, instruction.Opcode, colorType, TypeSymbol.Integer, "цвет");
    }

    private void ApplyDrawLineStackTypeEffect(
        BytecodeModule module,
        BytecodeRoutine routine,
        int instructionIndex,
        BytecodeInstruction instruction,
        List<TypeSymbol?> workingStack)
    {
        var colorType = PopStackType(workingStack);
        var y2Type = PopStackType(workingStack);
        var x2Type = PopStackType(workingStack);
        var y1Type = PopStackType(workingStack);
        var x1Type = PopStackType(workingStack);
        ValidateExpectedIntegralStackType(module, routine, instructionIndex, instruction.Opcode, x1Type, "координата X1");
        ValidateExpectedIntegralStackType(module, routine, instructionIndex, instruction.Opcode, y1Type, "координата Y1");
        ValidateExpectedIntegralStackType(module, routine, instructionIndex, instruction.Opcode, x2Type, "координата X2");
        ValidateExpectedIntegralStackType(module, routine, instructionIndex, instruction.Opcode, y2Type, "координата Y2");
        ValidateExpectedStackType(module, routine, instructionIndex, instruction.Opcode, colorType, TypeSymbol.Integer, "цвет");
    }

    private void ApplyDrawCircleStackTypeEffect(
        BytecodeModule module,
        BytecodeRoutine routine,
        int instructionIndex,
        BytecodeInstruction instruction,
        List<TypeSymbol?> workingStack)
    {
        var colorType = PopStackType(workingStack);
        var radiusType = PopStackType(workingStack);
        var yType = PopStackType(workingStack);
        var xType = PopStackType(workingStack);
        ValidateExpectedIntegralStackType(module, routine, instructionIndex, instruction.Opcode, xType, "координата X");
        ValidateExpectedIntegralStackType(module, routine, instructionIndex, instruction.Opcode, yType, "координата Y");
        ValidateExpectedIntegralStackType(module, routine, instructionIndex, instruction.Opcode, radiusType, "радиус");
        ValidateExpectedStackType(module, routine, instructionIndex, instruction.Opcode, colorType, TypeSymbol.Integer, "цвет");
    }

    private void ApplyDrawTextStackTypeEffect(
        BytecodeModule module,
        BytecodeRoutine routine,
        int instructionIndex,
        BytecodeInstruction instruction,
        List<TypeSymbol?> workingStack)
    {
        var colorType = PopStackType(workingStack);
        var yType = PopStackType(workingStack);
        var xType = PopStackType(workingStack);
        var textType = PopStackType(workingStack);
        ValidateExpectedStackType(module, routine, instructionIndex, instruction.Opcode, textType, TypeSymbol.String, "текст");
        ValidateExpectedIntegralStackType(module, routine, instructionIndex, instruction.Opcode, xType, "координата X");
        ValidateExpectedIntegralStackType(module, routine, instructionIndex, instruction.Opcode, yType, "координата Y");
        ValidateExpectedStackType(module, routine, instructionIndex, instruction.Opcode, colorType, TypeSymbol.Integer, "цвет");
    }

    private void ApplyDrawImageStackTypeEffect(
        BytecodeModule module,
        BytecodeRoutine routine,
        int instructionIndex,
        BytecodeInstruction instruction,
        List<TypeSymbol?> workingStack)
    {
        var hasScale = instruction.GetOperand(0) != 0;
        if (hasScale)
        {
            var heightType = PopStackType(workingStack);
            var widthType = PopStackType(workingStack);
            ValidateExpectedIntegralStackType(module, routine, instructionIndex, instruction.Opcode, widthType, "ширина DRAWIMAGE");
            ValidateExpectedIntegralStackType(module, routine, instructionIndex, instruction.Opcode, heightType, "высота DRAWIMAGE");
        }

        var yType = PopStackType(workingStack);
        var xType = PopStackType(workingStack);
        var imageType = PopStackType(workingStack);
        ValidateExpectedStackType(module, routine, instructionIndex, instruction.Opcode, imageType, TypeSymbol.Image, "изображение DRAWIMAGE");
        ValidateExpectedIntegralStackType(module, routine, instructionIndex, instruction.Opcode, xType, "координата X DRAWIMAGE");
        ValidateExpectedIntegralStackType(module, routine, instructionIndex, instruction.Opcode, yType, "координата Y DRAWIMAGE");
    }

    private void ApplyDrawSpriteStackTypeEffect(
        BytecodeModule module,
        BytecodeRoutine routine,
        int instructionIndex,
        BytecodeInstruction instruction,
        List<TypeSymbol?> workingStack)
    {
        var hasScale = instruction.GetOperand(0) != 0;
        if (hasScale)
        {
            var heightType = PopStackType(workingStack);
            var widthType = PopStackType(workingStack);
            ValidateExpectedIntegralStackType(module, routine, instructionIndex, instruction.Opcode, widthType, "ширина DRAWSPRITE");
            ValidateExpectedIntegralStackType(module, routine, instructionIndex, instruction.Opcode, heightType, "высота DRAWSPRITE");
        }

        var yType = PopStackType(workingStack);
        var xType = PopStackType(workingStack);
        var frameType = PopStackType(workingStack);
        var spriteType = PopStackType(workingStack);
        ValidateExpectedStackType(module, routine, instructionIndex, instruction.Opcode, spriteType, TypeSymbol.Sprite, "спрайт DRAWSPRITE");
        ValidateExpectedIntegralStackType(module, routine, instructionIndex, instruction.Opcode, frameType, "номер кадра DRAWSPRITE");
        ValidateExpectedIntegralStackType(module, routine, instructionIndex, instruction.Opcode, xType, "координата X DRAWSPRITE");
        ValidateExpectedIntegralStackType(module, routine, instructionIndex, instruction.Opcode, yType, "координата Y DRAWSPRITE");
    }

    private void ApplyDrawTileStackTypeEffect(
        BytecodeModule module,
        BytecodeRoutine routine,
        int instructionIndex,
        BytecodeInstruction instruction,
        List<TypeSymbol?> workingStack)
    {
        var yType = PopStackType(workingStack);
        var xType = PopStackType(workingStack);
        var tileIndexType = PopStackType(workingStack);
        var tilesetType = PopStackType(workingStack);
        ValidateExpectedStackType(module, routine, instructionIndex, instruction.Opcode, tilesetType, TypeSymbol.Tileset, "тайлсет DRAWTILE");
        ValidateExpectedIntegralStackType(module, routine, instructionIndex, instruction.Opcode, tileIndexType, "номер тайла DRAWTILE");
        ValidateExpectedIntegralStackType(module, routine, instructionIndex, instruction.Opcode, xType, "координата X DRAWTILE");
        ValidateExpectedIntegralStackType(module, routine, instructionIndex, instruction.Opcode, yType, "координата Y DRAWTILE");
    }

    private void ApplyDrawTilemapStackTypeEffect(
        BytecodeModule module,
        BytecodeRoutine routine,
        int instructionIndex,
        BytecodeInstruction instruction,
        List<TypeSymbol?> workingStack)
    {
        var yType = PopStackType(workingStack);
        var xType = PopStackType(workingStack);
        var tilemapType = PopStackType(workingStack);
        ValidateExpectedStackType(module, routine, instructionIndex, instruction.Opcode, tilemapType, TypeSymbol.Tilemap, "тайловая карта DRAWTILEMAP");
        ValidateExpectedIntegralStackType(module, routine, instructionIndex, instruction.Opcode, xType, "координата X DRAWTILEMAP");
        ValidateExpectedIntegralStackType(module, routine, instructionIndex, instruction.Opcode, yType, "координата Y DRAWTILEMAP");
    }

    private void ApplyDrawTileLayerStackTypeEffect(
        BytecodeModule module,
        BytecodeRoutine routine,
        int instructionIndex,
        BytecodeInstruction instruction,
        List<TypeSymbol?> workingStack)
    {
        var yType = PopStackType(workingStack);
        var xType = PopStackType(workingStack);
        var layerIndexType = PopStackType(workingStack);
        var tilemapType = PopStackType(workingStack);
        ValidateExpectedStackType(module, routine, instructionIndex, instruction.Opcode, tilemapType, TypeSymbol.Tilemap, "тайловая карта DRAWTILELAYER");
        ValidateExpectedIntegralStackType(module, routine, instructionIndex, instruction.Opcode, layerIndexType, "номер слоя DRAWTILELAYER");
        ValidateExpectedIntegralStackType(module, routine, instructionIndex, instruction.Opcode, xType, "координата X DRAWTILELAYER");
        ValidateExpectedIntegralStackType(module, routine, instructionIndex, instruction.Opcode, yType, "координата Y DRAWTILELAYER");
    }

    private void ApplySetMapTileStackTypeEffect(
        BytecodeModule module,
        BytecodeRoutine routine,
        int instructionIndex,
        BytecodeInstruction instruction,
        List<TypeSymbol?> workingStack)
    {
        var flagsType = PopStackType(workingStack);
        var tileIndexType = PopStackType(workingStack);
        var tilesetIndexType = PopStackType(workingStack);
        var rowType = PopStackType(workingStack);
        var columnType = PopStackType(workingStack);
        var layerIndexType = PopStackType(workingStack);
        var tilemapType = PopStackType(workingStack);
        ValidateExpectedStackType(module, routine, instructionIndex, instruction.Opcode, tilemapType, TypeSymbol.Tilemap, "тайловая карта SETMAPTILE");
        ValidateExpectedIntegralStackType(module, routine, instructionIndex, instruction.Opcode, layerIndexType, "номер слоя SETMAPTILE");
        ValidateExpectedIntegralStackType(module, routine, instructionIndex, instruction.Opcode, columnType, "столбец SETMAPTILE");
        ValidateExpectedIntegralStackType(module, routine, instructionIndex, instruction.Opcode, rowType, "строка SETMAPTILE");
        ValidateExpectedIntegralStackType(module, routine, instructionIndex, instruction.Opcode, tilesetIndexType, "номер тайлсета SETMAPTILE");
        ValidateExpectedIntegralStackType(module, routine, instructionIndex, instruction.Opcode, tileIndexType, "номер тайла SETMAPTILE");
        ValidateExpectedIntegralStackType(module, routine, instructionIndex, instruction.Opcode, flagsType, "флаги SETMAPTILE");
    }
}
