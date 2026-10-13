using SharpGen.Runtime;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;
using static Vortice.Direct3D11.D3D11;

namespace Axiom.Ide.Graphics;

internal readonly record struct CanvasDeviceAttempt(Result Result, ID3D11Device? Device, ID3D11DeviceContext? Context);

internal static class CanvasDeviceFactory
{
    public static CanvasDeviceAttempt Create(DriverType driver)
    {
        var result = D3D11CreateDevice(null, driver, DeviceCreationFlags.BgraSupport,
            [FeatureLevel.Level_11_0], out ID3D11Device? device, out _, out ID3D11DeviceContext? context);
        if (result.Success && device is not null)
        {
            var required = FormatSupport2.UnorderedAccessViewTypedLoad | FormatSupport2.UnorderedAccessViewTypedStore;
            if ((device.CheckFeatureFormatSupport2(Format.R32G32_UInt) & required) != required)
                result = new Result(unchecked((int)0x887A0004));
        }
        return new(result, device, context);
    }

    public static (ID3D11Device Device, ID3D11DeviceContext Context, string Name) Select(bool software,
        Func<DriverType, CanvasDeviceAttempt>? create = null)
    {
        create ??= Create;
        var driver = software ? DriverType.Warp : DriverType.Hardware;
        var attempt = create(driver);
        if (attempt.Result.Failure && !software)
        {
            attempt.Context?.Dispose(); attempt.Device?.Dispose();
            driver = DriverType.Warp; attempt = create(driver);
        }
        if (attempt.Result.Failure || attempt.Device is null || attempt.Context is null)
        {
            attempt.Context?.Dispose(); attempt.Device?.Dispose();
            attempt.Result.CheckError();
            throw new InvalidOperationException("Не удалось создать устройство CANVAS.");
        }
        return (attempt.Device, attempt.Context, driver == DriverType.Hardware ? "Hardware" : "WARP");
    }
}
