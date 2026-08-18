namespace Axiom.Semantics;

public static partial class BuiltinFunctions
{
    private static IReadOnlyList<BuiltinFunctionDescriptor> GetInputDescriptors()
    {
        return
        [
            CreateDefault(BuiltinFunctionId.InKey, "INKEY", TypeSymbol.String, []),
            CreateDefault(BuiltinFunctionId.KeyDown, "KEYDOWN", TypeSymbol.Boolean, [Parameter("keyText", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.KeyPress, "KEYPRESS", TypeSymbol.Boolean, [Parameter("keyText", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.MouseX, "MOUSEX", TypeSymbol.Long, []),
            CreateDefault(BuiltinFunctionId.MouseY, "MOUSEY", TypeSymbol.Long, []),
            CreateDefault(BuiltinFunctionId.MouseButton, "MOUSEBUTTON", TypeSymbol.Boolean, [Parameter("buttonNumber", TypeSymbol.Integer)]),
            CreateDefault(BuiltinFunctionId.MousePress, "MOUSEPRESS", TypeSymbol.Boolean, [Parameter("buttonNumber", TypeSymbol.Integer)]),
            CreateDefault(BuiltinFunctionId.ScreenIsActive, "SCREENISACTIVE", TypeSymbol.Boolean, []),
            CreateDefault(BuiltinFunctionId.ScreenMode, "SCREENMODE", TypeSymbol.Integer, []),
            CreateDefault(BuiltinFunctionId.ScreenWidth, "SCREENWIDTH", TypeSymbol.Integer, []),
            CreateDefault(BuiltinFunctionId.ScreenHeight, "SCREENHEIGHT", TypeSymbol.Integer, []),
            CreateDefault(BuiltinFunctionId.TextWidth, "TEXTWIDTH", TypeSymbol.Integer, [Parameter("text", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.TextHeight, "TEXTHEIGHT", TypeSymbol.Integer, [Parameter("text", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.PointPosition, "POINT", TypeSymbol.Integer, [Parameter("number", TypeSymbol.Integer)]),
            CreateDefault(BuiltinFunctionId.Point, "POINT", TypeSymbol.Integer, [Parameter("x", TypeSymbol.Long), Parameter("y", TypeSymbol.Long)]),
            CreateDefault(BuiltinFunctionId.PMap, "PMAP", TypeSymbol.Double, [Parameter("coordinate", TypeSymbol.Double), Parameter("selector", TypeSymbol.Integer)]),
            CreateDefault(BuiltinFunctionId.LoadImage, "LOADIMAGE", TypeSymbol.Image, [Parameter("path", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.ImageWidth, "IMAGEWIDTH", TypeSymbol.Integer, [Parameter("image", TypeSymbol.Image)]),
            CreateDefault(BuiltinFunctionId.ImageHeight, "IMAGEHEIGHT", TypeSymbol.Integer, [Parameter("image", TypeSymbol.Image)]),
            CreateDefault(BuiltinFunctionId.LoadSprite, "LOADSPRITE", TypeSymbol.Sprite, [Parameter("path", TypeSymbol.String), Parameter("frameWidth", TypeSymbol.Long), Parameter("frameHeight", TypeSymbol.Long)]),
            CreateDefault(BuiltinFunctionId.SpriteWidth, "SPRITEWIDTH", TypeSymbol.Integer, [Parameter("sprite", TypeSymbol.Sprite)]),
            CreateDefault(BuiltinFunctionId.SpriteHeight, "SPRITEHEIGHT", TypeSymbol.Integer, [Parameter("sprite", TypeSymbol.Sprite)]),
            CreateDefault(BuiltinFunctionId.SpriteCount, "SPRITECOUNT", TypeSymbol.Integer, [Parameter("sprite", TypeSymbol.Sprite)]),
            CreateDefault(BuiltinFunctionId.LoadTileset, "LOADTILESET", TypeSymbol.Tileset, [Parameter("path", TypeSymbol.String), Parameter("tileWidth", TypeSymbol.Long), Parameter("tileHeight", TypeSymbol.Long)]),
            CreateDefault(BuiltinFunctionId.LoadTilesetMargin, "LOADTILESET", TypeSymbol.Tileset, [Parameter("path", TypeSymbol.String), Parameter("tileWidth", TypeSymbol.Long), Parameter("tileHeight", TypeSymbol.Long), Parameter("margin", TypeSymbol.Long)]),
            CreateDefault(BuiltinFunctionId.LoadTilesetMarginSpacing, "LOADTILESET", TypeSymbol.Tileset, [Parameter("path", TypeSymbol.String), Parameter("tileWidth", TypeSymbol.Long), Parameter("tileHeight", TypeSymbol.Long), Parameter("margin", TypeSymbol.Long), Parameter("spacing", TypeSymbol.Long)]),
            CreateDefault(BuiltinFunctionId.LoadTilemap, "LOADTILEMAP", TypeSymbol.Tilemap, [Parameter("path", TypeSymbol.String)]),
            CreateDefault(BuiltinFunctionId.TileCount, "TILECOUNT", TypeSymbol.Integer, [Parameter("tileset", TypeSymbol.Tileset)]),
            CreateDefault(BuiltinFunctionId.TileWidth, "TILEWIDTH", TypeSymbol.Integer, [Parameter("tileset", TypeSymbol.Tileset)]),
            CreateDefault(BuiltinFunctionId.TileHeight, "TILEHEIGHT", TypeSymbol.Integer, [Parameter("tileset", TypeSymbol.Tileset)]),
            CreateDefault(BuiltinFunctionId.MapWidth, "MAPWIDTH", TypeSymbol.Integer, [Parameter("tilemap", TypeSymbol.Tilemap)]),
            CreateDefault(BuiltinFunctionId.MapHeight, "MAPHEIGHT", TypeSymbol.Integer, [Parameter("tilemap", TypeSymbol.Tilemap)]),
            CreateDefault(BuiltinFunctionId.MapPixelWidth, "MAPPIXELWIDTH", TypeSymbol.Integer, [Parameter("tilemap", TypeSymbol.Tilemap)]),
            CreateDefault(BuiltinFunctionId.MapPixelHeight, "MAPPIXELHEIGHT", TypeSymbol.Integer, [Parameter("tilemap", TypeSymbol.Tilemap)]),
            CreateDefault(BuiltinFunctionId.MapTileWidth, "MAPTILEWIDTH", TypeSymbol.Integer, [Parameter("tilemap", TypeSymbol.Tilemap)]),
            CreateDefault(BuiltinFunctionId.MapTileHeight, "MAPTILEHEIGHT", TypeSymbol.Integer, [Parameter("tilemap", TypeSymbol.Tilemap)]),
            CreateDefault(BuiltinFunctionId.MapLayerCount, "MAPLAYERCOUNT", TypeSymbol.Integer, [Parameter("tilemap", TypeSymbol.Tilemap)]),
            CreateDefault(BuiltinFunctionId.MapTile, "MAPTILE", TypeSymbol.Integer, [Parameter("tilemap", TypeSymbol.Tilemap), Parameter("layerIndex", TypeSymbol.Long), Parameter("column", TypeSymbol.Long), Parameter("row", TypeSymbol.Long)]),
            CreateDefault(BuiltinFunctionId.MapTileset, "MAPTILESET", TypeSymbol.Integer, [Parameter("tilemap", TypeSymbol.Tilemap), Parameter("layerIndex", TypeSymbol.Long), Parameter("column", TypeSymbol.Long), Parameter("row", TypeSymbol.Long)]),
            CreateDefault(BuiltinFunctionId.MapTileFlags, "MAPTILEFLAGS", TypeSymbol.Integer, [Parameter("tilemap", TypeSymbol.Tilemap), Parameter("layerIndex", TypeSymbol.Long), Parameter("column", TypeSymbol.Long), Parameter("row", TypeSymbol.Long)]),
            CreateDefault(BuiltinFunctionId.Rgb, "RGB", TypeSymbol.Integer, [Parameter("red", TypeSymbol.Long), Parameter("green", TypeSymbol.Long), Parameter("blue", TypeSymbol.Long)])
        ];
    }
}
