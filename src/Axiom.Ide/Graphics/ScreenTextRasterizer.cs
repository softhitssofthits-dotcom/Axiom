namespace Axiom.Ide.Graphics;

public static class ScreenTextRasterizer
{
    public const int CellWidth = 8;
    public const int CellHeight = 12;

    private static readonly IReadOnlyDictionary<char, string[]> Glyphs = new Dictionary<char, string[]>
    {
        [' '] = ["     ", "     ", "     ", "     ", "     ", "     ", "     "],
        ['!'] = ["  #  ", "  #  ", "  #  ", "  #  ", "     ", "  #  ", "     "],
        ['"'] = [" # # ", " # # ", "     ", "     ", "     ", "     ", "     "],
        ['#'] = [" # # ", "#####", " # # ", " # # ", "#####", " # # ", "     "],
        ['$'] = [" ### ", "# #  ", " ### ", "  # #", " ### ", "  #  ", "     "],
        ['%'] = ["#   #", "   # ", "  #  ", " #   ", "#   #", "     ", "     "],
        ['&'] = [" ##  ", "#  # ", " ##  ", "#  # ", " ## #", "     ", "     "],
        ['\''] = ["  #  ", "  #  ", "     ", "     ", "     ", "     ", "     "],
        ['('] = ["   # ", "  #  ", " #   ", " #   ", "  #  ", "   # ", "     "],
        [')'] = [" #   ", "  #  ", "   # ", "   # ", "  #  ", " #   ", "     "],
        ['*'] = ["     ", "# # #", " ### ", "# # #", "     ", "     ", "     "],
        ['+'] = ["     ", "  #  ", "  #  ", "#####", "  #  ", "  #  ", "     "],
        [','] = ["     ", "     ", "     ", "     ", "  #  ", " #   ", "     "],
        ['-'] = ["     ", "     ", "     ", "#####", "     ", "     ", "     "],
        ['.'] = ["     ", "     ", "     ", "     ", "     ", "  #  ", "     "],
        ['/'] = ["    #", "   # ", "  #  ", " #   ", "#    ", "     ", "     "],
        ['0'] = [" ### ", "#   #", "#  ##", "# # #", "##  #", "#   #", " ### "],
        ['1'] = ["  #  ", " ##  ", "  #  ", "  #  ", "  #  ", "  #  ", " ### "],
        ['2'] = [" ### ", "#   #", "    #", "  ## ", " #   ", "#    ", "#####"],
        ['3'] = [" ### ", "#   #", "    #", " ### ", "    #", "#   #", " ### "],
        ['4'] = ["   # ", "  ## ", " # # ", "#  # ", "#####", "   # ", "   # "],
        ['5'] = ["#####", "#    ", "#    ", "#### ", "    #", "#   #", " ### "],
        ['6'] = [" ### ", "#   #", "#    ", "#### ", "#   #", "#   #", " ### "],
        ['7'] = ["#####", "    #", "   # ", "  #  ", " #   ", " #   ", " #   "],
        ['8'] = [" ### ", "#   #", "#   #", " ### ", "#   #", "#   #", " ### "],
        ['9'] = [" ### ", "#   #", "#   #", " ####", "    #", "#   #", " ### "],
        [':'] = ["     ", "  #  ", "     ", "     ", "  #  ", "     ", "     "],
        [';'] = ["     ", "  #  ", "     ", "     ", "  #  ", " #   ", "     "],
        ['<'] = ["   # ", "  #  ", " #   ", "#    ", " #   ", "  #  ", "   # "],
        ['='] = ["     ", "#####", "     ", "#####", "     ", "     ", "     "],
        ['>'] = [" #   ", "  #  ", "   # ", "    #", "   # ", "  #  ", " #   "],
        ['?'] = [" ### ", "#   #", "    #", "  ## ", "  #  ", "     ", "  #  "],
        ['@'] = [" ### ", "#   #", "# ###", "# # #", "# ###", "#    ", " ### "],
        ['A'] = [" ### ", "#   #", "#   #", "#####", "#   #", "#   #", "#   #"],
        ['B'] = ["#### ", "#   #", "#   #", "#### ", "#   #", "#   #", "#### "],
        ['C'] = [" ### ", "#   #", "#    ", "#    ", "#    ", "#   #", " ### "],
        ['D'] = ["#### ", "#   #", "#   #", "#   #", "#   #", "#   #", "#### "],
        ['E'] = ["#####", "#    ", "#    ", "#### ", "#    ", "#    ", "#####"],
        ['F'] = ["#####", "#    ", "#    ", "#### ", "#    ", "#    ", "#    "],
        ['G'] = [" ### ", "#   #", "#    ", "# ###", "#   #", "#   #", " ### "],
        ['H'] = ["#   #", "#   #", "#   #", "#####", "#   #", "#   #", "#   #"],
        ['I'] = [" ### ", "  #  ", "  #  ", "  #  ", "  #  ", "  #  ", " ### "],
        ['J'] = ["  ###", "   # ", "   # ", "   # ", "   # ", "#  # ", " ##  "],
        ['K'] = ["#   #", "#  # ", "# #  ", "##   ", "# #  ", "#  # ", "#   #"],
        ['L'] = ["#    ", "#    ", "#    ", "#    ", "#    ", "#    ", "#####"],
        ['M'] = ["#   #", "## ##", "# # #", "#   #", "#   #", "#   #", "#   #"],
        ['N'] = ["#   #", "##  #", "# # #", "#  ##", "#   #", "#   #", "#   #"],
        ['O'] = [" ### ", "#   #", "#   #", "#   #", "#   #", "#   #", " ### "],
        ['P'] = ["#### ", "#   #", "#   #", "#### ", "#    ", "#    ", "#    "],
        ['Q'] = [" ### ", "#   #", "#   #", "#   #", "# # #", "#  # ", " ## #"],
        ['R'] = ["#### ", "#   #", "#   #", "#### ", "# #  ", "#  # ", "#   #"],
        ['S'] = [" ####", "#    ", "#    ", " ### ", "    #", "    #", "#### "],
        ['T'] = ["#####", "  #  ", "  #  ", "  #  ", "  #  ", "  #  ", "  #  "],
        ['U'] = ["#   #", "#   #", "#   #", "#   #", "#   #", "#   #", " ### "],
        ['V'] = ["#   #", "#   #", "#   #", "#   #", "#   #", " # # ", "  #  "],
        ['W'] = ["#   #", "#   #", "#   #", "# # #", "# # #", "## ##", "#   #"],
        ['X'] = ["#   #", "#   #", " # # ", "  #  ", " # # ", "#   #", "#   #"],
        ['Y'] = ["#   #", "#   #", " # # ", "  #  ", "  #  ", "  #  ", "  #  "],
        ['Z'] = ["#####", "    #", "   # ", "  #  ", " #   ", "#    ", "#####"],
        ['А'] = [" ### ", "#   #", "#   #", "#####", "#   #", "#   #", "#   #"],
        ['Б'] = ["#####", "#    ", "#    ", "#### ", "#   #", "#   #", "#### "],
        ['В'] = ["#### ", "#   #", "#   #", "#### ", "#   #", "#   #", "#### "],
        ['Г'] = ["#####", "#    ", "#    ", "#    ", "#    ", "#    ", "#    "],
        ['Д'] = [" ### ", " # # ", " # # ", " # # ", "#   #", "#####", "#   #"],
        ['Е'] = ["#####", "#    ", "#    ", "#### ", "#    ", "#    ", "#####"],
        ['Ё'] = [" # # ", "#####", "#    ", "#### ", "#    ", "#    ", "#####"],
        ['Ж'] = ["# # #", "# # #", " ### ", "  #  ", " ### ", "# # #", "# # #"],
        ['З'] = [" ### ", "#   #", "    #", "  ## ", "    #", "#   #", " ### "],
        ['И'] = ["#   #", "#  ##", "# # #", "##  #", "#   #", "#   #", "#   #"],
        ['Й'] = [" # # ", "  #  ", "#   #", "#  ##", "# # #", "##  #", "#   #"],
        ['К'] = ["#   #", "#  # ", "# #  ", "##   ", "# #  ", "#  # ", "#   #"],
        ['Л'] = ["  ###", " #  #", " #  #", " #  #", " #  #", "#   #", "#   #"],
        ['М'] = ["#   #", "## ##", "# # #", "#   #", "#   #", "#   #", "#   #"],
        ['Н'] = ["#   #", "#   #", "#   #", "#####", "#   #", "#   #", "#   #"],
        ['О'] = [" ### ", "#   #", "#   #", "#   #", "#   #", "#   #", " ### "],
        ['П'] = ["#####", "#   #", "#   #", "#   #", "#   #", "#   #", "#   #"],
        ['Р'] = ["#### ", "#   #", "#   #", "#### ", "#    ", "#    ", "#    "],
        ['С'] = [" ### ", "#   #", "#    ", "#    ", "#    ", "#   #", " ### "],
        ['Т'] = ["#####", "  #  ", "  #  ", "  #  ", "  #  ", "  #  ", "  #  "],
        ['У'] = ["#   #", "#   #", "#   #", " ####", "    #", "#   #", " ### "],
        ['Ф'] = [" ### ", "# # #", "# # #", "# # #", " ### ", "  #  ", "  #  "],
        ['Х'] = ["#   #", "#   #", " # # ", "  #  ", " # # ", "#   #", "#   #"],
        ['Ц'] = ["#   #", "#   #", "#   #", "#   #", "#   #", "#####", "    #"],
        ['Ч'] = ["#   #", "#   #", "#   #", " ####", "    #", "    #", "    #"],
        ['Ш'] = ["#   #", "#   #", "#   #", "# # #", "# # #", "# # #", "#####"],
        ['Щ'] = ["#   #", "#   #", "#   #", "# # #", "# # #", "#####", "    #"],
        ['Ъ'] = ["##   ", " #   ", " #   ", " ### ", " #  #", " #  #", " ### "],
        ['Ы'] = ["#   #", "#   #", "#   #", "##  #", "# # #", "# # #", "##  #"],
        ['Ь'] = ["#    ", "#    ", "#    ", "#### ", "#   #", "#   #", "#### "],
        ['Э'] = [" ### ", "#   #", "    #", "  ###", "    #", "#   #", " ### "],
        ['Ю'] = ["#  # ", "# # #", "# # #", "### #", "# # #", "# # #", "#  # "],
        ['Я'] = [" ####", "#   #", "#   #", " ####", "  # #", " #  #", "#   #"],
        ['Є'] = [" ### ", "#   #", "#    ", "#### ", "#    ", "#   #", " ### "],
        ['Ї'] = [" # # ", " ### ", "  #  ", "  #  ", "  #  ", "  #  ", " ### "],
        ['Ў'] = [" # # ", "  #  ", "#   #", "#   #", " ####", "    #", " ### "],
        ['['] = [" ### ", " #   ", " #   ", " #   ", " #   ", " ### ", "     "],
        ['\\'] = ["#    ", " #   ", "  #  ", "   # ", "    #", "     ", "     "],
        [']'] = [" ### ", "   # ", "   # ", "   # ", "   # ", " ### ", "     "],
        ['^'] = ["  #  ", " # # ", "#   #", "     ", "     ", "     ", "     "],
        ['_'] = ["     ", "     ", "     ", "     ", "     ", "     ", "#####"],
        ['`'] = [" #   ", "  #  ", "     ", "     ", "     ", "     ", "     "],
        ['{'] = ["   ##", "  #  ", "  #  ", "##   ", "  #  ", "  #  ", "   ##"],
        ['|'] = ["  #  ", "  #  ", "  #  ", "  #  ", "  #  ", "  #  ", "  #  "],
        ['}'] = ["##   ", "  #  ", "  #  ", "   ##", "  #  ", "  #  ", "##   "],
        ['~'] = ["     ", " ## #", "# ## ", "     ", "     ", "     ", "     "],
        ['°'] = [" ### ", "#   #", "#   #", " ### ", "     ", "     ", "     "],
        ['∙'] = ["     ", "     ", "     ", "  #  ", "     ", "     ", "     "],
        ['·'] = ["     ", "     ", "     ", "  #  ", "     ", "     ", "     "],
        ['√'] = ["    #", "    #", "   # ", "#  # ", " ##  ", "     ", "     "],
        ['№'] = ["#  # ", "## # ", "# ## ", "#  # ", "     ", "#### ", "     "],
        ['¤'] = ["     ", "#   #", " ### ", "# # #", " ### ", "#   #", "     "]
    };

