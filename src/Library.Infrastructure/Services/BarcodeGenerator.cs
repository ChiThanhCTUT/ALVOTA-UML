using Library.Application.Common.Interfaces;

namespace Library.Infrastructure.Services;

// CAT-05: Sinh mã vạch Code128 chuẩn xuất ra ảnh BMP/PNG
public sealed class BarcodeGenerator : IBarcodeGenerator
{
    private static readonly string[] Code128Patterns = new string[]
    {
        "212222", "222122", "222221", "121223", "121322", "131222", "122213", "122312", "132212", "221213",
        "221312", "231212", "112232", "122132", "122231", "113222", "123122", "123221", "223211", "221132",
        "221231", "213212", "223112", "312131", "311222", "321122", "321221", "312212", "322112", "322211",
        "212123", "212321", "232121", "111323", "131123", "131321", "112313", "132113", "132311", "211313",
        "231113", "231311", "112133", "112331", "132131", "113123", "113321", "133121", "313121", "211331",
        "231131", "213113", "213311", "213131", "311123", "311321", "331121", "312113", "312311", "332111",
        "314111", "221411", "431111", "111224", "111422", "121124", "121421", "141122", "141221", "112214",
        "112412", "122114", "122411", "142112", "142211", "241211", "221114", "413111", "241112", "134111",
        "111242", "121142", "121241", "114212", "124112", "124211", "411212", "421112", "421211", "212141",
        "214121", "412121", "111143", "111341", "131141", "114113", "114311", "411113", "411311", "113141",
        "114131", "311141", "411131", "211412", "211214", "211232", "2331112"
    };

    public byte[] GenerateCode128Barcode(string content, int width = 300, int height = 90)
    {
        // Encode Code128 Code B
        var patternBuilder = new System.Text.StringBuilder();
        patternBuilder.Append(Code128Patterns[104]); // Start B

        var checkSum = 104;
        for (int i = 0; i < content.Length; i++)
        {
            var charCode = (int)content[i] - 32;
            if (charCode < 0 || charCode > 95) charCode = 0;
            checkSum += (i + 1) * charCode;
            patternBuilder.Append(Code128Patterns[charCode]);
        }

        var checkDigit = checkSum % 103;
        patternBuilder.Append(Code128Patterns[checkDigit]);
        patternBuilder.Append(Code128Patterns[106]); // Stop

        var modules = new List<bool>();
        var pattern = patternBuilder.ToString();
        var isBar = true;
        foreach (var ch in pattern)
        {
            var count = ch - '0';
            for (int k = 0; k < count; k++)
            {
                modules.Add(isBar);
            }
            isBar = !isBar;
        }

        return CreateBmpImage(modules, width, height);
    }

    private static byte[] CreateBmpImage(List<bool> modules, int targetWidth, int targetHeight)
    {
        var totalModules = modules.Count;
        var scale = Math.Max(1, (targetWidth - 20) / totalModules);
        var actualWidth = totalModules * scale + 20;
        var actualHeight = targetHeight;

        var rowPadding = (4 - (actualWidth * 3) % 4) % 4;
        var rowSize = actualWidth * 3 + rowPadding;
        var imageSize = rowSize * actualHeight;
        var fileSize = 54 + imageSize;

        using var ms = new MemoryStream(fileSize);
        using var bw = new BinaryWriter(ms);

        // BMP Header
        bw.Write((byte)'B');
        bw.Write((byte)'M');
        bw.Write(fileSize);
        bw.Write(0); // Reserved
        bw.Write(54); // Offset

        // DIB Header
        bw.Write(40); // Header size
        bw.Write(actualWidth);
        bw.Write(actualHeight);
        bw.Write((short)1); // Color planes
        bw.Write((short)24); // Bits per pixel
        bw.Write(0); // Compression
        bw.Write(imageSize);
        bw.Write(2835); // Horizontal resolution
        bw.Write(2835); // Vertical resolution
        bw.Write(0); // Colors in color table
        bw.Write(0); // Important color count

        // Pixels (bottom-to-top)
        var rowBytes = new byte[rowSize];
        for (int y = 0; y < actualHeight; y++)
        {
            Array.Clear(rowBytes, 0, rowBytes.Length);
            // Fill with white
            for (int p = 0; p < actualWidth * 3; p++) rowBytes[p] = 255;

            // Draw bars if inside bar area (leave bottom 10px and top 10px for margins)
            if (y >= 10 && y <= actualHeight - 10)
            {
                var offset = 10;
                for (int m = 0; m < totalModules; m++)
                {
                    if (modules[m]) // Black bar
                    {
                        for (int s = 0; s < scale; s++)
                        {
                            var px = (offset + m * scale + s) * 3;
                            if (px + 2 < rowBytes.Length)
                            {
                                rowBytes[px] = 0;     // B
                                rowBytes[px + 1] = 0; // G
                                rowBytes[px + 2] = 0; // R
                            }
                        }
                    }
                }
            }

            bw.Write(rowBytes);
        }

        return ms.ToArray();
    }
}
