namespace nexxtporter
{
    class ExportPaletteConfig
    {
        public string? TargetFile { get; set; }
        public string? TargetSegmentName { get; set; }
        public string? TargetVariableName { get; set; }
        public bool? TargetAppend { get; set; }
        public int? SourceSubPalette { get; set; }
    }

    static class ExportPalette
    {
        public static async Task ProcessExportPalette(Logger log, NexxtporterConfig config, NSSConfig nssConfig, ExportPaletteConfig paletteConfig, Dictionary<string, string> tokens)
        {
            if (string.IsNullOrWhiteSpace(paletteConfig.TargetFile))
            {
                log.LogError("ProcessExportPalette missing or empty TargetFile attribute", null);
                return;
            }
            if (!tokens.ContainsKey("Palette"))
            {
                log.LogError(string.Format("NSS file {0} does not contain Palette token, unable to export Palette to {1}", nssConfig.SourceFile, paletteConfig.TargetFile), null);
                return;
            }

            bool targetAppend = paletteConfig.TargetAppend ?? true;
            int sourceSubPalette = paletteConfig.SourceSubPalette ?? 0;

            log.Log(string.Format("Exporting palette to file {0} from subpalette {1} (append: {2})", paletteConfig.TargetFile, sourceSubPalette, targetAppend));

            StreamWriter? writer = null;
            try
            {
                writer = new StreamWriter(paletteConfig.TargetFile, targetAppend);

                if (!string.IsNullOrWhiteSpace(paletteConfig.TargetSegmentName))
                {
                    await writer.WriteLineAsync(string.Format(".segment \"{0}\"", paletteConfig.TargetSegmentName));
                }
                if (!string.IsNullOrWhiteSpace(paletteConfig.TargetVariableName))
                {
                    await writer.WriteLineAsync(string.Format("{0}:", paletteConfig.TargetVariableName));
                }

                byte[] paletteData = Utils.ParseRLEBinary(log, nssConfig, "Palette", tokens["Palette"]);

                await Utils.WriteByteData(log, writer, paletteData, sourceSubPalette * 16, 4);
                await Utils.WriteByteData(log, writer, paletteData, sourceSubPalette * 16 + 4, 4);
                await Utils.WriteByteData(log, writer, paletteData, sourceSubPalette * 16 + 8, 4);
                await Utils.WriteByteData(log, writer, paletteData, sourceSubPalette * 16 + 12, 4);

                await writer.WriteLineAsync("");
                await writer.FlushAsync();
            }
            catch (Exception e)
            {
                log.LogError(string.Format("Error writing to file {0} while exporting palette", paletteConfig.TargetFile), e);
            }
            finally
            {
                if (writer != null)
                {
                    writer.Dispose();
                }
            }
        }
    }
}