    public static void DrawText(
        Span<uint> pixels,
        int width,
        int height,
        int x,
        int y,
        string text,
        uint foreground,
        long clipLeft,
        long clipTop,
        long clipRight,
        long clipBottom,
        uint? background = null)
    {
        ArgumentNullException.ThrowIfNull(text);

        var cursorX = x;
        var cursorY = y;
        foreach (var ch in text)
        {
            if (ch == '\r')
            {
                continue;
            }

            if (ch == '\n')
            {
                cursorX = x;
                cursorY += CellHeight;
                continue;
            }

            DrawGlyph(pixels, width, height, cursorX, cursorY, ch, foreground, clipLeft, clipTop, clipRight, clipBottom, background);
            cursorX += CellWidth;
        }
    }

    public static void DrawGlyph(
        Span<uint> pixels,
        int width,
        int height,
        int x,
        int y,
        char ch,
        uint foreground,
        long clipLeft,
        long clipTop,
        long clipRight,
        long clipBottom,
        uint? background)
    {
        if (IsFullBlock(ch))
        {
            FillCell(pixels, width, height, x, y, foreground, clipLeft, clipTop, clipRight, clipBottom);
            return;
        }

        if (TryDrawPartialBlock(pixels, width, height, x, y, ch, foreground, clipLeft, clipTop, clipRight, clipBottom, background))
        {
            return;
        }

        if (TryDrawBoxGlyph(pixels, width, height, x, y, ch, foreground, clipLeft, clipTop, clipRight, clipBottom, background))
        {
            return;
        }

        var glyph = GetGlyph(ch);
        for (var yy = 0; yy < CellHeight; yy++)
        {
            var py = y + yy;
            if (py < 0 || py >= height)
            {
                continue;
            }

            for (var xx = 0; xx < CellWidth; xx++)
            {
                var px = x + xx;
                if (px < 0 || px >= width || px < clipLeft || px > clipRight || py < clipTop || py > clipBottom)
                {
                    continue;
                }

                var isGlyphPixel = TryGetShadePixel(ch, xx, yy, out var shadePixel)
                    ? shadePixel
                    : yy >= 2 &&
                    yy < 9 &&
                    xx >= 1 &&
                    xx < 6 &&
                    glyph[yy - 2][xx - 1] != ' ';

                if (isGlyphPixel)
                {
                    pixels[(py * width) + px] = foreground;
                }
                else if (background.HasValue)
                {
                    pixels[(py * width) + px] = background.Value;
                }
            }
        }
    }

