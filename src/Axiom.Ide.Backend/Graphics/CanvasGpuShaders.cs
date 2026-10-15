using Vortice.D3DCompiler;

namespace Axiom.Ide.Graphics;

internal static class CanvasGpuShaders
{
    private static readonly Lazy<ReadOnlyMemory<byte>> ComputeBytecode = new(() =>
        Compiler.Compile(Source, "CSMain", "AxiomCanvas", "cs_5_0", ShaderFlags.OptimizationLevel3));
    public static ReadOnlyMemory<byte> Compute => ComputeBytecode.Value;
    private static readonly Lazy<(ReadOnlyMemory<byte> Vertex, ReadOnlyMemory<byte> Pixel)> Presentation = new(() => (
        Compiler.Compile(PresentationSource, "VSMain", "AxiomCanvasPresent", "vs_5_0", ShaderFlags.OptimizationLevel3),
        Compiler.Compile(PresentationSource, "PSMain", "AxiomCanvasPresent", "ps_5_0", ShaderFlags.OptimizationLevel3)));
    public static ReadOnlyMemory<byte> Vertex => Presentation.Value.Vertex;
    public static ReadOnlyMemory<byte> Pixel => Presentation.Value.Pixel;

    private const string PresentationSource = """
        Texture2D<uint2> Frame : register(t0);
        cbuffer FrameParameters : register(b0) { uint4 Sizes; uint4 Options; };
        struct VertexOutput { float4 position : SV_POSITION; };
        VertexOutput VSMain(uint id : SV_VertexID)
        {
            float2 uv = float2((id << 1) & 2, id & 2);
            VertexOutput result; result.position = float4(uv.x * 2 - 1, 1 - uv.y * 2, 0, 1); return result;
        }
        float4 pixel(int2 p)
        {
            p = clamp(p, int2(0, 0), int2(Sizes.xy) - 1);
            uint c = Frame.Load(int3(p, 0)).x;
            return float4((c >> 16) & 255, (c >> 8) & 255, c & 255, 255) / 255.0;
        }
        float4 PSMain(VertexOutput input) : SV_TARGET
        {
            uint2 p = uint2(input.position.xy) - Options.xy;
            if (Options.z == 0 || all(Sizes.xy == Sizes.zw)) return pixel((p * Sizes.xy) / Sizes.zw);
            float2 q = (float2(p) + 0.5) * float2(Sizes.xy) / float2(Sizes.zw) - 0.5;
            int2 origin = int2(floor(q)); float2 f = frac(q);
            return lerp(lerp(pixel(origin), pixel(origin + int2(1, 0)), f.x),
                        lerp(pixel(origin + int2(0, 1)), pixel(origin + int2(1, 1)), f.x), f.y);
        }
        """;

