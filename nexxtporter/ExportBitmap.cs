using SkiaSharp;

namespace nexxtporter
{
    public enum BitmapLayout { Linear, Rect, Rect8By16 }

    class ExportBitmapConfig
    {
        public static readonly BitmapLayout DefaultBitmapLayout = BitmapLayout.Linear;

        public string? TargetFile { get; set; }
        public string? StartTileIndex { get; set; }
        public string? TileCount { get; set; }
        public BitmapLayout? Layout { get; set; }
        public string? RGBLookupID { get; set; }
        public string? PaletteSetIndex { get; set; }
        public string? PaletteIndex { get; set; }
    }

    static class ExportBitmap
    {
        public static async Task ProcessExportBitmap(Logger log, NexxtporterConfig config, NSSConfig nssConfig, ExportBitmapConfig bitmapConfig, Dictionary<string, string> tokens, List<RGBLookup> rgbLookupTables)
        {
            if (string.IsNullOrWhiteSpace(bitmapConfig.TargetFile))
            {
                log.LogError("ExportBitmapConfig missing or empty TargetFile attribute", null);
                return;
            }
            if (string.IsNullOrWhiteSpace(bitmapConfig.RGBLookupID))
            {
                bitmapConfig.RGBLookupID = "Default";
            }
            if (string.IsNullOrWhiteSpace(bitmapConfig.StartTileIndex))
            {
                bitmapConfig.StartTileIndex = "0";
            }
            if (string.IsNullOrWhiteSpace(bitmapConfig.TileCount))
            {
                bitmapConfig.TileCount = "256";
            }
            if (!bitmapConfig.Layout.HasValue)
            {
                bitmapConfig.Layout = ExportBitmapConfig.DefaultBitmapLayout;
            }
            if (string.IsNullOrWhiteSpace(bitmapConfig.PaletteSetIndex))
            {
                bitmapConfig.PaletteSetIndex = "0";
            }
            if (string.IsNullOrWhiteSpace(bitmapConfig.PaletteIndex))
            {
                bitmapConfig.PaletteIndex = "0";
            }

            int startTileIndex = 0;
            int tileCount = 256;
            if (!Utils.TryParseNumber(bitmapConfig.StartTileIndex, out startTileIndex))
            {
                log.LogError(string.Format("NSS file {0}, bitmap export to {1}, has invalid StartTileIndex attribute {2}", nssConfig.SourceFile, bitmapConfig.TargetFile, bitmapConfig.StartTileIndex), null);
                return;
            }
            if (!Utils.TryParseNumber(bitmapConfig.TileCount, out tileCount))
            {
                log.LogError(string.Format("NSS file {0}, bitmap export to {1}, has invalid TileCount attribute {2}", nssConfig.SourceFile, bitmapConfig.TargetFile, bitmapConfig.TileCount), null);
                return;
            }

            int paletteSetIndex = 0;
            int paletteIndex = 0;
            if (!Utils.TryParseNumber(bitmapConfig.PaletteSetIndex, out paletteSetIndex))
            {
                log.LogError(string.Format("NSS file {0}, bitmap export to {1}, has invalid PaletteSetIndex attribute {2}", nssConfig.SourceFile, bitmapConfig.TargetFile, bitmapConfig.PaletteSetIndex), null);
                return;
            }
            if (paletteSetIndex < 0 || paletteSetIndex >= 4)
            {
                log.LogError(string.Format("NSS file {0}, bitmap export to {1}, has out of bounds PaletteSetIndex attribute {2}", nssConfig.SourceFile, bitmapConfig.TargetFile, paletteSetIndex), null);
                return;
            }
            if (!Utils.TryParseNumber(bitmapConfig.PaletteIndex, out paletteIndex))
            {
                log.LogError(string.Format("NSS file {0}, bitmap export to {1}, has invalid PaletteIndex attribute {2}", nssConfig.SourceFile, bitmapConfig.TargetFile, bitmapConfig.PaletteIndex), null);
                return;
            }
            if (paletteIndex < 0 || paletteIndex >= 4)
            {
                log.LogError(string.Format("NSS file {0}, bitmap export to {1}, has out of bounds PaletteIndex attribute {2}", nssConfig.SourceFile, bitmapConfig.TargetFile, paletteIndex), null);
                return;
            }

            if (!tokens.ContainsKey("Palette"))
            {
                log.LogError(string.Format("NSS file {0}, bitmap export to {1}, is missing Palette token", nssConfig.SourceFile, bitmapConfig.TargetFile), null);
                return;
            }
            byte[] paletteData = Utils.ParseRLEBinary(log, nssConfig, "Palette", tokens["Palette"]);
            if (paletteData.Length != 64)
            {
                log.LogError(string.Format("NSS file {0}, bitmap export to {1}, has invalid Palette token with length {2}", nssConfig.SourceFile, bitmapConfig.TargetFile, paletteData.Length), null);
                return;
            }
            PaletteSet[] paletteSets = new PaletteSet[4];
            for (int i = 0; i < 4; i++)
            {
                PaletteSet? paletteSet = PaletteSet.ParsePaletteSet(paletteData, 16 * i);
                if (paletteSet == null)
                {
                    log.LogError(string.Format("NSS file {0}, bitmap export to {1}, has invalid PaletteSet in position {2}", nssConfig.SourceFile, bitmapConfig.TargetFile, i), null);
                    return;
                }
                paletteSets[i] = paletteSet;
            }

            RGBLookup rgbLookup = rgbLookupTables.GetByID(bitmapConfig.RGBLookupID);

            if (!tokens.ContainsKey("CHRMain"))
            {
                log.LogError(string.Format("NSS file {0}, bitmap export to {1}, is missing CHRMain token", nssConfig.SourceFile, bitmapConfig.TargetFile), null);
                return;
            }
            byte[] chrData = Utils.ParseRLEBinary(log, nssConfig, "CHRMain", tokens["CHRMain"]);
            if (startTileIndex * 16 + tileCount * 16 > chrData.Length)
            {
                log.LogError(string.Format("NSS file {0}, CHRMain is {0} bytes which is smaller than the requested StartTileIndex {1} and TileCount {2}", nssConfig.SourceFile, chrData.Length, startTileIndex, tileCount), null);
                return;
            }

            Pattern[] patterns = new Pattern[tileCount];
            for (int i = 0; i < tileCount; i++)
            {
                Pattern? pattern = Pattern.ParsePattern(chrData, startTileIndex * 16 + 16 * i);
                if (pattern == null)
                {
                    log.LogError(string.Format("NSS file {0}, bitmap export to {1}, failed to parse pattern {2}", nssConfig.SourceFile, bitmapConfig.TargetFile, i), null);
                    return;
                }
                patterns[i] = pattern;
            }

            (int Width, int Height) outputSize = MeasureOutputSize(log, bitmapConfig.Layout.Value, patterns.Length);

            SKBitmap? outputBitmap = null;
            try
            {
                outputBitmap = new SKBitmap(outputSize.Width, outputSize.Height);
                // Fill with global background
                int backgroundColorIndex = paletteSets[paletteSetIndex].Palettes[paletteIndex].ParsedData[0];
                outputBitmap.Erase(rgbLookup.Colors[backgroundColorIndex]);

                switch (bitmapConfig.Layout)
                {
                    case BitmapLayout.Linear:
                        WriteLinearLayout(outputBitmap, patterns, paletteSets[paletteSetIndex].Palettes[paletteIndex], rgbLookup);
                        break;
                    case BitmapLayout.Rect:
                        WriteRectLayout(outputBitmap, patterns, paletteSets[paletteSetIndex].Palettes[paletteIndex], rgbLookup);
                        break;
                    case BitmapLayout.Rect8By16:
                        WriteRect8by16Layout(outputBitmap, patterns, paletteSets[paletteSetIndex].Palettes[paletteIndex], rgbLookup);
                        break;
                    default:
                        log.LogError(string.Format("Unknown BitmapLayout value {0}", bitmapConfig.Layout), null);
                        return;
                }

                log.Log(string.Format("Writing {0}x{1} bitmap to {2}", outputSize.Width, outputSize.Height, bitmapConfig.TargetFile));
                await Utils.WriteSKBitmap(log, bitmapConfig.TargetFile, outputBitmap);
            }
            catch (Exception e)
            {
                log.LogError("Exception while creating Skia bitmap", e);
            }
            finally
            {
                if (outputBitmap != null)
                {
                    outputBitmap.Dispose();
                }
            }
        }