    private static void FillCell(Span<uint> pixels, int width, int height, int x, int y, uint color, long clipLeft, long clipTop, long clipRight, long clipBottom)
    {
        for (var yy = 0; yy < CellHeight; yy++)
        {
            var py = y + yy;
            if (py < 0 || py >= height || py < clipTop || py > clipBottom)
            {
                continue;
            }

            for (var xx = 0; xx < CellWidth; xx++)
            {
                var px = x + xx;
                if (px < 0 || px >= width || px < clipLeft || px > clipRight)
                {
                    continue;
                }

                pixels[(py * width) + px] = color;
            }
        }
    }

    private static bool IsFullBlock(char ch) =>
        ch is '\u2588' or '\u25A0';

    private static bool TryDrawPartialBlock(
        Span<uint> pixels,
        int width,
        int height,
        int x,
        int y,
        char ch,
        uint foreground,
        long clipLeft,
        long clipTop,
        long clipRight,
        long clipBottom,
        uint? background)
    {
        if (ch is not ('\u2580' or '\u2584' or '\u258C' or '\u2590'))
        {
            return false;
        }

        for (var yy = 0; yy < CellHeight; yy++)
        {
            for (var xx = 0; xx < CellWidth; xx++)
            {
                var isGlyphPixel = ch switch
                {
                    '\u2580' => yy < CellHeight / 2,
                    '\u2584' => yy >= CellHeight / 2,
                    '\u258C' => xx < CellWidth / 2,
                    '\u2590' => xx >= CellWidth / 2,
                    _ => false
                };

                PutCellPixel(pixels, width, height, x, y, xx, yy, isGlyphPixel, foreground, clipLeft, clipTop, clipRight, clipBottom, background);
            }
        }

        return true;
    }

