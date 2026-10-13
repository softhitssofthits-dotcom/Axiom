using Vortice.D3DCompiler;

namespace Axiom.Ide.Graphics;

internal static class ScreenFrameShaders
{
    // Компилируются один раз на процесс, а не при каждом кадре или открытии SCREEN.
    private static readonly Lazy<(ReadOnlyMemory<byte> Vertex, ReadOnlyMemory<byte> Pixel)> Bytecode = new(() => (
        Compiler.Compile(Source, "VSMain", "AxiomScreenFrame", "vs_4_0", ShaderFlags.OptimizationLevel3),
        Compiler.Compile(Source, "PSMain", "AxiomScreenFrame", "ps_4_0", ShaderFlags.OptimizationLevel3)));

    public static ReadOnlyMemory<byte> Vertex => Bytecode.Value.Vertex;
    public static ReadOnlyMemory<byte> Pixel => Bytecode.Value.Pixel;

    private const string Source = """
        Texture2D Frame : register(t0);
        SamplerState LinearClamp : register(s0);
        cbuffer FrameParameters : register(b0)
        {
            uint4 Sizes;   // исходные ширина/высота, выходные ширина/высота
            uint4 Options; // смещение x/y, признак SMOOTH, резерв
        };
        struct VertexOutput { float4 position : SV_POSITION; };
        VertexOutput VSMain(uint id : SV_VertexID)
        {
            float2 uv = float2((id << 1) & 2, id & 2);
            VertexOutput result;
            result.position = float4(uv.x * 2 - 1, 1 - uv.y * 2, 0, 1);
            return result;
        }
        float4 PSMain(VertexOutput input) : SV_TARGET
        {
            uint2 pixel = uint2(input.position.xy) - Options.xy;
            if (Options.z != 0 && any(Sizes.xy != Sizes.zw))
                return Frame.SampleLevel(LinearClamp, (float2(pixel) + 0.5) / float2(Sizes.zw), 0);
            // Целочисленная выборка сохраняет прежние пиксели SMOOTH OFF.
            return Frame.Load(int3((pixel * Sizes.xy) / Sizes.zw, 0));
        }
        """;
}
