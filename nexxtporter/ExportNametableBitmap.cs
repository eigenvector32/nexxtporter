namespace nexxtporter
{
    class ExportNametableBitmapConfig
    {
        public string? TargetFile { get; set; }
        public string? RGBLookupID { get; set; }
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
            if (string.IsNullOrWhiteSpace(exportConfig.RGBLookupID))
            {
                exportConfig.RGBLookupID = "Default";
            }
            // TODO
        }

    }
}