    private static bool TryDrawBoxGlyph(
        Span<uint> pixels,
        int width,
        int height,
        int x,
        int y,
        char ch,
        uint foreground,
        long clipLeft,
        long clipTop,
        long clipRight,
        long clipBottom,
        uint? background)
    {
        var (left, right, up, down, thick) = ch switch
        {
            '\u2500' => (true, true, false, false, false),
            '\u2502' => (false, false, true, true, false),
            '\u250C' => (false, true, false, true, false),
            '\u2510' => (true, false, false, true, false),
            '\u2514' => (false, true, true, false, false),
            '\u2518' => (true, false, true, false, false),
            '\u251C' => (false, true, true, true, false),
            '\u2524' => (true, false, true, true, false),
            '\u252C' => (true, true, false, true, false),
            '\u2534' => (true, true, true, false, false),
            '\u253C' => (true, true, true, true, false),
            '\u2550' => (true, true, false, false, true),
            '\u2551' => (false, false, true, true, true),
            '\u2554' => (false, true, false, true, true),
            '\u2557' => (true, false, false, true, true),
            '\u255A' => (false, true, true, false, true),
            '\u255D' => (true, false, true, false, true),
            '\u2560' => (false, true, true, true, true),
            '\u2563' => (true, false, true, true, true),
            '\u2566' => (true, true, false, true, true),
            '\u2569' => (true, true, true, false, true),
            '\u256C' => (true, true, true, true, true),
            '\u2561' or '\u2562' => (true, false, true, true, true),
            '\u2555' or '\u2556' => (true, false, false, true, true),
            '\u255B' or '\u255C' => (true, false, true, false, true),
            '\u255E' or '\u255F' => (false, true, true, true, true),
            '\u2564' or '\u2565' => (true, true, false, true, true),
            '\u2567' or '\u2568' => (true, true, true, false, true),
            '\u256A' or '\u256B' => (true, true, true, true, true),
            _ => (false, false, false, false, false)
        };

        if (!left && !right && !up && !down)
        {
            return false;
        }

        var centerX = CellWidth / 2;
        var centerY = CellHeight / 2;
        var halfThickness = thick ? 1 : 0;
        for (var yy = 0; yy < CellHeight; yy++)
        {
            for (var xx = 0; xx < CellWidth; xx++)
            {
                var onHorizontal = Math.Abs(yy - centerY) <= halfThickness &&
                    ((left && xx <= centerX) || (right && xx >= centerX));
                var onVertical = Math.Abs(xx - centerX) <= halfThickness &&
                    ((up && yy <= centerY) || (down && yy >= centerY));

                PutCellPixel(pixels, width, height, x, y, xx, yy, onHorizontal || onVertical, foreground, clipLeft, clipTop, clipRight, clipBottom, background);
            }
        }

        return true;
    }

