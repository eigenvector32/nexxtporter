namespace nexxtporter
{
    class ExportNametableAttributesConfig
    {
        public string? TargetFile { get; set; }
    }

    static class ExportNametableAttributes
    {
        public static async Task ProcessExportNametableAttributes(Logger log, NexxtporterConfig config, NSSConfig nssConfig, ExportNametableAttributesConfig nametableAttributesConfig, Dictionary<string, string> tokens)
        {
            if (string.IsNullOrWhiteSpace(nametableAttributesConfig.TargetFile))
            {
                log.LogError("ExportNametableAttributesConfig missing or empty TargetFile attribute", null);
                return;
            }
            if (!tokens.ContainsKey("AttrTable"))
            {
                log.LogError(string.Format("NSS file {0} does not contain AttrTable token, unable to export nametable attributes to {1}", nssConfig.SourceFile, nametableAttributesConfig.TargetFile), null);
                return;
            }

            byte[] data = Utils.ParseRLEBinary(log, nssConfig, "AttrTable", tokens["AttrTable"]);

            log.Log(string.Format("Writing nametable attributes to file {0}", nametableAttributesConfig.TargetFile));

            await Utils.WriteBinaryFile(log, nametableAttributesConfig.TargetFile, data, 0, data.Length);
        }
    }
}
