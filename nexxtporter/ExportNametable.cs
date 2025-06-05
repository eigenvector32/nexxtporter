namespace nexxtporter
{
    class ExportNametableConfig
    {
        public string? TargetFile { get; set; }
    }

    static class ExportNametable
    {
        public static async Task ProcessExportNametable(Logger log, NexxtporterConfig config, NSSConfig nssConfig, ExportNametableConfig nametableConfig, Dictionary<string, string> tokens)
        {
            if (string.IsNullOrWhiteSpace(nametableConfig.TargetFile))
            {
                log.LogError("ExportNametableConfig missing or empty TargetFile attribute", null);
                return;
            }
            if (!tokens.ContainsKey("NameTable"))
            {
                log.LogError(string.Format("NSS file {0} does not contain NameTable token, unable to export nametable to {1}", nssConfig.SourceFile, nametableConfig.TargetFile), null);
                return;
            }

            byte[] data = Utils.ParseRLEBinary(log, nssConfig, "NameTable", tokens["NameTable"]);

            log.Log(string.Format("Writing nametable to file {0}", nametableConfig.TargetFile));

            await Utils.WriteBinaryFile(log, nametableConfig.TargetFile, data, 0, data.Length);
        }
    }
}