    private static bool TryGetShadePixel(char ch, int x, int y, out bool isGlyphPixel)
    {
        isGlyphPixel = ch switch
        {
            '\u2593' => (x + y) % 4 != 0,
            '\u2592' => (x + y) % 2 == 0,
            '\u2591' => (x + (y * 2)) % 5 == 0,
            _ => false
        };

        return ch is '\u2593' or '\u2592' or '\u2591';
    }

    private static void PutCellPixel(
        Span<uint> pixels,
        int width,
        int height,
        int x,
        int y,
        int cellX,
        int cellY,
        bool isGlyphPixel,
        uint foreground,
        long clipLeft,
        long clipTop,
        long clipRight,
        long clipBottom,
        uint? background)
    {
        var px = x + cellX;
        var py = y + cellY;
        if (px < 0 || px >= width || py < 0 || py >= height || px < clipLeft || px > clipRight || py < clipTop || py > clipBottom)
        {
            return;
        }

        if (isGlyphPixel)
        {
            pixels[(py * width) + px] = foreground;
        }
        else if (background.HasValue)
        {
            pixels[(py * width) + px] = background.Value;
        }
    }

    private static string[] GetGlyph(char ch)
    {
        if (Glyphs.TryGetValue(ch, out var glyph))
        {
            return glyph;
        }

        var upper = char.ToUpperInvariant(ch);
        if (Glyphs.TryGetValue(upper, out glyph))
        {
            return glyph;
        }

        return ["#####", "#   #", "# # #", "# # #", "#   #", "#####", "     "];
    }
}
