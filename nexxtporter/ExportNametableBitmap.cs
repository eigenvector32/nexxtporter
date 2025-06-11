using SkiaSharp;

namespace nexxtporter
{
    class ExportNametableBitmapConfig
    {
        public string? TargetFile { get; set; }
        public string? RGBLookupID { get; set; }
        public string? CHRIndex { get; set; }
        public string? PaletteSetIndex { get; set; }
    }

    static class ExportNametableBitmap
    {
        public static async Task ProcessExportNametableBitmap(Logger log, NexxtporterConfig config, NSSConfig nssConfig, ExportNametableBitmapConfig exportConfig, Dictionary<string, string> tokens, List<RGBLookup> rgbLookupTables)
        {
            if (string.IsNullOrWhiteSpace(exportConfig.TargetFile))
            {
                log.LogError("ExportNametableBitmapConfig missing or empty TargetFile attribute", null);
                return;
            }
            if (exportConfig.CHRIndex == null)
            {
                exportConfig.CHRIndex = "0";
            }
            if (exportConfig.PaletteSetIndex == null)
            {
                exportConfig.PaletteSetIndex = "0";
            }
            if (string.IsNullOrWhiteSpace(exportConfig.RGBLookupID))
            {
                exportConfig.RGBLookupID = "Default";
            }

            int chrIndex = 0;
            int paletteSetIndex = 0;
            if (!Utils.TryParseNumber(exportConfig.CHRIndex, out chrIndex))
            {
                log.LogError(string.Format("NSS file {0}, nametable bitmap export to {1}, has invalid CHRIndex attribute {2}", nssConfig.SourceFile, exportConfig.TargetFile, exportConfig.CHRIndex), null);
                return;
            }
            if (chrIndex < 0 || chrIndex >= 4)
            {
                log.LogError(string.Format("NSS file {0}, nametable bitmap export to {1}, has out of bounds CHRIndex attribute {2}", nssConfig.SourceFile, exportConfig.TargetFile, chrIndex), null);
                return;
            }
            if (!Utils.TryParseNumber(exportConfig.PaletteSetIndex, out paletteSetIndex))
            {
                log.LogError(string.Format("NSS file {0}, nametable bitmap export to {1}, has invalid PaletteSetIndex attribute {2}", nssConfig.SourceFile, exportConfig.TargetFile, exportConfig.PaletteSetIndex), null);
                return;
            }
            if (paletteSetIndex < 0 || paletteSetIndex >= 4)
            {
                log.LogError(string.Format("NSS file {0}, nametable bitmap export to {1}, has out of bounds PaletteSetIndex attribute {2}", nssConfig.SourceFile, exportConfig.TargetFile, paletteSetIndex), null);
                return;
            }

            byte[] paletteData = Utils.ParseRLEBinary(log, nssConfig, "Palette", tokens["Palette"]);
            if (paletteData.Length != 64)
            {
                log.LogError(string.Format("NSS file {0}, nametable bitmap export to {1}, has invalid Palette token with length {2}", nssConfig.SourceFile, exportConfig.TargetFile, paletteData.Length), null);
                return;
            }
            PaletteSet[] paletteSets = new PaletteSet[4];
            for (int i = 0; i < 4; i++)
            {
                PaletteSet? paletteSet = PaletteSet.ParsePaletteSet(paletteData, 16 * i);
                if (paletteSet == null)
                {
                    log.LogError(string.Format("NSS file {0}, nametable bitmap export to {1}, has invalid PaletteSet in position {2}", nssConfig.SourceFile, exportConfig.TargetFile, i), null);
                    return;
                }
                paletteSets[i] = paletteSet;
            }

            RGBLookup rgbLookup = rgbLookupTables.GetByID(exportConfig.RGBLookupID);

            if (!tokens.ContainsKey("CHRMain"))
            {
                log.LogError(string.Format("NSS file {0}, nametable bitmap export to {1}, is missing CHRMain token", nssConfig.SourceFile, exportConfig.TargetFile), null);
                return;
            }
            byte[] chrData = Utils.ParseRLEBinary(log, nssConfig, "CHRMain", tokens["CHRMain"]);
            if (chrIndex * 4096 + 4096 > chrData.Length)
            {
                log.LogError(string.Format("NSS file {0}, CHRMain is {0} bytes which is smaller than the requested CHRIndex {1}", nssConfig.SourceFile, chrData.Length, chrIndex), null);
                return;
            }

            int tileCount = 256;
            Pattern[] patterns = new Pattern[tileCount];
            for (int i = 0; i < tileCount; i++)
            {
                Pattern? pattern = Pattern.ParsePattern(chrData, chrIndex * 4096 + 16 * i);
                if (pattern == null)
                {
                    log.LogError(string.Format("NSS file {0}, nametable bitmap export to {1}, failed to parse pattern {2}", nssConfig.SourceFile, exportConfig.TargetFile, i), null);
                    return;
                }
                patterns[i] = pattern;
            }

            if (!tokens.ContainsKey("NameTable"))
            {
                log.LogError(string.Format("NSS file {0}, nametable bitmap export to {1}, is missing NameTable token", nssConfig.SourceFile, exportConfig.TargetFile), null);
                return;
            }
            byte[] nametableData = Utils.ParseRLEBinary(log, nssConfig, "NameTable", tokens["NameTable"]);
            if (nametableData.Length != 960)
            {
                log.LogError(string.Format("NSS file {0}, nametable bitmap export to {1}, found a NameTable of incorrect size {2}", nssConfig.SourceFile, exportConfig.TargetFile, nametableData.Length), null);
                return;
            }

            if (!tokens.ContainsKey("AttrTable"))
            {
                log.LogError(string.Format("NSS file {0}, nametable bitmap export to {1}, is missing AttrTable token", nssConfig.SourceFile, exportConfig.TargetFile), null);
                return;
            }
            byte[] attributeData = Utils.ParseRLEBinary(log, nssConfig, "AttrTable", tokens["AttrTable"]);
            AttributeTable? attributeTable = AttributeTable.ParseAttributeTable(attributeData);
            if (attributeTable == null)
            {
                log.LogError(string.Format("NSS file {0}, nametable bitmap export to {1}, failed to parse nametable attributes", nssConfig.SourceFile, exportConfig.TargetFile), null);
                return;
            }

            SKBitmap? outputBitmap = null;
            try
            {
                outputBitmap = new SKBitmap(256, 240);
                // Fill with global background
                int backgroundColorIndex = paletteSets[paletteSetIndex].Palettes[0].ParsedData[0];
                outputBitmap.Erase(rgbLookup.Colors[backgroundColorIndex]);

                for (int y = 0; y < 30; y++)
                {
                    for (int x = 0; x < 32; x++)
                    {
                        int patternIndex = nametableData[32 * y + x];
                        int paletteIndex = attributeTable.GetForPosition(x, y);
                        ExportBitmap.WriteTile(outputBitmap, x * 8, y * 8, patterns[patternIndex], paletteSets[paletteSetIndex].Palettes[paletteIndex], rgbLookup);
                    }
                }

                log.Log(string.Format("Writing {0}x{1} nametable bitmap to {2}", 256, 240, exportConfig.TargetFile));
                await Utils.WriteSKBitmap(log, exportConfig.TargetFile, outputBitmap);
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
    }
}
