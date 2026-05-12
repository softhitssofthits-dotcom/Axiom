using System.Text;

namespace Axiom.Runtime;

internal static class RuntimeCharacterEncoding
{
    private static readonly Rune[] Cp866ExtendedRunes =
    [
        new('\u0410'), new('\u0411'), new('\u0412'), new('\u0413'), new('\u0414'), new('\u0415'), new('\u0416'), new('\u0417'),
        new('\u0418'), new('\u0419'), new('\u041A'), new('\u041B'), new('\u041C'), new('\u041D'), new('\u041E'), new('\u041F'),
        new('\u0420'), new('\u0421'), new('\u0422'), new('\u0423'), new('\u0424'), new('\u0425'), new('\u0426'), new('\u0427'),
        new('\u0428'), new('\u0429'), new('\u042A'), new('\u042B'), new('\u042C'), new('\u042D'), new('\u042E'), new('\u042F'),
        new('\u0430'), new('\u0431'), new('\u0432'), new('\u0433'), new('\u0434'), new('\u0435'), new('\u0436'), new('\u0437'),
        new('\u0438'), new('\u0439'), new('\u043A'), new('\u043B'), new('\u043C'), new('\u043D'), new('\u043E'), new('\u043F'),
        new('\u2591'), new('\u2592'), new('\u2593'), new('\u2502'), new('\u2524'), new('\u2561'), new('\u2562'), new('\u2556'),
        new('\u2555'), new('\u2563'), new('\u2551'), new('\u2557'), new('\u255D'), new('\u255C'), new('\u255B'), new('\u2510'),
        new('\u2514'), new('\u2534'), new('\u252C'), new('\u251C'), new('\u2500'), new('\u253C'), new('\u255E'), new('\u255F'),
        new('\u255A'), new('\u2554'), new('\u2569'), new('\u2566'), new('\u2560'), new('\u2550'), new('\u256C'), new('\u2567'),
        new('\u2568'), new('\u2564'), new('\u2565'), new('\u2559'), new('\u2558'), new('\u2552'), new('\u2553'), new('\u256B'),
        new('\u256A'), new('\u2518'), new('\u250C'), new('\u2588'), new('\u2584'), new('\u258C'), new('\u2590'), new('\u2580'),
        new('\u0440'), new('\u0441'), new('\u0442'), new('\u0443'), new('\u0444'), new('\u0445'), new('\u0446'), new('\u0447'),
        new('\u0448'), new('\u0449'), new('\u044A'), new('\u044B'), new('\u044C'), new('\u044D'), new('\u044E'), new('\u044F'),
        new('\u0401'), new('\u0451'), new('\u0404'), new('\u0454'), new('\u0407'), new('\u0457'), new('\u040E'), new('\u045E'),
        new('\u00B0'), new('\u2219'), new('\u00B7'), new('\u221A'), new('\u2116'), new('\u00A4'), new('\u25A0'), new('\u00A0')
    ];

    private static readonly Lazy<IReadOnlyDictionary<int, int>> Cp866CodesByRune = new(CreateCp866CodesByRune);

    public static string Chr(int code)
    {
        if (code < 256)
        {
            return code < 128
                ? new Rune(code).ToString()
                : Cp866ExtendedRunes[code - 128].ToString();
        }

        return new Rune(code).ToString();
    }

    public static string ChrW(int code)
    {
        return new Rune(code).ToString();
    }

    public static int Asc(Rune rune)
    {
        if (rune.Value < 128)
        {
            return rune.Value;
        }

        return Cp866CodesByRune.Value.TryGetValue(rune.Value, out var code)
            ? code
            : rune.Value;
    }

    public static int AscW(Rune rune)
    {
        return rune.Value;
    }

    private static IReadOnlyDictionary<int, int> CreateCp866CodesByRune()
    {
        var result = new Dictionary<int, int>();
        for (var index = 0; index < Cp866ExtendedRunes.Length; index++)
        {
            result.TryAdd(Cp866ExtendedRunes[index].Value, index + 128);
        }

        return result;
    }
}
