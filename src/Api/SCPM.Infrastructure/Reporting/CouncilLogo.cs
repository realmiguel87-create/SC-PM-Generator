using System.Buffers.Binary;
using System.Reflection;

namespace SCPM.Infrastructure.Reporting;

/// <summary>
/// The council logo, as PNG bytes, for the footer of a branded document.
///
/// Held as an embedded resource and resolved at runtime, and absent is a supported state rather
/// than an error: the file has not been supplied yet, so every document currently renders a
/// text-only footer. Dropping `Assets/stirling-logo.png` into this project is the whole of adding
/// it — no code change, and nothing to reconfigure.
///
/// A transparent background matters. A PNG with a white matte will show as a pale rectangle
/// against the footer rule, which is more obvious on paper than on screen.
/// </summary>
internal static class CouncilLogo
{
    private const string ResourceName = "SCPM.Infrastructure.Reporting.Assets.stirling-logo.png";

    private static readonly Lazy<byte[]?> Loaded = new(Load);

    /// <summary>The logo's bytes, or null when no logo file has been embedded.</summary>
    public static byte[]? Png => Loaded.Value;

    private static byte[]? Load()
    {
        using var stream = typeof(CouncilLogo).Assembly.GetManifestResourceStream(ResourceName);
        if (stream is null) return null;

        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }

    /// <summary>
    /// The pixel dimensions in a PNG's IHDR chunk, or null when the bytes are not a PNG.
    ///
    /// Read rather than assumed because the drawing in a Word document is sized in EMU, and
    /// picking a width without knowing the aspect ratio is how a logo ends up stretched. The IHDR
    /// is always the first chunk and always at a fixed offset, so this needs no image library —
    /// which matters, since the alternative is a native dependency on a build server for the sake
    /// of two integers.
    /// </summary>
    public static (int Width, int Height)? Dimensions(byte[] png)
    {
        // 8-byte signature, 4-byte length, "IHDR", then width and height as big-endian uint32.
        if (png.Length < 24) return null;

        ReadOnlySpan<byte> signature = [0x89, (byte)'P', (byte)'N', (byte)'G', 0x0D, 0x0A, 0x1A, 0x0A];
        if (!png.AsSpan(0, 8).SequenceEqual(signature)) return null;

        if (!png.AsSpan(12, 4).SequenceEqual("IHDR"u8)) return null;

        var width = BinaryPrimitives.ReadUInt32BigEndian(png.AsSpan(16, 4));
        var height = BinaryPrimitives.ReadUInt32BigEndian(png.AsSpan(20, 4));

        // A zero dimension is a malformed header, and would divide by zero when scaling.
        if (width == 0 || height == 0) return null;

        // Guarded so a corrupt header cannot produce a negative size after the cast.
        if (width > int.MaxValue || height > int.MaxValue) return null;

        return ((int)width, (int)height);
    }
}