        private static (int Width, int Height) MeasureOutputSize(Logger log, BitmapLayout layout, int tileCount)
        {
            switch (layout)
            {
                case BitmapLayout.Linear:
                    // Output bitmap will be 8 pixels tall and 8 * tileCount wide
                    return (8 * tileCount, 8);
                case BitmapLayout.Rect:
                    // Output bitmap will be at most 128 pixels (16 tiles) wide and rectangular
                    int rectWidth = Math.Min(128, 8 * tileCount);
                    int rectHeight = 8 * (int)Math.Ceiling((float)tileCount / 16.0f);
                    return (rectWidth, rectHeight);
                case BitmapLayout.Rect8By16:
                    // Output bitmap will be at most 128 pixels (16 tiles) wide and rectangular, tiles are stacked for 8x16 sprite mode
                    int tallRectWidth = Math.Min(128, (int)Math.Ceiling((float)tileCount / 2.0f) * 8);
                    int tallRectHeight = 16 * (int)(Math.Ceiling((float)tileCount / 2.0f) / 16.0);
                    return (tallRectWidth, tallRectHeight);
                default:
                    log.LogError(string.Format("Unknown BitmapLayout value {0}", layout), null);
                    return (0, 0);
            }
        }

        private static void WriteLinearLayout(SKBitmap bitmap, Pattern[] patterns, Palette palette, RGBLookup rgbLookup)
        {
            for (int i = 0; i < patterns.Length; i++)
            {
                WriteTile(bitmap, i * 8, 0, patterns[i], palette, rgbLookup);
            }
        }

        private static void WriteRectLayout(SKBitmap bitmap, Pattern[] patterns, Palette palette, RGBLookup rgbLookup)
        {
            for (int i = 0; i < patterns.Length; i++)
            {
                int row = (int)Math.Floor((float)i / 16.0f);
                int column = i % 16;
                WriteTile(bitmap, column * 8, row * 8, patterns[i], palette, rgbLookup);
            }
        }

        private static void WriteRect8by16Layout(SKBitmap bitmap, Pattern[] patterns, Palette palette, RGBLookup rgbLookup)
        {
            for (int i = 0; i < patterns.Length; i++)
            {
                int row = 2 * (int)Math.Floor((float)i / 32.0f);
                if (int.IsOddInteger(i))
                {
                    row += 1;
                }

                int column = (int)Math.Floor((float)i / 2.0f) % 16;
                WriteTile(bitmap, column * 8, row * 8, patterns[i], palette, rgbLookup);
            }
        }

        public static void WriteTile(SKBitmap bitmap, int targetX, int targetY, Pattern pattern, Palette palette, RGBLookup rgbLookup)
        {
            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    int paletteIndex = pattern.ParsedData[y * 8 + x];
                    int colorIndex = palette.ParsedData[paletteIndex];
                    SKColor color = rgbLookup.Colors[colorIndex];

                    bitmap.SetPixel(targetX + x, targetY + y, color);
                }
            }
        }
    }
}
