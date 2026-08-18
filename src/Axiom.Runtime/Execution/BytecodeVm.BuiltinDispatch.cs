using Axiom.Bytecode;
using Axiom.Core;
using Axiom.Runtime.Builtins;
using Axiom.Semantics;

namespace Axiom.Runtime;

public sealed partial class BytecodeVm
{
    public static bool SupportsBuiltin(BuiltinFunctionId builtinId, int parameterCount)
    {
        return TryCreateBuiltinExecution(new BuiltinBinding(-1, builtinId, builtinId.ToString(), parameterCount, returnsValue: true, function: null)) is not null;
    }

    private static BuiltinExecution CreateBuiltinExecution(BuiltinBinding builtin)
    {
        return TryCreateBuiltinExecution(builtin)
            ?? throw new InvalidOperationException($"Bytecode VM does not support built-in '{builtin.Name}' ({builtin.CanonicalId}).");
    }

    private static BuiltinExecution? TryCreateBuiltinExecution(BuiltinBinding builtin)
    {
        ArgumentNullException.ThrowIfNull(builtin);

        return builtin.CanonicalId switch
        {
            BuiltinFunctionId.Len => BuiltinExecution.UnaryNoLocation(static (vm, value) => RuntimeValue.FromInteger(GetStringUnchecked(value).Length)),
            BuiltinFunctionId.Left => BuiltinExecution.Binary(static (vm, text, count, location) =>
                BytecodeVm.EvaluateLeft(
                    GetStringUnchecked(text),
                    vm.RequirePositiveCount("LEFT", "count", GetIntegerUnchecked(count), location))),
            BuiltinFunctionId.Right => BuiltinExecution.Binary(static (vm, text, count, location) =>
                BytecodeVm.EvaluateRightBuiltin(
                    text,
                    vm.RequirePositiveCount("RIGHT", "count", GetIntegerUnchecked(count), location))),
            BuiltinFunctionId.Mid => BuiltinExecution.Ternary(static (vm, text, start, count, location) =>
                BytecodeVm.EvaluateMid(
                    GetStringUnchecked(text),
                    vm.RequirePositiveStart("MID", GetIntegerUnchecked(start), location),
                    vm.RequirePositiveCount("MID", "count", GetIntegerUnchecked(count), location))),
            BuiltinFunctionId.TrimWhitespace => BuiltinExecution.UnaryNoLocation(static (vm, value) => BytecodeVm.EvaluateTrimWhitespaceBuiltin(value)),
            BuiltinFunctionId.TrimCharacters => BuiltinExecution.BinaryNoLocation(static (vm, value, chars) => RuntimeValue.FromString(BytecodeVm.EvaluateTrimCharacters(GetStringUnchecked(value), GetStringUnchecked(chars)))),
            BuiltinFunctionId.LTrimWhitespace => BuiltinExecution.UnaryNoLocation(static (vm, value) => BytecodeVm.EvaluateLeftTrimWhitespaceBuiltin(value)),
            BuiltinFunctionId.LTrimCharacters => BuiltinExecution.BinaryNoLocation(static (vm, value, chars) => RuntimeValue.FromString(BytecodeVm.EvaluateLeftTrimCharacters(GetStringUnchecked(value), GetStringUnchecked(chars)))),
            BuiltinFunctionId.RTrimWhitespace => BuiltinExecution.UnaryNoLocation(static (vm, value) => BytecodeVm.EvaluateRightTrimWhitespaceBuiltin(value)),
            BuiltinFunctionId.RTrimCharacters => BuiltinExecution.BinaryNoLocation(static (vm, value, chars) => RuntimeValue.FromString(BytecodeVm.EvaluateRightTrimCharacters(GetStringUnchecked(value), GetStringUnchecked(chars)))),
            BuiltinFunctionId.Reverse => BuiltinExecution.UnaryNoLocation(static (vm, value) => BytecodeVm.EvaluateReverseBuiltin(value)),
            BuiltinFunctionId.Contains => BuiltinExecution.BinaryNoLocation(static (vm, left, right) => RuntimeValue.FromBoolean(BytecodeVm.EvaluateContains(GetStringUnchecked(left), GetStringUnchecked(right)))),
            BuiltinFunctionId.StartsWith => BuiltinExecution.BinaryNoLocation(static (vm, left, right) => RuntimeValue.FromBoolean(BytecodeVm.EvaluateStartsWith(GetStringUnchecked(left), GetStringUnchecked(right)))),
            BuiltinFunctionId.EndsWith => BuiltinExecution.BinaryNoLocation(static (vm, left, right) => RuntimeValue.FromBoolean(BytecodeVm.EvaluateEndsWith(GetStringUnchecked(left), GetStringUnchecked(right)))),
            BuiltinFunctionId.Replace => BuiltinExecution.Ternary(static (vm, text, oldValue, newValue, location) =>
                vm.EvaluateReplaceBuiltin(text, oldValue, newValue, location)),
            BuiltinFunctionId.ReplaceMany => BuiltinExecution.Ternary(static (vm, text, oldItems, newItems, location) =>
                vm.EvaluateReplaceManyBuiltin(text, oldItems, newItems, location)),
            BuiltinFunctionId.InStr => BuiltinExecution.BinaryNoLocation(static (vm, text, value) => RuntimeValue.FromInteger(BytecodeVm.EvaluateInStr(1, GetStringUnchecked(text), GetStringUnchecked(value)))),
            BuiltinFunctionId.InStrFrom => BuiltinExecution.Ternary(static (vm, start, text, value, location) => RuntimeValue.FromInteger(
                BytecodeVm.EvaluateInStr(vm.RequirePositiveStart("INSTR", GetIntegerUnchecked(start), location), GetStringUnchecked(text), GetStringUnchecked(value)))),
            BuiltinFunctionId.InStrRev => BuiltinExecution.BinaryNoLocation(static (vm, text, value) => RuntimeValue.FromInteger(
                BytecodeVm.EvaluateInStrRev(GetStringUnchecked(text).Length, GetStringUnchecked(text), GetStringUnchecked(value)))),
            BuiltinFunctionId.InStrRevFrom => BuiltinExecution.Ternary(static (vm, start, text, value, location) => RuntimeValue.FromInteger(
                BytecodeVm.EvaluateInStrRev(vm.RequirePositiveStart("INSTRREV", GetIntegerUnchecked(start), location), GetStringUnchecked(text), GetStringUnchecked(value)))),
            BuiltinFunctionId.SubStrCount => BuiltinExecution.Binary(static (vm, text, value, location) => RuntimeValue.FromInteger(vm.EvaluateSubStrCount(GetStringUnchecked(text), GetStringUnchecked(value), location))),
            BuiltinFunctionId.Repeat => BuiltinExecution.Binary(static (vm, value, count, location) =>
                BytecodeVm.EvaluateRepeat(GetStringUnchecked(value), vm.RequireNonNegativeCount("REPEAT", "count", GetIntegerUnchecked(count), location))),
            BuiltinFunctionId.Space => BuiltinExecution.Unary(static (vm, count, location) =>
                BytecodeVm.EvaluateSpace(vm.RequireNonNegativeCount("SPACE", "count", GetIntegerUnchecked(count), location))),
            BuiltinFunctionId.StringFill => BuiltinExecution.Binary(static (vm, count, fill, location) => RuntimeValue.FromString(
                vm.EvaluateStringFill(
                    vm.RequireNonNegativeCount("STRING", "count", GetIntegerUnchecked(count), location),
                    GetStringUnchecked(fill),
                    location))),
            BuiltinFunctionId.PadLeft => BuiltinExecution.Binary(static (vm, value, width, location) =>
                EvaluatePadLeftBuiltin(
                    value,
                    vm.RequireNonNegativeCount("PADLEFT", "width", GetIntegerUnchecked(width), location))),
            BuiltinFunctionId.PadLeftFill => BuiltinExecution.Ternary(static (vm, value, width, fill, location) =>
                vm.EvaluatePadLeftFillBuiltin(
                    value,
                    vm.RequireNonNegativeCount("PADLEFT", "width", GetIntegerUnchecked(width), location),
                    GetStringUnchecked(fill),
                    location)),
            BuiltinFunctionId.PadRight => BuiltinExecution.Binary(static (vm, value, width, location) =>
                EvaluatePadRightBuiltin(
                    value,
                    vm.RequireNonNegativeCount("PADRIGHT", "width", GetIntegerUnchecked(width), location))),
            BuiltinFunctionId.PadRightFill => BuiltinExecution.Ternary(static (vm, value, width, fill, location) =>
                vm.EvaluatePadRightFillBuiltin(
                    value,
                    vm.RequireNonNegativeCount("PADRIGHT", "width", GetIntegerUnchecked(width), location),
                    GetStringUnchecked(fill),
                    location)),
            BuiltinFunctionId.Center => BuiltinExecution.Binary(static (vm, value, width, location) =>
                EvaluateCenterBuiltin(
                    value,
                    vm.RequireNonNegativeCount("CENTER", "width", GetIntegerUnchecked(width), location))),
            BuiltinFunctionId.CenterFill => BuiltinExecution.Ternary(static (vm, value, width, fill, location) =>
                vm.EvaluateCenterFillBuiltin(
                    value,
                    vm.RequireNonNegativeCount("CENTER", "width", GetIntegerUnchecked(width), location),
                    GetStringUnchecked(fill),
                    location)),
            BuiltinFunctionId.Insert => BuiltinExecution.Ternary(static (vm, value, start, insertText, location) =>
                BytecodeVm.EvaluateInsertBuiltin(
                    value,
                    vm.RequirePositiveStart("INSERT", GetIntegerUnchecked(start), location),
                    insertText)),
            BuiltinFunctionId.Remove => BuiltinExecution.Ternary(static (vm, value, start, count, location) => RuntimeValue.FromString(
                BytecodeVm.EvaluateRemove(
                    GetStringUnchecked(value),
                    vm.RequirePositiveStart("REMOVE", GetIntegerUnchecked(start), location),
                    vm.RequireNonNegativeCount("REMOVE", "count", GetIntegerUnchecked(count), location)))),
            BuiltinFunctionId.Asc => BuiltinExecution.Unary(static (vm, value, location) => RuntimeValue.FromInteger(vm.EvaluateAsc(GetStringUnchecked(value), location))),
            BuiltinFunctionId.AscW => BuiltinExecution.Unary(static (vm, value, location) => RuntimeValue.FromInteger(vm.EvaluateAscW(GetStringUnchecked(value), location))),
            BuiltinFunctionId.Chr => BuiltinExecution.Unary(static (vm, value, location) => RuntimeValue.FromString(
                BytecodeVm.EvaluateChr(vm.RequireValidUnicodeCodePoint("CHR", "code", GetIntegerUnchecked(value), location)))),
            BuiltinFunctionId.ChrW => BuiltinExecution.Unary(static (vm, value, location) => RuntimeValue.FromString(
                BytecodeVm.EvaluateChrW(vm.RequireValidUnicodeCodePoint("CHRW", "code", GetIntegerUnchecked(value), location)))),
            BuiltinFunctionId.Split => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateSplitBuiltin(GetStringUnchecked(value), ",", location)),
            BuiltinFunctionId.SplitSeparator => BuiltinExecution.Binary(static (vm, value, separator, location) => vm.EvaluateSplitBuiltin(GetStringUnchecked(value), GetStringUnchecked(separator), location)),
            BuiltinFunctionId.Join => BuiltinExecution.UnaryNoLocation(static (vm, value) => BytecodeVm.EvaluateJoinBuiltin(value.AsArray(), ",")),
            BuiltinFunctionId.JoinSeparator => BuiltinExecution.BinaryNoLocation(static (vm, value, separator) => BytecodeVm.EvaluateJoinBuiltin(value.AsArray(), GetStringUnchecked(separator))),
            BuiltinFunctionId.RegexIsMatch => BuiltinExecution.Binary(static (vm, text, pattern, location) => vm.EvaluateRegexIsMatchBuiltin(GetStringUnchecked(text), GetStringUnchecked(pattern), "", location)),
            BuiltinFunctionId.RegexIsMatchModes => BuiltinExecution.Ternary(static (vm, text, pattern, modes, location) => vm.EvaluateRegexIsMatchBuiltin(GetStringUnchecked(text), GetStringUnchecked(pattern), GetStringUnchecked(modes), location)),
            BuiltinFunctionId.RegexMatch => BuiltinExecution.Binary(static (vm, text, pattern, location) => vm.EvaluateRegexMatchBuiltin(GetStringUnchecked(text), GetStringUnchecked(pattern), "", location)),
            BuiltinFunctionId.RegexMatchModes => BuiltinExecution.Ternary(static (vm, text, pattern, modes, location) => vm.EvaluateRegexMatchBuiltin(GetStringUnchecked(text), GetStringUnchecked(pattern), GetStringUnchecked(modes), location)),
            BuiltinFunctionId.RegexMatches => BuiltinExecution.Binary(static (vm, text, pattern, location) => vm.EvaluateRegexMatchesBuiltin(GetStringUnchecked(text), GetStringUnchecked(pattern), "", location)),
            BuiltinFunctionId.RegexMatchesModes => BuiltinExecution.Ternary(static (vm, text, pattern, modes, location) => vm.EvaluateRegexMatchesBuiltin(GetStringUnchecked(text), GetStringUnchecked(pattern), GetStringUnchecked(modes), location)),
            BuiltinFunctionId.RegexSplit => BuiltinExecution.Binary(static (vm, text, pattern, location) => vm.EvaluateRegexSplitBuiltin(GetStringUnchecked(text), GetStringUnchecked(pattern), "", location)),
            BuiltinFunctionId.RegexSplitModes => BuiltinExecution.Ternary(static (vm, text, pattern, modes, location) => vm.EvaluateRegexSplitBuiltin(GetStringUnchecked(text), GetStringUnchecked(pattern), GetStringUnchecked(modes), location)),
            BuiltinFunctionId.RegexCount => BuiltinExecution.Binary(static (vm, text, pattern, location) => vm.EvaluateRegexCountBuiltin(GetStringUnchecked(text), GetStringUnchecked(pattern), "", location)),
            BuiltinFunctionId.RegexCountModes => BuiltinExecution.Ternary(static (vm, text, pattern, modes, location) => vm.EvaluateRegexCountBuiltin(GetStringUnchecked(text), GetStringUnchecked(pattern), GetStringUnchecked(modes), location)),
            BuiltinFunctionId.RegexExtract => BuiltinExecution.Ternary(static (vm, text, pattern, groupIndex, location) => vm.EvaluateRegexExtractBuiltin(GetStringUnchecked(text), GetStringUnchecked(pattern), GetIntegerUnchecked(groupIndex), "", location)),
            BuiltinFunctionId.RegexExtractModes => BuiltinExecution.Variadic(static (vm, arguments, location) => vm.EvaluateRegexExtractBuiltin(GetStringUnchecked(arguments[0]), GetStringUnchecked(arguments[1]), GetIntegerUnchecked(arguments[2]), GetStringUnchecked(arguments[3]), location)),
            BuiltinFunctionId.RegexExtractAll => BuiltinExecution.Ternary(static (vm, text, pattern, groupIndex, location) => vm.EvaluateRegexExtractAllBuiltin(GetStringUnchecked(text), GetStringUnchecked(pattern), GetIntegerUnchecked(groupIndex), "", location)),
            BuiltinFunctionId.RegexExtractAllModes => BuiltinExecution.Variadic(static (vm, arguments, location) => vm.EvaluateRegexExtractAllBuiltin(GetStringUnchecked(arguments[0]), GetStringUnchecked(arguments[1]), GetIntegerUnchecked(arguments[2]), GetStringUnchecked(arguments[3]), location)),
            BuiltinFunctionId.RegexReplace => BuiltinExecution.Ternary(static (vm, text, pattern, replacement, location) => vm.EvaluateRegexReplaceBuiltin(text, pattern, replacement, "", count: null, location)),
            BuiltinFunctionId.RegexReplaceModes => BuiltinExecution.Variadic(static (vm, arguments, location) => vm.EvaluateRegexReplaceBuiltin(arguments[0], arguments[1], arguments[2], GetStringUnchecked(arguments[3]), count: null, location)),
            BuiltinFunctionId.RegexReplaceCount => BuiltinExecution.Variadic(static (vm, arguments, location) => vm.EvaluateRegexReplaceBuiltin(arguments[0], arguments[1], arguments[2], "", GetIntegerUnchecked(arguments[3]), location)),
            BuiltinFunctionId.RegexReplaceModesCount => BuiltinExecution.Variadic(static (vm, arguments, location) => vm.EvaluateRegexReplaceBuiltin(arguments[0], arguments[1], arguments[2], GetStringUnchecked(arguments[3]), GetIntegerUnchecked(arguments[4]), location)),
            BuiltinFunctionId.InKey => BuiltinExecution.NullaryNoLocation(static vm => RuntimeValue.FromString(vm.ReadKeyboardInput())),
            BuiltinFunctionId.KeyDown => BuiltinExecution.UnaryNoLocation(static (vm, keyText) => RuntimeValue.FromBoolean(vm.IsKeyboardKeyDown(GetStringUnchecked(keyText)))),
            BuiltinFunctionId.KeyPress => BuiltinExecution.UnaryNoLocation(static (vm, keyText) => RuntimeValue.FromBoolean(vm.ConsumeKeyboardKeyPress(GetStringUnchecked(keyText)))),
            BuiltinFunctionId.MouseX => BuiltinExecution.NullaryNoLocation(static vm => RuntimeValue.FromLong(vm.ReadMouseX())),
            BuiltinFunctionId.MouseY => BuiltinExecution.NullaryNoLocation(static vm => RuntimeValue.FromLong(vm.ReadMouseY())),
            BuiltinFunctionId.MouseButton => BuiltinExecution.UnaryNoLocation(static (vm, buttonNumber) => RuntimeValue.FromBoolean(vm.IsMouseButtonDown(GetIntegerUnchecked(buttonNumber)))),
            BuiltinFunctionId.MousePress => BuiltinExecution.UnaryNoLocation(static (vm, buttonNumber) => RuntimeValue.FromBoolean(vm.ConsumeMouseButtonPress(GetIntegerUnchecked(buttonNumber)))),
            BuiltinFunctionId.ScreenIsActive => BuiltinExecution.NullaryNoLocation(static vm => RuntimeValue.FromBoolean(vm._host.IsScreenActive)),
            BuiltinFunctionId.ScreenMode => BuiltinExecution.NullaryNoLocation(static vm => RuntimeValue.FromInteger(vm._host.ScreenMode)),
            BuiltinFunctionId.ScreenWidth => BuiltinExecution.NullaryNoLocation(static vm => RuntimeValue.FromInteger(vm._host.ScreenWidth)),
            BuiltinFunctionId.ScreenHeight => BuiltinExecution.NullaryNoLocation(static vm => RuntimeValue.FromInteger(vm._host.ScreenHeight)),
            BuiltinFunctionId.TextWidth => BuiltinExecution.Unary(static (vm, text, location) => vm.EvaluateTextWidthBuiltin(GetStringUnchecked(text), location)),
            BuiltinFunctionId.TextHeight => BuiltinExecution.Unary(static (vm, text, location) => vm.EvaluateTextHeightBuiltin(GetStringUnchecked(text), location)),
            BuiltinFunctionId.PointPosition => BuiltinExecution.Unary(static (vm, selector, location) => vm.EvaluatePointBuiltin([selector], location)),
            BuiltinFunctionId.Point => BuiltinExecution.Binary(static (vm, x, y, location) => vm.EvaluatePointBuiltin([x, y], location)),
            BuiltinFunctionId.PMap => BuiltinExecution.Binary(static (vm, coordinate, selector, location) => vm.EvaluatePMapBuiltin([coordinate, selector], location)),
            BuiltinFunctionId.LoadImage => BuiltinExecution.Unary(static (vm, path, location) => vm.EvaluateLoadImageBuiltin(GetStringUnchecked(path), location)),
            BuiltinFunctionId.ImageWidth => BuiltinExecution.UnaryNoLocation(static (vm, image) => RuntimeValue.FromInteger(BytecodeVm.GetImageWidth(image))),
            BuiltinFunctionId.ImageHeight => BuiltinExecution.UnaryNoLocation(static (vm, image) => RuntimeValue.FromInteger(BytecodeVm.GetImageHeight(image))),
            BuiltinFunctionId.LoadSprite => BuiltinExecution.Ternary(static (vm, path, frameWidth, frameHeight, location) => vm.EvaluateLoadSpriteBuiltin(GetStringUnchecked(path), GetLongUnchecked(frameWidth), GetLongUnchecked(frameHeight), location)),
            BuiltinFunctionId.LoadTileset => BuiltinExecution.Ternary(static (vm, path, tileWidth, tileHeight, location) => vm.EvaluateLoadTilesetBuiltin(GetStringUnchecked(path), GetLongUnchecked(tileWidth), GetLongUnchecked(tileHeight), margin: 0, spacing: 0, location)),
            BuiltinFunctionId.LoadTilesetMargin => BuiltinExecution.Variadic(static (vm, arguments, location) => vm.EvaluateLoadTilesetBuiltin(GetStringUnchecked(arguments[0]), GetLongUnchecked(arguments[1]), GetLongUnchecked(arguments[2]), GetLongUnchecked(arguments[3]), spacing: 0, location)),
            BuiltinFunctionId.LoadTilesetMarginSpacing => BuiltinExecution.Variadic(static (vm, arguments, location) => vm.EvaluateLoadTilesetBuiltin(GetStringUnchecked(arguments[0]), GetLongUnchecked(arguments[1]), GetLongUnchecked(arguments[2]), GetLongUnchecked(arguments[3]), GetLongUnchecked(arguments[4]), location)),
            BuiltinFunctionId.LoadTilemap => BuiltinExecution.Unary(static (vm, path, location) => vm.EvaluateLoadTilemapBuiltin(GetStringUnchecked(path), location)),
            BuiltinFunctionId.SpriteWidth => BuiltinExecution.UnaryNoLocation(static (vm, sprite) => RuntimeValue.FromInteger(BytecodeVm.GetSpriteWidth(sprite))),
            BuiltinFunctionId.SpriteHeight => BuiltinExecution.UnaryNoLocation(static (vm, sprite) => RuntimeValue.FromInteger(BytecodeVm.GetSpriteHeight(sprite))),
            BuiltinFunctionId.SpriteCount => BuiltinExecution.UnaryNoLocation(static (vm, sprite) => RuntimeValue.FromInteger(BytecodeVm.GetSpriteCount(sprite))),
            BuiltinFunctionId.TileCount => BuiltinExecution.UnaryNoLocation(static (vm, tileset) => RuntimeValue.FromInteger(BytecodeVm.GetTileCount(tileset))),
            BuiltinFunctionId.TileWidth => BuiltinExecution.UnaryNoLocation(static (vm, tileset) => RuntimeValue.FromInteger(BytecodeVm.GetTileWidth(tileset))),
            BuiltinFunctionId.TileHeight => BuiltinExecution.UnaryNoLocation(static (vm, tileset) => RuntimeValue.FromInteger(BytecodeVm.GetTileHeight(tileset))),
            BuiltinFunctionId.MapWidth => BuiltinExecution.UnaryNoLocation(static (vm, tilemap) => RuntimeValue.FromInteger(BytecodeVm.GetMapWidth(tilemap))),
            BuiltinFunctionId.MapHeight => BuiltinExecution.UnaryNoLocation(static (vm, tilemap) => RuntimeValue.FromInteger(BytecodeVm.GetMapHeight(tilemap))),
            BuiltinFunctionId.MapPixelWidth => BuiltinExecution.UnaryNoLocation(static (vm, tilemap) => RuntimeValue.FromInteger(BytecodeVm.GetMapPixelWidth(tilemap))),
            BuiltinFunctionId.MapPixelHeight => BuiltinExecution.UnaryNoLocation(static (vm, tilemap) => RuntimeValue.FromInteger(BytecodeVm.GetMapPixelHeight(tilemap))),
            BuiltinFunctionId.MapTileWidth => BuiltinExecution.UnaryNoLocation(static (vm, tilemap) => RuntimeValue.FromInteger(BytecodeVm.GetMapTileWidth(tilemap))),
            BuiltinFunctionId.MapTileHeight => BuiltinExecution.UnaryNoLocation(static (vm, tilemap) => RuntimeValue.FromInteger(BytecodeVm.GetMapTileHeight(tilemap))),
            BuiltinFunctionId.MapLayerCount => BuiltinExecution.UnaryNoLocation(static (vm, tilemap) => RuntimeValue.FromInteger(BytecodeVm.GetMapLayerCount(tilemap))),
            BuiltinFunctionId.MapTile => BuiltinExecution.Variadic(static (vm, arguments, location) => RuntimeValue.FromInteger(BytecodeVm.GetMapTile(arguments[0], GetLongUnchecked(arguments[1]), GetLongUnchecked(arguments[2]), GetLongUnchecked(arguments[3])))),
            BuiltinFunctionId.MapTileset => BuiltinExecution.Variadic(static (vm, arguments, location) => RuntimeValue.FromInteger(BytecodeVm.GetMapTileset(arguments[0], GetLongUnchecked(arguments[1]), GetLongUnchecked(arguments[2]), GetLongUnchecked(arguments[3])))),
            BuiltinFunctionId.MapTileFlags => BuiltinExecution.Variadic(static (vm, arguments, location) => RuntimeValue.FromInteger(BytecodeVm.GetMapTileFlags(arguments[0], GetLongUnchecked(arguments[1]), GetLongUnchecked(arguments[2]), GetLongUnchecked(arguments[3])))),
            BuiltinFunctionId.Rgb => BuiltinExecution.Ternary(static (vm, red, green, blue, location) => RuntimeValue.FromInteger(vm.EvaluateRgbBuiltin(GetLongUnchecked(red), GetLongUnchecked(green), GetLongUnchecked(blue), location))),
            BuiltinFunctionId.Int => BuiltinExecution.Unary(static (vm, value, location) => RuntimeValue.FromInteger(vm.EvaluateIntBuiltin(value, location))),
            BuiltinFunctionId.Str => BuiltinExecution.UnaryNoLocation(static (vm, value) => value.ToDisplayRuntimeValue()),
            BuiltinFunctionId.Val => BuiltinExecution.UnaryNoLocation(static (vm, value) => RuntimeValue.FromDouble(BuiltinStringHelpers.ParseVal(GetStringUnchecked(value)))),
            BuiltinFunctionId.LCase => BuiltinExecution.UnaryNoLocation(static (vm, value) => EvaluateLowerCaseBuiltin(value)),
            BuiltinFunctionId.UCase => BuiltinExecution.UnaryNoLocation(static (vm, value) => EvaluateUpperCaseBuiltin(value)),
            BuiltinFunctionId.Rnd => BuiltinExecution.NullaryNoLocation(static vm => vm.EvaluateRndBuiltin()),
            BuiltinFunctionId.Abs => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateAbsBuiltin(value, location)),
            BuiltinFunctionId.Sgn => BuiltinExecution.Unary(static (vm, value, location) => RuntimeValue.FromInteger(vm.EvaluateSgnBuiltin(value, location))),
            BuiltinFunctionId.Min => BuiltinExecution.Binary(static (vm, left, right, location) => vm.EvaluateMinBuiltin(left, right, location)),
            BuiltinFunctionId.Max => BuiltinExecution.Binary(static (vm, left, right, location) => vm.EvaluateMaxBuiltin(left, right, location)),
            BuiltinFunctionId.Clamp => BuiltinExecution.Ternary(static (vm, value, minValue, maxValue, location) => vm.EvaluateClampBuiltin(value, minValue, maxValue, location)),
            BuiltinFunctionId.Sin => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateDoubleMathBuiltin("SIN", "value", value, Math.Sin, location)),
            BuiltinFunctionId.Cos => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateDoubleMathBuiltin("COS", "value", value, Math.Cos, location)),
            BuiltinFunctionId.Tan => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateDoubleMathBuiltin("TAN", "value", value, Math.Tan, location)),
            BuiltinFunctionId.Atn => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateDoubleMathBuiltin("ATN", "value", value, Math.Atan, location)),
            BuiltinFunctionId.Atn2 => BuiltinExecution.Binary(static (vm, left, right, location) => vm.EvaluateAtn2Builtin(left, right, location)),
            BuiltinFunctionId.Sqr => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateSqrBuiltin(value, location)),
            BuiltinFunctionId.Cbrt => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateDoubleMathBuiltin("CBRT", "value", value, Math.Cbrt, location)),
            BuiltinFunctionId.Exp => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateDoubleMathBuiltin("EXP", "value", value, Math.Exp, location)),
            BuiltinFunctionId.Log => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateLogBuiltin("LOG", value, Math.Log, location)),
            BuiltinFunctionId.Log10 => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateLogBuiltin("LOG10", value, Math.Log10, location)),
            BuiltinFunctionId.Log2 => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateLogBuiltin("LOG2", value, Math.Log2, location)),
            BuiltinFunctionId.Pow => BuiltinExecution.Binary(static (vm, left, right, location) => vm.EvaluatePowBuiltin(left, right, location)),
            BuiltinFunctionId.Floor => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateFloorBuiltin(value, location)),
            BuiltinFunctionId.Ceil => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateCeilBuiltin(value, location)),
            BuiltinFunctionId.Round => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateRoundBuiltin(value, location)),
            BuiltinFunctionId.Fix => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateFixBuiltin(value, location)),
            BuiltinFunctionId.Asin => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateInverseTrigBuiltin("ASIN", value, Math.Asin, location)),
            BuiltinFunctionId.Acos => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateInverseTrigBuiltin("ACOS", value, Math.Acos, location)),
            BuiltinFunctionId.Asinh => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateDoubleMathBuiltin("ASINH", "value", value, Math.Asinh, location)),
            BuiltinFunctionId.Acosh => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateAcoshBuiltin(value, location)),
            BuiltinFunctionId.Atanh => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateAtanhBuiltin(value, location)),
            BuiltinFunctionId.Sinh => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateDoubleMathBuiltin("SINH", "value", value, Math.Sinh, location)),
            BuiltinFunctionId.Cosh => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateDoubleMathBuiltin("COSH", "value", value, Math.Cosh, location)),
            BuiltinFunctionId.Tanh => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateDoubleMathBuiltin("TANH", "value", value, Math.Tanh, location)),
            BuiltinFunctionId.SetSign => BuiltinExecution.Binary(static (vm, left, right, location) => vm.EvaluateSetSignBuiltin(left, right, location)),
            BuiltinFunctionId.ArrayLength => BuiltinExecution.UnaryNoLocation(static (vm, value) => RuntimeValue.FromInteger(BytecodeVm.EvaluateArrayLengthBuiltin(value))),
            BuiltinFunctionId.ArrayIsAllocated => BuiltinExecution.UnaryNoLocation(static (vm, value) => RuntimeValue.FromBoolean(value.AsArray().IsAllocated)),
            BuiltinFunctionId.ArrayRank => BuiltinExecution.UnaryNoLocation(static (vm, value) => RuntimeValue.FromInteger(BytecodeVm.EvaluateArrayRankBuiltin(value))),
            BuiltinFunctionId.ArrayBounds => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateArrayBoundsBuiltin(value, location)),
            BuiltinFunctionId.ArrayClone => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateArrayCloneBuiltin(value, location)),
            BuiltinFunctionId.ArrayFlatten => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateArrayFlattenBuiltin(value, location)),
            BuiltinFunctionId.ArrayReshape => BuiltinExecution.Binary(static (vm, left, right, location) => vm.EvaluateArrayReshapeBuiltin(left, right, location)),
            BuiltinFunctionId.ArrayDistinct => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateArrayDistinctBuiltin(value, location)),
            BuiltinFunctionId.ArrayFill => BuiltinExecution.Binary(static (vm, left, right, location) => vm.EvaluateArrayFillBuiltin(left, right, location)),
            BuiltinFunctionId.ArrayContains => BuiltinExecution.Binary(static (vm, left, right, location) => RuntimeValue.FromBoolean(vm.EvaluateArrayContainsBuiltin(left, right, location))),
            BuiltinFunctionId.ArrayIndexOf => BuiltinExecution.Binary(static (vm, left, right, location) => vm.EvaluateArrayIndexOfBuiltin(left, right, searchFromEnd: false, location)),
            BuiltinFunctionId.ArrayLastIndexOf => BuiltinExecution.Binary(static (vm, left, right, location) => vm.EvaluateArrayIndexOfBuiltin(left, right, searchFromEnd: true, location)),
            BuiltinFunctionId.ArrayCount => BuiltinExecution.Binary(static (vm, left, right, location) => RuntimeValue.FromInteger(vm.EvaluateArrayCountBuiltin(left, right, location))),
            BuiltinFunctionId.ArrayFindAll => BuiltinExecution.Binary(static (vm, left, right, location) => vm.EvaluateArrayFindAllBuiltin(left, right, location)),
            BuiltinFunctionId.ArrayEvery => BuiltinExecution.Binary(static (vm, left, right, location) => RuntimeValue.FromBoolean(vm.EvaluateArrayEveryBuiltin(left, right, location))),
            BuiltinFunctionId.ArraySome => BuiltinExecution.Binary(static (vm, left, right, location) => RuntimeValue.FromBoolean(vm.EvaluateArraySomeBuiltin(left, right, location))),
            BuiltinFunctionId.ArrayFilter => BuiltinExecution.Binary(static (vm, left, right, location) => vm.EvaluateArrayFilterBuiltin(left, right, location)),
            BuiltinFunctionId.ArrayMap => BuiltinExecution.Binary(static (vm, left, right, location) => vm.EvaluateArrayMapBuiltin(left, right, location)),
            BuiltinFunctionId.ArrayReduce => BuiltinExecution.Ternary(static (vm, array, reducer, seed, location) => vm.EvaluateArrayReduceBuiltin(array, reducer, seed, location)),
            BuiltinFunctionId.Date => BuiltinExecution.NullaryNoLocation(static vm => vm.EvaluateDateBuiltin()),
            BuiltinFunctionId.DateInZone => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateDateBuiltin(GetStringUnchecked(value), location)),
            BuiltinFunctionId.Time => BuiltinExecution.NullaryNoLocation(static vm => vm.EvaluateTimeBuiltin()),
            BuiltinFunctionId.TimeInZone => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateTimeBuiltin(GetStringUnchecked(value), location)),
            BuiltinFunctionId.Now => BuiltinExecution.NullaryNoLocation(static vm => vm.EvaluateNowBuiltin()),
            BuiltinFunctionId.NowInZone => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateNowBuiltin(GetStringUnchecked(value), location)),
            BuiltinFunctionId.UtcDate => BuiltinExecution.NullaryNoLocation(static vm => vm.EvaluateUtcDateBuiltin()),
            BuiltinFunctionId.UtcTime => BuiltinExecution.NullaryNoLocation(static vm => vm.EvaluateUtcTimeBuiltin()),
            BuiltinFunctionId.UtcNow => BuiltinExecution.NullaryNoLocation(static vm => vm.EvaluateUtcNowBuiltin()),
            BuiltinFunctionId.Timer => BuiltinExecution.NullaryNoLocation(static vm => vm.EvaluateTimerBuiltin()),
            BuiltinFunctionId.DateSerial => BuiltinExecution.Ternary(static (vm, year, month, day, location) => vm.EvaluateDateSerialBuiltin(GetIntegerUnchecked(year), GetIntegerUnchecked(month), GetIntegerUnchecked(day), location)),
            BuiltinFunctionId.TimeSerial => BuiltinExecution.Ternary(static (vm, hour, minute, second, location) => vm.EvaluateTimeSerialBuiltin(GetIntegerUnchecked(hour), GetIntegerUnchecked(minute), GetIntegerUnchecked(second), location)),
            BuiltinFunctionId.Year => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateYearBuiltin(GetStringUnchecked(value), location)),
            BuiltinFunctionId.Month => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateMonthBuiltin(GetStringUnchecked(value), location)),
            BuiltinFunctionId.MonthName => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateMonthNameBuiltin(GetIntegerUnchecked(value), location)),
            BuiltinFunctionId.Day => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateDayBuiltin(GetStringUnchecked(value), location)),
            BuiltinFunctionId.Hour => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateHourBuiltin(GetStringUnchecked(value), location)),
            BuiltinFunctionId.Minute => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateMinuteBuiltin(GetStringUnchecked(value), location)),
            BuiltinFunctionId.Second => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateSecondBuiltin(GetStringUnchecked(value), location)),
            BuiltinFunctionId.Weekday => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateWeekdayBuiltin(GetStringUnchecked(value), location)),
            BuiltinFunctionId.WeekdayName => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateWeekdayNameBuiltin(GetIntegerUnchecked(value), location)),
            BuiltinFunctionId.DateAdd => BuiltinExecution.Ternary(static (vm, interval, value, dateText, location) => vm.EvaluateDateAddBuiltin(GetStringUnchecked(interval), GetIntegerUnchecked(value), GetStringUnchecked(dateText), location)),
            BuiltinFunctionId.DateDiff => BuiltinExecution.Ternary(static (vm, interval, left, right, location) => vm.EvaluateDateDiffBuiltin(GetStringUnchecked(interval), GetStringUnchecked(left), GetStringUnchecked(right), location)),
            BuiltinFunctionId.DatePart => BuiltinExecution.Binary(static (vm, interval, dateText, location) => vm.EvaluateDatePartBuiltin(GetStringUnchecked(interval), GetStringUnchecked(dateText), location)),
            BuiltinFunctionId.DateValue => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateDateValueBuiltin(GetStringUnchecked(value), location)),
            BuiltinFunctionId.TimeValue => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateTimeValueBuiltin(GetStringUnchecked(value), location)),
            BuiltinFunctionId.IsDate => BuiltinExecution.UnaryNoLocation(static (vm, value) => vm.EvaluateIsDateBuiltin(GetStringUnchecked(value))),
            BuiltinFunctionId.UtcOffset => BuiltinExecution.NullaryNoLocation(static vm => vm.EvaluateUtcOffsetBuiltin()),
            BuiltinFunctionId.UtcOffsetValue => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateUtcOffsetBuiltin(GetStringUnchecked(value), location)),
            BuiltinFunctionId.UtcOffsetValueZone => BuiltinExecution.Binary(static (vm, value, zone, location) => vm.EvaluateUtcOffsetBuiltin(GetStringUnchecked(value), GetStringUnchecked(zone), location)),
            BuiltinFunctionId.ToUtc => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateToUtcBuiltin(GetStringUnchecked(value), location)),
            BuiltinFunctionId.ToUtcZone => BuiltinExecution.Binary(static (vm, value, zone, location) => vm.EvaluateToUtcBuiltin(GetStringUnchecked(value), GetStringUnchecked(zone), location)),
            BuiltinFunctionId.FromUtc => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateFromUtcBuiltin(GetStringUnchecked(value), location)),
            BuiltinFunctionId.FromUtcZone => BuiltinExecution.Binary(static (vm, value, zone, location) => vm.EvaluateFromUtcBuiltin(GetStringUnchecked(value), GetStringUnchecked(zone), location)),
            BuiltinFunctionId.IsLeapYear => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateIsLeapYearBuiltin(GetIntegerUnchecked(value), location)),
            BuiltinFunctionId.DaysInMonth => BuiltinExecution.Binary(static (vm, year, month, location) => vm.EvaluateDaysInMonthBuiltin(GetIntegerUnchecked(year), GetIntegerUnchecked(month), location)),
            BuiltinFunctionId.StartOfMonth => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateStartOfMonthBuiltin(GetStringUnchecked(value), location)),
            BuiltinFunctionId.EndOfMonth => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateEndOfMonthBuiltin(GetStringUnchecked(value), location)),
            BuiltinFunctionId.StartOfYear => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateStartOfYearBuiltin(GetStringUnchecked(value), location)),
            BuiltinFunctionId.EndOfYear => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateEndOfYearBuiltin(GetStringUnchecked(value), location)),
            BuiltinFunctionId.IsOpen => BuiltinExecution.UnaryNoLocation(static (vm, value) => vm.EvaluateIsOpenBuiltin(value)),
            BuiltinFunctionId.FilePath => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateFilePathBuiltin(value, location)),
            BuiltinFunctionId.FileMode => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateFileModeBuiltin(value, location)),
            BuiltinFunctionId.FilePos => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateFilePosBuiltin(value, location)),
            BuiltinFunctionId.FileLength => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateFileLengthBuiltin(value, location)),
            BuiltinFunctionId.LineNumber => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateLineNumberBuiltin(value, location)),
            BuiltinFunctionId.Eof => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateEofBuiltin(value, location)),
            BuiltinFunctionId.ReadToEnd => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateReadToEndBuiltin(value, location)),
            BuiltinFunctionId.ReadText => BuiltinExecution.Binary(static (vm, value, count, location) => vm.EvaluateReadTextBuiltin(
                value,
                vm.RequireNonNegativeCount("READTEXT", "count", GetIntegerUnchecked(count), location),
                location)),
            BuiltinFunctionId.ReadLine => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateReadLineBuiltin(value, location)),
            BuiltinFunctionId.ReadLines => BuiltinExecution.Binary(static (vm, value, count, location) => vm.EvaluateReadLinesBuiltin(
                value,
                vm.RequireNonNegativeCount("READLINES", "count", GetIntegerUnchecked(count), location),
                location)),
            BuiltinFunctionId.SkipLine => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateSkipLineBuiltin(value, location)),
            BuiltinFunctionId.Write => BuiltinExecution.Binary(static (vm, handle, text, location) => vm.EvaluateWriteBuiltin(handle, GetStringUnchecked(text), appendLineBreak: false, location)),
            BuiltinFunctionId.WriteLine => BuiltinExecution.Binary(static (vm, handle, text, location) => vm.EvaluateWriteBuiltin(handle, GetStringUnchecked(text), appendLineBreak: true, location)),
            BuiltinFunctionId.NewLine => BuiltinExecution.Unary(static (vm, handle, location) => vm.EvaluateNewLineBuiltin(handle, location)),
            BuiltinFunctionId.Flush => BuiltinExecution.Unary(static (vm, handle, location) => vm.EvaluateFlushBuiltin(handle, location)),
            BuiltinFunctionId.SeekFile => BuiltinExecution.Binary(static (vm, handle, position, location) => vm.EvaluateSeekFileBuiltin(handle, position, location)),
            BuiltinFunctionId.Rewind => BuiltinExecution.Unary(static (vm, handle, location) => vm.EvaluateRewindBuiltin(handle, location)),
            BuiltinFunctionId.FileExists => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateFileExistsBuiltin(GetStringUnchecked(value), location)),
            BuiltinFunctionId.DirExists => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateDirExistsBuiltin(GetStringUnchecked(value), location)),
            BuiltinFunctionId.PathExists => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluatePathExistsBuiltin(GetStringUnchecked(value), location)),
            BuiltinFunctionId.ReadFile => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateReadFileBuiltin(GetStringUnchecked(value), location)),
            BuiltinFunctionId.ReadAllLines => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateReadAllLinesBuiltin(GetStringUnchecked(value), location)),
            BuiltinFunctionId.WriteFile => BuiltinExecution.Binary(static (vm, path, text, location) => vm.EvaluateWriteFileBuiltin(GetStringUnchecked(path), GetStringUnchecked(text), location)),
            BuiltinFunctionId.AppendFile => BuiltinExecution.Binary(static (vm, path, text, location) => vm.EvaluateAppendFileBuiltin(GetStringUnchecked(path), GetStringUnchecked(text), location)),
            BuiltinFunctionId.FileSize => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateFileSizeBuiltin(GetStringUnchecked(value), location)),
            BuiltinFunctionId.FileDate => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateFileDateBuiltin(GetStringUnchecked(value), location)),
            BuiltinFunctionId.FileCreateDate => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateFileCreateDateBuiltin(GetStringUnchecked(value), location)),
            BuiltinFunctionId.FileAccessDate => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateFileAccessDateBuiltin(GetStringUnchecked(value), location)),
            BuiltinFunctionId.DirDate => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateDirDateBuiltin(GetStringUnchecked(value), location)),
            BuiltinFunctionId.GetFiles => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateGetFilesBuiltin(GetStringUnchecked(value), "*", -1, location)),
            BuiltinFunctionId.GetFilesPattern => BuiltinExecution.Binary(static (vm, path, pattern, location) => vm.EvaluateGetFilesBuiltin(GetStringUnchecked(path), GetStringUnchecked(pattern), 1, location)),
            BuiltinFunctionId.GetDirs => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateGetDirsBuiltin(GetStringUnchecked(value), "*", -1, location)),
            BuiltinFunctionId.GetDirsPattern => BuiltinExecution.Binary(static (vm, path, pattern, location) => vm.EvaluateGetDirsBuiltin(GetStringUnchecked(path), GetStringUnchecked(pattern), 1, location)),
            BuiltinFunctionId.CombinePath => BuiltinExecution.Binary(static (vm, left, right, location) => vm.EvaluateCombinePathBuiltin(GetStringUnchecked(left), GetStringUnchecked(right), location)),
            BuiltinFunctionId.FileName => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateFileNameBuiltin(GetStringUnchecked(value), location)),
            BuiltinFunctionId.DirName => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateDirNameBuiltin(GetStringUnchecked(value), location)),
            BuiltinFunctionId.ExtName => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateExtNameBuiltin(GetStringUnchecked(value), location)),
            BuiltinFunctionId.FullPath => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateFullPathBuiltin(GetStringUnchecked(value), location)),
            BuiltinFunctionId.PathRoot => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluatePathRootBuiltin(GetStringUnchecked(value), location)),
            BuiltinFunctionId.IsAbsolutePath => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateIsAbsolutePathBuiltin(GetStringUnchecked(value), location)),
            BuiltinFunctionId.MakeDir => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateMakeDirBuiltin(GetStringUnchecked(value), location)),
            BuiltinFunctionId.DeleteFile => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateDeleteFileBuiltin(GetStringUnchecked(value), location)),
            BuiltinFunctionId.CopyFile => BuiltinExecution.Binary(static (vm, source, target, location) => vm.EvaluateCopyFileBuiltin(GetStringUnchecked(source), GetStringUnchecked(target), overwrite: false, location)),
            BuiltinFunctionId.CopyFileOverwrite => BuiltinExecution.Ternary(static (vm, source, target, overwrite, location) => vm.EvaluateCopyFileBuiltin(GetStringUnchecked(source), GetStringUnchecked(target), GetBooleanUnchecked(overwrite), location)),
            BuiltinFunctionId.MoveFile => BuiltinExecution.Binary(static (vm, source, target, location) => vm.EvaluateMoveFileBuiltin(GetStringUnchecked(source), GetStringUnchecked(target), overwrite: false, location)),
            BuiltinFunctionId.MoveFileOverwrite => BuiltinExecution.Ternary(static (vm, source, target, overwrite, location) => vm.EvaluateMoveFileBuiltin(GetStringUnchecked(source), GetStringUnchecked(target), GetBooleanUnchecked(overwrite), location)),
            BuiltinFunctionId.RenameFile => BuiltinExecution.Binary(static (vm, source, target, location) => vm.EvaluateRenameFileBuiltin(GetStringUnchecked(source), GetStringUnchecked(target), location)),
            BuiltinFunctionId.DeleteDir => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateDeleteDirBuiltin(GetStringUnchecked(value), location)),
            BuiltinFunctionId.DeleteDirRecursive => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateDeleteDirRecursiveBuiltin(GetStringUnchecked(value), location)),
            BuiltinFunctionId.CopyDir => BuiltinExecution.Binary(static (vm, source, target, location) => vm.EvaluateCopyDirBuiltin(GetStringUnchecked(source), GetStringUnchecked(target), overwrite: false, location)),
            BuiltinFunctionId.CopyDirOverwrite => BuiltinExecution.Ternary(static (vm, source, target, overwrite, location) => vm.EvaluateCopyDirBuiltin(GetStringUnchecked(source), GetStringUnchecked(target), GetBooleanUnchecked(overwrite), location)),
            BuiltinFunctionId.MoveDir => BuiltinExecution.Binary(static (vm, source, target, location) => vm.EvaluateMoveDirBuiltin(GetStringUnchecked(source), GetStringUnchecked(target), location)),
            BuiltinFunctionId.RenameDir => BuiltinExecution.Binary(static (vm, source, target, location) => vm.EvaluateRenameDirBuiltin(GetStringUnchecked(source), GetStringUnchecked(target), location)),
            BuiltinFunctionId.CurrentDir => BuiltinExecution.Nullary(static (vm, location) => vm.EvaluateCurrentDirBuiltin(location)),
            BuiltinFunctionId.ChangeDir => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateChangeDirBuiltin(GetStringUnchecked(value), location)),
            BuiltinFunctionId.DirSize => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateDirSizeBuiltin(GetStringUnchecked(value), location)),
            BuiltinFunctionId.FreeSpace => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateFreeSpaceBuiltin(GetStringUnchecked(value), location)),
            BuiltinFunctionId.TotalFreeSpace => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateTotalFreeSpaceBuiltin(GetStringUnchecked(value), location)),
            BuiltinFunctionId.TotalSpace => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateTotalSpaceBuiltin(GetStringUnchecked(value), location)),
            BuiltinFunctionId.IsReadOnly => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateIsReadOnlyBuiltin(GetStringUnchecked(value), location)),
            BuiltinFunctionId.IsHidden => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateIsHiddenBuiltin(GetStringUnchecked(value), location)),
            BuiltinFunctionId.DriveName => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateDriveNameBuiltin(GetStringUnchecked(value), location)),
            BuiltinFunctionId.DriveFormat => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateDriveFormatBuiltin(GetStringUnchecked(value), location)),
            BuiltinFunctionId.DriveType => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateDriveTypeBuiltin(GetStringUnchecked(value), location)),
            BuiltinFunctionId.MakeTempFile => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateMakeTempFileBuiltin(GetStringUnchecked(value), location)),
            BuiltinFunctionId.MakeTempDir => BuiltinExecution.Unary(static (vm, value, location) => vm.EvaluateMakeTempDirBuiltin(GetStringUnchecked(value), location)),

            BuiltinFunctionId.LBound
            or BuiltinFunctionId.UBound
            or BuiltinFunctionId.ArraySlice
            or BuiltinFunctionId.ArrayToSorted
            or BuiltinFunctionId.ArrayToReversed
            or BuiltinFunctionId.ArrayTake
            or BuiltinFunctionId.ArraySkip
            or BuiltinFunctionId.ArrayConcat
            or BuiltinFunctionId.ArrayReverse
            or BuiltinFunctionId.ArraySort
            or BuiltinFunctionId.ArrayPush
            or BuiltinFunctionId.ArrayPop
            or BuiltinFunctionId.ArrayInsert
            or BuiltinFunctionId.ArrayRemove
            or BuiltinFunctionId.ArrayBinarySearch
                => BuiltinExecution.Variadic((vm, arguments, location) => vm.ExecuteBuiltin(builtin, arguments, location)),

            _ => null
        };
    }

    private enum BuiltinExecutionArity
    {
        Nullary,
        Unary,
        Binary,
        Ternary,
        Variadic,
        NullaryNoLocation,
        UnaryNoLocation,
        BinaryNoLocation,
        TernaryNoLocation
    }

    private delegate RuntimeValue BuiltinExecution0(BytecodeVm vm, TextLocation location);
    private delegate RuntimeValue BuiltinExecution1(BytecodeVm vm, RuntimeValue first, TextLocation location);
    private delegate RuntimeValue BuiltinExecution2(BytecodeVm vm, RuntimeValue first, RuntimeValue second, TextLocation location);
    private delegate RuntimeValue BuiltinExecution3(BytecodeVm vm, RuntimeValue first, RuntimeValue second, RuntimeValue third, TextLocation location);
    private delegate RuntimeValue BuiltinExecutionMany(BytecodeVm vm, ReadOnlySpan<RuntimeValue> arguments, TextLocation location);
    private delegate RuntimeValue BuiltinExecution0NoLocation(BytecodeVm vm);
    private delegate RuntimeValue BuiltinExecution1NoLocation(BytecodeVm vm, RuntimeValue first);
    private delegate RuntimeValue BuiltinExecution2NoLocation(BytecodeVm vm, RuntimeValue first, RuntimeValue second);
    private delegate RuntimeValue BuiltinExecution3NoLocation(BytecodeVm vm, RuntimeValue first, RuntimeValue second, RuntimeValue third);

    private sealed class BuiltinExecution
    {
        private BuiltinExecution(
            BuiltinExecutionArity arity,
            BuiltinExecution0? zero,
            BuiltinExecution1? one,
            BuiltinExecution2? two,
            BuiltinExecution3? three,
            BuiltinExecutionMany? many,
            BuiltinExecution0NoLocation? zeroNoLocation,
            BuiltinExecution1NoLocation? oneNoLocation,
            BuiltinExecution2NoLocation? twoNoLocation,
            BuiltinExecution3NoLocation? threeNoLocation)
        {
            Arity = arity;
            Zero = zero;
            One = one;
            Two = two;
            Three = three;
            Many = many;
            ZeroNoLocation = zeroNoLocation;
            OneNoLocation = oneNoLocation;
            TwoNoLocation = twoNoLocation;
            ThreeNoLocation = threeNoLocation;
        }

        public BuiltinExecutionArity Arity { get; }

        public bool RequiresLocation => Arity is not BuiltinExecutionArity.NullaryNoLocation
            and not BuiltinExecutionArity.UnaryNoLocation
            and not BuiltinExecutionArity.BinaryNoLocation
            and not BuiltinExecutionArity.TernaryNoLocation;

        public BuiltinExecution0? Zero { get; }

        public BuiltinExecution1? One { get; }

        public BuiltinExecution2? Two { get; }

        public BuiltinExecution3? Three { get; }

        public BuiltinExecutionMany? Many { get; }

        public BuiltinExecution0NoLocation? ZeroNoLocation { get; }

        public BuiltinExecution1NoLocation? OneNoLocation { get; }

        public BuiltinExecution2NoLocation? TwoNoLocation { get; }

        public BuiltinExecution3NoLocation? ThreeNoLocation { get; }

        public static BuiltinExecution Nullary(BuiltinExecution0 execution) => new(
            BuiltinExecutionArity.Nullary,
            execution,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null);

        public static BuiltinExecution NullaryNoLocation(BuiltinExecution0NoLocation execution) => new(
            BuiltinExecutionArity.NullaryNoLocation,
            (vm, _) => execution(vm),
            null,
            null,
            null,
            null,
            execution,
            null,
            null,
            null);

        public static BuiltinExecution Unary(BuiltinExecution1 execution) => new(
            BuiltinExecutionArity.Unary,
            null,
            execution,
            null,
            null,
            null,
            null,
            null,
            null,
            null);

        public static BuiltinExecution UnaryNoLocation(BuiltinExecution1NoLocation execution) => new(
            BuiltinExecutionArity.UnaryNoLocation,
            null,
            (vm, first, _) => execution(vm, first),
            null,
            null,
            null,
            null,
            execution,
            null,
            null);

        public static BuiltinExecution Binary(BuiltinExecution2 execution) => new(
            BuiltinExecutionArity.Binary,
            null,
            null,
            execution,
            null,
            null,
            null,
            null,
            null,
            null);

        public static BuiltinExecution BinaryNoLocation(BuiltinExecution2NoLocation execution) => new(
            BuiltinExecutionArity.BinaryNoLocation,
            null,
            null,
            (vm, first, second, _) => execution(vm, first, second),
            null,
            null,
            null,
            null,
            execution,
            null);

        public static BuiltinExecution Ternary(BuiltinExecution3 execution) => new(
            BuiltinExecutionArity.Ternary,
            null,
            null,
            null,
            execution,
            null,
            null,
            null,
            null,
            null);

        public static BuiltinExecution TernaryNoLocation(BuiltinExecution3NoLocation execution) => new(
            BuiltinExecutionArity.TernaryNoLocation,
            null,
            null,
            null,
            (vm, first, second, third, _) => execution(vm, first, second, third),
            null,
            null,
            null,
            null,
            execution);

        public static BuiltinExecution Variadic(BuiltinExecutionMany execution) => new(
            BuiltinExecutionArity.Variadic,
            null,
            null,
            null,
            null,
            execution,
            null,
            null,
            null,
            null);
    }
}