    private const string Source = """
        RWTexture2D<uint2> Target : register(u0);
        Texture2D<uint> Image : register(t0);
        Texture2D<uint2> Overlay : register(t1);
        cbuffer Parameters : register(b0)
        {
            int4 Rect;
            int4 Origin;
            uint4 Values;
            uint4 Options;
        };
        uint blend(uint source, uint dest)
        {
            uint a = source >> 24;
            uint b = ((source & 255) * a + (dest & 255) * (255 - a) + 127) / 255;
            uint g = (((source >> 8) & 255) * a + ((dest >> 8) & 255) * (255 - a) + 127) / 255;
            uint r = (((source >> 16) & 255) * a + ((dest >> 16) & 255) * (255 - a) + 127) / 255;
            return 0xff000000 | (r << 16) | (g << 8) | b;
        }
        void plot(int2 p)
        {
            if (all(p >= Rect.xy) && all(p < Rect.xy + Rect.zw))
                Target[p] = uint2(Values.z, Values.w);
        }
        [numthreads(8, 8, 1)]
        void CSMain(uint3 id : SV_DispatchThreadID)
        {
            if (Options.x == 9)
            {
                if (id.x >= (uint)Rect.z || id.y >= (uint)Rect.w || id.y * (uint)Rect.z + id.x >= (uint)Origin.x) return;
                uint packedPoint = Image.Load(int3(id.xy, 0));
                Target[int2(packedPoint & 65535, packedPoint >> 16)] = uint2(Values.z, Values.w);
                return;
            }
            if (Options.x == 3)
            {
                if (any(id != 0)) return;
                int2 p = Origin.xy, end = Origin.zw;
                int dx = abs(end.x - p.x), dy = -abs(end.y - p.y);
                int sx = p.x < end.x ? 1 : -1, sy = p.y < end.y ? 1 : -1;
                int error = dx + dy;
                for (int i = 0; i <= max(dx, -dy); i++)
                {
                    plot(p);
                    if (all(p == end)) break;
                    int twice = error * 2;
                    if (twice >= dy) { error += dy; p.x += sx; }
                    if (twice <= dx) { error += dx; p.y += sy; }
                }
                return;
            }
            if (Options.x == 4)
            {
                if (any(id != 0)) return;
                int x = 0, y = Origin.z;
                float dx = 0, dy = 2 * y, d = 1 - y + 0.25;
                for (int phase = 0; phase < 2; phase++)
                {
                    if (phase == 1) d = (x + 0.5) * (x + 0.5) + (y - 1.0) * (y - 1.0) - Origin.z * Origin.z;
                    for (int i = 0; i <= Origin.z * 2 + 4; i++)
                    {
                        if (phase == 0) {
                            plot(Origin.xy + int2(x, -y)); plot(Origin.xy + int2(-x, -y));
                            plot(Origin.xy + int2(-x, y)); plot(Origin.xy + int2(x, y));
                            if (dx >= dy) break;
                            x++; dx += 2;
                            if (d < 0) d += dx + 1;
                            else { y--; dy -= 2; d += dx - dy + 1; }
                        }
                        else {
                            if (y <= 0) break;
                            y--; dy -= 2;
                            if (d > 0) d += 1 - dy;
                            else { x++; dx += 2; d += dx - dy + 1; }
                            plot(Origin.xy + int2(x, -y)); plot(Origin.xy + int2(-x, -y));
                            plot(Origin.xy + int2(-x, y)); plot(Origin.xy + int2(x, y));
                        }
                    }
                }
                return;
            }
            if (id.x >= (uint)Rect.z || id.y >= (uint)Rect.w) return;
            int2 p = Rect.xy + int2(id.xy);
            if (Options.x == 10) { Target[p] = uint2(0, 0xffffffff); return; }
            if (Options.x == 11)
            {
                uint source = Overlay.Load(int3(p, 0)).x;
                if ((source >> 24) != 0) Target[p] = uint2(blend(source, Target[p].x), 0xffffffff);
                return;
            }
            if (Options.x == 8)
            {
                uint2 pixel = Target[p];
                if (pixel.y != 0xffffffff) Target[p] = uint2(Image.Load(int3(pixel.y, 0, 0)), pixel.y);
                return;
            }
            if (Options.x == 7) { Target[p] = uint2(Target[p].x, 0xffffffff); return; }
            if (Options.x == 2)
            {
                int2 d = p - Origin.xy;
                if (dot(d, d) <= Origin.z * Origin.z) Target[p] = uint2(Values.z, Values.w);
                return;
            }
            if (Options.x == 0)
            {
                Target[p] = uint2(Values.z, Values.w);
                return;
            }
            uint2 relative = uint2(p - Origin.xy);
            uint2 sourcePixel = (relative * Values.xy) >> 16;
            uint source = Image.Load(int3(Origin.zw + int2(sourcePixel), 0));
            if (Options.x == 5) source = (source & 0xff000000) | (Values.z & 0xffffff);
            if ((source >> 24) == 0) return;
            Target[p] = uint2(blend(source, Target[p].x), 0xffffffff);
        }
        """;
}
